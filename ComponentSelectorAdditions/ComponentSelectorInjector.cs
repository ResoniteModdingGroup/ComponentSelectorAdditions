using ComponentSelectorAdditions.Events;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using MonkeyLoader;
using MonkeyLoader.Configuration;
using MonkeyLoader.Events;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions
{
    [HarmonyPatch(typeof(ComponentSelector))]
    [HarmonyPatchCategory(nameof(ComponentSelectorInjector))]
    internal sealed class ComponentSelectorInjector : ResoniteMonkey<ComponentSelectorInjector>,
        IEventSource<BuildSelectorHeaderEvent>, IEventSource<BuildSelectorFooterEvent>,
        ICancelableEventSource<EnumerateCategoriesEvent>, ICancelableEventSource<EnumerateComponentsEvent>,
        ICancelableEventSource<BuildCategoryButtonEvent>, ICancelableEventSource<BuildGroupButtonEvent>, ICancelableEventSource<BuildComponentButtonEvent>,
        IEventSource<EnumerateConcreteGenericsEvent>
    {
        private static readonly ConditionalWeakTable<ComponentSelector, SelectorData> _selectorData = new();
        private static CancelableEventDispatching<BuildCategoryButtonEvent>? _buildCategoryButton;
        private static CancelableEventDispatching<BuildComponentButtonEvent>? _buildComponentButton;
        private static EventDispatching<BuildSelectorFooterEvent>? _buildFooter;
        private static CancelableEventDispatching<BuildGroupButtonEvent>? _buildGroupButton;
        private static EventDispatching<BuildSelectorHeaderEvent>? _buildHeader;
        private static CancelableEventDispatching<EnumerateCategoriesEvent>? _enumerateCategories;
        private static CancelableEventDispatching<EnumerateComponentsEvent>? _enumerateComponents;
        private static EventDispatching<EnumerateConcreteGenericsEvent>? _enumerateConcreteGenerics;

        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        protected override bool OnEngineReady()
        {
            Mod.RegisterEventSource<BuildSelectorHeaderEvent>(this);
            Mod.RegisterEventSource<BuildSelectorFooterEvent>(this);

            Mod.RegisterEventSource<EnumerateCategoriesEvent>(this);
            Mod.RegisterEventSource<EnumerateComponentsEvent>(this);
            Mod.RegisterEventSource<EnumerateConcreteGenericsEvent>(this);

            Mod.RegisterEventSource<BuildCategoryButtonEvent>(this);
            Mod.RegisterEventSource<BuildGroupButtonEvent>(this);
            Mod.RegisterEventSource<BuildComponentButtonEvent>(this);

            return base.OnEngineReady();
        }

        private static void BuildCategoryUI(ComponentSelector selector, UIBuilder ui, SelectorData selectorData, bool doNotGenerateBack)
        {
            CategoryNode<Type> rootCategory;
            if (selectorData.CurrentPath.IsRootCategory)
            {
                rootCategory = WorkerInitializer.ComponentLibrary;
            }
            else
            {
                rootCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(selectorData.CurrentPath.Path);

                if (rootCategory is null)
                {
                    selectorData.CurrentPath = new SelectorPath(selector._rootPath, selectorData.CurrentPath.Search, false, null, true);
                    rootCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(selectorData.CurrentPath.Path);

                    if (rootCategory is null)
                    {
                        selector._rootPath.Value = null;
                        selectorData.CurrentPath = new SelectorPath("/", selectorData.CurrentPath.Search, false, null, true);
                        rootCategory = WorkerInitializer.ComponentLibrary;
                    }
                }
            }

            var path = selectorData.CurrentPath;

            if (rootCategory != WorkerInitializer.ComponentLibrary && !doNotGenerateBack)
            {
                ui.Button("ComponentSelector.Back".AsLocaleKey(), RadiantUI_Constants.BUTTON_COLOR,
                    selector.OnOpenCategoryPressed, path.OpenParentCategoryPath, 0.35f);
            }

            var enumerateComponentsData = OnEnumerateComponents(selector, path, rootCategory, selector.ComponentFilter.Target ?? Yes);

            KeyCounter<string>? groupCounter = null;
            HashSet<string>? groupNames = null;

            if (!path.HasGroup)
            {
                var enumerateCategoriesData = OnEnumerateCategories(selector, path, rootCategory);

                foreach (var category in enumerateCategoriesData.Items)
                    OnBuildCategoryButton(selector, ui, rootCategory, category);

                groupCounter = new KeyCounter<string>();
                groupNames = new HashSet<string>();

                foreach (var component in enumerateComponentsData.Items.Where(component => component.HasGroup))
                    groupCounter.Increment(component.Group!);
            }

            foreach (var component in enumerateComponentsData.Items)
            {
                if (path.HasGroup && component.Group != path.Group)
                    continue;

                if (!path.HasGroup && component.HasGroup && groupCounter![component.Group] > 1)
                {
                    if (groupNames!.Add(component.Group))
                        OnBuildGroupButton(selector, ui, rootCategory, component);

                    continue;
                }

                OnBuildComponentButton(selector, ui, path, rootCategory, component);
            }
        }

        private static void BuildGenericTypeUI(ComponentSelector selector, UIBuilder ui, SelectorPath path, bool doNotGenerateBack)
        {
            var type = WorkerManager.GetType(PathUtility.GetFileName(path.PathSegments[^1]));
            selector._genericType.Value = type;

            if (!doNotGenerateBack)
                ui.Button("ComponentSelector.Back".AsLocaleKey(), RadiantUI_Constants.BUTTON_COLOR, selector.OnOpenCategoryPressed, path.OpenParentCategoryPath, 0.35f);

            ui.Text("ComponentSelector.CustomGenericArguments".AsLocaleKey());

            var genericArguments = type.GetGenericArguments();
            foreach (var genericArgument in genericArguments)
            {
                var textField = ui.HorizontalElementWithLabel(genericArgument.Name, .05f, () => ui.TextField(string.Empty, undo: false, null, parseRTF: false));

                if (selector.GenericArgumentPrefiller.Target != null)
                    textField.TargetString = selector.GenericArgumentPrefiller.Target(type, genericArgument);

                selector._customGenericArguments.Add(textField);
            }

            var button = ui.Button((LocaleString)string.Empty, RadiantUI_Constants.BUTTON_COLOR, selector.OnCreateCustomType, .35f);
            selector._customGenericTypeLabel.Target = button.Label.Content;
            selector._customGenericTypeColor.Target = button.BaseColor;

            var concreteGenericsEventData = OnEnumerateConcreteGenerics(selector, type);

            ui.Text("ComponentSelector.CustomGenericArguments".AsLocaleKey());

            foreach (var concreteType in concreteGenericsEventData.Items)
            {
                try
                {
                    if (concreteType.IsValidGenericType(true))
                    {
                        ui.Button(concreteType.GetNiceName(), RadiantUI_Constants.Sub.CYAN, selector.OnAddComponentPressed,
                            TypeHelper.TryGetAlias(concreteType) ?? concreteType.FullName, .35f).Label.ParseRichText.Value = false;
                    }
                }
                catch (Exception ex)
                {
                    UniLog.Warning("Exception checking validity of a generic type: " + concreteType?.ToString() + "for " + type?.ToString() + "\n" + ex);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ComponentSelector.BuildUI))]
        private static bool BuildUIPrefix(ComponentSelector __instance, string path, bool genericType, string group, bool doNotGenerateBack)
        {
            if (!_selectorData.TryGetValue(__instance, out var selectorData))
                return true;

            if (doNotGenerateBack)
                __instance._rootPath.Value = path;
            else if ((string.IsNullOrEmpty(path?.TrimStart('/')) || __instance._rootPath.Value == path?.TrimStart('/')) && group is null)
                doNotGenerateBack = true;

            var selectorPath = new SelectorPath(path, selectorData.SearchBar?.Text.Content, genericType, group, __instance._rootPath.Value == path);
            selectorData.CurrentPath = selectorPath;

            OnSelectorBackButtonChanged(selectorData.BackButtonChanged, selectorPath, !doNotGenerateBack);
            doNotGenerateBack = selectorData.HasBackButton;

            __instance._uiRoot.Target.DestroyChildren();
            __instance._customGenericArguments.Clear();
            __instance._genericType.Value = null;

            var ui = SetupStyle(new UIBuilder(__instance._uiRoot.Target));

            if (genericType)
            {
                BuildGenericTypeUI(__instance, ui, selectorPath, doNotGenerateBack);
                return false;
            }

            BuildCategoryUI(__instance, ui, selectorData, doNotGenerateBack);

            if (!selectorData.HasCancelButton)
                ui.Button("General.Cancel".AsLocaleKey(), RadiantUI_Constants.Sub.RED, __instance.OnCancelPressed, 0.35f).Slot.OrderOffset = 1000000L;

            return false;
        }

        private static string GetPrettyPath<T>(CategoryNode<T> category)
            => category.GetPath()[1..].Replace("/", " > ") + " >";

        private static SyncFieldEvent<string> MakeBuildUICall(ComponentSelector selector, SelectorData details)
        {
            return field =>
            {
                var token = details.SearchBar!.UpdateSearch();

                selector.StartTask(async () =>
                {
                    if (details.SearchBar!.SearchRefreshDelay > 0)
                    {
                        await default(ToBackground);
                        await Task.Delay(details.SearchBar!.SearchRefreshDelay);
                        await default(NextUpdate);
                    }

                    // Only refresh UI with search results if there was no further update immediately following it
                    if (token.IsCancellationRequested || selector.IsDestroyed)
                        return;

                    selector.BuildUI(details.CurrentPath.Path, false);
                });
            };
        }

        private static void OnBuildCategoryButton(ComponentSelector selector, UIBuilder ui, CategoryNode<Type> rootCategory, CategoryNode<Type> subCategory)
        {
            var root = ui.Root;
            var eventData = new BuildCategoryButtonEvent(selector, ui, rootCategory, subCategory);

            try
            {
                _buildCategoryButton?.TryInvokeAll(eventData);
            }
            catch (AggregateException ex)
            {
                Logger.Warn(() => ex.Format("Some Build Category Button Event handlers threw an exception:"));
            }

            if (!eventData.Canceled)
            {
                ui.Button(
                eventData.IsDirectItem ? $"{subCategory.Name} >" : GetPrettyPath(subCategory),
                RadiantUI_Constants.Sub.YELLOW, selector.OnOpenCategoryPressed, subCategory.GetPath(),
                0.35f).Label.ParseRichText.Value = false;
            }

            ui.NestInto(root);
        }

        private static void OnBuildComponentButton(ComponentSelector selector, UIBuilder ui, SelectorPath path, CategoryNode<Type> rootCategory, ComponentResult component)
        {
            var root = ui.Root;
            var eventData = new BuildComponentButtonEvent(selector, ui, path, rootCategory, component);

            try
            {
                _buildComponentButton?.TryInvokeAll(eventData);
            }
            catch (AggregateException ex)
            {
                Logger.Warn(() => ex.Format("Some Build Component Button Event handlers threw an exception:"));
            }

            if (!eventData.Canceled)
            {
                ui.PushStyle();
                ui.Style.MinHeight = eventData.IsDirectItem ? 32 : 48;

                var name = component.NiceName;
                var fullName = path.HasGroup ? $"{component.FullName}?{path.Group}" : component.FullName;

                var button = component.IsGeneric ?
                         ui.Button(name, RadiantUI_Constants.Sub.GREEN, selector.OpenGenericTypesPressed, path.Path + "/" + fullName, .35f)
                         : ui.Button(name, RadiantUI_Constants.Sub.CYAN, selector.OnAddComponentPressed, component.FullName, .35f);

                if (!eventData.IsDirectItem)
                {
                    var buttonLabel = button.Label;
                    buttonLabel.ParseRichText.Value = false;
                    ui.NestInto(button.RectTransform);

                    var panel = ui.Panel();
                    panel.OffsetMin.Value = buttonLabel.RectTransform.OffsetMin;
                    panel.OffsetMax.Value = buttonLabel.RectTransform.OffsetMax;

                    ui.HorizontalHeader(18, out var header, out var content);

                    buttonLabel.Slot.Parent = content.Slot;
                    buttonLabel.RectTransform.OffsetMin.Value = new(16, 0);
                    buttonLabel.RectTransform.OffsetMax.Value = float2.Zero;

                    ui.NestInto(header);
                    var text = ui.Text(GetPrettyPath(component.Category), parseRTF: false);
                    text.Color.Value = RadiantUI_Constants.Neutrals.LIGHT;
                }

                ui.PopStyle();
            }

            ui.NestInto(root);
        }

        private static BuildSelectorFooterEvent OnBuildFooter(ComponentSelector selector, UIBuilder ui, SelectorSearchBar? searchBar, bool hasBackButton, bool hasCancelButton)
        {
            var eventData = new BuildSelectorFooterEvent(selector, ui, searchBar, hasBackButton, hasCancelButton);

            try
            {
                _buildFooter?.TryInvokeAll(eventData);
            }
            catch (AggregateException ex)
            {
                Logger.Warn(() => ex.Format("Some Build Selector Footer Event handlers threw an exception:"));
            }

            return eventData;
        }

        private static void OnBuildGroupButton(ComponentSelector selector, UIBuilder ui, CategoryNode<Type> rootCategory, ComponentResult groupComponent)
        {
            var root = ui.Root;
            var eventData = new BuildGroupButtonEvent(selector, ui, rootCategory, groupComponent);

            try
            {
                _buildGroupButton?.TryInvokeAll(eventData);
            }
            catch (AggregateException ex)
            {
                Logger.Warn(() => ex.Format("Some Build Group Button Event handlers threw an exception:"));
            }

            if (!eventData.Canceled)
            {
                ui.Button(
                eventData.IsDirectItem ? groupComponent.GroupName : GetPrettyPath(groupComponent.Category) + groupComponent.GroupName,
                RadiantUI_Constants.Sub.PURPLE, selector.OpenGroupPressed, $"{groupComponent.Category.GetPath()}:{groupComponent.Group}",
                0.35f).Label.ParseRichText.Value = false;
            }

            ui.NestInto(root);
        }

        private static BuildSelectorHeaderEvent OnBuildHeader(ComponentSelector selector, UIBuilder ui)
        {
            var eventData = new BuildSelectorHeaderEvent(selector, ui);

            try
            {
                _buildHeader?.TryInvokeAll(eventData);
            }
            catch (AggregateException ex)
            {
                Logger.Warn(() => ex.Format("Some Build Selector Header Event handlers threw an exception:"));
            }

            return eventData;
        }

        private static EnumerateCategoriesEvent OnEnumerateCategories(ComponentSelector selector, SelectorPath path, CategoryNode<Type> rootCategory)
        {
            var eventData = new EnumerateCategoriesEvent(selector, path, rootCategory);

            try
            {
                _enumerateCategories?.TryInvokeAll(eventData);
            }
            catch (AggregateException ex)
            {
                Logger.Warn(() => ex.Format("Some Enumerate Categories Event handlers threw an exception:"));
            }

            if (!eventData.Canceled && !path.HasGroup)
            {
                foreach (var subcategory in rootCategory.Subcategories)
                    eventData.AddItem(subcategory);
            }

            return eventData;
        }

        private static EnumerateComponentsEvent OnEnumerateComponents(ComponentSelector selector, SelectorPath path, CategoryNode<Type> category, Predicate<Type> componentFilter)
        {
            var eventData = new EnumerateComponentsEvent(selector, path, category, componentFilter);

            try
            {
                _enumerateComponents?.TryInvokeAll(eventData);
            }
            catch (AggregateException ex)
            {
                Logger.Warn(() => ex.Format("Some Enumerate Components Event handlers threw an exception:"));
            }

            if (!eventData.Canceled)
            {
                foreach (var type in category.Elements)
                    eventData.AddItem(new ComponentResult(category, type));
            }

            return eventData;
        }

        private static EnumerateConcreteGenericsEvent OnEnumerateConcreteGenerics(ComponentSelector selector, Type component)
        {
            var eventData = new EnumerateConcreteGenericsEvent(selector, component);

            try
            {
                _enumerateConcreteGenerics?.TryInvokeAll(eventData);
            }
            catch (AggregateException ex)
            {
                Logger.Warn(() => ex.Format("Some Enumerate Concrete Generics Event handlers threw an exception:"));
            }

            foreach (var concreteGeneric in WorkerInitializer.GetCommonGenericTypes(component))
                eventData.AddItem(concreteGeneric);

            return eventData;
        }

        private static void OnSelectorBackButtonChanged(Action<SelectorPath, bool>? handlers, SelectorPath path, bool showBackButton)
        {
            try
            {
                handlers?.TryInvokeAll(path, showBackButton);
            }
            catch (AggregateException ex)
            {
                Logger.Warn(() => ex.Format("Some Show Selector Back Button Event handlers threw an exception:"));
            }
        }

        private static UIBuilder SetupStyle(UIBuilder builder)
        {
            RadiantUI_Constants.SetupEditorStyle(builder, extraPadding: true);

            builder.Style.TextAlignment = Alignment.MiddleLeft;
            builder.Style.ButtonTextAlignment = Alignment.MiddleLeft;
            builder.Style.MinHeight = 32;

            return builder;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ComponentSelector.SetupUI))]
        private static bool SetupUIPrefix(ComponentSelector __instance, LocaleString title, float2 size)
        {
            var ui = RadiantUI_Panel.SetupPanel(__instance.Slot, title, size);
            SetupStyle(ui);
            ui.Style.ForceExpandHeight = false;

            var verticalLayout = ui.VerticalLayout(16, 8);

            var headerEventData = OnBuildHeader(__instance, ui);

            ui.Style.FlexibleHeight = 1;
            ui.ScrollArea();
            ui.VerticalLayout(8f);
            ui.FitContent(SizeFit.Disabled, SizeFit.MinSize);
            __instance._uiRoot.Target = ui.Root;
            ui.Style.FlexibleHeight = -1;

            ui.NestInto(verticalLayout.RectTransform);
            var footerEventData = OnBuildFooter(__instance, ui, headerEventData.SearchBar, headerEventData.AddsBackButton, headerEventData.AddsCancelButton);

            var showBackButtonChangedHandlers = headerEventData.BackButtonChangedHandlers;
            showBackButtonChangedHandlers += footerEventData.BackButtonChangedHandlers;

            var selectorData = new SelectorData(
                headerEventData.AddsBackButton || footerEventData.AddsBackButton,
                headerEventData.AddsCancelButton || footerEventData.AddsCancelButton,
                showBackButtonChangedHandlers,
                headerEventData.SearchBar ?? footerEventData.SearchBar);

            _selectorData.Add(__instance, selectorData);

            if (selectorData.HasSearchBar)
                selectorData.SearchBar.Text.Content.OnValueChange += MakeBuildUICall(__instance, selectorData);

            __instance.BuildUI(null);

            return false;
        }

        private static bool Yes(Type _) => true;

        event EventDispatching<BuildSelectorFooterEvent>? IEventSource<BuildSelectorFooterEvent>.Dispatching
        {
            add => _buildFooter += value;
            remove => _buildFooter -= value;
        }

        event EventDispatching<BuildSelectorHeaderEvent>? IEventSource<BuildSelectorHeaderEvent>.Dispatching
        {
            add => _buildHeader += value;
            remove => _buildHeader -= value;
        }

        event CancelableEventDispatching<EnumerateComponentsEvent>? ICancelableEventSource<EnumerateComponentsEvent>.Dispatching
        {
            add => _enumerateComponents += value;
            remove => _enumerateComponents -= value;
        }

        event CancelableEventDispatching<EnumerateCategoriesEvent>? ICancelableEventSource<EnumerateCategoriesEvent>.Dispatching
        {
            add => _enumerateCategories += value;
            remove => _enumerateCategories -= value;
        }

        event EventDispatching<EnumerateConcreteGenericsEvent>? IEventSource<EnumerateConcreteGenericsEvent>.Dispatching
        {
            add => _enumerateConcreteGenerics += value;
            remove => _enumerateConcreteGenerics -= value;
        }

        event CancelableEventDispatching<BuildComponentButtonEvent>? ICancelableEventSource<BuildComponentButtonEvent>.Dispatching
        {
            add => _buildComponentButton += value;
            remove => _buildComponentButton -= value;
        }

        event CancelableEventDispatching<BuildGroupButtonEvent>? ICancelableEventSource<BuildGroupButtonEvent>.Dispatching
        {
            add => _buildGroupButton += value;
            remove => _buildGroupButton -= value;
        }

        event CancelableEventDispatching<BuildCategoryButtonEvent>? ICancelableEventSource<BuildCategoryButtonEvent>.Dispatching
        {
            add => _buildCategoryButton += value;
            remove => _buildCategoryButton -= value;
        }

        private sealed class SelectorData
        {
            public Action<SelectorPath, bool>? BackButtonChanged { get; }
            public SelectorPath CurrentPath { get; set; }
            public bool HasBackButton { get; }
            public bool HasCancelButton { get; }

            [MemberNotNullWhen(true, nameof(SearchBar))]
            public bool HasSearchBar => SearchBar is not null;

            public SelectorSearchBar? SearchBar { get; }

            public SelectorData(bool hasBackButton, bool hasCancelButton, Action<SelectorPath, bool>? backButtonChanged, SelectorSearchBar? searchBar)
            {
                HasBackButton = hasBackButton;
                HasCancelButton = hasCancelButton;
                BackButtonChanged = backButtonChanged;
                SearchBar = searchBar;

                CurrentPath = new SelectorPath(null, null, false, null, true);
            }
        }
    }
}