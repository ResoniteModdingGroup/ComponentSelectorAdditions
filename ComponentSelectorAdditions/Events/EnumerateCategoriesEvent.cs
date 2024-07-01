using FrooxEngine;
using MonkeyLoader.Events;
using MonkeyLoader.Resonite.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ComponentSelectorAdditions.Events
{
    public sealed class EnumerateCategoriesEvent : CancelableSortedItemsEvent<CategoryNode<Type>>
    {
        public override IEnumerable<CategoryNode<Type>> Items
            => sortableItems.OrderBy(entry => entry.Value)
                .ThenBy(entry => entry.Key.Name)
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