using ComponentSelectorAdditions.Events;
using FrooxEngine;
using FrooxEngine.UIX;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions
{
    internal sealed class CurrentPathIndicator : ResoniteEventHandlerMonkey<CurrentPathIndicator, BuildSelectorFooterEvent>
    {
        public override bool CanBeDisabled => true;
        public override int Priority => HarmonyLib.Priority.Normal;

        protected override bool AppliesTo(BuildSelectorFooterEvent eventData) => Enabled;

        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        protected override void Handle(BuildSelectorFooterEvent eventData)
        {
            var text = eventData.UI.Text("Path: /", 16);
            text.Slot.AttachComponent<Button>();
            var proxy = text.Slot.AttachComponent<ValueProxySource<string>>();

            eventData.SelectorUIChanged += (path, _) =>
            {
                var formatted = Format(path);
                proxy.Value.Value = formatted;
                text.Content.Value = $"Path: {Break(formatted)}";
            };
        }

        /// <summary>
        /// Inserts zero-width breaking spaces after / and . symbols.
        /// </summary>
        private static string Break(string path)
            => path.Replace("/", "/\u200b").Replace(".", ".\u200b");

        private static string Format(SelectorPath path)
            => $"{path.Path}{(path.HasGroup ? $"{(path.GenericType ? ":" : "?")}{path.Group}" : "")}";
    }
}