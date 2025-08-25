using FrooxEngine;
using FrooxEngine.UIX;

namespace ComponentSelectorAdditions.Events
{
    /// <summary>
    /// Represents the event data for the Build Selector Footer Event.
    /// </summary>
    /// <remarks>
    /// Fired when a <see cref="ComponentSelector"/>'s header is constructed.
    /// </remarks>
    public sealed class BuildSelectorHeaderEvent : BuildSelectorEvent
    {
        /// <inheritdoc/>
        internal BuildSelectorHeaderEvent(ComponentSelector selector, UIBuilder ui) : base(selector, ui)
        { }
    }
}