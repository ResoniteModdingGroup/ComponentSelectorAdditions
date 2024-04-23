using FrooxEngine;
using MonkeyLoader.Resonite.Events;
using System;

namespace ComponentSelectorAdditions.Events
{
    public class EnumerateConcreteGenericsEvent : SortedItemsEvent<Type>
    {
        /// <summary>
        /// Gets the Type of the <see cref="FrooxEngine.Component"/>
        /// that the generic parameters are meant to be listed for.
        /// </summary>
        public Type Component { get; }

        public ComponentSelector Selector { get; }

        public EnumerateConcreteGenericsEvent(ComponentSelector selector, Type component)
        {
            Selector = selector;
            Component = component;
        }
    }
}