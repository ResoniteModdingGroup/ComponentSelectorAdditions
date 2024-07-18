using ComponentSelectorAdditions.Events;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using MonkeyLoader;
using MonkeyLoader.Events;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions
{
    internal sealed class GenericPresetsHandler : ConfiguredResoniteMonkey<GenericPresetsHandler, GenericPresetsConfig>,
        IEventHandler<BuildCustomGenericBuilder>, IEventHandler<EnumerateConcreteGenericsEvent>
    {
        public override bool CanBeDisabled => true;
        public int Priority => HarmonyLib.Priority.High;

        public void Handle(EnumerateConcreteGenericsEvent eventData)
        {
            if (!Enabled)
                return;

            var parameters = eventData.Component.GetGenericArguments().Length;
            var concreteOptions = ConfigSection.GenericArgumentPresets
                .Where(preset => preset.Length == parameters)
                .Select(preset => MakeGenericType(eventData.Component, preset))
                .Where(type => type is not null && type.IsValidGenericType(true));

            foreach (var concreteOption in concreteOptions)
                eventData.AddItem(concreteOption!);
        }

        public void Handle(BuildCustomGenericBuilder eventData)
        {
            if (!Enabled)
                return;

            var ui = eventData.UI;
            var selector = eventData.Selector;

            ui.PushStyle();
            ui.Style.FlexibleWidth = 1;
            ui.Style.ForceExpandHeight = true;

            ui.HorizontalLayout(4, 0, Alignment.MiddleLeft);
            ui.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);
            ui.VerticalLayout(8, 0);

            foreach (var genericArgument in eventData.GenericArguments)
            {
                var textField = DefaultHandler.MakeGenericArgumentInput(ui, eventData.Component, genericArgument, selector.GenericArgumentPrefiller.Target);

                selector._customGenericArguments.Add(textField);
            }

            ui.NestOut();
            ui.Style.FlexibleWidth = -1;

            ui.Style.PreferredHeight = 32;
            ui.Style.PreferredWidth = 32;
            var addPresetButton = ui.Button(OfficialAssets.Graphics.Icons.World_Categories.FeaturedRibbon, RadiantUI_Constants.BUTTON_COLOR, RadiantUI_Constants.Neutrals.DARKLIGHT);
            var icon = addPresetButton.Slot.GetComponentsInChildren<Image>().Last();

            addPresetButton.Slot.DestroyWhenLocalUserLeaves();
            addPresetButton.StartTask(UpdatePresetButtonAsync);
            addPresetButton.LocalPressed += (btn, btnEvent) => ToggleSavedPreset(selector.GetCustomGenericType());

            eventData.AddsGenericArgumentInputs = true;
            ui.PopStyle();
            ui.NestOut();

            async Task UpdatePresetButtonAsync()
            {
                while (addPresetButton.FilterWorldElement() is not null)
                {
                    await default(NextUpdate);

                    var concreteType = selector.GetCustomGenericType();

                    addPresetButton.Enabled = IsValidConcreteType(concreteType);
                    icon.Tint.Value = addPresetButton.Enabled && IsPreset(concreteType.GenericTypeArguments) ?
                        RadiantUI_Constants.Hero.YELLOW : RadiantUI_Constants.Neutrals.DARKLIGHT;
                }
            }
        }

        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        protected override bool OnEngineReady()
        {
            Mod.RegisterEventHandler<BuildCustomGenericBuilder>(this);
            Mod.RegisterEventHandler<EnumerateConcreteGenericsEvent>(this);

            return base.OnEngineReady();
        }

        protected override bool OnShutdown(bool applicationExiting)
        {
            if (!applicationExiting)
            {
                Mod.UnregisterEventHandler<BuildCustomGenericBuilder>(this);
                Mod.RegisterEventHandler<EnumerateConcreteGenericsEvent>(this);
            }

            return base.OnShutdown(applicationExiting);
        }

        private static bool IsPreset(Sequence<Type> arguments)
            => ConfigSection.GenericArgumentPresets.Contains(arguments);

        private static bool IsValidConcreteType([NotNullWhen(true)] Type? concreteType)
            => concreteType is not null && concreteType.IsValidGenericType(true);

        private static Type? MakeGenericType(Type genericType, Type[] arguments)
        {
            try
            {
                return genericType.MakeGenericType(arguments);
            }
            catch
            {
                return null;
            }
        }

        private static void ToggleSavedPreset(Type? concreteType)
        {
            if (!IsValidConcreteType(concreteType))
                return;

            var arguments = concreteType.GenericTypeArguments;

            if (ConfigSection.GenericArgumentPresets.Add(arguments))
            {
                Config.Save();
                return;
            }

            ConfigSection.GenericArgumentPresets.Remove(arguments);
            Config.Save();
        }
    }
}