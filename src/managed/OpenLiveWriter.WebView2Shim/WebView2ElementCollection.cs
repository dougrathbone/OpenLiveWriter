// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections;

namespace OpenLiveWriter.WebView2Shim
{
    /// <summary>
    /// WebView2 element collection - standalone class for now.
    /// TODO: Implement IHTMLElementCollection once all members are added.
    /// </summary>
    public class WebView2ElementCollection : IEnumerable
    {
        private readonly WebView2Bridge _bridge;
        private readonly string[] _elementIds;
        private readonly WebView2Document _document;

        public WebView2ElementCollection(WebView2Bridge bridge, string[] elementIds, WebView2Document document)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _elementIds = elementIds ?? Array.Empty<string>();
            _document = document;
        }

        public int length
        {
            get => _elementIds.Length;
            set { /* MSHTML allows setting but we ignore it */ }
        }

        public object item(object name = null, object index = null)
        {
            // If name is a number, treat as index
            if (name is int i)
            {
                return GetElementAt(i);
            }
            
            // If index is provided, name is a string to match
            if (index is int idx && name is string nameStr)
            {
                int matchCount = 0;
                foreach (var id in _elementIds)
                {
                    var el = new WebView2Element(_bridge, id) { ParentDocument = _document };
                    if (el.id == nameStr || el.GetAttribute("name")?.ToString() == nameStr)
                    {
                        if (matchCount == idx)
                            return el;
                        matchCount++;
                    }
                }
                return null;
            }
            
            // If only name is a string, return first match or collection of matches
            if (name is string str)
            {
                WebView2Element firstMatch = null;
                int matchCount = 0;
                
                foreach (var id in _elementIds)
                {
                    var el = new WebView2Element(_bridge, id) { ParentDocument = _document };
                    if (el.id == str || el.GetAttribute("name")?.ToString() == str)
                    {
                        if (firstMatch == null) firstMatch = el;
                        matchCount++;
                    }
                }
                
                if (matchCount == 0) return null;
                if (matchCount == 1) return firstMatch;
                
                // Multiple matches - return new collection
                var matchIds = new System.Collections.Generic.List<string>();
                foreach (var id in _elementIds)
                {
                    var el = new WebView2Element(_bridge, id) { ParentDocument = _document };
                    if (el.id == str || el.GetAttribute("name")?.ToString() == str)
                    {
                        matchIds.Add(id);
                    }
                }
                return new WebView2ElementCollection(_bridge, matchIds.ToArray(), _document);
            }
            
            return null;
        }

        public object tags(object tagName)
        {
            if (tagName == null) return this;
            
            string tag = tagName.ToString().ToUpperInvariant();
            var matchIds = new System.Collections.Generic.List<string>();
            
            foreach (var id in _elementIds)
            {
                var el = new WebView2Element(_bridge, id) { ParentDocument = _document };
                if (el.tagName?.ToUpperInvariant() == tag)
                {
                    matchIds.Add(id);
                }
            }
            
            return new WebView2ElementCollection(_bridge, matchIds.ToArray(), _document);
        }

        private WebView2Element GetElementAt(int index)
        {
            if (index < 0 || index >= _elementIds.Length)
                return null;
            return new WebView2Element(_bridge, _elementIds[index]) { ParentDocument = _document };
        }

        // IEnumerable implementation for foreach support
        public IEnumerator GetEnumerator()
        {
            return new WebView2ElementEnumerator(_bridge, _elementIds, _document);
        }

        private class WebView2ElementEnumerator : IEnumerator
        {
            private readonly WebView2Bridge _bridge;
            private readonly string[] _elementIds;
            private readonly WebView2Document _document;
            private int _index = -1;

            public WebView2ElementEnumerator(WebView2Bridge bridge, string[] elementIds, WebView2Document document)
            {
                _bridge = bridge;
                _elementIds = elementIds;
                _document = document;
            }

            public object Current
            {
                get
                {
                    if (_index < 0 || _index >= _elementIds.Length)
                        throw new InvalidOperationException();
                    return new WebView2Element(_bridge, _elementIds[_index]) { ParentDocument = _document };
                }
            }

            public bool MoveNext()
            {
                _index++;
                return _index < _elementIds.Length;
            }

            public void Reset()
            {
                _index = -1;
            }
        }

        // Indexer for convenience
        public WebView2Element this[int index] => GetElementAt(index);

        public string toString() => $"[object HTMLCollection]";

        public override string ToString() => $"WebView2ElementCollection[{_elementIds.Length} elements]";
    }
}
