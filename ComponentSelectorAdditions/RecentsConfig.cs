using MonkeyLoader.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions
{
    internal sealed class RecentsConfig : ConfigSection
    {
        private static readonly DefiningConfigKey<bool> _addRecentConcreteComponentsToSelection = new("AddRecentConcreteComponentsToSelection", "Ensure that recent concrete versions of generic Components / Nodes appear in the selection.", () => true);
        private static readonly DefiningConfigKey<List<string>> _componentsKey = new("RecentComponents", "Recent Components", () => new List<string>() { "FrooxEngine.ValueMultiDriver`1", "FrooxEngine.ReferenceMultiDriver`1" }, true);
        private static readonly DefiningConfigKey<List<string>> _protoFluxNodesKey = new("RecentProtoFluxNodes", "Recent ProtoFlux Nodes", () => new List<string>() { }, true);

        private static readonly DefiningConfigKey<int> _recentCapKey = new("TrackedCapacity", "How many recent components / nodes to save and show.", () => 32)
        {
            new ConfigKeyRange<int>(1, 128)
        };

        private static readonly DefiningConfigKey<bool> _trackConcreteComponentsKey = new("TrackConcreteComponents", "Whether the concrete version of a recent generic Component / Node gets saved.", () => true);
        private static readonly DefiningConfigKey<bool> _trackGenericComponentsKey = new("TrackGenericComponents", "Whether the generic version of a recent Component / Node gets saved.", () => true);

        public bool AddRecentConcreteComponentsToSelection => _addRecentConcreteComponentsToSelection.GetValue();
        public List<string> Components => _componentsKey.GetValue()!;
        public override string Description => "Contains the recent components.";
        public override string Id => "Recents";

        public List<string> ProtoFluxNodes => _protoFluxNodesKey.GetValue()!;

        public int RecentCap => _recentCapKey.GetValue();
        public bool TrackConcreteComponents => _trackConcreteComponentsKey.GetValue();
        public bool TrackGenericComponents => _trackGenericComponentsKey.GetValue();
        public override Version Version { get; } = new(1, 0, 0);
    }
}