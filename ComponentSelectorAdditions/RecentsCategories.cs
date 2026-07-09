using ComponentSelectorAdditions.Events;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using MonkeyLoader.Events;
using MonkeyLoader.Resonite;

namespace ComponentSelectorAdditions
{
    [HarmonyPatch(typeof(ComponentSelector))]
    [HarmonyPatchCategory(nameof(RecentsCategories))]
    internal sealed class RecentsCategories
        : ConfiguredResoniteCancelableEventHandlerMonkey<RecentsCategories, RecentsConfig, EnumerateCategoriesEvent, EnumerateComponentsEvent>,
        IEventHandler<EnumerateConcreteGenericsEvent>
    {
        private const string ProtoFluxPath = "/ProtoFlux/Runtimes/Execution/Nodes";
        private const string ProtoFluxRecentsPath = "/ProtoFlux/Runtimes/Execution/Nodes/Recents";
        private const string RecentsPath = "/Recents";

        private CategoryNode<Type> _protoFluxRecentsCategory = null!;
        private CategoryNode<Type> _protoFluxRootCategory = null!;
        private CategoryNode<Type> _recentsCategory = null!;
        private CategoryNode<Type> _rootCategory = null!;

        public override bool CanBeDisabled => true;

        public override int Priority => HarmonyLib.Priority.High;

        public override bool SkipCanceled => true;

        public void Handle(EnumerateConcreteGenericsEvent eventData)
        {
            if (!Enabled)
                return;

            var concreteGenerics = ConfigSection.Components
                .Concat(ConfigSection.ProtoFluxNodes)
                .Where(type => type is not null && type.IsGenericType && !type.ContainsGenericParameters && type.GetGenericTypeDefinition() == eventData.Component);

            foreach (var concreteGeneric in concreteGenerics)
                eventData.AddItem(concreteGeneric);
        }

        protected override void Handle(EnumerateComponentsEvent eventData)
        {
            if (eventData.RootCategory != _recentsCategory && eventData.RootCategory != _protoFluxRecentsCategory)
                return;

            var recentElements = (eventData.RootCategory == _recentsCategory ? ConfigSection.Components : ConfigSection.ProtoFluxNodes)
                .Where(type => type is not null)
                .Select(type => (Type: type, Category: WorkerInitializer.ComponentLibrary.GetSubcategory(WorkerInitializer.GetInitInfo(type).CategoryPath)));

            foreach (var element in recentElements)
                eventData.AddItem(new ComponentResult(element.Category, element.Type));

            eventData.Canceled = true;
        }

        protected override void Handle(EnumerateCategoriesEvent eventData)
        {
            if (eventData.RootCategory == _rootCategory)
                eventData.AddItem(_recentsCategory, -1000, true);
            else if (eventData.RootCategory == _protoFluxRootCategory)
                eventData.AddItem(_protoFluxRecentsCategory, -1000, true);
        }

        protected override void OnDisabled() => RemoveCategories();

        protected override void OnEnabled() => AddCategories();

        protected override bool OnEngineReady()
        {
            AddCategories();

            _rootCategory = WorkerInitializer.ComponentLibrary;
            _protoFluxRootCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(ProtoFluxPath);

            Mod.RegisterEventHandler<EnumerateConcreteGenericsEvent>(this);

            return base.OnEngineReady();
        }

        protected override bool OnShutdown(bool applicationExiting)
        {
            if (!applicationExiting)
            {
                Mod.UnregisterEventHandler<EnumerateConcreteGenericsEvent>(this);

                RemoveCategories();
            }

            return base.OnShutdown(applicationExiting);
        }

        private static void AddRecent(List<Type> recents, Type type)
        {
            recents.RemoveAll(recentType => recentType == type);
            recents.Add(type);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ComponentSelector.OnAddComponentPressed))]
        private static void OnAddComponentPressedPostfix(ComponentSelector __instance, string typename)
        {
            if (!Enabled)
                return;

            var type = __instance.World.Types.DecodeType(typename);
            if (type is null || type.IsGenericTypeDefinition)
                return;

            if (__instance._rootPath.Value == ProtoFluxHelper.PROTOFLUX_ROOT)
                UpdateRecents(ConfigSection.ProtoFluxNodes, type);
            else
                UpdateRecents(ConfigSection.Components, type);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ComponentSelector.OnCreateCustomType))]
        private static void OnCreateCustomTypePostfix(ComponentSelector __instance)
        {
            if (!Enabled)
                return;

            var type = __instance.GetCustomGenericType();
            if (type is null || !type.IsValidGenericType(true))
                return;

            if (__instance._rootPath.Value == ProtoFluxHelper.PROTOFLUX_ROOT)
                UpdateRecents(ConfigSection.ProtoFluxNodes, type);
            else
                UpdateRecents(ConfigSection.Components, type);
        }

        private static void UpdateRecents(List<Type> recents, Type type)
        {
            if (type.IsGenericType && ConfigSection.TrackGenericComponents)
                AddRecent(recents, type.GetGenericTypeDefinition());

            if (!type.IsGenericType || ConfigSection.TrackConcreteComponents)
                AddRecent(recents, type);

            if (recents.Count > ConfigSection.RecentCap)
                recents.RemoveRange(0, recents.Count - ConfigSection.RecentCap);
        }

        private void AddCategories()
        {
            _recentsCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(RecentsPath);
            SearchConfig.Instance.AddExcludedCategory(RecentsPath);

            _protoFluxRecentsCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(ProtoFluxRecentsPath);
            SearchConfig.Instance.AddExcludedCategory(ProtoFluxRecentsPath);
        }

        private void RemoveCategories()
        {
            _rootCategory._subcategories.Remove("Recents");
            SearchConfig.Instance.RemoveExcludedCategory(RecentsPath);

            _protoFluxRootCategory._subcategories.Remove("Recents");
            SearchConfig.Instance.RemoveExcludedCategory(ProtoFluxRecentsPath);
        }
    }
}