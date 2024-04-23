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
        private CancellationTokenSource _lastResultUpdate = new CancellationTokenSource();

        public TextEditor Editor { get; }

        public Slot Search { get; }

        public int SearchRefreshDelay { get; }
        public Text Text => (Text)Editor.Text.Target;

        public SelectorSearchBar(Slot search, TextEditor editor, int searchRefreshDelay)
        {
            Search = search;
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