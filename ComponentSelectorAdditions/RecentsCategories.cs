using ComponentSelectorAdditions.Events;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.UIX;
using HarmonyLib;
using MonkeyLoader.Events;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ComponentSelectorAdditions
{
    [HarmonyPatchCategory(nameof(RecentsCategories))]
    [HarmonyPatch(typeof(ComponentSelector))]
    internal sealed class RecentsCategories : ConfiguredResoniteMonkey<RecentsCategories, RecentsConfig>,
        ICancelableEventHandler<EnumerateCategoriesEvent>, ICancelableEventHandler<EnumerateComponentsEvent>
    {
        private const string ProtoFluxPath = "/ProtoFlux/Runtimes/Execution/Nodes";
        private const string ProtoFluxRecentsPath = "/ProtoFlux/Runtimes/Execution/Nodes/Recents";
        private const string RecentsPath = "/Recents";

        private CategoryNode<Type> _protoFluxRecentsCategory = null!;
        private CategoryNode<Type> _protoFluxRootCategory = null!;
        private CategoryNode<Type> _recentsCategory = null!;
        private CategoryNode<Type> _rootCategory = null!;

        public int Priority => HarmonyLib.Priority.Normal;

        public bool SkipCanceled => true;

        public void Handle(EnumerateComponentsEvent eventData)
        {
            if (eventData.RootCategory != _recentsCategory && eventData.RootCategory != _protoFluxRecentsCategory)
                return;

            var recentElements = (eventData.RootCategory == _recentsCategory ? ConfigSection.Components : ConfigSection.ProtoFluxNodes)
                .Select(typeName => WorkerManager.ParseNiceType(typeName))
                .Where(type => type is not null)
                .Select(type => (Type: type, Category: WorkerInitializer.ComponentLibrary.GetSubcategory(WorkerInitializer.GetInitInfo(type).CategoryPath)));

            foreach (var element in recentElements)
                eventData.AddItem(new ComponentResult(element.Category, element.Type));

            eventData.Canceled = true;
        }

        public void Handle(EnumerateCategoriesEvent eventData)
        {
            if (eventData.RootCategory == _rootCategory)
                eventData.AddItem(_recentsCategory, -1000, true);
            else if (eventData.RootCategory == _protoFluxRootCategory)
                eventData.AddItem(_protoFluxRecentsCategory, -1000, true);
        }

        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        protected override bool OnEngineReady()
        {
            _recentsCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(RecentsPath);
            SearchConfig.Instance.AddExcludedCategory(RecentsPath);

            _protoFluxRecentsCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(ProtoFluxRecentsPath);
            SearchConfig.Instance.AddExcludedCategory(ProtoFluxRecentsPath);

            _rootCategory = WorkerInitializer.ComponentLibrary;
            _protoFluxRootCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(ProtoFluxPath);

            Mod.RegisterEventHandler<EnumerateCategoriesEvent>(this);
            Mod.RegisterEventHandler<EnumerateComponentsEvent>(this);

            return base.OnEngineReady();
        }

        protected override bool OnShutdown(bool applicationExiting)
        {
            if (!applicationExiting)
            {
                Mod.UnregisterEventHandler<EnumerateCategoriesEvent>(this);
                Mod.UnregisterEventHandler<EnumerateComponentsEvent>(this);

                _rootCategory._subcategories.Remove("Recents");
                SearchConfig.Instance.RemoveExcludedCategory(RecentsPath);

                _protoFluxRootCategory._subcategories.Remove("Recents");
                SearchConfig.Instance.RemoveExcludedCategory(ProtoFluxRecentsPath);
            }

            return base.OnShutdown(applicationExiting);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ComponentSelector.OnAddComponentPressed))]
        private static void OnAddComponentPressedPostfix(ComponentSelector __instance, string typename)
        {
            if (!Enabled)
                return;

            var type = WorkerManager.ParseNiceType(typename);
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

        private static bool ToggleHashSetContains<T>(ISet<T> set, T value)
        {
            if (set.Add(value))
                return true;

            set.Remove(value);
            return false;
        }

        private static void UpdateRecents(List<string> recents, Type type)
        {
            if (type.IsGenericType && ConfigSection.TrackGenericComponents)
                recents.Add(type.GetGenericTypeDefinition().FullName);

            if (!type.IsGenericType || ConfigSection.TrackConcreteComponents)
                recents.Add(type.FullName);

            if (recents.Count > ConfigSection.RecentCap)
                recents.RemoveRange(0, recents.Count - ConfigSection.RecentCap);
        }
    }
}