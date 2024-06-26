﻿using ComponentSelectorAdditions.Events;
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
using System.Xml.Linq;

namespace ComponentSelectorAdditions
{
    internal sealed class Favorites : ConfiguredResoniteMonkey<Favorites, FavoritesConfig>,
        ICancelableEventHandler<EnumerateCategoriesEvent>, ICancelableEventHandler<EnumerateComponentsEvent>,
        IEventHandler<PostProcessButtonsEvent>
    {
        private const string FavoritesPath = "/Favorites";
        private const string ProtoFluxFavoritesPath = "/ProtoFlux/Runtimes/Execution/Nodes/Favorites";
        private const string ProtoFluxPath = "/ProtoFlux/Runtimes/Execution/Nodes";

        private CategoryNode<Type> _favoritesCategory = null!;
        private CategoryNode<Type> _protoFluxFavoritesCategory = null!;
        private CategoryNode<Type> _protoFluxRootCategory = null!;
        private CategoryNode<Type> _rootCategory = null!;
        public override bool CanBeDisabled => true;

        public int Priority => HarmonyLib.Priority.Normal;

        public bool SkipCanceled => true;

        public void Handle(EnumerateComponentsEvent eventData)
        {
            if (eventData.RootCategory != _favoritesCategory && eventData.RootCategory != _protoFluxFavoritesCategory)
                return;

            var favoriteElements = eventData.RootCategory == _favoritesCategory ? ConfigSection.Components : ConfigSection.ProtoFluxNodes;

            foreach (var typeName in favoriteElements)
                eventData.AddItem(new ComponentResult(eventData.RootCategory, WorkerManager.GetType(typeName)));

            eventData.Canceled = true;
        }

        public void Handle(EnumerateCategoriesEvent eventData)
        {
            if (eventData.Path.IsSelectorRoot)
            {
                if (eventData.RootCategory == _rootCategory)
                    eventData.AddItem(_favoritesCategory, -1000, true);
                else if (eventData.RootCategory == _protoFluxRootCategory)
                    eventData.AddItem(_protoFluxFavoritesCategory, -1000, true);

                return;
            }

            if (eventData.RootCategory != _favoritesCategory && eventData.RootCategory != _protoFluxFavoritesCategory)
                return;

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

        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        protected override bool OnEngineReady()
        {
            _favoritesCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(FavoritesPath);
            _protoFluxFavoritesCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(ProtoFluxFavoritesPath);

            _rootCategory = WorkerInitializer.ComponentLibrary;
            _protoFluxRootCategory = WorkerInitializer.ComponentLibrary.GetSubcategory(ProtoFluxPath);

            Mod.RegisterEventHandler<EnumerateCategoriesEvent>(this);
            Mod.RegisterEventHandler<EnumerateComponentsEvent>(this);
            Mod.RegisterEventHandler<PostProcessButtonsEvent>(this);

            return base.OnEngineReady();
        }

        protected override bool OnShutdown(bool applicationExiting)
        {
            if (!applicationExiting)
            {
                Mod.UnregisterEventHandler<EnumerateCategoriesEvent>(this);
                Mod.UnregisterEventHandler<EnumerateComponentsEvent>(this);
                Mod.UnregisterEventHandler<PostProcessButtonsEvent>(this);
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
            => ConfigSection.Components.Contains(name);

        private bool IsFavoriteProtoFluxNode(string name)
            => ConfigSection.ProtoFluxNodes.Contains(name);

        private bool IsProtoFluxFavoriteCategory(string name)
            => ConfigSection.ProtoFluxCategories.Contains(name);

        private bool ToggleFavoriteCategory(string name)
            => ToggleHashSetContains(ConfigSection.Categories, name);

        private bool ToggleFavoriteComponent(string name)
            => ToggleHashSetContains(ConfigSection.Components, name);

        private bool ToggleFavoriteProtoFluxNode(string name)
            => ToggleHashSetContains(ConfigSection.ProtoFluxNodes, name);

        private bool ToggleProtoFluxFavoriteCategory(string name)
            => ToggleHashSetContains(ConfigSection.ProtoFluxCategories, name);
    }
}