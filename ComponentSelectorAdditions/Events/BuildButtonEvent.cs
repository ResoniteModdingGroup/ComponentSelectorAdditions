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
    public abstract class BuildButtonEvent : CancelableBuildUIEvent
    {
        public bool IsDirectItem { get; }
        public CategoryNode<Type> ItemCategory { get; }
        public CategoryNode<Type>? RootCategory { get; }
        public ComponentSelector Selector { get; }

        protected BuildButtonEvent(ComponentSelector selector, UIBuilder ui, CategoryNode<Type>? rootCategory, CategoryNode<Type> itemCategory, bool isDirectItem) : base(ui)
        {
            Selector = selector;
            RootCategory = rootCategory;
            ItemCategory = itemCategory;
            IsDirectItem = isDirectItem;
        }
    }
}