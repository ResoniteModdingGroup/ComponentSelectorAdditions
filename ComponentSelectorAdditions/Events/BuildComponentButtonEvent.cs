using FrooxEngine;
using FrooxEngine.UIX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions.Events
{
    /// <summary>
    /// Represents the event data for the Build Component Button Event.
    /// </summary>
    /// <remarks>
    /// This is used to generate the button to attach a concrete component,
    /// or to open the custom generic selection page for open generics.
    /// </remarks>
    public sealed class BuildComponentButtonEvent : BuildButtonEvent
    {
        /// <summary>
        /// Gets the component result that the button targets.
        /// </summary>
        public ComponentResult Component { get; }

        /// <summary>
        /// Gets the current selector path.
        /// </summary>
        public SelectorPath Path { get; }

        /// <inheritdoc/>
        internal BuildComponentButtonEvent(ComponentSelector selector, UIBuilder ui, SelectorPath path, CategoryNode<Type>? rootCategory, ComponentResult component)
            : base(selector, ui, rootCategory, component.Category, component.Category == rootCategory)
        {
            Path = path;
            Component = component;
        }
    }
}