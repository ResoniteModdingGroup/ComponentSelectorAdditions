using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Text;

namespace ComponentSelectorAdditions.Events
{
    /// <summary>
    /// Defines the interface for the events that enumerate selector categories and components.
    /// </summary>
    public interface IEnumerateSelectorResultEvent
    {
        /// <summary>
        /// Gets the current path of the <see cref="ComponentSelector">selector</see>.
        /// </summary>
        public SelectorPath Path { get; }

        /// <summary>
        /// Gets the current root category of the <see cref="ComponentSelector">selector</see>,
        /// based on which categories and components should be enumerated.
        /// </summary>
        public CategoryNode<Type> RootCategory { get; }

        /// <summary>
        /// Gets the selector that results should be enumerated for.
        /// </summary>
        public ComponentSelector Selector { get; }
    }
}