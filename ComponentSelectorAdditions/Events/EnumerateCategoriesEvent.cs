using FrooxEngine;
using MonkeyLoader.Resonite.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ComponentSelectorAdditions.Events
{
    /// <summary>
    /// Represents the event data for the Enumerate Categories Event.
    /// </summary>
    /// <remarks>
    /// Fired to enumerate the categories to show in the <see cref="ComponentSelector">selector</see>.
    /// </remarks>
    public sealed class EnumerateCategoriesEvent : CancelableSortedItemsEvent<CategoryNode<Type>>, IEnumerateSelectorResultEvent
    {
        /// <summary>
        /// Gets the sorted categories that should be shown in the <see cref="ComponentSelector">selector</see>.
        /// </summary>
        public override IEnumerable<CategoryNode<Type>> Items
            => sortableItems.OrderBy(entry => entry.Value)
                .ThenBy(entry => entry.Key.Name)
                .Select(entry => entry.Key);

        /// <inheritdoc/>
        public SelectorPath Path { get; }

        /// <inheritdoc/>
        public CategoryNode<Type> RootCategory { get; }

        /// <inheritdoc/>
        public ComponentSelector Selector { get; }

        internal EnumerateCategoriesEvent(ComponentSelector selector, SelectorPath path, CategoryNode<Type> rootCategory)
        {
            Selector = selector;
            Path = path;
            RootCategory = rootCategory;
        }
    }
}