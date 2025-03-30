using ComponentSelectorAdditions.Events;
using Elements.Core;
using FrooxEngine.UIX;
using FrooxEngine;
using HarmonyLib;
using MonkeyLoader.Events;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ComponentSelectorAdditions
{
    /// <summary>
    /// Handles the default behavior for the <see cref="Injector"/> events.
    /// </summary>
    public sealed class DefaultHandler : ConfiguredResoniteMonkey<DefaultHandler, DefaultConfig>,
        ICancelableEventHandler<EnumerateCategoriesEvent>, ICancelableEventHandler<EnumerateComponentsEvent>,
        ICancelableEventHandler<BuildCategoryButtonEvent>, ICancelableEventHandler<BuildGroupButtonEvent>, ICancelableEventHandler<BuildComponentButtonEvent>,
        IEventHandler<BuildCustomGenericBuilder>, IEventHandler<EnumerateConcreteGenericsEvent>
    {
        /// <inheritdoc/>
        public int Priority => HarmonyLib.Priority.Normal;

        /// <inheritdoc/>
        public bool SkipCanceled => true;

        /// <summary>
        /// Formats the path from the <paramref name="rootCategory"/> to the <paramref name="subCategory"/>
        /// or to its root prettily with the <paramref name="delimiter"/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="CategoryNode{T}"/>s.</typeparam>
        /// <param name="subCategory">The category the path leads to.</param>
        /// <param name="rootCategory">The root that the path starts from.</param>
        /// <param name="delimiter">The string between the category names.</param>
        /// <returns>The trimmed, path-like string.</returns>
        public static string? GetPrettyPath<T>(CategoryNode<T>? subCategory, CategoryNode<T>? rootCategory = null, string delimiter = " > ")
        {
            var segments = EnumerateParents(subCategory, rootCategory);

            if (!segments.Any())
                return null;

            return segments.Reverse().Join(delimiter: delimiter) + delimiter.TrimEnd();
        }

        /// <summary>
        /// Creates a labeled text field as a generic argument input.
        /// </summary>
        /// <param name="ui">The <see cref="UIBuilder"/> to use for the construction.</param>
        /// <param name="component">The type that the generic argument input is for.</param>
        /// <param name="genericArgument">The generic argument "type".</param>
        /// <param name="genericArgumentPrefiller">The <see cref="ComponentSelector.GenericArgumentPrefiller"/> target.</param>
        /// <returns>The labeled <see cref="TextField"/> that was created.</returns>
        public static TextField MakeGenericArgumentInput(UIBuilder ui, Type component, Type genericArgument, GenericArgumentPrefiller? genericArgumentPrefiller = null)
        {
            var textField = ui.HorizontalElementWithLabel(genericArgument.Name, .05f, () =>
            {
                var textField = ui.TextField(parseRTF: false);
                textField.Text.NullContent.AssignLocaleString(Mod.GetLocaleString("EnterType"));

                return textField;
            }, out var label);

            label.HorizontalAlign.Value = Elements.Assets.TextHorizontalAlignment.Center;

            if (genericArgumentPrefiller != null)
                textField.TargetString = genericArgumentPrefiller(component, genericArgument);

            return textField;
        }

        /// <summary>
        /// Creates a labeled button that can optionally show a category path as well.
        /// </summary>
        /// <param name="ui">The <see cref="UIBuilder"/> to use for the construction.</param>
        /// <param name="name">The label on the button.</param>
        /// <param name="tint">The background color of the button.</param>
        /// <param name="callback">The <see cref="ButtonEventHandler{T}"/> to call when the button is pressed.</param>
        /// <param name="argument">The argument for the <paramref name="callback"/>.</param>
        /// <param name="category">The optional category path to show.</param>
        public static void MakePermanentButton(UIBuilder ui, LocaleString name, colorX tint, ButtonEventHandler<string> callback, string argument, string? category = null)
        {
            ui.PushStyle();
            ui.Style.MinHeight = category is not null ? ConfigSection.IndirectButtonHeight : ConfigSection.DirectButtonHeight;

            ui.HorizontalLayout(4, 0, Alignment.MiddleCenter);
            ui.Style.FlexibleWidth = 1;

            var button = ui.Button(name, tint, callback, argument, .35f);
            button.Label.ParseRichText.Value = false;

            if (category is not null)
            {
                var buttonLabel = button.Label;
                ui.NestInto(button.RectTransform);

                var panel = ui.Panel();
                panel.OffsetMin.Value = buttonLabel.RectTransform.OffsetMin;
                panel.OffsetMax.Value = buttonLabel.RectTransform.OffsetMax;

                ui.HorizontalHeader(ConfigSection.IndirectButtonHeight / 2.666f, out var header, out var content);

                buttonLabel.Slot.Parent = content.Slot;
                buttonLabel.RectTransform.OffsetMin.Value = new(ConfigSection.IndirectButtonHeight / 3, 0);
                buttonLabel.RectTransform.OffsetMax.Value = float2.Zero;

                ui.NestInto(header);
                ui.Text(category, parseRTF: false);
            }

            ui.PopStyle();
        }

        void ICancelableEventHandler<EnumerateComponentsEvent>.Handle(EnumerateComponentsEvent eventData)
        {
            var types = eventData.RootCategory.Elements;

            if (eventData.Path.HasGroup)
            {
                static IEnumerable<Type> GetAllSubElements(CategoryNode<Type> category)
                    => category.Elements.Concat(category.Subcategories.SelectMany(GetAllSubElements));

                types = GetAllSubElements(eventData.RootCategory);
            }

            foreach (var type in types)
                eventData.AddItem(new ComponentResult(eventData.RootCategory, type));

            eventData.Canceled = true;
        }

        void ICancelableEventHandler<EnumerateCategoriesEvent>.Handle(EnumerateCategoriesEvent eventData)
        {
            if (!eventData.Path.HasGroup)
            {
                foreach (var subcategory in eventData.RootCategory.Subcategories)
                    eventData.AddItem(subcategory);
            }

            eventData.Canceled = true;
        }

        void ICancelableEventHandler<BuildGroupButtonEvent>.Handle(BuildGroupButtonEvent eventData)
        {
            var selector = eventData.Selector;

            var category = GetPrettyPath(eventData.ItemCategory, eventData.RootCategory);
            var tint = RadiantUI_Constants.Sub.PURPLE;
            var argument = $"{eventData.RootCategory!.GetPath()}:{eventData.Group}";

            MakePermanentButton(eventData.UI, eventData.GroupName, tint, selector.OpenGroupPressed, argument, category);

            eventData.Canceled = true;
        }

        void IEventHandler<EnumerateConcreteGenericsEvent>.Handle(EnumerateConcreteGenericsEvent eventData)
        {
            foreach (var concreteGeneric in WorkerInitializer.GetCommonGenericTypes(eventData.Component))
                eventData.AddItem(concreteGeneric);
        }

        void IEventHandler<BuildCustomGenericBuilder>.Handle(BuildCustomGenericBuilder eventData)
        {
            var ui = eventData.UI;
            var selector = eventData.Selector;

            if (!eventData.AddsGenericArgumentInputs)
            {
                foreach (var genericArgument in eventData.GenericArguments)
                {
                    var textField = MakeGenericArgumentInput(ui, eventData.Component, genericArgument, selector.GenericArgumentPrefiller.Target);

                    selector._customGenericArguments.Add(textField);
                }
            }

            if (!eventData.AddsCreateCustomTypeButton)
                eventData.CreateCustomTypeButton = ui.Button((LocaleString)string.Empty, RadiantUI_Constants.BUTTON_COLOR, selector.OnCreateCustomType, .35f);
        }

        void ICancelableEventHandler<BuildComponentButtonEvent>.Handle(BuildComponentButtonEvent eventData)
        {
            var path = eventData.Path;
            var selector = eventData.Selector;
            var component = eventData.Component;

            var category = GetPrettyPath(component.Category, eventData.RootCategory);
            var tint = component.IsGeneric ? RadiantUI_Constants.Sub.GREEN : RadiantUI_Constants.Sub.CYAN;
            ButtonEventHandler<string> callback = component.IsGeneric ? selector.OpenGenericTypesPressed : selector.OnAddComponentPressed;
            var argument = $"{(component.IsGeneric ? $"{path.Path}/{component.Type.AssemblyQualifiedName}" : selector.World.Types.EncodeType(component.Type))}{(component.IsGeneric && path.HasGroup ? $"?{path.Group}" : "")}";

            MakePermanentButton(eventData.UI, component.NiceName, tint, callback, argument, category);

            eventData.Canceled = true;
        }

        void ICancelableEventHandler<BuildCategoryButtonEvent>.Handle(BuildCategoryButtonEvent eventData)
        {
            MakePermanentButton(eventData.UI, GetPrettyPath(eventData.ItemCategory, eventData.RootCategory),
                RadiantUI_Constants.Sub.YELLOW,
                eventData.Selector.OnOpenCategoryPressed,
                eventData.ItemCategory!.GetPath());

            eventData.Canceled = true;
        }

        /// <inheritdoc/>
        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        /// <inheritdoc/>
        protected override bool OnLoaded()
        {
            Mod.RegisterEventHandler<EnumerateCategoriesEvent>(this);
            Mod.RegisterEventHandler<EnumerateComponentsEvent>(this);

            Mod.RegisterEventHandler<BuildCategoryButtonEvent>(this);
            Mod.RegisterEventHandler<BuildGroupButtonEvent>(this);
            Mod.RegisterEventHandler<BuildComponentButtonEvent>(this);

            Mod.RegisterEventHandler<BuildCustomGenericBuilder>(this);
            Mod.RegisterEventHandler<EnumerateConcreteGenericsEvent>(this);

            return base.OnLoaded();
        }

        /// <inheritdoc/>
        protected override bool OnShutdown(bool applicationExiting)
        {
            if (!applicationExiting)
            {
                Mod.UnregisterEventHandler<EnumerateCategoriesEvent>(this);
                Mod.UnregisterEventHandler<EnumerateComponentsEvent>(this);

                Mod.UnregisterEventHandler<BuildCategoryButtonEvent>(this);
                Mod.UnregisterEventHandler<BuildGroupButtonEvent>(this);
                Mod.UnregisterEventHandler<BuildComponentButtonEvent>(this);

                Mod.UnregisterEventHandler<BuildCustomGenericBuilder>(this);
                Mod.UnregisterEventHandler<EnumerateConcreteGenericsEvent>(this);
            }

            return base.OnShutdown(applicationExiting);
        }

        private static IEnumerable<string> EnumerateParents<T>(CategoryNode<T>? start, CategoryNode<T>? end = null)
        {
            var current = start;

            while (current is not null && current != end)
            {
                yield return current.Parent is null ? "" : current.Name;
                current = current.Parent;
            }
        }
    }
}