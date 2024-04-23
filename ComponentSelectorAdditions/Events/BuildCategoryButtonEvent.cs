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
    public sealed class BuildCategoryButtonEvent : BuildButtonEvent
    {
        /// <inheritdoc/>
        internal BuildCategoryButtonEvent(ComponentSelector selector, UIBuilder ui, CategoryNode<Type> rootCategory, CategoryNode<Type> itemCategory)
            : base(selector, ui, rootCategory, itemCategory, itemCategory.Parent == rootCategory)
        { }
    }
}