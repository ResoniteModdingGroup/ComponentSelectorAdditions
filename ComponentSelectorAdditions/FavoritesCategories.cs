using ComponentSelectorAdditions.Events;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using MonkeyLoader.Events;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions
{
    internal sealed class FavoritesCategories : ConfiguredResoniteMonkey<FavoritesCategories, FavoritesConfig>,
        ICancelableEventHandler<EnumerateCategoriesEvent>, ICancelableEventHandler<EnumerateComponentsEvent>,
        IEventHandler<EnumerateConcreteGenericsEvent>,
        IEventHandler<PostProcessButtonsEvent>
    {
        private const string FavoritesPath = "/Favorites";
        private const string ProtoFluxFavoritesPath = "/ProtoFlux/Runtimes/Execution/Nodes/Favorites";
        private const string ProtoFluxPath = "/ProtoFlux/Runtimes/Execution/Nodes";

        private CategoryNode<Type> _favoritesCategory = null!;
        private CategoryNode<Type> _protoFluxFavoritesCategory = null!;
        private CategoryNode<Type> _protoFluxRootCategory = null!;
        private CategoryNode<Type> _rootCategory = null!;

        public int Priority => HarmonyLib.Priority.Normal;

        public bool SkipCanceled => true;

        public void Handle(EnumerateComponentsEvent eventData)
        {
            if (eventData.RootCategory != _favoritesCategory && eventData.RootCategory != _protoFluxFavoritesCategory)
            {
                if (ConfigSection.SortFavoriteComponentsToTop)
                {
                    foreach (var element in eventData.RootCategory.Elements)
                    {
                        if (ConfigSection.Components.Contains(element) || ConfigSection.ProtoFluxNodes.Contains(element))
                            eventData.AddItem(new ComponentResult(eventData.RootCategory, element), -1000, true);
                    }
                }

                return;
            }

            var favoriteElements = (eventData.RootCategory == _favoritesCategory ? ConfigSection.Components : ConfigSection.ProtoFluxNodes)
                .Where(type => type is not null)
                .Select(type => (Type: type, Category: WorkerInitializer.ComponentLibrary.GetSubcategory(WorkerInitializer.GetInitInfo(type).CategoryPath)));

            foreach (var element in favoriteElements)
                eventData.AddItem(new ComponentResult(element.Category, element.Type));

            eventData.Canceled = true;
        }

        public void Handle(EnumerateCategoriesEvent eventData)
        {
            if (eventData.RootCategory == _rootCategory || eventData.RootCategory == _protoFluxRootCategory)
            {
                if (eventData.RootCategory == _rootCategory)
                    eventData.AddItem(_favoritesCategory, -1000, true);
                else
                    eventData.AddItem(_protoFluxFavoritesCategory, -1000, true);

                return;
            }

            if (eventData.RootCategory != _favoritesCategory && eventData.RootCategory != _protoFluxFavoritesCategory)
            {
                if (ConfigSection.SortFavoriteCategoriesToTop)
                {
                    foreach (var category in eventData.RootCategory.Subcategories)
                    {
                        var path = category.GetPath();

                        if (ConfigSection.Categories.Contains(path) || ConfigSection.ProtoFluxCategories.Contains(path))
                            eventData.AddItem(category, -1000, true);
                    }
                }

                return;
            }

            var favoriteCategories = eventData.RootCategory == _favoritesCategory ?
                ConfigSection.Categories : ConfigSection.ProtoFluxCategories;

            foreach (var category in favoriteCategories)
                eventData.AddItem(WorkerInitializer.ComponentLibrary.GetSubcategory(category));

            eventData.Canceled = true;
        }

        public void Handle(PostProcessButtonsEvent eventData)
        {
            if (eventData.Path.IsSelectorRoot)
                return;

            var isProtoFlux = eventData.Path.Path.StartsWith(ProtoFluxPath);

            foreach (var categoryButton in eventData.CategoryButtons)
            {
                AddFavoriteButton(eventData.UI, categoryButton, false,
                    isProtoFlux ? IsProtoFluxFavoriteCategory : IsFavoriteCategory,
                    isProtoFlux ? ToggleProtoFluxFavoriteCategory : ToggleFavoriteCategory);
            }

            foreach (var addButton in eventData.AddButtons.Concat(eventData.GenericButtons))
            {
                AddFavoriteButton(eventData.UI, addButton, true,
                    isProtoFlux ? IsFavoriteProtoFluxNode : IsFavoriteComponent,
                    isProtoFlux ? ToggleFavoriteProtoFluxNode : ToggleFavoriteComponent);
            }
        }

        public void Handle(EnumerateConcreteGenericsEvent eventData)
        {
            var concreteGenerics = ConfigSection.Components
                .Concat(ConfigSection.ProtoFluxNodes)
                .Where(type => type is not null && type.IsGenericType && !type.ContainsGenericParameters && type.GetGenericTypeDefinition() == eventData.Component);

            foreach (var concreteGeneric in concreteGenerics)
                eventData.AddItem(concreteGeneric, ConfigSection.SortFavoriteConcreteGenericsToTop ? -100 : 0, ConfigSection.SortFavoriteConcreteGenericsToTop);
        }

        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        protected override bool OnEngineReady()
        {
            _favoritesCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(FavoritesPath);
            SearchConfig.Instance.AddExcludedCategory(FavoritesPath);

            _protoFluxFavoritesCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(ProtoFluxFavoritesPath);
            SearchConfig.Instance.AddExcludedCategory(ProtoFluxFavoritesPath);

            _rootCategory = WorkerInitializer.ComponentLibrary;
            _protoFluxRootCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(ProtoFluxPath);

            Mod.RegisterEventHandler<EnumerateCategoriesEvent>(this);
            Mod.RegisterEventHandler<EnumerateComponentsEvent>(this);
            Mod.RegisterEventHandler<EnumerateConcreteGenericsEvent>(this);
            Mod.RegisterEventHandler<PostProcessButtonsEvent>(this);

            return base.OnEngineReady();
        }

        protected override bool OnShutdown(bool applicationExiting)
        {
            if (!applicationExiting)
            {
                Mod.UnregisterEventHandler<EnumerateCategoriesEvent>(this);
                Mod.UnregisterEventHandler<EnumerateComponentsEvent>(this);
                Mod.UnregisterEventHandler<EnumerateConcreteGenericsEvent>(this);
                Mod.UnregisterEventHandler<PostProcessButtonsEvent>(this);

                _rootCategory._subcategories.Remove("Favorites");
                SearchConfig.Instance.RemoveExcludedCategory(FavoritesPath);

                _protoFluxRootCategory._subcategories.Remove("Favorites");
                SearchConfig.Instance.RemoveExcludedCategory(ProtoFluxFavoritesPath);
            }

            return base.OnShutdown(applicationExiting);
        }

        private static void AddFavoriteButton(UIBuilder builder, Button button, bool isComponent, Predicate<string> isFavorite, Func<string, bool> toggleFavorite)
        {
            builder.NestInto(button.Slot.Parent);

            var height = button.Slot.GetComponent<LayoutElement>().MinHeight;
            builder.Style.MinHeight = height;

            var panel = builder.Panel();
            builder.VerticalFooter(height + 4, out var footer, out var content);

            button.Slot.Parent = content.Slot;
            panel.Slot.OrderOffset = button.Slot.OrderOffset;
            button.Slot.OrderOffset = 0;

            footer.OffsetMin.Value += new float2(4, 0);
            builder.NestInto(footer);

            var name = button.Slot.GetComponent<ButtonRelay<string>>().Argument.Value;
            if (isComponent)
            {
                var lastSlashIndex = name.LastIndexOf('/');
                name = name.Substring(lastSlashIndex + 1);
            }

            var favColor = isFavorite(name) ? RadiantUI_Constants.Hero.YELLOW : RadiantUI_Constants.Neutrals.DARKLIGHT;

            var favoriteButton = builder.Button(OfficialAssets.Graphics.Icons.World_Categories.FeaturedRibbon, RadiantUI_Constants.BUTTON_COLOR, favColor);
            var icon = favoriteButton.Slot.GetComponentsInChildren<Image>().Last();

            favoriteButton.LocalPressed += (btn, btnEvent) =>
            {
                icon.Tint.Value = toggleFavorite(name) ?
                    RadiantUI_Constants.Hero.YELLOW : RadiantUI_Constants.Neutrals.DARKLIGHT;

                Config.Save();
            };
        }

        private static bool ToggleHashSetContains<T>(ISet<T> set, T value)
        {
            if (set.Add(value))
                return true;

            set.Remove(value);
            return false;
        }

        private bool IsFavoriteCategory(string name)
            => ConfigSection.Categories.Contains(name);

        private bool IsFavoriteComponent(string name)
            => ConfigSection.Components.Contains(WorkerManager.ParseNiceType(name));

        private bool IsFavoriteProtoFluxNode(string name)
            => ConfigSection.ProtoFluxNodes.Contains(WorkerManager.ParseNiceType(name));

        private bool IsProtoFluxFavoriteCategory(string name)
            => ConfigSection.ProtoFluxCategories.Contains(name);

        private bool ToggleFavoriteCategory(string name)
            => ToggleHashSetContains(ConfigSection.Categories, name);

        private bool ToggleFavoriteComponent(string name)
            => ToggleHashSetContains(ConfigSection.Components, WorkerManager.ParseNiceType(name));

        private bool ToggleFavoriteProtoFluxNode(string name)
            => ToggleHashSetContains(ConfigSection.ProtoFluxNodes, WorkerManager.ParseNiceType(name));

        private bool ToggleProtoFluxFavoriteCategory(string name)
            => ToggleHashSetContains(ConfigSection.ProtoFluxCategories, name);
    }
}