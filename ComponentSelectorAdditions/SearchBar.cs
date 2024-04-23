using ComponentSelectorAdditions.Events;
using Elements.Core;
using FrooxEngine.UIX;
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
using System.Runtime.CompilerServices;
using ComponentSelectorSearch;
using MonkeyLoader.Configuration;
using System.Threading;
using System.Globalization;

namespace ComponentSelectorAdditions
{
    internal sealed class SearchBar : ConfiguredResoniteMonkey<SearchBar, SearchConfig>, IEventHandler<BuildSelectorHeaderEvent>,
        IEventHandler<EnumerateCategoriesEvent>, IEventHandler<EnumerateComponentsEvent>
    {
        private static readonly char[] _searchSplits = new[] { ' ', ',', '+', '|' };
        private static readonly ConditionalWeakTable<ComponentSelector, SelectorData> _selectorData = new();
        public override bool CanBeDisabled => true;
        public int Priority => HarmonyLib.Priority.HigherThanNormal;

        public void Handle(BuildSelectorHeaderEvent eventData)
        {
            if (!Enabled)
                return;

            var ui = eventData.UI;
            var root = ui.Root;

            ui.PushStyle();
            ui.Style.MinHeight = 48;

            var searchPanel = ui.Panel().Slot;

            ui.VerticalFooter(56, out var footer, out var content);
            footer.OffsetMin.Value += new float2(8, 0);

            ui.NestInto(content);

            var textField = ui.TextField(null, parseRTF: false);
            var details = new SelectorData(searchPanel, textField.Editor.Target);
            _selectorData.Add(eventData.Selector, details);

            details.Text.NullContent.AssignLocaleString($"{Mod.Id}.Search".AsLocaleKey());
            details.Editor.FinishHandling.Value = TextEditor.FinishAction.NullOnWhitespace;
            details.Text.Content.OnValueChange += MakeBuildUICall(eventData.Selector, details);

            ui.NestInto(footer);
            ui.Style.ButtonTextAlignment = Alignment.MiddleCenter;
            ui.LocalActionButton("∅", _ => details.Text.Content.Value = null);

            eventData.BackButtonChanged += (path, showBackButton) =>
            {
                searchPanel.ActiveSelf = !path.GenericType && !path.HasGroup;
                details.LastPath = path;
            };

            ui.NestInto(root);
            ui.PopStyle();
        }

        public void Handle(EnumerateCategoriesEvent eventData)
        {
            if (!eventData.Path.HasSearch || !_selectorData.TryGetValue(eventData.Selector, out var details)
                || (eventData.Path.IsSelectorRoot && eventData.Path.Search.Length < 3))
                return;

            var search = eventData.Path.Search.Split(_searchSplits);

            foreach (var category in SearchCategories(eventData.RootCategory, search))
                eventData.AddItem(category);

            eventData.Canceled = true;
        }

        public void Handle(EnumerateComponentsEvent eventData)
        {
            if (!eventData.Path.HasSearch || !_selectorData.TryGetValue(eventData.Selector, out var details)
                || (eventData.Path.IsSelectorRoot && eventData.Path.Search.Length < 3))
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
                .OrderByDescending(match => match.Matches);

            foreach (var result in results)
                eventData.AddItem(new(result.Category, result.Type), result.Matches);

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

        private static SyncFieldEvent<string> MakeBuildUICall(ComponentSelector selector, SelectorData details)
        {
            return field =>
            {
                var token = details.UpdateSearch();

                selector.StartTask(async () =>
                {
                    if (ConfigSection.SearchRefreshDelay > 0)
                    {
                        await default(ToBackground);
                        await Task.Delay(ConfigSection.SearchRefreshDelay);
                        await default(NextUpdate);
                    }

                    // Only refresh UI with search results if there was no further update immediately following it
                    if (token.IsCancellationRequested || selector.IsDestroyed)
                        return;

                    selector.BuildUI($"{details.LastPath.Path}/{SelectorPath.SearchSegment}/{field.Value}", false);
                });
            };
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

        private static IEnumerable<ComponentResult> SearchTypes(CategoryNode<Type> root, string[] search)
            => root.Elements
                .Select(type => (Category: root, Type: type, Matches: SearchContains(type.Name, search)))
                .Concat(
                    SearchCategories(root)
                    .SelectMany(category => category.Elements
                        .Select(type => (Category: category, Type: type, Matches: SearchContains(type.Name, search)))))
                .Where(match => match.Matches > 0)
                .OrderBy(match => match.Type.Name)
                .OrderByDescending(match => match.Matches)
                .Select(match => new ComponentResult(match.Category, match.Type))
                .Take(ConfigSection.MaxResultCount);

        private sealed class SelectorData
        {
            private CancellationTokenSource _lastResultUpdate = new CancellationTokenSource();

            public TextEditor Editor { get; }

            public SelectorPath LastPath { get; set; }

            public Slot Search { get; }

            public Text Text => (Text)Editor.Text.Target;

            public SelectorData(Slot search, TextEditor editor)
            {
                Search = search;
                Editor = editor;

                LastPath = new SelectorPath(null, false, null, true);
            }

            public CancellationToken UpdateSearch()
            {
                _lastResultUpdate.Cancel();
                _lastResultUpdate = new CancellationTokenSource();

                return _lastResultUpdate.Token;
            }
        }
    }
}