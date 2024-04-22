using MonkeyLoader.Events;
using MonkeyLoader.Resonite.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ComponentSelectorAdditions.Events
{
    public sealed class EnumerateComponentsEvent : SortedItemsEvent<Type>, ICancelableEvent
    {
        /// <inheritdoc/>
        public bool Canceled { get; set; }

        public override IEnumerable<Type> Items
            => sortableItems.OrderBy(entry => entry.Key.Name)
                .OrderBy(entry => entry.Value)
                .Select(entry => entry.Key);

        public SelectorPath Path { get; }

        /// <inheritdoc/>
        public EnumerateComponentsEvent(SelectorPath path)
        {
            Path = path;
        }
    }
}