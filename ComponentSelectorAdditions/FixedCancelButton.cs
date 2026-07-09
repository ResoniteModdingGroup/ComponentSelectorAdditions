using ComponentSelectorAdditions.Events;
using Elements.Core;
using FrooxEngine;
using MonkeyLoader.Resonite;

namespace ComponentSelectorAdditions
{
    internal sealed class FixedCancelButton : ResoniteEventHandlerMonkey<FixedCancelButton, BuildSelectorFooterEvent>
    {
        public override bool CanBeDisabled => true;

        public override int Priority => HarmonyLib.Priority.Normal;

        protected override bool AppliesTo(BuildSelectorFooterEvent eventData)
            => base.AppliesTo(eventData) && !eventData.AddsCancelButton;

        protected override void Handle(BuildSelectorFooterEvent eventData)
        {
            eventData.UI.Button("General.Cancel".AsLocaleKey(), RadiantUI_Constants.Sub.RED, eventData.Selector.OnCancelPressed, 0.35f).Slot.OrderOffset = 1000000;

            eventData.AddsCancelButton = true;
        }
    }
}