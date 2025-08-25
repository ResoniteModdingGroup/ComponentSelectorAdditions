using FrooxEngine;
using FrooxEngine.UIX;
using MonkeyLoader.Resonite.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions.Events
{
    /// <summary>
    /// Abstract base class for button generation events.
    /// </summary>
    public abstract class BuildButtonEvent : CancelableBuildUIEvent
    {
        /// <summary>
        /// Gets whether the target of the button being generated is
        /// a direct <see cref="CategoryNode{T}.Elements">element</see>
        /// of the current <see cref="RootCategory">root category</see>.
        /// </summary>
        public bool IsDirectItem { get; }

        /// <summary>
        /// Gets the category that the target of the button belongs to, if available.
        /// </summary>
        public CategoryNode<Type>? ItemCategory { get; }

        /// <summary>
        /// Gets the current root category of the <see cref="ComponentSelector">selector</see>
        /// that the button is being generated for, if available.
        /// </summary>
        public CategoryNode<Type>? RootCategory { get; }

        /// <summary>
        /// Gets the selector that the button is being generated for.
        /// </summary>
        public ComponentSelector Selector { get; }

        /// <summary>
        /// Creates a new button generation event with the given data.
        /// </summary>
        /// <param name="selector">The selector that the button is being generated for.</param>
        /// <param name="ui">The <see cref="UIBuilder"/> to use while generating extra UI elements.</param>
        /// <param name="rootCategory">
        /// The current root category of the <see cref="ComponentSelector">selector</see>
        /// that the button is being generated for, if available.
        /// </param>
        /// <param name="itemCategory">The category that the target of the button belongs to, if available.</param>
        /// <param name="isDirectItem">
        /// Whether the target of the button being generated is
        /// a direct <see cref="CategoryNode{T}.Elements">element</see>
        /// of the current <see cref="RootCategory">root category</see>.
        /// </param>
        protected BuildButtonEvent(ComponentSelector selector, UIBuilder ui, CategoryNode<Type>? rootCategory, CategoryNode<Type>? itemCategory, bool isDirectItem) : base(ui)
        {
            Selector = selector;
            RootCategory = rootCategory;
            ItemCategory = itemCategory;
            IsDirectItem = isDirectItem;
        }
    }
}