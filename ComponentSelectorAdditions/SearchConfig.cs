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
    /// <summary>
    /// Represents the configuration for the search functionality.<br/>
    /// Use the <see cref="AddExcludedCategory(string)"/> and <see cref="RemoveExcludedCategory(string)"/>
    /// methods to add categories which shouldn't be searched into because they're added.
    /// </summary>
    public sealed class SearchConfig : SingletonConfigSection<SearchConfig>
    {
        private static readonly DefiningConfigKey<bool> _alwaysSearchRoot = new("AlwaysSearchRoot", "Always starts searching from the root category, regardless of the current one.", () => false);

        private static readonly Dictionary<string, bool> _excludedCategories = new(StringComparer.OrdinalIgnoreCase);

        private static readonly DefiningConfigKey<bool> _includeOpenGenericsWithGenericArgument = new("IncludeOpenGenericsWithGenericArgument", "Include the open generic versions of components / nodes in the results even when the generic argument can be applied to them successfully.", () => false);

        private static readonly DefiningConfigKey<int> _maxResultCount = new("MaxResultCount", "The maximum number of component / node results to display. 'Better' results are listed first. Categories don't count.", () => 64)
        {
            new ConfigKeyRange<int>(1, 128)
        };

        private static readonly DefiningConfigKey<float> _searchRefreshDelay = new("SearchRefreshDelay", "Time to wait after search input change before refreshing the results. 0 to always refresh.", () => .4f)
        {
            new ConfigKeyQuantity<float, Time>(new UnitConfiguration("s", "0", " ", new [] {"s", "ms"}), null, 0, 2)
        };

        private static readonly DefiningConfigKey<string> _userExcludedCategories = new("UserExcludedCategories", "Excludes specific categories from being searched into by path (case sensitive). Separate entries by semicolon. Search will work when started inside them.", () => "/ProtoFlux");

        private static readonly char[] _userExclusionSeparator = new[] { ';' };

        /// <summary>
        /// Gets whether the search always searches from the root category of the component selector / node browser.
        /// </summary>
        public bool AlwaysSearchRoot => _alwaysSearchRoot;

        /// <inheritdoc/>
        public override string Description => "Contains settings for the Component Selector Search.";

        /// <inheritdoc/>
        public override string Id => "Search";

        /// <summary>
        /// Gets whether the open generic versions of components / nodes should be included in the results
        /// even when the generic argument can be applied to them successfully.
        /// </summary>
        public bool IncludeOpenGenericsWithGenericArgument => _includeOpenGenericsWithGenericArgument;

        /// <summary>
        /// Gets how many results will be listed at most.
        /// </summary>
        public int MaxResultCount => _maxResultCount;

        /// <summary>
        /// Gets how many milliseconds to wait after the last change in the search input before actually searching.
        /// </summary>
        public int SearchRefreshDelay => (int)(1000 * _searchRefreshDelay);

        /// <inheritdoc/>
        public override Version Version { get; } = new(1, 0, 0);

        static SearchConfig()
        {
            _userExcludedCategories.Changed += UserExcludedCategoriesChanged;
        }

        /// <summary>
        /// Adds the given category path as an excluded category for searches.
        /// </summary>
        /// <remarks>
        /// This should be used to exclude any categories added by mods.
        /// Make sure to <see cref="RemoveExcludedCategory">remove</see> them again when being shutdown without the application being exited.<br/>
        /// Searching <i>inside</i> of them is still possible.
        /// </remarks>
        /// <param name="category">The path of the category to exclude. Must not be null or whitespace.</param>
        /// <returns><c>true</c> if the category is excluded now; otherwise, <c>false</c>.</returns>
        public bool AddExcludedCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return false;

            // Don't override user status of already excluded category
            if (!HasExcludedCategory(category))
                _excludedCategories[category] = false;

            return true;
        }

        /// <summary>
        /// Checks whether the given category path is an excluded category for searches.
        /// </summary>
        /// <param name="category">The path of the category to check.</param>
        /// <param name="isUserCategory">Whether the excluded category was added by the user if it is excluded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the given category is excluded; otherwise, <c>false</c>.</returns>
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

        /// <inheritdoc cref="HasExcludedCategory(string, out bool?)"/>
        public bool HasExcludedCategory(string category)
            => _excludedCategories.ContainsKey(category);

        /// <summary>
        /// Removes the given category path from being an excluded category for searches,
        /// if it wasn't added by the user.
        /// </summary>
        /// <remarks>
        /// This should be used to remove any excluded categories added by mods again
        /// when they're being shutdown without the application being exited.
        /// </remarks>
        /// <param name="category">The path of the category to remove from being excluded. Must not be null or whitespace.</param>
        /// <returns><c>true</c> if the category is not excluded anymore; otherwise, <c>false</c>.</returns>
        public bool RemoveExcludedCategory(string category)
        {
            if (!_excludedCategories.TryGetValue(category, out var userCategory))
                return true;

            if (userCategory)
                return false;

            _excludedCategories.Remove(category);

            return true;
        }

        /// <inheritdoc/>
        protected override void OnLoad(JObject source, JsonSerializer jsonSerializer)
        {
            base.OnLoad(source, jsonSerializer);

            LoadUserExcludedCategories(_userExcludedCategories);
        }

        private static void LoadUserExcludedCategories(string? categoryList)
        {
            foreach (var category in ProcessCategoryString(categoryList))
                _excludedCategories.TryAdd(category, true);
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