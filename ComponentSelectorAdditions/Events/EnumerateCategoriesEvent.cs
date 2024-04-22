using FrooxEngine;
using MonkeyLoader.Events;
using MonkeyLoader.Resonite.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ComponentSelectorAdditions.Events
{
    public sealed class EnumerateCategoriesEvent : SortedItemsEvent<CategoryNode<Type>>, ICancelableEvent
    {
        /// <inheritdoc/>
        public bool Canceled { get; set; }

        public override IEnumerable<CategoryNode<Type>> Items
            => sortableItems.OrderBy(entry => entry.Key.Name)
                .OrderBy(entry => entry.Value)
                .Select(entry => entry.Key);

        public SelectorPath Path { get; }

        /// <inheritdoc/>
        public EnumerateCategoriesEvent(SelectorPath path)
        {
            Path = path;
        }
    }
}