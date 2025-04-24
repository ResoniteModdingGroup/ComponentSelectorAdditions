using ComponentSelectorAdditions.Events;
using Elements.Core;
using FrooxEngine;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonkeyLoader.Events;
using System.Threading;
using System.Globalization;
using MonkeyLoader;
using MonkeyLoader.Resonite.UI;
using FrooxEngine.UIX;

namespace ComponentSelectorAdditions
{
    internal sealed class SearchBar : ConfiguredResoniteMonkey<SearchBar, SearchConfig>, IEventHandler<BuildSelectorHeaderEvent>,
        ICancelableEventHandler<EnumerateCategoriesEvent>, ICancelableEventHandler<EnumerateComponentsEvent>
    {
        private const string ProtoFluxPath = "/ProtoFlux/Runtimes/Execution/Nodes";

        public override bool CanBeDisabled => true;

        public int Priority => HarmonyLib.Priority.VeryHigh;

        public bool SkipCanceled => true;

        public void Handle(BuildSelectorHeaderEvent eventData)
        {
            if (!Enabled || eventData.AddsSearchBar)
                return;

            var ui = eventData.UI;

            ui.PushStyle();
            ui.Style.MinHeight = 48;
            ui.Style.MinWidth = 48;

            var searchLayout = ui.HorizontalLayout(8).Slot;
            searchLayout.DestroyWhenLocalUserLeaves();

            ui.Style.FlexibleWidth = 1;
            var textField = ui.TextField(null!, parseRTF: false);
            textField.Slot.GetComponent<Button>()?.WithTooltip(Mod.GetLocaleString("Search.Tooltip"));

            var details = new SelectorSearchBar(searchLayout, textField.Editor.Target, () => ConfigSection.SearchRefreshDelay);
            eventData.SearchBar = details;

            details.Text.NullContent.AssignLocaleString(Mod.GetLocaleString("Search"));
            details.Editor.FinishHandling.Value = TextEditor.FinishAction.NullOnWhitespace;

            ui.Style.FlexibleWidth = -1;
            ui.Style.ButtonTextAlignment = Alignment.MiddleCenter;

            var clearButton = ui.Button("∅").WithTooltip(Mod.GetLocaleString("Search.Clear"));
            var clearAction = clearButton.Slot.AttachComponent<ButtonValueSet<string>>();
            clearAction.TargetValue.Target = details.Text.Content;

            ui.PopStyle();
            ui.NestOut();
        }

        public void Handle(EnumerateCategoriesEvent eventData)
        {
            if (!eventData.Path.HasSearch || ((eventData.Path.IsSelectorRoot || ConfigSection.AlwaysSearchRoot) && eventData.Path.Search.Length < 3 && eventData.Path.SearchFragments.Length > 0))
                return;

            foreach (var category in SearchCategories(PickSearchCategory(eventData), eventData.Path.SearchFragments))
                eventData.AddItem(category);

            eventData.Canceled = true;
        }

        public void Handle(EnumerateComponentsEvent eventData)
        {
            if (!eventData.Path.HasSearch || (eventData.Path.IsSelectorRoot && eventData.Path.Search.Length < 3))
                return;

            var searchCategory = PickSearchCategory(eventData);

            var results = searchCategory.Elements
                .Select(type => (Category: searchCategory, Type: type, Matches: SearchContains(type.Name, eventData.Path.SearchFragments)))
                .Concat(
                    SearchCategories(searchCategory)
                    .SelectMany(category => category.Elements
                        .Select(type => (Category: category, Type: type, Matches: SearchContains(type.Name, eventData.Path.SearchFragments)))))
                .Where(match => match.Matches > 0)
                .OrderByDescending(match => match.Matches)
                .ThenBy(match => match.Type.Name)
                .Select(match => (Component: new ComponentResult(match.Category, match.Type), Order: -match.Matches));

            var remaining = ConfigSection.MaxResultCount;
            var knownGroups = new HashSet<string>();
            var parsedGeneric = eventData.Path.HasSearchGeneric ? eventData.Selector.World.Types.ParseNiceType(eventData.Path.SearchGeneric, true) : null;

            foreach (var result in results.TakeWhile(result => (!result.Component.HasGroup || knownGroups.Add(result.Component.Group) ? --remaining : remaining) >= 0))
            {
                eventData.AddItem(result.Component, result.Order);

                if (result.Component.IsGeneric && parsedGeneric is not null)
                {
                    try
                    {
                        var concreteType = result.Component.Type.MakeGenericType(parsedGeneric);

                        if (!concreteType.IsValidGenericType(true))
                            continue;

                        --remaining;
                        eventData.AddItem(new(result.Component.Category, concreteType), result.Order);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex.LogFormat($"Failed to make generic type for component [{result.Component.NiceName}] with [{parsedGeneric.GetNiceName()}] (from \"{eventData.Path.GenericType}\")!"));
                    }
                }
            }

            eventData.Canceled = true;
        }

        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        protected override bool OnEngineReady()
        {
            Mod.RegisterEventHandler<BuildSelectorHeaderEvent>(this);
            Mod.RegisterEventHandler<EnumerateCategoriesEvent>(this);
            Mod.RegisterEventHandler<EnumerateComponentsEvent>(this);

            return base.OnEngineReady();
        }

        protected override bool OnShutdown(bool applicationExiting)
        {
            if (!applicationExiting)
            {
                Mod.UnregisterEventHandler<BuildSelectorHeaderEvent>(this);
                Mod.UnregisterEventHandler<EnumerateCategoriesEvent>(this);
                Mod.UnregisterEventHandler<EnumerateComponentsEvent>(this);
            }

            return base.OnShutdown(applicationExiting);
        }

        private static IEnumerable<CategoryNode<Type>> SearchCategories(CategoryNode<Type> root, string[]? search = null)
        {
            var returnAll = search is null;
            var queue = new Queue<CategoryNode<Type>>();

            foreach (var subCategory in root.Subcategories)
                queue.Enqueue(subCategory);

            while (queue.Count > 0)
            {
                var category = queue.Dequeue();

                if (ConfigSection.HasExcludedCategory(category.GetPath()))
                    continue;

                if (returnAll || SearchContains(category.Name, search!) > 0)
                    yield return category;

                foreach (var subCategory in category.Subcategories)
                    queue.Enqueue(subCategory);
            }
        }

        private static int SearchContains(string haystack, string[] needles)
            => needles.Count(needle => CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase) >= 0);

        private CategoryNode<Type> PickSearchCategory(IEnumerateSelectorResultEvent eventData)
        {
            var isProtoFlux = eventData.Path.Path.StartsWith(ProtoFluxPath);

            return ConfigSection.AlwaysSearchRoot
                ? (isProtoFlux
                    ? WorkerInitializer.ComponentLibrary.GetSubcategory(ProtoFluxPath)
                    : WorkerInitializer.ComponentLibrary)
                : eventData.RootCategory;
        }
    }
}