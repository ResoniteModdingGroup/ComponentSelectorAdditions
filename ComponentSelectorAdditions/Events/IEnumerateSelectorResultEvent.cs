using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Text;

namespace ComponentSelectorAdditions.Events
{
    public interface IEnumerateSelectorResultEvent
    {
        public SelectorPath Path { get; }
        public CategoryNode<Type> RootCategory { get; }
        public ComponentSelector Selector { get; }
    }
}