using FrooxEngine;
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
        private static readonly DefiningConfigKey<List<Type>> _components = new("RecentComponents", "Recent Components", () => new List<Type> { typeof(ValueMultiDriver<>), typeof(ReferenceMultiDriver<>) }, true, value => value is not null);
        private static readonly DefiningConfigKey<List<Type>> _protoFluxNodes = new("RecentProtoFluxNodes", "Recent ProtoFlux Nodes", () => new List<Type> { }, true, value => value is not null);

        private static readonly DefiningConfigKey<int> _recentCap = new("TrackedCapacity", "How many recent components / nodes to save and show.", () => 32)
        {
            new ConfigKeyRange<int>(1, 128)
        };

        private static readonly DefiningConfigKey<bool> _trackConcreteComponents = new("TrackConcreteComponents", "Whether the concrete version of a recent generic Component / Node gets saved.", () => true);
        private static readonly DefiningConfigKey<bool> _trackGenericComponents = new("TrackGenericComponents", "Whether the generic version of a recent Component / Node gets saved.", () => true);

        public bool AddRecentConcreteComponentsToSelection => _addRecentConcreteComponentsToSelection;
        public List<Type> Components => _components!;
        public override string Description => "Contains the recent components.";
        public override string Id => "Recents";

        public List<Type> ProtoFluxNodes => _protoFluxNodes!;

        public int RecentCap => _recentCap;
        public bool TrackConcreteComponents => _trackConcreteComponents;
        public bool TrackGenericComponents => _trackGenericComponents;
        public override Version Version { get; } = new(1, 1, 0);
    }
}