using ComponentSelectorAdditions.Events;
using Elements.Core;
using FrooxEngine;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using MonkeyLoader.Resonite.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonkeyLoader.Events;
using System.Threading;
using System.Globalization;

namespace ComponentSelectorAdditions
{
    internal sealed class SearchBar : ConfiguredResoniteMonkey<SearchBar, SearchConfig>, IEventHandler<BuildSelectorHeaderEvent>,
        ICancelableEventHandler<EnumerateCategoriesEvent>, ICancelableEventHandler<EnumerateComponentsEvent>
    {
        private static readonly char[] _searchSplits = new[] { ' ', ',', '+', '|' };
        public override bool CanBeDisabled => true;
        public int Priority => HarmonyLib.Priority.HigherThanNormal;

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
            var textField = ui.TextField(null, parseRTF: false);
            var details = new SelectorSearchBar(searchLayout, textField.Editor.Target, ConfigSection.SearchRefreshDelay);
            eventData.SearchBar = details;

            details.Text.NullContent.AssignLocaleString($"{Mod.Id}.Search".AsLocaleKey());
            details.Editor.FinishHandling.Value = TextEditor.FinishAction.NullOnWhitespace;

            ui.Style.FlexibleWidth = -1;
            ui.Style.ButtonTextAlignment = Alignment.MiddleCenter;
            ui.LocalActionButton("∅", _ => details.Content = null);

            ui.PopStyle();
            ui.NestOut();
        }

        public void Handle(EnumerateCategoriesEvent eventData)
        {
            if (!eventData.Path.HasSearch || (eventData.Path.IsSelectorRoot && eventData.Path.Search.Length < 3))
                return;

            var search = eventData.Path.Search.Split(_searchSplits);

            foreach (var category in SearchCategories(eventData.RootCategory, search))
                eventData.AddItem(category);

            eventData.Canceled = true;
        }

        public void Handle(EnumerateComponentsEvent eventData)
        {
            if (!eventData.Path.HasSearch || (eventData.Path.IsSelectorRoot && eventData.Path.Search.Length < 3))
                return;

            var search = eventData.Path.Search.Split(_searchSplits);
            var results = eventData.RootCategory.Elements
                .Select(type => (Category: eventData.RootCategory, Type: type, Matches: SearchContains(type.Name, search)))
                .Concat(
                    SearchCategories(eventData.RootCategory)
                    .SelectMany(category => category.Elements
                        .Select(type => (Category: category, Type: type, Matches: SearchContains(type.Name, search)))))
                .Where(match => match.Matches > 0)
                .OrderBy(match => match.Type.Name)
                .OrderByDescending(match => match.Matches)
                .Select(match => (Component: new ComponentResult(match.Category, match.Type), Order: -match.Matches));

            var remaining = ConfigSection.MaxResultCount;
            var knownGroups = new HashSet<string>();

            foreach (var result in results.TakeWhile(result => (!result.Component.HasGroup || knownGroups.Add(result.Component.Group) ? --remaining : remaining) >= 0))
                eventData.AddItem(result.Component, result.Order);

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
    }
}