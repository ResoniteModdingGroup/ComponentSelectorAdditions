using FrooxEngine;
using MonkeyLoader.Configuration;

namespace ComponentSelectorAdditions
{
    internal sealed class FavoritesConfig : ConfigSection
    {
        private readonly DefiningConfigKey<HashSet<string>> _categories = new("FavoriteCategories", "Favorited Categories", () => ["/Data/Dynamic"], true, value => value is not null);
        private readonly DefiningConfigKey<HashSet<Type>> _components = new("FavoriteComponents", "Favorited Components", () => [typeof(ValueMultiDriver<>), typeof(ReferenceMultiDriver<>)], true, value => value is not null);
        private readonly DefiningConfigKey<HashSet<string>> _protoFluxCategories = new("FavoriteProtoFluxCategories", "Favorited ProtoFlux Categories", () => [], true, value => value is not null);
        private readonly DefiningConfigKey<HashSet<Type>> _protoFluxNodes = new("FavoriteProtoFluxNodes", "Favorited ProtoFlux Nodes", () => [], true, value => value is not null);
        private readonly DefiningConfigKey<bool> _sortFavoriteCategoriesToTop = new("SortFavoriteCategoriesToTop", "Sort favorited Categories above unfavorited ones.", () => false);
        private readonly DefiningConfigKey<bool> _sortFavoriteComponentsToTop = new("SortFavoriteComponentsToTop", "Sort favorited Components / Nodes above unfavorited ones.", () => true);
        private readonly DefiningConfigKey<bool> _sortFavoriteConcreteGenericsToTop = new("SortFavoriteConcreteGenericsToTop", "Sort favorited concrete generic Components / Nodes above unfavorited ones.", () => true);

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