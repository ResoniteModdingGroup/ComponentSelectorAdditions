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

        public CategoryNode<Type> RootCategory { get; }
        public ComponentSelector Selector { get; }

        /// <inheritdoc/>
        public EnumerateCategoriesEvent(ComponentSelector selector, SelectorPath path, CategoryNode<Type> rootCategory)
        {
            Selector = selector;
            Path = path;
            RootCategory = rootCategory;
        }
    }
}