using ComponentSelectorAdditions.Events;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using MonkeyLoader;
using MonkeyLoader.Events;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions
{
    [HarmonyPatchCategory(nameof(Injector))]
    [HarmonyPatch(typeof(ComponentSelector))]
    internal sealed class Injector : ResoniteMonkey<Injector>,
        IEventSource<BuildSelectorHeaderEvent>, IEventSource<BuildSelectorFooterEvent>,
        ICancelableEventSource<EnumerateCategoriesEvent>, ICancelableEventSource<EnumerateComponentsEvent>,
        ICancelableEventSource<BuildCategoryButtonEvent>, ICancelableEventSource<BuildGroupButtonEvent>, ICancelableEventSource<BuildComponentButtonEvent>,
        IEventSource<BuildCustomGenericBuilder>, IEventSource<EnumerateConcreteGenericsEvent>,
        IEventSource<PostProcessButtonsEvent>
    {
        private static readonly ConditionalWeakTable<ComponentSelector, SelectorData> _selectorData = new();

        private static CancelableEventDispatching<BuildCategoryButtonEvent>? _buildCategoryButton;
        private static CancelableEventDispatching<BuildComponentButtonEvent>? _buildComponentButton;
        private static EventDispatching<BuildCustomGenericBuilder>? _buildCustomGenericBuilder;
        private static EventDispatching<BuildSelectorFooterEvent>? _buildFooter;
        private static CancelableEventDispatching<BuildGroupButtonEvent>? _buildGroupButton;
        private static EventDispatching<BuildSelectorHeaderEvent>? _buildHeader;
        private static CancelableEventDispatching<EnumerateCategoriesEvent>? _enumerateCategories;
        private static CancelableEventDispatching<EnumerateComponentsEvent>? _enumerateComponents;
        private static EventDispatching<EnumerateConcreteGenericsEvent>? _enumerateConcreteGenerics;
        private static EventDispatching<PostProcessButtonsEvent>? _postProcessButtons;
        public override bool CanBeDisabled => true;

        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        protected override bool OnEngineReady()
        {
            Mod.RegisterEventSource<BuildSelectorHeaderEvent>(this);
            Mod.RegisterEventSource<BuildSelectorFooterEvent>(this);

            Mod.RegisterEventSource<EnumerateCategoriesEvent>(this);
            Mod.RegisterEventSource<EnumerateComponentsEvent>(this);
            Mod.RegisterEventSource<EnumerateConcreteGenericsEvent>(this);

            Mod.RegisterEventSource<BuildCustomGenericBuilder>(this);

            Mod.RegisterEventSource<BuildCategoryButtonEvent>(this);
            Mod.RegisterEventSource<BuildGroupButtonEvent>(this);
            Mod.RegisterEventSource<BuildComponentButtonEvent>(this);

            Mod.RegisterEventSource<PostProcessButtonsEvent>(this);

            return base.OnEngineReady();
        }

        protected override bool OnShutdown(bool applicationExiting)
        {
            if (!applicationExiting)
            {
                Mod.UnregisterEventSource<BuildSelectorHeaderEvent>(this);
                Mod.UnregisterEventSource<BuildSelectorFooterEvent>(this);

                Mod.UnregisterEventSource<EnumerateCategoriesEvent>(this);
                Mod.UnregisterEventSource<EnumerateComponentsEvent>(this);
                Mod.UnregisterEventSource<EnumerateConcreteGenericsEvent>(this);

                Mod.RegisterEventSource<BuildCustomGenericBuilder>(this);

                Mod.UnregisterEventSource<BuildCategoryButtonEvent>(this);
                Mod.UnregisterEventSource<BuildGroupButtonEvent>(this);
                Mod.UnregisterEventSource<BuildComponentButtonEvent>(this);

                Mod.UnregisterEventSource<PostProcessButtonsEvent>(this);
            }

            return base.OnShutdown(applicationExiting);
        }

        private static void BuildCategoryUI(ComponentSelector selector, UIBuilder ui, SelectorData selectorData, bool doNotGenerateBack, out Button? backButton)
        {
            backButton = null;
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
                backButton = ui.Button("ComponentSelector.Back".AsLocaleKey(), RadiantUI_Constants.BUTTON_COLOR,
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

        private static void BuildGenericTypeUI(ComponentSelector selector, UIBuilder ui, SelectorPath path,
            bool doNotGenerateBack, out Button? backButton, out Button? customGenericButton, out HashSet<Button> otherAddedButtons)
        {
            backButton = null;

            var type = Type.GetType(path.PathSegments[^1]);
            selector._genericType.Value = type;

            if (!doNotGenerateBack)
                backButton = ui.Button("ComponentSelector.Back".AsLocaleKey(), RadiantUI_Constants.BUTTON_COLOR, selector.OnOpenCategoryPressed, path.OpenParentCategoryPath, 0.35f);

            ui.Text("ComponentSelector.CustomGenericArguments".AsLocaleKey());

            var customGenericBuilderData = OnBuildCustomGenericBuilder(selector, ui, type);
            customGenericButton = customGenericBuilderData.CreateCustomTypeButton!;
            otherAddedButtons = customGenericBuilderData.OtherAddedButtonsSet;

            ui.Panel();
            ui.NestOut();

            var concreteGenericsEventData = OnEnumerateConcreteGenerics(selector, type);

            if (concreteGenericsEventData.Items.Any())
            {
                ui.Text("ComponentSelector.CommonGenericTypes".AsLocaleKey());

                foreach (var concreteType in concreteGenericsEventData.Items)
                {
                    try
                    {
                        if (concreteType.IsValidGenericType(true))
                        {
                            OnBuildComponentButton(selector, ui, path, null, new ComponentResult(null, concreteType));
                        }
                    }
                    catch (Exception ex)
                    {
                        UniLog.Warning(ex.Format("Exception checking validity of a generic type: " + concreteType?.ToString() + " for " + type?.ToString()));
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ComponentSelector.BuildUI))]
        private static bool BuildUIPrefix(ComponentSelector __instance, string path, bool genericType, string group, bool doNotGenerateBack)
        {
            if (!Enabled || !_selectorData.TryGetValue(__instance, out var selectorData))
                return true;

            if (doNotGenerateBack)
                __instance._rootPath.Value = path;
            else if ((string.IsNullOrEmpty(path?.TrimStart('/')) || __instance._rootPath.Value == path?.TrimStart('/')) && group is null)
                doNotGenerateBack = true;

            if (selectorData.HasSearchBar)
                selectorData.SearchBar.Active = !genericType && group is null;

            var selectorPath = new SelectorPath(path, selectorData.SearchBar?.Content, genericType, group, doNotGenerateBack);
            selectorData.CurrentPath = selectorPath;

            OnSelectorBackButtonChanged(selectorData.BackButtonChanged, selectorPath, !doNotGenerateBack);
            doNotGenerateBack = selectorData.HasBackButton;

            __instance._uiRoot.Target.DestroyChildren();
            __instance._customGenericArguments.Clear();
            __instance._genericType.Value = null;

            var ui = SetupStyle(new UIBuilder(__instance._uiRoot.Target));

            Button? backButton;
            Button? customGenericButton = null;
            Button? cancelButton = null;
            HashSet<Button>? otherAddedButtons = null;

            if (genericType)
                BuildGenericTypeUI(__instance, ui, selectorPath, doNotGenerateBack, out backButton, out customGenericButton, out otherAddedButtons);
            else
                BuildCategoryUI(__instance, ui, selectorData, doNotGenerateBack, out backButton);

            if (!selectorData.HasCancelButton)
            {
                cancelButton = ui.Button("General.Cancel".AsLocaleKey(), RadiantUI_Constants.Sub.RED, __instance.OnCancelPressed, 0.35f);
                cancelButton.Slot.OrderOffset = 1000000L;
            }

            OnPostProcessButtons(__instance, selectorData.CurrentPath, ui, backButton, customGenericButton, cancelButton, otherAddedButtons);

            return false;
        }

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
                    if (token.IsCancellationRequested || selector.FilterWorldElement() is null)
                        return;

                    selector.BuildUI(details.CurrentPath.Path, false);
                });
            };
        }

        private static void OnBuildCategoryButton(ComponentSelector selector, UIBuilder ui, CategoryNode<Type> rootCategory, CategoryNode<Type> subCategory)
        {
            var root = ui.Root;
            var eventData = new BuildCategoryButtonEvent(selector, ui, rootCategory, subCategory);

            _buildCategoryButton?.Invoke(eventData);

            ui.NestInto(root);

            if (!eventData.Canceled)
                Logger.Warn(() => "No event handler handled building a category button!");
        }

        private static void OnBuildComponentButton(ComponentSelector selector, UIBuilder ui, SelectorPath path, CategoryNode<Type>? rootCategory, ComponentResult component)
        {
            var root = ui.Root;
            var eventData = new BuildComponentButtonEvent(selector, ui, path, rootCategory, component);

            _buildComponentButton?.Invoke(eventData);

            ui.NestInto(root);

            if (!eventData.Canceled)
                Logger.Warn(() => "No event handler handled building a component button!");
        }

        private static BuildCustomGenericBuilder OnBuildCustomGenericBuilder(ComponentSelector selector, UIBuilder ui, Type component)
        {
            var root = ui.Root;
            var eventData = new BuildCustomGenericBuilder(selector, ui, component);

            _buildCustomGenericBuilder?.Invoke(eventData);

            ui.NestInto(root);

            if (!eventData.AddsGenericArgumentInputs)
                Logger.Warn(() => "No event handler handled adding generic argument inputs!");

            if (!eventData.AddsCreateCustomTypeButton)
                Logger.Warn(() => "No event handler handled adding a create custom type button!");

            selector._customGenericTypeLabel.Target = eventData.CreateCustomTypeButton?.Label.Content;
            selector._customGenericTypeColor.Target = eventData.CreateCustomTypeButton?.BaseColor;

            return eventData;
        }

        private static BuildSelectorFooterEvent OnBuildFooter(ComponentSelector selector, UIBuilder ui, SelectorSearchBar? searchBar, bool hasBackButton, bool hasCancelButton)
        {
            var root = ui.Root;
            var eventData = new BuildSelectorFooterEvent(selector, ui, searchBar, hasBackButton, hasCancelButton);

            _buildFooter?.Invoke(eventData);

            ui.NestInto(root);

            return eventData;
        }

        private static void OnBuildGroupButton(ComponentSelector selector, UIBuilder ui, CategoryNode<Type> rootCategory, ComponentResult groupComponent)
        {
            var root = ui.Root;
            var eventData = new BuildGroupButtonEvent(selector, ui, rootCategory, groupComponent);

            _buildGroupButton?.Invoke(eventData);

            ui.NestInto(root);

            if (!eventData.Canceled)
                Logger.Warn(() => "No event handler handled building a group button!");
        }

        private static BuildSelectorHeaderEvent OnBuildHeader(ComponentSelector selector, UIBuilder ui)
        {
            var root = ui.Root;
            var eventData = new BuildSelectorHeaderEvent(selector, ui);

            _buildHeader?.Invoke(eventData);

            ui.NestInto(root);

            return eventData;
        }

        private static EnumerateCategoriesEvent OnEnumerateCategories(ComponentSelector selector, SelectorPath path, CategoryNode<Type> rootCategory)
        {
            var eventData = new EnumerateCategoriesEvent(selector, path, rootCategory);

            _enumerateCategories?.Invoke(eventData);

            if (!eventData.Canceled)
                Logger.Warn(() => "No event handler handled enumerating sub-categories!");

            return eventData;
        }

        private static EnumerateComponentsEvent OnEnumerateComponents(ComponentSelector selector, SelectorPath path, CategoryNode<Type> category, Predicate<Type> componentFilter)
        {
            var eventData = new EnumerateComponentsEvent(selector, path, category, componentFilter);

            _enumerateComponents?.Invoke(eventData);

            if (!eventData.Canceled)
                Logger.Warn(() => "No event handler handled enumerating components!");

            return eventData;
        }

        private static EnumerateConcreteGenericsEvent OnEnumerateConcreteGenerics(ComponentSelector selector, Type component)
        {
            var eventData = new EnumerateConcreteGenericsEvent(selector, component);

            _enumerateConcreteGenerics?.Invoke(eventData);

            return eventData;
        }

        private static void OnPostProcessButtons(ComponentSelector selector, SelectorPath path,
            UIBuilder ui, Button? backButton, Button? customGenericButton, Button? cancelButton, HashSet<Button>? otherAddedButtons)
        {
            // Lock in UI Order through the Offsets
            var index = 0;
            foreach (var child in selector._uiRoot.Target.Children.ToArray())
                child.OrderOffset = 10 * index++;

            var root = ui.Root;
            var eventData = new PostProcessButtonsEvent(selector, path, ui, backButton, customGenericButton, cancelButton, otherAddedButtons);

            _postProcessButtons?.Invoke(eventData);

            ui.NestInto(root);
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

            var verticalLayout = ui.VerticalLayout(8, 8);

            var headerEventData = OnBuildHeader(__instance, ui);

            ui.Style.FlexibleHeight = 1;
            ui.ScrollArea();
            ui.VerticalLayout(8, 8, 0, 8, 0);
            ui.FitContent(SizeFit.Disabled, SizeFit.MinSize);
            __instance._uiRoot.Target = ui.Root;
            ui.Style.FlexibleHeight = -1;

            ui.NestInto(verticalLayout.RectTransform);
            var footerEventData = OnBuildFooter(__instance, ui, headerEventData.SearchBar, headerEventData.AddsBackButton, headerEventData.AddsCancelButton);

            var showBackButtonChangedHandlers = headerEventData.SelectorUIChangedHandlers;
            showBackButtonChangedHandlers += footerEventData.SelectorUIChangedHandlers;

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

        event EventDispatching<PostProcessButtonsEvent>? IEventSource<PostProcessButtonsEvent>.Dispatching
        {
            add => _postProcessButtons += value;
            remove => _postProcessButtons -= value;
        }

        event EventDispatching<BuildCustomGenericBuilder>? IEventSource<BuildCustomGenericBuilder>.Dispatching
        {
            add => _buildCustomGenericBuilder += value;
            remove => _buildCustomGenericBuilder -= value;
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