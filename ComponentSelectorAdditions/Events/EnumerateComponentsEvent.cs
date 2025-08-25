using FrooxEngine;
using MonkeyLoader.Resonite.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ComponentSelectorAdditions.Events
{
    /// <summary>
    /// Represents the event data for the Enumerate Components Event.
    /// </summary>
    /// <remarks>
    /// Fired to enumerate the components to show in the <see cref="ComponentSelector">selector</see>.
    /// </remarks>
    public sealed class EnumerateComponentsEvent : CancelableSortedItemsEvent<ComponentResult>, IEnumerateSelectorResultEvent
    {
        /// <summary>
        /// Gets the predicate that determines whether a given type should be included.
        /// </summary>
        public Predicate<Type> ComponentFilter { get; }

        /// <summary>
        /// Gets the sorted components that should be shown in the <see cref="ComponentSelector">selector</see>.
        /// </summary>
        public override IEnumerable<ComponentResult> Items
        {
            get
            {
                var items = sortableItems
                    .Where(entry => ComponentFilter(entry.Key.Type))
                    .Where(entry => Selector.World.Types.IsSupported(entry.Key.Type))
                    .OrderBy(Value)
                    .ThenBy(Name);

                // Sort for (concrete) genericness when search has a generic argument
                if (Path.HasSearchGeneric)
                    items = items.ThenBy(GenericnessRating);

                return items.Select(entry => entry.Key);
            }
        }

        /// <inheritdoc/>
        public SelectorPath Path { get; }

        /// <inheritdoc/>
        public CategoryNode<Type> RootCategory { get; }

        /// <inheritdoc/>
        public ComponentSelector Selector { get; }

        internal EnumerateComponentsEvent(ComponentSelector selector, SelectorPath path, CategoryNode<Type> rootCategory, Predicate<Type> componentFilter)
        {
            Selector = selector;
            Path = path;
            RootCategory = rootCategory;
            ComponentFilter = componentFilter;
        }

        private static int GenericnessRating(KeyValuePair<ComponentResult, int> entry)
            => entry.Key.Type.IsGenericType ? (entry.Key.Type.IsGenericTypeDefinition ? 1 : 0) : 2;

        private static string Name(KeyValuePair<ComponentResult, int> entry)
            => entry.Key.GroupName ?? entry.Key.Type.Name;

        private static int Value(KeyValuePair<ComponentResult, int> entry)
            => entry.Value;
    }
}