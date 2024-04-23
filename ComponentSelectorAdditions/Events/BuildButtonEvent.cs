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
    public abstract class BuildButtonEvent : BuildUIEvent, ICancelableEvent
    {
        /// <inheritdoc/>
        public bool Canceled { get; set; }

        public bool IsDirectItem { get; }
        public CategoryNode<Type> RootCategory { get; }

        public ComponentSelector Selector { get; }
        public CategoryNode<Type> SubCategory { get; }

        protected BuildButtonEvent(ComponentSelector selector, UIBuilder ui, CategoryNode<Type> rootCategory, CategoryNode<Type> subCategory) : base(ui)
        {
            Selector = selector;
            RootCategory = rootCategory;
            SubCategory = subCategory;
            IsDirectItem = subCategory == rootCategory || subCategory.Parent == rootCategory;
        }
    }
}