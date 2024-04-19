using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ComponentAttacherFavorites
{
    public class ComponentAttacherFavorites : NeosMod
    {
        internal static ModConfiguration Config;

        private const string FavoritesPath = "/Favorites";

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<HashSet<string>> FavoriteCategories = new("favoriteCategories", "Favorited Categories", () => new HashSet<string>() { "Data > Dynamic" }, true);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<HashSet<string>> FavoriteComponents = new("favoriteComponents", "Favorited Components", () => new HashSet<string>() { "FrooxEngine.ValueMultiDriver`1", "FrooxEngine.ReferenceMultiDriver`1" }, true);

        private static CategoryNode<Type> favoritesCategory;
        public override string Author => "Banane9";
        public override string Link => "https://github.com/Banane9/NeosComponentAttacherFavorites";
        public override string Name => "ComponentAttacherFavorites";
        public override string Version => "2.1.0";

        private static CategoryNode<Type> FavoritesCategory
        {
            get => favoritesCategory;
            set
            {
                favoritesCategory = value;

                foreach (var typeName in Config.GetValue(FavoriteComponents))
                    favoritesCategory.AddElement(WorkerManager.GetType(typeName));

                foreach (var category in Config.GetValue(FavoriteCategories))
                    favoritesCategory.GetSubcategory(category);
            }
        }

        public override void OnEngineInit()
        {
            var harmony = new Harmony($"{Author}.{Name}");
            Config = GetConfiguration();
            Config.Set(FavoriteComponents, Config.GetValue(FavoriteComponents));
            Config.Set(FavoriteCategories, Config.GetValue(FavoriteCategories));
            Config.Save(true);
            harmony.PatchAll();

            Engine.Current.OnReady += () =>
            {
                FavoritesCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(FavoritesPath);

                if (ModLoader.Mods().FirstOrDefault(mod => mod.Name == "ComponentAttacherSearch") is NeosModBase searchMod
                 && (searchMod.GetConfiguration()?.TryGetValue(new ModConfigurationKey<HashSet<string>>("ExcludedCategories"), out var excludedCategories) ?? false))
                        excludedCategories.Add(FavoritesPath);
            };
        }

        [HarmonyPatch(typeof(ComponentAttacher), "BuildUI")]
        private static class ComponentAttacherPatch
        {
            private static readonly color favoriteColor = new(1f, 1f, 0.8f);
            private static readonly color nonFavoriteColor = new(.8f);

            [HarmonyPostfix]
            public static void BuildUIPostfix(string path, bool genericType, SyncRef<Slot> ____uiRoot)
            {
                if (genericType)
                    return;

                if ((string.IsNullOrEmpty(path) || path == "/")
                 && ____uiRoot.Target.GetComponentInChildren<ButtonRelay<string>>(relay => relay.Argument == FavoritesPath) is ButtonRelay<string> relay)
                {
                    relay.Slot.OrderOffset = long.MinValue;
                    return;
                }

                var isFavorites = path == FavoritesPath;

                // Skip Back button at the start and stop before cancel button at the end
                var buttons = ____uiRoot.Target.GetComponentsInChildren<Button>();
                foreach (var button in buttons.Skip(1).Take(buttons.Count - 2))
                {
                    var buttonRelay = button.Slot.GetComponent<ButtonRelay<string>>();
                    var buttonPath = buttonRelay.Argument.Value;
                    var isComponent = buttonPath.Contains('.');

                    if (isFavorites && !isComponent)
                    {
                        buttonPath = GetCategoryPathFromFavorite(buttonPath);
                        buttonRelay.Argument.Value = buttonPath;
                    }

                    AddFavoriteButton(button, buttonPath, isComponent);
                }

                // Make sure cancel button is at the end
                buttons[buttons.Count - 1].Slot.OrderOffset = long.MaxValue;
            }

            private static void AddFavoriteButton(Button button, string buttonPath, bool isComponent)
            {
                var builder = new UIBuilder(button.Slot.Parent);
                builder.Style.MinHeight = button.Slot.GetComponent<LayoutElement>().MinHeight;

                var panel = builder.Panel();
                builder.VerticalFooter(36, out var footer, out var content);
                button.Slot.Parent = content.Slot;

                footer.OffsetMin.Value += new float2(4, 0);
                builder = new UIBuilder(footer);

                var favoritesSetting = Config.GetValue(isComponent ? FavoriteComponents : FavoriteCategories);
                var name = isComponent ? GetTypeNameFromPath(buttonPath) : GetFavoriteFromCategoryPath(buttonPath);
                var favColor = favoritesSetting.Contains(name) ? favoriteColor : nonFavoriteColor;
                Action<string> addToFavoriteCategory = isComponent ? AddFavoriteComponent : AddFavoriteCategory;
                Action<string> removeFromFavoriteCategory = isComponent ? RemoveFavoriteComponent : RemoveFavoriteCategory;

                var favorite = builder.Button(NeosAssets.Common.Icons.Star, favColor);
                favorite.LocalPressed += (btn, btnEvent) =>
                {
                    if (favoritesSetting.Contains(name))
                    {
                        favoritesSetting.Remove(name);
                        removeFromFavoriteCategory(name);
                        favorite.SetColors(nonFavoriteColor);
                    }
                    else
                    {
                        favoritesSetting.Add(name);
                        addToFavoriteCategory(name);
                        favorite.SetColors(favoriteColor);
                    }

                    Config.Save(true);
                };
            }

            private static void AddFavoriteCategory(string path)
                => FavoritesCategory.GetSubcategory(path);

            private static void AddFavoriteComponent(string name)
                => FavoritesCategory.AddElement(WorkerManager.GetType(name));

            private static string GetCategoryPathFromFavorite(string favoriteSubCategory)
                => favoriteSubCategory.Substring(FavoritesPath.Length).Replace(" > ", "/");

            private static string GetFavoriteFromCategoryPath(string path)
                => path.Substring(1).Replace("/", " > ");

            private static string GetTypeNameFromPath(string path)
            {
                var pathEnd = MathX.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));

                if (pathEnd < 0)
                    return path;

                return path.Substring(pathEnd + 1);
            }

            private static void RemoveFavoriteCategory(string path)
                => new Traverse(FavoritesCategory).Field<SortedDictionary<string, CategoryNode<Type>>>("_subcategories").Value.Remove(path);

            private static void RemoveFavoriteComponent(string name)
                => ((List<Type>)FavoritesCategory.Elements).Remove(WorkerManager.GetType(name));
        }
    }
}