using FrooxEngine;
using FrooxEngine.UIX;

namespace ComponentSelectorAdditions.Events
{
    public sealed class BuildSelectorHeaderEvent : BuildSelectorEvent
    {
        /// <inheritdoc/>
        internal BuildSelectorHeaderEvent(ComponentSelector selector, UIBuilder ui) : base(selector, ui)
        { }
    }
}