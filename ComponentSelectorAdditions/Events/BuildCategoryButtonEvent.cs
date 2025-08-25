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
    /// Represents the event data for the Build Category Button Event.
    /// </summary>
    /// <remarks>
    /// This is used to generate the button for opening a nested
    /// <see cref="CategoryNode{T}">category</see> in a <see cref="ComponentSelector">selector</see>.
    /// </remarks>
    public sealed class BuildCategoryButtonEvent : BuildButtonEvent
    {
        /// <inheritdoc/>
        internal BuildCategoryButtonEvent(ComponentSelector selector, UIBuilder ui, CategoryNode<Type> rootCategory, CategoryNode<Type> itemCategory)
            : base(selector, ui, rootCategory, itemCategory, itemCategory.Parent == rootCategory)
        { }
    }
}