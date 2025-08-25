using FrooxEngine;
using MonkeyLoader.Resonite.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ComponentSelectorAdditions.Events
{
    /// <summary>
    /// Represents the event data for the Enumerate Concrete Generics Event.
    /// </summary>
    /// <remarks>
    /// Fired to enumerate the components to show in the <see cref="ComponentSelector">selector</see>.
    /// </remarks>
    public sealed class EnumerateConcreteGenericsEvent : SortedItemsEvent<Type>
    {
        /// <summary>
        /// Gets the Type of the <see cref="FrooxEngine.Component"/>
        /// that the generic parameters are meant to be listed for.
        /// </summary>
        public Type Component { get; }

        /// <summary>
        /// Gets the sorted concrete generic components that should be shown in
        /// the <see cref="ComponentSelector">selector</see>'s custom generic builder selection.
        /// </summary>
        public override IEnumerable<Type> Items
            => sortableItems
                .Where(entry => Selector.World.Types.IsSupported(entry.Key))
                .OrderBy(entry => entry.Value)
                .Select(entry => entry.Key);

        /// <summary>
        /// Gets the selector that results should be enumerated for.
        /// </summary>
        public ComponentSelector Selector { get; }

        internal EnumerateConcreteGenericsEvent(ComponentSelector selector, Type component)
        {
            Selector = selector;
            Component = component;
        }
    }
}