using FrooxEngine;
using FrooxEngine.UIX;
using MonkeyLoader.Resonite.Events;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ComponentSelectorAdditions.Events
{
    public abstract class BuildSelectorEvent : BuildUIEvent
    {
        public bool AddsBackButton { get; set; }
        public bool AddsCancelButton { get; set; }

        [MemberNotNullWhen(true, nameof(SearchBar))]
        public bool AddsSearchBar => SearchBar is not null;

        public SelectorSearchBar? SearchBar { get; set; }
        public ComponentSelector Selector { get; }
        internal Action<SelectorPath, bool>? SelectorUIChangedHandlers => SelectorUIChanged;

        internal BuildSelectorEvent(ComponentSelector selector, UIBuilder ui) : base(ui)
        {
            Selector = selector;
        }

        public event Action<SelectorPath, bool>? SelectorUIChanged;
    }
}