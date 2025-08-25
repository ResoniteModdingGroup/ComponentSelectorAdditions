using FrooxEngine;
using FrooxEngine.UIX;

namespace ComponentSelectorAdditions.Events
{
    /// <summary>
    /// Represents the event data for the Build Selector Footer Event.
    /// </summary>
    /// <remarks>
    /// Fired when a <see cref="ComponentSelector"/>'s footer is constructed.
    /// </remarks>
    public sealed class BuildSelectorFooterEvent : BuildSelectorEvent
    {
        /// <summary>
        /// Gets whether a back button was already added in
        /// the <see cref="BuildSelectorHeaderEvent">header</see>.
        /// </summary>
        public bool HasBackButton { get; }

        /// <summary>
        /// Gets whether a cancel button was already added in
        /// the <see cref="BuildSelectorHeaderEvent">header</see>.
        /// </summary>
        public bool HasCancelButton { get; }

        internal BuildSelectorFooterEvent(ComponentSelector selector, UIBuilder ui, SelectorSearchBar? searchBar, bool hasBackButton, bool hasCancelButton)
            : base(selector, ui)
        {
            HasBackButton = hasBackButton;
            HasCancelButton = hasCancelButton;
            SearchBar = searchBar;
        }
    }
}