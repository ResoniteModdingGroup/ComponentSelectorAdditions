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
    public sealed class PostProcessButtonsEvent : BuildUIEvent
    {
        private readonly Button[] _addButtons;
        private readonly Button[] _categoryButtons;
        private readonly Button[] _genericButtons;
        private readonly Button[] _groupButtons;

        public IEnumerable<Button> AddButtons => _addButtons;
        public Button? BackButton { get; }
        public Button? CancelButton { get; }
        public IEnumerable<Button> CategoryButtons => _categoryButtons;
        public Button? CustomGenericButton { get; }

        public IEnumerable<Button> GenericButtons => _genericButtons;

        public IEnumerable<Button> GroupButtons => _groupButtons;

        [MemberNotNullWhen(true, nameof(BackButton))]
        public bool HasBackButton => BackButton != null;

        [MemberNotNullWhen(true, nameof(CancelButton))]
        public bool HasCancelButton => CancelButton != null;

        [MemberNotNullWhen(true, nameof(CustomGenericButton))]
        public bool HasCustomGenericButton => CustomGenericButton != null;

        public SelectorPath Path { get; }

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