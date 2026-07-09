using ComponentSelectorAdditions.Events;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using MonkeyLoader.Resonite;

namespace ComponentSelectorAdditions
{
    internal sealed class FixedBackButton : ResoniteEventHandlerMonkey<FixedBackButton, BuildSelectorHeaderEvent>
    {
        public override bool CanBeDisabled => true;

        public override int Priority => HarmonyLib.Priority.Normal;

        protected override bool AppliesTo(BuildSelectorHeaderEvent eventData)
            => base.AppliesTo(eventData) && !eventData.AddsBackButton;

        protected override void Handle(BuildSelectorHeaderEvent eventData)
        {
            var button = eventData.UI.Button("ComponentSelector.Back".AsLocaleKey(), RadiantUI_Constants.BUTTON_COLOR, eventData.Selector.OnOpenCategoryPressed, "/", 0.35f);
            var relay = button.Slot.GetComponent<ButtonRelay<string>>();

            eventData.AddsBackButton = true;
            eventData.SelectorUIChanged += (path, showBackButton) =>
            {
                button.Slot.ActiveSelf = showBackButton;
                relay.Argument.Value = path.OpenParentCategoryPath;
            };
        }
    }
}