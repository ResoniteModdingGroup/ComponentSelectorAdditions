using FrooxEngine;
using MonkeyLoader.Resonite.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ComponentSelectorAdditions.Events
{
    public sealed class EnumerateComponentsEvent : CancelableSortedItemsEvent<ComponentResult>, IEnumerateSelectorResultEvent
    {
        public Predicate<Type> ComponentFilter { get; }

        public override IEnumerable<ComponentResult> Items
            => sortableItems
                .Where(entry => ComponentFilter(entry.Key.Type))
                .Where(entry => Selector.World.Types.IsSupported(entry.Key.Type))
                .OrderBy(entry => entry.Value)
                .ThenBy(entry => entry.Key.GroupName ?? entry.Key.Type.Name)
                .Select(entry => entry.Key);

        public SelectorPath Path { get; }

        public CategoryNode<Type> RootCategory { get; }
        public ComponentSelector Selector { get; }

        /// <inheritdoc/>
        internal EnumerateComponentsEvent(ComponentSelector selector, SelectorPath path, CategoryNode<Type> rootCategory, Predicate<Type> componentFilter)
        {
            Selector = selector;
            Path = path;
            RootCategory = rootCategory;
            ComponentFilter = componentFilter;
        }
    }
}