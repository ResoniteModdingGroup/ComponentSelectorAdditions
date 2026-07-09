using FrooxEngine;
using MonkeyLoader.Configuration;

namespace ComponentSelectorAdditions
{
    internal sealed class RecentsConfig : ConfigSection
    {
        private readonly DefiningConfigKey<bool> _addRecentConcreteComponentsToSelection = new("AddRecentConcreteComponentsToSelection", "Ensure that recent concrete versions of generic Components / Nodes appear in the selection.", () => true);
        private readonly DefiningConfigKey<List<Type>> _components = new("RecentComponents", "Recent Components", () => [typeof(ValueMultiDriver<>), typeof(ReferenceMultiDriver<>)], true, value => value is not null);
        private readonly DefiningConfigKey<List<Type>> _protoFluxNodes = new("RecentProtoFluxNodes", "Recent ProtoFlux Nodes", () => [], true, value => value is not null);

        private readonly DefiningConfigKey<int> _recentCap = new("TrackedCapacity", "How many recent components / nodes to save and show.", () => 32)
        {
            new ConfigKeyRange<int>(1, 128)
        };

        private readonly DefiningConfigKey<bool> _trackConcreteComponents = new("TrackConcreteComponents", "Whether the concrete version of a recent generic Component / Node gets saved.", () => true);
        private readonly DefiningConfigKey<bool> _trackGenericComponents = new("TrackGenericComponents", "Whether the generic version of a recent Component / Node gets saved.", () => true);

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