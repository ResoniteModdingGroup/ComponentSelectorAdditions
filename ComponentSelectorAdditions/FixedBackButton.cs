using ComponentSelectorAdditions.Events;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions
{
    internal sealed class FixedBackButton : ResoniteEventHandlerMonkey<FixedBackButton, BuildSelectorHeaderEvent>
    {
        public override bool CanBeDisabled => true;
        public override int Priority => HarmonyLib.Priority.Normal;

        protected override bool AppliesTo(BuildSelectorHeaderEvent eventData) => Enabled && !eventData.AddsBackButton;

        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

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