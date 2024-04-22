using FrooxEngine;
using FrooxEngine.UIX;
using MonkeyLoader.Resonite.Events;

namespace ComponentSelectorAdditions.Events
{
    public sealed class BuildSelectorHeaderEvent : BuildSelectorEvent
    {
        /// <inheritdoc/>
        public BuildSelectorHeaderEvent(ComponentSelector selector, UIBuilder ui) : base(selector, ui)
        { }
    }
}