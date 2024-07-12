using FrooxEngine;
using FrooxEngine.UIX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions
{
    /// <summary>
    /// Contains details about a <see cref="ComponentSelector"/>'s <see cref="SearchBar"/>.
    /// </summary>
    public sealed class SelectorSearchBar
    {
        private readonly Func<int> _getSearchRefreshDelay;
        private CancellationTokenSource _lastResultUpdate = new();

        /// <summary>
        /// Gets or sets whether the search bar is currently shown.
        /// </summary>
        public bool Active
        {
            get => Root.ActiveSelf;
            set => Root.ActiveSelf = value;
        }

        /// <summary>
        /// Gets or sets the search bar's current content.
        /// <c>null</c> when inactive.
        /// </summary>
        public string? Content
        {
            get => Active ? Text.Content.Value : null;
            set => Text.Content.Value = value;
        }

        /// <summary>
        /// Gets the search bar's <see cref="TextEditor"/>.
        /// </summary>
        public TextEditor Editor { get; }

        /// <summary>
        /// Gets the search bar's root <see cref="Slot"/>.
        /// </summary>
        public Slot Root { get; }

        /// <summary>
        /// Gets the search bar's delay before updating when its content changes.
        /// </summary>
        public int SearchRefreshDelay => _getSearchRefreshDelay();

        /// <summary>
        /// Gets the search bar's <see cref="Text"/> element.
        /// </summary>
        public Text Text => (Text)Editor.Text.Target;

        /// <summary>
        /// Creates a new search bar with the given details.
        /// </summary>
        /// <param name="root">The search bar's root slot.</param>
        /// <param name="editor">The search bar's text editor.</param>
        /// <param name="getSearchRefreshDelay">The search bar's delay before updating when its content changes.</param>
        public SelectorSearchBar(Slot root, TextEditor editor, Func<int> getSearchRefreshDelay)
        {
            Root = root;
            Editor = editor;
            _getSearchRefreshDelay = getSearchRefreshDelay ?? (() => 0);
        }

        /// <summary>
        /// Cancels the last triggered search and creates a new cancellation token for the new one.
        /// </summary>
        /// <returns>The newly created <see cref="CancellationToken"/> for the new search.</returns>
        public CancellationToken UpdateSearch()
        {
            _lastResultUpdate.Cancel();
            _lastResultUpdate = new CancellationTokenSource();

            return _lastResultUpdate.Token;
        }
    }
}