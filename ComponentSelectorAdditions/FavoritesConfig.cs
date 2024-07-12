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
        private static readonly DefiningConfigKey<HashSet<string>> _categories = new("FavoriteCategories", "Favorited Categories", () => new HashSet<string> { "/Data/Dynamic" }, true, value => value is not null);
        private static readonly DefiningConfigKey<HashSet<Type>> _components = new("FavoriteComponents", "Favorited Components", () => new HashSet<Type> { typeof(ValueMultiDriver<>), typeof(ReferenceMultiDriver<>) }, true, value => value is not null);
        private static readonly DefiningConfigKey<HashSet<string>> _protoFluxCategories = new("FavoriteProtoFluxCategories", "Favorited ProtoFlux Categories", () => new HashSet<string> { }, true, value => value is not null);
        private static readonly DefiningConfigKey<HashSet<Type>> _protoFluxNodes = new("FavoriteProtoFluxNodes", "Favorited ProtoFlux Nodes", () => new HashSet<Type> { }, true, value => value is not null);
        private static readonly DefiningConfigKey<bool> _sortFavoriteCategoriesToTop = new("SortFavoriteCategoriesToTop", "Sort favorited Categories above unfavorited ones.", () => false);
        private static readonly DefiningConfigKey<bool> _sortFavoriteComponentsToTop = new("SortFavoriteComponentsToTop", "Sort favorited Components / Nodes above unfavorited ones.", () => true);
        private static readonly DefiningConfigKey<bool> _sortFavoriteConcreteGenericsToTop = new("SortFavoriteConcreteGenericsToTop", "Sort favorited concrete generic Components / Nodes above unfavorited ones.", () => true);

        public HashSet<string> Categories => _categories!;

        public HashSet<Type> Components => _components!;

        public override string Description => "Contains the favorited categories and components.";

        public override string Id => "Favorites";

        public HashSet<string> ProtoFluxCategories => _protoFluxCategories!;

        public HashSet<Type> ProtoFluxNodes => _protoFluxNodes!;

        public bool SortFavoriteCategoriesToTop => _sortFavoriteCategoriesToTop;

        public bool SortFavoriteComponentsToTop => _sortFavoriteComponentsToTop;

        public bool SortFavoriteConcreteGenericsToTop => _sortFavoriteConcreteGenericsToTop;

        public override Version Version { get; } = new(1, 1, 0);
    }
}