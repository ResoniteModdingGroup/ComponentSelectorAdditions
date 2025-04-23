using ComponentSelectorAdditions.Events;
using EnumerableToolkit;
using FrooxEngine;
using MonkeyLoader.Resonite;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace ComponentSelectorAdditions
{
    public sealed class CategoryOverrideHandler : ResoniteCancelableEventHandlerMonkey<CategoryOverrideHandler, EnumerateComponentsEvent>
    {
        private static readonly Dictionary<CategoryNode<Type>, HashSet<CategoryOverride>> _overridesByCategory = new();

        /// <inheritdoc/>
        public override int Priority => HarmonyLib.Priority.VeryLow;

        /// <inheritdoc/>
        public override bool SkipCanceled => false;

        public static bool AddOverride(CategoryOverride categoryOverride)
            => _overridesByCategory.GetOrCreateValue(categoryOverride.TargetCategory).Add(categoryOverride);

        public static IEnumerable<CategoryOverride> GetOverrides(CategoryNode<Type> category)
            => _overridesByCategory.TryGetValue(category, out var overrides) ? overrides.AsSafeEnumerable() : Enumerable.Empty<CategoryOverride>();

        public static bool HasAnyOverride(CategoryNode<Type> category)
            => _overridesByCategory.TryGetValue(category, out var overrides) && overrides.Count > 0;

        public static bool HasOverride(CategoryOverride categoryOverride)
            => _overridesByCategory.TryGetValue(categoryOverride.TargetCategory, out var overrides) && overrides.Contains(categoryOverride);

        public static bool RemoveOverride(CategoryOverride categoryOverride)
            => _overridesByCategory.TryGetValue(categoryOverride.TargetCategory, out var overrides) && overrides.Remove(categoryOverride);

        /// <inheritdoc/>
        protected override void Handle(EnumerateComponentsEvent eventData)
        {
            foreach (var categoryOverride in GetOverrides(eventData.RootCategory))
            {
                foreach (var additionalResult in categoryOverride.GetAdditionalComponents(eventData))
                    eventData.AddItem(additionalResult);
            }
        }

        /// <inheritdoc/>
        protected override bool OnEngineReady()
        {
            var transformDriversCategory = WorkerInitializer.ComponentLibrary.GetSubcategory("Transform/Drivers");

            var convertibleValueDrivers = WorkerInitializer.ComponentLibrary.GetSubcategory("Uncategorized").Elements
                .Where(type => type.Name.StartsWith("Convertible") && type.Name.Contains("Driver"))
                .Select(type => new ComponentResult(transformDriversCategory, type, "Convertible Value Driver"))
                .ToImmutableArray();

            AddOverride(new(transformDriversCategory, _ => convertibleValueDrivers));

            return base.OnEngineReady();
        }
    }
}