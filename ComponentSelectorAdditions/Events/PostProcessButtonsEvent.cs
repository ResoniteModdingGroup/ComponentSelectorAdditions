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
    /// Represents the event data for the Post-Process Buttons Event.
    /// </summary>
    /// <remarks>
    /// Fired to post-process all the <see cref="Button"/>s in the <see cref="ComponentSelector">selector</see>'s results.
    /// </remarks>
    public sealed class PostProcessButtonsEvent : BuildUIEvent
    {
        private readonly Button[] _addButtons;
        private readonly Button[] _categoryButtons;
        private readonly Button[] _genericButtons;
        private readonly Button[] _groupButtons;

        /// <summary>
        /// Gets all buttons that directly add a component.
        /// </summary>
        public IEnumerable<Button> AddButtons => _addButtons;

        /// <summary>
        /// Gets the back button if it's part of the results.
        /// </summary>
        public Button? BackButton { get; }

        /// <summary>
        /// Gets the cancel button if it's part of the results.
        /// </summary>
        public Button? CancelButton { get; }

        /// <summary>
        /// Gets all buttons that open a category.
        /// </summary>
        public IEnumerable<Button> CategoryButtons => _categoryButtons;

        /// <summary>
        /// Gets the button that adds the constructed custom generic component, if available.
        /// </summary>
        public Button? CustomGenericButton { get; }

        /// <summary>
        /// Gets all buttons that open the custom generic builder page.
        /// </summary>
        public IEnumerable<Button> GenericButtons => _genericButtons;

        /// <summary>
        /// Gets all buttons that open a group of components.
        /// </summary>
        public IEnumerable<Button> GroupButtons => _groupButtons;

        /// <summary>
        /// Gets whether there is a <see cref="BackButton">back button</see>.
        /// </summary>
        /// <value><see langword="true"/> if it's <see langword="not"/> <see langword="null"/>; otherwise, <see langword="false"/>.</value>
        [MemberNotNullWhen(true, nameof(BackButton))]
        public bool HasBackButton => BackButton != null;

        /// <summary>
        /// Gets whether there is a <see cref="CancelButton">cancel button</see>.
        /// </summary>
        /// <value><see langword="true"/> if it's <see langword="not"/> <see langword="null"/>; otherwise, <see langword="false"/>.</value>
        [MemberNotNullWhen(true, nameof(CancelButton))]
        public bool HasCancelButton => CancelButton != null;

        /// <summary>
        /// Gets whether there is a <see cref="CustomGenericButton">custom generic button</see>.
        /// </summary>
        /// <value><see langword="true"/> if it's <see langword="not"/> <see langword="null"/>; otherwise, <see langword="false"/>.</value>
        [MemberNotNullWhen(true, nameof(CustomGenericButton))]
        public bool HasCustomGenericButton => CustomGenericButton != null;

        /// <summary>
        /// Gets the current path of the <see cref="ComponentSelector">selector</see>.
        /// </summary>
        public SelectorPath Path { get; }

        /// <summary>
        /// Gets the selector that results should be enumerated for.
        /// </summary>
        public ComponentSelector Selector { get; }

        internal PostProcessButtonsEvent(ComponentSelector selector, SelectorPath path, UIBuilder ui, Button? backButton, Button? customGenericButton, Button? cancelButton, HashSet<Button>? otherAddedButtons)
            : base(ui)
        {
            Selector = selector;
            Path = path;

            BackButton = backButton;
            CustomGenericButton = customGenericButton;
            CancelButton = cancelButton;

            var buttons = selector._uiRoot.Target
                .GetComponentsInChildren<Button>(button => button != backButton && button != cancelButton && button != customGenericButton && (otherAddedButtons is null || !otherAddedButtons.Contains(button)))
                .Select(button => (Button: button, Relay: button.Slot.GetComponent<ButtonRelay<string>>()))
                .Where(data => data.Relay != null)
                .ToArray();

            _categoryButtons = buttons.Where(data => data.Relay.ButtonPressed.Target == selector.OnOpenCategoryPressed)
                .Select(data => data.Button)
                .ToArray();

            _addButtons = buttons.Where(data => data.Relay.ButtonPressed.Target == selector.OnAddComponentPressed)
                .Select(data => data.Button)
                .ToArray();

            _genericButtons = buttons.Where(data => data.Relay.ButtonPressed.Target == selector.OpenGenericTypesPressed)
                .Select(data => data.Button)
                .ToArray();

            _groupButtons = buttons.Where(data => data.Relay.ButtonPressed.Target == selector.OpenGroupPressed)
                .Select(data => data.Button)
                .ToArray();
        }
    }
}