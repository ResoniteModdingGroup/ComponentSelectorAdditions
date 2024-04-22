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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions
{
    [HarmonyPatch(typeof(ComponentSelector))]
    [HarmonyPatch(nameof(ComponentSelectorInjector))]
    internal sealed class ComponentSelectorInjector : ResoniteMonkey<ComponentSelectorInjector>,
        IEventSource<BuildSelectorHeaderEvent>, IEventSource<BuildSelectorEvent>,
        ICancelableEventSource<EnumerateCategoriesEvent>, ICancelableEventSource<EnumerateComponentsEvent>,
        IEventSource<EnumerateConcreteGenericsEvent>
    {
        private static readonly ConditionalWeakTable<ComponentSelector, SelectorData> _selectorData = new();
        private static EventDispatching<BuildSelectorEvent>? _buildFooter;
        private static EventDispatching<BuildSelectorHeaderEvent>? _buildHeader;
        private static CancelableEventDispatching<EnumerateCategoriesEvent>? _enumerateCategories;
        private static CancelableEventDispatching<EnumerateComponentsEvent>? _enumerateComponents;
        private static EventDispatching<EnumerateConcreteGenericsEvent>? _enumerateConcreteGenerics;

        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        protected override bool OnEngineReady()
        {
            Mod.RegisterEventSource<BuildSelectorHeaderEvent>(this);
            Mod.RegisterEventSource<BuildSelectorEvent>(this);
            Mod.RegisterEventSource<EnumerateCategoriesEvent>(this);
            Mod.RegisterEventSource<EnumerateComponentsEvent>(this);
            Mod.RegisterEventSource<EnumerateConcreteGenericsEvent>(this);

            return base.OnEngineReady();
        }

        private static void BuildCategoryUI(ComponentSelector selector, UIBuilder ui, SelectorPath path, bool doNotGenerateBack)
        {
            CategoryNode<Type> rootCategory;
            if (path.IsRootCategory)
            {
                rootCategory = WorkerInitializer.ComponentLibrary;
            }
            else
            {
                rootCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(path.Path);

                if (rootCategory is null)
                {
                    path = new SelectorPath(selector._rootPath, true);
                    rootCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(path.Path);

                    if (rootCategory is null)
                    {
                        selector._rootPath.Value = "/";
                        path = new SelectorPath("/", true);
                        rootCategory = WorkerInitializer.ComponentLibrary;
                    }
                }
            }

            if (rootCategory != WorkerInitializer.ComponentLibrary && !doNotGenerateBack)
            {
                ui.Button("ComponentSelector.Back".AsLocaleKey(), RadiantUI_Constants.BUTTON_COLOR,
                    selector.OnOpenCategoryPressed, path.HasGroup ? path.OpenCategoryPath : rootCategory.Parent.GetPath(), 0.35f);
            }

            KeyCounter<string> keyCounter = null;
            HashSet<string> hashSet = null;
            if (group == null)
            {
                foreach (CategoryNode<Type> subcategory in rootCategory.Subcategories)
                {
                    UIBuilder uIBuilder8 = ui;
                    text2 = subcategory.Name + " >";
                    tint = RadiantUI_Constants.Sub.YELLOW;
                    uIBuilder8.Button(in text2, in tint, OnOpenCategoryPressed, path + "/" + subcategory.Name, 0.35f).Label.ParseRichText.Value = false;
                }
                keyCounter = new KeyCounter<string>();
                hashSet = new HashSet<string>();
                foreach (Type element in rootCategory.Elements)
                {
                    GroupingAttribute customAttribute = element.GetCustomAttribute<GroupingAttribute>();
                    if (customAttribute != null)
                    {
                        keyCounter.Increment(customAttribute.GroupName);
                    }
                }
            }

            List<Button> list = Pool.BorrowList<Button>();
            foreach (Type element2 in rootCategory.Elements)
            {
                if (ComponentFilter.Target != null && !ComponentFilter.Target(element2))
                {
                    continue;
                }
                GroupingAttribute customAttribute2 = element2.GetCustomAttribute<GroupingAttribute>();
                if (group != null && customAttribute2?.GroupName != group)
                {
                    continue;
                }
                Button button2;
                if (group == null && customAttribute2 != null && keyCounter[customAttribute2.GroupName] > 1)
                {
                    if (!hashSet.Add(customAttribute2.GroupName))
                    {
                        continue;
                    }
                    string text3 = customAttribute2.GroupName.Split('.')?.Last();
                    UIBuilder uIBuilder9 = ui;
                    text2 = text3;
                    tint = RadiantUI_Constants.Sub.PURPLE;
                    button2 = uIBuilder9.Button(in text2, in tint, OpenGroupPressed, path + ":" + customAttribute2.GroupName, 0.35f);
                    list.Add(button2);
                }
                else if (element2.IsGenericTypeDefinition)
                {
                    string text4 = Path.Combine(path, element2.FullName);
                    if (group != null)
                    {
                        text4 = text4 + "?" + group;
                    }
                    UIBuilder uIBuilder10 = ui;
                    text2 = element2.GetNiceName();
                    tint = RadiantUI_Constants.Sub.GREEN;
                    button2 = uIBuilder10.Button(in text2, in tint, OpenGenericTypesPressed, text4, 0.35f);
                    list.Add(button2);
                }
                else
                {
                    UIBuilder uIBuilder11 = ui;
                    text2 = element2.GetNiceName();
                    tint = RadiantUI_Constants.Sub.CYAN;
                    button2 = uIBuilder11.Button(in text2, in tint, OnAddComponentPressed, element2.FullName, 0.35f);
                }
                button2.Label.ParseRichText.Value = false;
                list.Add(button2);
            }
            list.Sort((Button a, Button b) => a.LabelText.CompareTo(b.LabelText));
            for (int j = 0; j < list.Count; j++)
            {
                list[j].Slot.OrderOffset = 10 + j;
            }
        }

        private static void BuildGenericTypeUI(ComponentSelector selector, UIBuilder ui, SelectorPath path, bool doNotGenerateBack)
        {
            var type = WorkerManager.GetType(PathUtility.GetFileName(path.PathSegments[^1]));
            selector._genericType.Value = type;

            if (!doNotGenerateBack)
                ui.Button("ComponentSelector.Back".AsLocaleKey(), RadiantUI_Constants.BUTTON_COLOR, selector.OnOpenCategoryPressed, path.OpenCategoryPath, 0.35f);

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

            var concreteGenericsEventData = OnEnumerateConcreteGenerics(type);
            foreach (var concreteGeneric in WorkerInitializer.GetCommonGenericTypes(type))
                concreteGenericsEventData.AddItem(concreteGeneric);

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
            else if (__instance._rootPath.Value == path && group is null)
                doNotGenerateBack = true;

            var selectorPath = new SelectorPath(path, group, __instance._rootPath.Value == path);

            OnShowSelectorBackButtonChanged(__instance, selectorData.ShowBackButtonChanged, doNotGenerateBack);
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

            BuildCategoryUI(__instance, ui, selectorPath);

            if (!selectorData.HasCancelButton)
                ui.Button("General.Cancel".AsLocaleKey(), RadiantUI_Constants.Sub.RED, __instance.OnCancelPressed, 0.35f).Slot.OrderOffset = 1000000L;

            return false;
        }

        private static BuildSelectorFooterEvent OnBuildFooter(ComponentSelector selector, UIBuilder ui, bool hasBackButton, bool hasCancelButton)
        {
            var eventData = new BuildSelectorFooterEvent(selector, ui, hasBackButton, hasCancelButton);

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

        private static EnumerateConcreteGenericsEvent OnEnumerateConcreteGenerics(Type component)
        {
            var eventData = new EnumerateConcreteGenericsEvent(component);

            try
            {
                _enumerateConcreteGenerics?.TryInvokeAll(eventData);
            }
            catch (AggregateException ex)
            {
                Logger.Warn(() => ex.Format("Some Enumerate Concrete Generics Event handlers threw an exception:"));
            }

            return eventData;
        }

        private static void OnShowSelectorBackButtonChanged(ComponentSelector selector, Action<bool>? handlers, bool showBackButton)
        {
            try
            {
                handlers?.TryInvokeAll(showBackButton);
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

            ui.VerticalLayout(8, 8);

            var headerEventData = OnBuildHeader(__instance, ui);

            ui.Style.FlexibleHeight = 1;
            ui.ScrollArea();
            ui.VerticalLayout(8f, 8f);
            ui.FitContent(SizeFit.Disabled, SizeFit.MinSize);
            __instance._uiRoot.Target = ui.Root;
            ui.Style.FlexibleHeight = -1;

            var footerEventData = OnBuildFooter(__instance, ui, headerEventData.AddsBackButton, headerEventData.AddsCancelButton);

            var showBackButtonChangedHandlers = headerEventData.ShowBackButtonChangedHandlers;
            showBackButtonChangedHandlers += footerEventData.ShowBackButtonChangedHandlers;

            _selectorData.Add(__instance, new SelectorData(
                headerEventData.AddsBackButton || footerEventData.AddsBackButton,
                headerEventData.AddsCancelButton || footerEventData.AddsCancelButton,
                showBackButtonChangedHandlers));

            __instance.BuildUI(null);

            return false;
        }

        event EventDispatching<BuildSelectorEvent>? IEventSource<BuildSelectorEvent>.Dispatching
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

        private sealed class SelectorData
        {
            public bool HasBackButton { get; }
            public bool HasCancelButton { get; }
            public Action<bool>? ShowBackButtonChanged { get; }

            public SelectorData(bool hasBackButton, bool hasCancelButton, Action<bool>? showBackButtonChanged)
            {
                HasBackButton = hasBackButton;
                HasCancelButton = hasCancelButton;
                ShowBackButtonChanged = showBackButtonChanged;
            }
        }
    }