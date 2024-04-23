using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using MonkeyLoader;
using MonkeyLoader.Configuration;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ComponentSelectorAdditions
{
    // [HarmonyPatch(typeof(ComponentSelector), "BuildUI")]
    // [HarmonyPatchCategory(nameof(ComponentSelectorFavorites))]
    internal sealed class ComponentSelectorFavorites : ConfiguredResoniteMonkey<ComponentSelectorFavorites, FavoritesConfig>
    {
        private const string FavoritesPath = "Favorites";
        private const string ProtoFluxFavoritesPath = "ProtoFlux/Favorites";
        private const string ProtoFluxPath = "ProtoFlux";

        private static CategoryNode<Type>? _favoritesCategory;
        private static CategoryNode<Type>? _protoFluxFavoritesCategory;

        private static CategoryNode<Type> FavoritesCategory
        {
            get => _favoritesCategory!;
            set
            {
                _favoritesCategory = value;

                foreach (var typeName in ConfigSection.Components)
                    _favoritesCategory.AddElement(WorkerManager.GetType(typeName));

                foreach (var category in ConfigSection.Categories)
                    _favoritesCategory.GetSubcategory(category);
            }
        }

        private static CategoryNode<Type> ProtoFluxFavoritesCategory
        {
            get => _protoFluxFavoritesCategory!;
            set
            {
                _protoFluxFavoritesCategory = value;

                foreach (var typeName in ConfigSection.ProtoFluxNodes)
                    _protoFluxFavoritesCategory.AddElement(WorkerManager.GetType(typeName));

                foreach (var category in ConfigSection.ProtoFluxCategories)
                    _protoFluxFavoritesCategory.GetSubcategory(category);
            }
        }

        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        protected override bool OnEngineReady()
        {
            SearchConfig.Instance.AddExcludedCategory($"/{FavoritesPath}");
            SearchConfig.Instance.AddExcludedCategory($"/{ProtoFluxFavoritesPath}");

            FavoritesCategory = WorkerInitializer.ComponentLibrary.GetSubcategory($"/{FavoritesPath}");
            ProtoFluxFavoritesCategory = WorkerInitializer.ComponentLibrary.GetSubcategory($"/{ProtoFluxFavoritesPath}");

            return base.OnEngineReady();
        }

        private static void AddFavoriteButton(UIBuilder builder, Button button, string buttonPath, bool isComponent, bool isProtoFlux)
        {
            builder.NestInto(button.Slot.Parent);

            var height = button.Slot.GetComponent<LayoutElement>().MinHeight;
            builder.Style.MinHeight = height;

            var panel = builder.Panel();
            builder.VerticalFooter(height + 4, out var footer, out var content);
            button.Slot.Parent = content.Slot;

            footer.OffsetMin.Value += new float2(4, 0);
            builder.NestInto(footer);

            var favoritesSetting = isComponent ?
                (isProtoFlux ? ConfigSection.ProtoFluxNodes : ConfigSection.Components)
                : (isProtoFlux ? ConfigSection.ProtoFluxCategories : ConfigSection.Categories);

            var name = isComponent ? GetTypeNameFromPath(buttonPath) : GetFavoriteFromCategoryPath(buttonPath);
            var favColor = favoritesSetting.Contains(name) ? RadiantUI_Constants.Hero.YELLOW : RadiantUI_Constants.Neutrals.DARKLIGHT;

            Action<string> addToFavoriteCategory = isComponent ?
                (isProtoFlux ? AddFavoriteProtoFluxNode : AddFavoriteComponent)
                : (isProtoFlux ? AddFavoriteProtoFluxCategory : AddFavoriteCategory);

            Action<string> removeFromFavoriteCategory = isComponent ?
                (isProtoFlux ? RemoveFavoriteProtoFluxNode : RemoveFavoriteComponent)
                : (isProtoFlux ? RemoveFavoriteProtoFluxCategory : RemoveFavoriteCategory);

            var favoriteButton = builder.Button(OfficialAssets.Graphics.Icons.World_Categories.FeaturedRibbon, RadiantUI_Constants.BUTTON_COLOR, favColor);
            var icon = favoriteButton.Slot.GetComponentsInChildren<Image>().Last();

            favoriteButton.LocalPressed += (btn, btnEvent) =>
            {
                if (favoritesSetting.Contains(name))
                {
                    favoritesSetting.Remove(name);
                    removeFromFavoriteCategory(name);
                    icon.Tint.Value = RadiantUI_Constants.Neutrals.DARKLIGHT;
                }
                else
                {
                    favoritesSetting.Add(name);
                    addToFavoriteCategory(name);
                    icon.Tint.Value = RadiantUI_Constants.Hero.YELLOW;
                }

                Config.Save();
            };
        }

        private static void AddFavoriteCategory(string path)
            => FavoritesCategory.GetSubcategory(path);

        private static void AddFavoriteComponent(string name)
            => FavoritesCategory.AddElement(WorkerManager.GetType(name));

        private static void AddFavoriteProtoFluxCategory(string path)
            => ProtoFluxFavoritesCategory.GetSubcategory(path);

        private static void AddFavoriteProtoFluxNode(string name)
            => ProtoFluxFavoritesCategory.AddElement(WorkerManager.GetType(name));

        [HarmonyPostfix]
        private static void BuildUIPostfix(ComponentSelector __instance, string path, bool genericType)
        {
            if (genericType)
                return;

            if ((string.IsNullOrEmpty(path) || path is "/" or ProtoFluxPath)
                && __instance._uiRoot.Target.GetComponentInChildren<ButtonRelay<string>>(relay => relay.Argument.Value.EndsWith(FavoritesPath) || relay.Argument.Value.EndsWith(ProtoFluxFavoritesPath)) is ButtonRelay<string> relay)
            {
                relay.Slot.OrderOffset = long.MinValue;

                return;
            }

            // Vanilla method removes the leading /, so gotta put it back.
            var isFavorites = path is FavoritesPath or ProtoFluxFavoritesPath;
            var builder = new UIBuilder(__instance._uiRoot).SetupStyle();

            // Skip Back button at the start and stop before cancel button at the end
            var buttons = __instance._uiRoot.Target.GetComponentsInChildren<Button>();
            foreach (var button in buttons.Skip(1).Take(buttons.Count - 2))
            {
                var buttonRelay = button.Slot.GetComponent<ButtonRelay<string>>();
                var buttonPath = buttonRelay.Argument.Value;
                var isComponent = buttonRelay.ButtonPressed.Target != __instance.OnOpenCategoryPressed;

                if (isFavorites && !isComponent)
                {
                    buttonPath = GetCategoryPathFromFavorite(buttonPath);
                    buttonRelay.Argument.Value = buttonPath;
                }

                AddFavoriteButton(builder, button, buttonPath, isComponent, buttonPath.StartsWith(ProtoFluxPath));
            }

            // Make sure cancel button is at the end
            buttons[^1].Slot.OrderOffset = long.MaxValue;
        }

        private static string GetCategoryPathFromFavorite(string favoriteSubCategory)
            => favoriteSubCategory.Replace(FavoritesPath, "").Replace(ProtoFluxFavoritesPath, "").Replace(" > ", "/");

        private static string GetFavoriteFromCategoryPath(string path)
            => path.Replace("/", " > ");

        private static string GetTypeNameFromPath(string path)
        {
            var pathEnd = MathX.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));

            if (pathEnd < 0)
                return path;

            return path.Substring(pathEnd + 1);
        }

        private static void RemoveFavoriteCategory(string path)
            => FavoritesCategory._subcategories.Remove(path);

        private static void RemoveFavoriteComponent(string name)
            => FavoritesCategory._elements.Remove(WorkerManager.GetType(name));

        private static void RemoveFavoriteProtoFluxCategory(string path)
            => ProtoFluxFavoritesCategory._subcategories.Remove(path);

        private static void RemoveFavoriteProtoFluxNode(string name)
            => ProtoFluxFavoritesCategory._elements.Remove(WorkerManager.GetType(name));
    }
}