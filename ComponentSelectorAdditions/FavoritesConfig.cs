using FrooxEngine;
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
        private static readonly DefiningConfigKey<HashSet<string>> _categoriesKey = new("FavoriteCategories", "Favorited Categories", () => new HashSet<string> { "/Data/Dynamic" }, true);
        private static readonly DefiningConfigKey<HashSet<Type>> _componentsKey = new("FavoriteComponents", "Favorited Components", () => new HashSet<Type> { typeof(ValueMultiDriver<>), typeof(ReferenceMultiDriver<>) }, true);
        private static readonly DefiningConfigKey<HashSet<string>> _protoFluxCategoriesKey = new("FavoriteProtoFluxCategories", "Favorited ProtoFlux Categories", () => new HashSet<string> { }, true);
        private static readonly DefiningConfigKey<HashSet<Type>> _protoFluxNodesKey = new("FavoriteProtoFluxNodes", "Favorited ProtoFlux Nodes", () => new HashSet<Type> { }, true);
        private static readonly DefiningConfigKey<bool> _sortFavoriteCategoriesToTop = new("SortFavoriteCategoriesToTop", "Sort favorited Categories above unfavorited ones.", () => false);
        private static readonly DefiningConfigKey<bool> _sortFavoriteComponentsToTop = new("SortFavoriteComponentsToTop", "Sort favorited Components / Nodes above unfavorited ones.", () => true);
        private static readonly DefiningConfigKey<bool> _sortFavoriteConcreteGenericsToTop = new("SortFavoriteConcreteGenericsToTop", "Sort favorited concrete generic Components / Nodes above unfavorited ones.", () => true);
        public HashSet<string> Categories => _categoriesKey.GetValue()!;

        public HashSet<Type> Components => _componentsKey.GetValue()!;
        public override string Description => "Contains the favorited categories and components.";
        public override string Id => "Favorites";
        public HashSet<string> ProtoFluxCategories => _protoFluxCategoriesKey.GetValue()!;
        public HashSet<Type> ProtoFluxNodes => _protoFluxNodesKey.GetValue()!;
        public bool SortFavoriteCategoriesToTop => _sortFavoriteCategoriesToTop.GetValue();
        public bool SortFavoriteComponentsToTop => _sortFavoriteComponentsToTop.GetValue();
        public bool SortFavoriteConcreteGenericsToTop => _sortFavoriteConcreteGenericsToTop.GetValue();
        public override Version Version { get; } = new(1, 1, 0);
    }
}