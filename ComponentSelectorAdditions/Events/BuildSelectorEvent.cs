using FrooxEngine;
using FrooxEngine.UIX;
using MonkeyLoader.Resonite.Events;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ComponentSelectorAdditions.Events
{
    /// <summary>
    /// Abstract base class for Build Selector Events.
    /// </summary>
    public abstract class BuildSelectorEvent : BuildUIEvent
    {
        /// <summary>
        /// Gets or sets whether a back button has been added already during this event.
        /// </summary>
        /// <value><see langword="true"/> if a back button has been added; otherwise, <see langword="false"/>.</value>
        public bool AddsBackButton { get; set; }

        /// <summary>
        /// Gets or sets whether a cancel button has been added already during this event.
        /// </summary>
        /// <value><see langword="true"/> if a cancel button has been added; otherwise, <see langword="false"/>.</value>
        public bool AddsCancelButton { get; set; }

        /// <summary>
        /// Gets or sets whether a <see cref="SearchBar">search bar</see> has been added already during this event.
        /// </summary>
        /// <value><see langword="true"/> if a search bar has been added; otherwise, <see langword="false"/>.</value>
        [MemberNotNullWhen(true, nameof(SearchBar))]
        public bool AddsSearchBar => SearchBar is not null;

        /// <summary>
        /// Gets or sets the search bar that has been added.
        /// </summary>
        public SelectorSearchBar? SearchBar { get; set; }

        /// <summary>
        /// Gets the selector that is being build.
        /// </summary>
        public ComponentSelector Selector { get; }

        internal Action<SelectorPath, bool>? SelectorUIChangedHandlers => SelectorUIChanged;

        internal BuildSelectorEvent(ComponentSelector selector, UIBuilder ui) : base(ui)
        {
            Selector = selector;
        }

        /// <summary>
        /// Fired when the selector UI is changed.<br/>
        /// The <see langword="bool"/> determines, whether the back button should be shown.
        /// </summary>
        /// <remarks>
        /// This allows UI generated from this event to respond to changes in the path or back button state.
        /// </remarks>
        public event Action<SelectorPath, bool>? SelectorUIChanged;
    }
}