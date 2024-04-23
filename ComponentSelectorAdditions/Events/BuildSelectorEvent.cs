using FrooxEngine;
using FrooxEngine.UIX;
using MonkeyLoader.Resonite.Events;
using System;

namespace ComponentSelectorAdditions.Events
{
    public abstract class BuildSelectorEvent : BuildUIEvent
    {
        public bool AddsBackButton { get; set; }
        public bool AddsCancelButton { get; set; }
        public ComponentSelector Selector { get; }
        internal Action<SelectorPath, bool>? BackButtonChangedHandlers => BackButtonChanged;

        internal BuildSelectorEvent(ComponentSelector selector, UIBuilder ui) : base(ui)
        {
            Selector = selector;
        }

        public event Action<SelectorPath, bool>? BackButtonChanged;
    }
}