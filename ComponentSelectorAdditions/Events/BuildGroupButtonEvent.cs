using FrooxEngine;
using FrooxEngine.UIX;

namespace ComponentSelectorAdditions.Events
{
    /// <summary>
    /// Represents the event data for the Build Group Button Event.
    /// </summary>
    /// <remarks>
    /// This is used to generate the button to open a group of components.
    /// </remarks>
    public sealed class BuildGroupButtonEvent : BuildButtonEvent
    {
        /// <summary>
        /// Gets the group identifier that the button targets.
        /// </summary>
        public string Group { get; }

        /// <summary>
        /// Gets the group name that the button should show.
        /// </summary>
        public string GroupName { get; }

        /// <inheritdoc/>
        internal BuildGroupButtonEvent(ComponentSelector selector, UIBuilder ui, CategoryNode<Type> rootCategory, ComponentResult groupComponent)
            : base(selector, ui, rootCategory, groupComponent.Category, groupComponent.Category == rootCategory)
        {
            Group = groupComponent.Group!;
            GroupName = groupComponent.GroupName!;
        }
    }
}