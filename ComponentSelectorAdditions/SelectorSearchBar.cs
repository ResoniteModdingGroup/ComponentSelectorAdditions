using FrooxEngine;
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

        public SelectorPath LastPath { get; set; }

        public Slot Search { get; }

        public Text Text => (Text)Editor.Text.Target;

        public SelectorData(Slot search, TextEditor editor)
        {
            Search = search;
            Editor = editor;

            LastPath = new SelectorPath(null, false, null, true);
        }

        public CancellationToken UpdateSearch()
        {
            _lastResultUpdate.Cancel();
            _lastResultUpdate = new CancellationTokenSource();

            return _lastResultUpdate.Token;
        }
    }
}