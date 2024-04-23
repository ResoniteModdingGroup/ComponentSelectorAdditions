using FrooxEngine;
using MonkeyLoader.Events;
using MonkeyLoader.Resonite.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ComponentSelectorAdditions.Events
{
    public sealed class EnumerateComponentsEvent : SortedItemsEvent<ComponentResult>, ICancelableEvent
    {
        /// <inheritdoc/>
        public bool Canceled { get; set; }

        public Predicate<Type> ComponentFilter { get; }

        public override IEnumerable<ComponentResult> Items
            => sortableItems
                .Where(entry => ComponentFilter(entry.Key.Type))
                .OrderBy(entry => entry.Key.GroupName ?? entry.Key.Type.Name)
                .OrderBy(entry => entry.Value)
                .Select(entry => entry.Key);

        public SelectorPath Path { get; }

        public CategoryNode<Type> RootCategory { get; }
        public ComponentSelector Selector { get; }

        /// <inheritdoc/>
        public EnumerateComponentsEvent(ComponentSelector selector, SelectorPath path, CategoryNode<Type> rootCategory, Predicate<Type> componentFilter)
        {
            Selector = selector;
            Path = path;
            RootCategory = rootCategory;
            ComponentFilter = componentFilter;
        }
    }
}