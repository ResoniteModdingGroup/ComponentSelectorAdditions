using Elements.Quantity;
using FrooxEngine;
using MonkeyLoader.Configuration;
using MonkeyLoader.Resonite.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions
{
    public sealed class SearchConfig : ConfigSection
    {
        private static readonly Dictionary<string, bool> _excludedCategories = new(StringComparer.OrdinalIgnoreCase);

        private static readonly DefiningConfigKey<int> _maxResultCountKey = new("MaxResultCount", "The maximum number of component / node results to display. 'Better' results are listed first. Categories don't count.", () => 64)
        {
            new ConfigKeyRange<int>(1, 128)
        };

        private static readonly DefiningConfigKey<float> _searchRefreshDelayKey = new("SearchRefreshDelay", "Time to wait after search input change before refreshing the results. 0 to always refresh.", () => .75f)
        {
            new ConfigKeyQuantity<float, Time>(new UnitConfiguration("s", "0", " ", new [] {"s", "ms"}), null, 0, 2)
        };

        private static readonly DefiningConfigKey<string> _userExcludedCategoriesKey = new("UserExcludedCategories", "Excludes specific categories from being searched into by path (case sensitive). Separate entries by semicolon. Search will work when started inside them.", () => "/ProtoFlux");
        private static readonly char[] _userExclusionSeparator = new[] { ';' };
        public static SearchConfig Instance { get; private set; }
        public override string Description => "Contains settings for the Component Selector Search.";
        public override string Id => "Search";
        public override Version Version { get; } = new(1, 0, 0);
        internal int MaxResultCount => _maxResultCountKey.GetValue();
        internal int SearchRefreshDelay => (int)_searchRefreshDelayKey.GetValue();

        static SearchConfig()
        {
            _userExcludedCategoriesKey.Changed += UserExcludedCategoriesChanged;
        }

        public SearchConfig()
        {
            if (Instance is not null)
                throw new InvalidOperationException();

            Instance = this;
        }

        public bool AddExcludedCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return false;

            _excludedCategories[category] = false;

            return true;
        }

        public bool HasExcludedCategory(string category, [NotNullWhen(true)] out bool? isUserCategory)
        {
            if (_excludedCategories.TryGetValue(category, out var userCategory))
            {
                isUserCategory = userCategory;
                return true;
            }

            isUserCategory = null;
            return false;
        }

        public bool HasExcludedCategory(string category)
            => _excludedCategories.ContainsKey(category);

        public bool RemoveExcludedCategory(string category)
        {
            if (!_excludedCategories.TryGetValue(category, out var userCategory))
                return true;

            if (userCategory)
                return false;

            _excludedCategories.Remove(category);

            return true;
        }

        protected override void OnLoad(JObject source, JsonSerializer jsonSerializer)
        {
            base.OnLoad(source, jsonSerializer);

            Instance = this;
            LoadUserExcludedCategories(_userExcludedCategoriesKey.GetValue());
        }

        private static void LoadUserExcludedCategories(string? categoryList)
        {
            foreach (var category in ProcessCategoryString(categoryList))
            {
                if (!_excludedCategories.ContainsKey(category))
                    _excludedCategories.Add(category, true);
            }
        }

        private static IEnumerable<string> ProcessCategoryString(string? categoryList)
            => categoryList?
                .Split(_userExclusionSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(category => category.Trim())
                .Where(category => !string.IsNullOrWhiteSpace(category))
            ?? Enumerable.Empty<string>();

        private static void UserExcludedCategoriesChanged(object sender, ConfigKeyChangedEventArgs<string> configKeyChangedEventArgs)
        {
            foreach (var category in ProcessCategoryString(configKeyChangedEventArgs.OldValue))
            {
                if (_excludedCategories.TryGetValue(category, out var userCategory) && userCategory)
                    _excludedCategories.Remove(category);
            }

            LoadUserExcludedCategories(configKeyChangedEventArgs.NewValue);
        }
    }
}