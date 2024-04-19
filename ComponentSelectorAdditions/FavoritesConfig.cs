using MonkeyLoader.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions
{
    internal sealed class FavoritesConfig : ConfigSection
    {
        private static readonly DefiningConfigKey<HashSet<string>> _categoriesKey = new("Categories", "Favorited Categories", () => new HashSet<string>() { "Data > Dynamic" }, true);
        private static readonly DefiningConfigKey<HashSet<string>> _componentsKey = new("Components", "Favorited Components", () => new HashSet<string>() { "FrooxEngine.ValueMultiDriver`1", "FrooxEngine.ReferenceMultiDriver`1" }, true);
        private static readonly DefiningConfigKey<HashSet<string>> _protoFluxCategoriesKey = new("ProtoFluxCategories", "Favorited ProtoFlux Categories", () => new HashSet<string>() { }, true);
        private static readonly DefiningConfigKey<HashSet<string>> _protoFluxNodesKey = new("ProtoFluxNodes", "Favorited ProtoFlux Nodes", () => new HashSet<string>() { }, true);
        public HashSet<string> Categories => _categoriesKey.GetValue()!;

        public HashSet<string> Components => _componentsKey.GetValue()!;

        public override string Description => "Contains the favorited categories and components.";
        public override string Id => "Favorites";

        public HashSet<string> ProtoFluxCategories => _protoFluxCategoriesKey.GetValue()!;

        public HashSet<string> ProtoFluxNodes => _protoFluxNodesKey.GetValue()!;

        public override Version Version { get; } = new(1, 0, 0);
    }
}