using FrooxEngine;
using MonkeyLoader;
using MonkeyLoader.Configuration;
using System;
using System.Collections.Generic;

namespace ComponentSelectorAdditions
{
    internal sealed class GenericPresetsConfig : ConfigSection
    {
        private static readonly DefiningConfigKey<HashSet<Sequence<Type>>> _presetsKey = new("Presets", "Generic argument presets to attempt to add as concrete types to the generic Component / Node selection.", () => new HashSet<Sequence<Type>> { new[] { typeof(User) }, new[] { typeof(Slot) } }, true);
        public override string Description => "Contains options for adding more concrete versions of generic Components / Nodes to the selection.";
        public HashSet<Sequence<Type>> GenericArgumentPresets => _presetsKey.GetValue()!;
        public override string Id => "GenericPresets";
        public override bool InternalAccessOnly => true;
        public override Version Version { get; } = new Version(1, 0, 0);
    }
}