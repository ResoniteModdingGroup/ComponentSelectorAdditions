using FrooxEngine;
using MonkeyLoader.Resonite.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ComponentSelectorAdditions.Events
{
    public class EnumerateConcreteGenericsEvent : SortedItemsEvent<Type>
    {
        /// <summary>
        /// Gets the Type of the <see cref="FrooxEngine.Component"/>
        /// that the generic parameters are meant to be listed for.
        /// </summary>
        public Type Component { get; }

        /// <inheritdoc/>
        public override IEnumerable<Type> Items
            => sortableItems
                .Where(entry => Selector.World.Types.IsSupported(entry.Key))
                .OrderBy(entry => entry.Value)
                .Select(entry => entry.Key);

        public ComponentSelector Selector { get; }

        internal EnumerateConcreteGenericsEvent(ComponentSelector selector, Type component)
        {
            Selector = selector;
            Component = component;
        }
    }
}