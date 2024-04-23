using FrooxEngine;
using FrooxEngine.UIX;

namespace ComponentSelectorAdditions.Events
{
    /// <summary>
    /// Represents the event fired when a <see cref="ComponentSelector"/>'s footer is constructed.
    /// </summary>
    public sealed class BuildSelectorFooterEvent : BuildSelectorEvent
    {
        public bool HasBackButton { get; }

        public bool HasCancelButton { get; }

        public BuildSelectorFooterEvent(ComponentSelector selector, UIBuilder ui, SelectorSearchBar? searchBar, bool hasBackButton, bool hasCancelButton)
            : base(selector, ui)
        {
            HasBackButton = hasBackButton;
            HasCancelButton = hasCancelButton;
            SearchBar = searchBar;
        }
    }
}