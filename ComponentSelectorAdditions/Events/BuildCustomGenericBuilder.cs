using EnumerableToolkit;
using FrooxEngine;
using FrooxEngine.UIX;
using MonkeyLoader.Resonite.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions.Events
{
    public sealed class BuildCustomGenericBuilder : BuildUIEvent
    {
        internal readonly HashSet<Button> OtherAddedButtonsSet = new();

        [MemberNotNullWhen(true, nameof(CreateCustomTypeButton))]
        public bool AddsCreateCustomTypeButton => CreateCustomTypeButton is not null;

        public bool AddsGenericArgumentInputs { get; set; }

        /// <summary>
        /// Gets the Type of the <see cref="FrooxEngine.Component"/>
        /// that the custom generic builder is meant for.
        /// </summary>
        public Type Component { get; }

        /// <summary>
        /// Gets or sets the button that triggers creating the custom type with the defined generic arguments.<br/>
        /// Assigning the <see cref="ComponentSelector._customGenericTypeLabel">Label</see>
        /// and <see cref="ComponentSelector._customGenericTypeColor">Color</see>
        /// drives is handled by the event source.
        /// </summary>
        public Button? CreateCustomTypeButton { get; set; }

        public Type[] GenericArguments { get; }
        public IEnumerable<Button> OtherAddedButtons => OtherAddedButtonsSet.AsSafeEnumerable();
        public ComponentSelector Selector { get; }

        internal BuildCustomGenericBuilder(ComponentSelector selector, UIBuilder ui, Type component) : base(ui)
        {
            Selector = selector;
            Component = component;
            GenericArguments = component.GetGenericArguments();
        }

        public bool AddOtherButton(Button button)
            => OtherAddedButtonsSet.Add(button);

        public bool HasOtherButton(Button button)
            => OtherAddedButtonsSet.Contains(button);

        public bool RemoveOtherButton(Button button)
            => OtherAddedButtonsSet.Remove(button);
    }
}