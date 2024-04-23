using FrooxEngine;
using FrooxEngine.UIX;
using MonkeyLoader.Events;
using MonkeyLoader.Resonite.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions.Events
{
    public sealed class BuildComponentButtonEvent : BuildButtonEvent
    {
        public ComponentResult Component { get; }

        public SelectorPath Path { get; }

        /// <inheritdoc/>
        internal BuildComponentButtonEvent(ComponentSelector selector, UIBuilder ui, SelectorPath path, CategoryNode<Type> rootCategory, ComponentResult component)
            : base(selector, ui, rootCategory, component.Category)
        {
            Path = path;
            Component = component;
            // need to set is direct category with element check
        }
    }
}