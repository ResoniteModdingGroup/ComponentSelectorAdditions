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
    /// <summary>
    /// Represents the event data for the Build Custom Generic Builder Event.
    /// </summary>
    /// <remarks>
    /// This is used to generate the UI for picking custom generic arguments for generic components.
    /// </remarks>
    public sealed class BuildCustomGenericBuilder : BuildUIEvent
    {
        internal readonly HashSet<Button> OtherAddedButtonsSet = new();

        /// <summary>
        /// Gets or sets whether a create custom type button has been added already during this event.
        /// </summary>
        /// <value><see langword="true"/> if a create custom type button has been added; otherwise, <see langword="false"/>.</value>
        [MemberNotNullWhen(true, nameof(CreateCustomTypeButton))]
        public bool AddsCreateCustomTypeButton => CreateCustomTypeButton is not null;

        /// <summary>
        /// Gets or sets whether the inputs for generic arguments have been added already during this event.
        /// </summary>
        /// <value><see langword="true"/> if the inputs for generic arguments have been added; otherwise, <see langword="false"/>.</value>
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

        /// <summary>
        /// Gets the generic arguments that inputs need to be generated for.
        /// </summary>
        public Type[] GenericArguments { get; }

        /// <summary>
        /// Gets the other buttons that have been added to the custom generic builder.
        /// </summary>
        public IEnumerable<Button> OtherAddedButtons => OtherAddedButtonsSet.AsSafeEnumerable();

        /// <summary>
        /// Gets the selector that the custom generic builder is being build for.
        /// </summary>
        public ComponentSelector Selector { get; }

        internal BuildCustomGenericBuilder(ComponentSelector selector, UIBuilder ui, Type component) : base(ui)
        {
            Selector = selector;
            Component = component;
            GenericArguments = component.GetGenericArguments();
        }

        /// <summary>
        /// Adds <see cref="OtherAddedButtons">another</see> button to the custom generic builder.
        /// </summary>
        /// <param name="button">The button to add.</param>
        /// <returns><see langword="true"/> if it was newly added; otherwise, <see langword="false"/>.</returns>
        public bool AddOtherButton(Button button)
            => OtherAddedButtonsSet.Add(button);

        /// <summary>
        /// Determines whether <see cref="OtherAddedButtons">another</see> button is already part of the custom generic builder.
        /// </summary>
        /// <param name="button">The button to check for.</param>
        /// <returns><see langword="true"/> if it was found; otherwise, <see langword="false"/>.</returns>
        public bool HasOtherButton(Button button)
            => OtherAddedButtonsSet.Contains(button);

        /// <summary>
        /// Removes <see cref="OtherAddedButtons">another</see> button from the custom generic builder.
        /// </summary>
        /// <param name="button">The button to remove.</param>
        /// <returns><see langword="true"/> if it was found and removed; otherwise, <see langword="false"/>.</returns>
        public bool RemoveOtherButton(Button button)
            => OtherAddedButtonsSet.Remove(button);
    }
}