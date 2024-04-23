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
    public sealed class SelectorSearchBar
    {
        private CancellationTokenSource _lastResultUpdate = new();

        public bool Active
        {
            get => Root.ActiveSelf;
            set => Root.ActiveSelf = value;
        }

        public string? Content
        {
            get => Active ? Text.Content.Value : null;
            set => Text.Content.Value = value;
        }

        public TextEditor Editor { get; }
        public Slot Root { get; }

        public int SearchRefreshDelay { get; }
        public Text Text => (Text)Editor.Text.Target;

        public SelectorSearchBar(Slot root, TextEditor editor, int searchRefreshDelay)
        {
            Root = root;
            Editor = editor;
            SearchRefreshDelay = searchRefreshDelay;
        }

        public CancellationToken UpdateSearch()
        {
            _lastResultUpdate.Cancel();
            _lastResultUpdate = new CancellationTokenSource();

            return _lastResultUpdate.Token;
        }
    }
}