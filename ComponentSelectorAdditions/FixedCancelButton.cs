using ComponentSelectorAdditions.Events;
using Elements.Core;
using FrooxEngine;
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
    internal sealed class FixedCancelButton : ResoniteEventHandlerMonkey<FixedCancelButton, BuildSelectorFooterEvent>
    {
        public override bool CanBeDisabled => true;
        public override int Priority => HarmonyLib.Priority.Normal;

        protected override bool AppliesTo(BuildSelectorFooterEvent eventData) => Enabled && !eventData.AddsCancelButton;

        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        protected override void Handle(BuildSelectorFooterEvent eventData)
        {
            if (!Enabled)
                return;

            eventData.UI.Button("General.Cancel".AsLocaleKey(), RadiantUI_Constants.Sub.RED, eventData.Selector.OnCancelPressed, 0.35f).Slot.OrderOffset = 1000000;

            eventData.AddsCancelButton = true;
        }
    }
}