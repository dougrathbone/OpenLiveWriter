// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using mshtml;

namespace OpenLiveWriter.WebView2Shim
{
    /// <summary>
    /// WebView2 style wrapper implementing IHTMLStyle.
    /// Translates MSHTML interface calls to JavaScript DOM operations via WebView2.
    /// </summary>
    public class WebView2Style : IHTMLStyle
    {
        private readonly WebView2Bridge _bridge;
        private readonly string _elementId;

        public WebView2Style(WebView2Bridge bridge, string elementId)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _elementId = elementId ?? throw new ArgumentNullException(nameof(elementId));
        }

        // ===== Helper Methods =====

        private string GetStyle(string property)
        {
            return _bridge.ElementGetStyleProperty(_elementId, property);
        }

        private void SetStyle(string property, string value)
        {
            _bridge.ElementSetStyleProperty(_elementId, property, value);
        }

        // ===== IHTMLStyle Implementation =====
        // Note: Most properties in IHTMLStyle are typed as 'object' but actually expect strings or variant values

        public string fontFamily { get => GetStyle("fontFamily"); set => SetStyle("fontFamily", value); }
        public string fontStyle { get => GetStyle("fontStyle"); set => SetStyle("fontStyle", value); }
        public string fontVariant { get => GetStyle("fontVariant"); set => SetStyle("fontVariant", value); }
        public string fontWeight { get => GetStyle("fontWeight"); set => SetStyle("fontWeight", value); }
        object IHTMLStyle.fontSize { get => GetStyle("fontSize"); set => SetStyle("fontSize", value?.ToString()); }
        public string font { get => GetStyle("font"); set => SetStyle("font", value); }
        object IHTMLStyle.color { get => GetStyle("color"); set => SetStyle("color", value?.ToString()); }
        public string background { get => GetStyle("background"); set => SetStyle("background", value); }
        object IHTMLStyle.backgroundColor { get => GetStyle("backgroundColor"); set => SetStyle("backgroundColor", value?.ToString()); }
        public string backgroundImage { get => GetStyle("backgroundImage"); set => SetStyle("backgroundImage", value); }
        public string backgroundRepeat { get => GetStyle("backgroundRepeat"); set => SetStyle("backgroundRepeat", value); }
        public string backgroundAttachment { get => GetStyle("backgroundAttachment"); set => SetStyle("backgroundAttachment", value); }
        public string backgroundPosition { get => GetStyle("backgroundPosition"); set => SetStyle("backgroundPosition", value); }
        object IHTMLStyle.backgroundPositionX { get => GetStyle("backgroundPositionX"); set => SetStyle("backgroundPositionX", value?.ToString()); }
        object IHTMLStyle.backgroundPositionY { get => GetStyle("backgroundPositionY"); set => SetStyle("backgroundPositionY", value?.ToString()); }
        object IHTMLStyle.wordSpacing { get => GetStyle("wordSpacing"); set => SetStyle("wordSpacing", value?.ToString()); }
        object IHTMLStyle.letterSpacing { get => GetStyle("letterSpacing"); set => SetStyle("letterSpacing", value?.ToString()); }
        public string textDecoration { get => GetStyle("textDecoration"); set => SetStyle("textDecoration", value); }
        public bool textDecorationNone { get => GetStyle("textDecoration") == "none"; set { if (value) SetStyle("textDecoration", "none"); } }
        public bool textDecorationUnderline { get => (GetStyle("textDecoration") ?? "").Contains("underline"); set { /* complex logic */ } }
        public bool textDecorationOverline { get => (GetStyle("textDecoration") ?? "").Contains("overline"); set { /* complex logic */ } }
        public bool textDecorationLineThrough { get => (GetStyle("textDecoration") ?? "").Contains("line-through"); set { /* complex logic */ } }
        public bool textDecorationBlink { get => (GetStyle("textDecoration") ?? "").Contains("blink"); set { /* complex logic */ } }
        object IHTMLStyle.verticalAlign { get => GetStyle("verticalAlign"); set => SetStyle("verticalAlign", value?.ToString()); }
        public string textTransform { get => GetStyle("textTransform"); set => SetStyle("textTransform", value); }
        public string textAlign { get => GetStyle("textAlign"); set => SetStyle("textAlign", value); }
        object IHTMLStyle.textIndent { get => GetStyle("textIndent"); set => SetStyle("textIndent", value?.ToString()); }
        object IHTMLStyle.lineHeight { get => GetStyle("lineHeight"); set => SetStyle("lineHeight", value?.ToString()); }
        object IHTMLStyle.marginTop { get => GetStyle("marginTop"); set => SetStyle("marginTop", value?.ToString()); }
        object IHTMLStyle.marginRight { get => GetStyle("marginRight"); set => SetStyle("marginRight", value?.ToString()); }
        object IHTMLStyle.marginBottom { get => GetStyle("marginBottom"); set => SetStyle("marginBottom", value?.ToString()); }
        object IHTMLStyle.marginLeft { get => GetStyle("marginLeft"); set => SetStyle("marginLeft", value?.ToString()); }
        public string margin { get => GetStyle("margin"); set => SetStyle("margin", value); }
        object IHTMLStyle.paddingTop { get => GetStyle("paddingTop"); set => SetStyle("paddingTop", value?.ToString()); }
        object IHTMLStyle.paddingRight { get => GetStyle("paddingRight"); set => SetStyle("paddingRight", value?.ToString()); }
        object IHTMLStyle.paddingBottom { get => GetStyle("paddingBottom"); set => SetStyle("paddingBottom", value?.ToString()); }
        object IHTMLStyle.paddingLeft { get => GetStyle("paddingLeft"); set => SetStyle("paddingLeft", value?.ToString()); }
        public string padding { get => GetStyle("padding"); set => SetStyle("padding", value); }
        public string border { get => GetStyle("border"); set => SetStyle("border", value); }
        public string borderTop { get => GetStyle("borderTop"); set => SetStyle("borderTop", value); }
        public string borderRight { get => GetStyle("borderRight"); set => SetStyle("borderRight", value); }
        public string borderBottom { get => GetStyle("borderBottom"); set => SetStyle("borderBottom", value); }
        public string borderLeft { get => GetStyle("borderLeft"); set => SetStyle("borderLeft", value); }
        public string borderColor { get => GetStyle("borderColor"); set => SetStyle("borderColor", value); }
        object IHTMLStyle.borderTopColor { get => GetStyle("borderTopColor"); set => SetStyle("borderTopColor", value?.ToString()); }
        object IHTMLStyle.borderRightColor { get => GetStyle("borderRightColor"); set => SetStyle("borderRightColor", value?.ToString()); }
        object IHTMLStyle.borderBottomColor { get => GetStyle("borderBottomColor"); set => SetStyle("borderBottomColor", value?.ToString()); }
        object IHTMLStyle.borderLeftColor { get => GetStyle("borderLeftColor"); set => SetStyle("borderLeftColor", value?.ToString()); }
        public string borderWidth { get => GetStyle("borderWidth"); set => SetStyle("borderWidth", value); }
        object IHTMLStyle.borderTopWidth { get => GetStyle("borderTopWidth"); set => SetStyle("borderTopWidth", value?.ToString()); }
        object IHTMLStyle.borderRightWidth { get => GetStyle("borderRightWidth"); set => SetStyle("borderRightWidth", value?.ToString()); }
        object IHTMLStyle.borderBottomWidth { get => GetStyle("borderBottomWidth"); set => SetStyle("borderBottomWidth", value?.ToString()); }
        object IHTMLStyle.borderLeftWidth { get => GetStyle("borderLeftWidth"); set => SetStyle("borderLeftWidth", value?.ToString()); }
        public string borderStyle { get => GetStyle("borderStyle"); set => SetStyle("borderStyle", value); }
        public string borderTopStyle { get => GetStyle("borderTopStyle"); set => SetStyle("borderTopStyle", value); }
        public string borderRightStyle { get => GetStyle("borderRightStyle"); set => SetStyle("borderRightStyle", value); }
        public string borderBottomStyle { get => GetStyle("borderBottomStyle"); set => SetStyle("borderBottomStyle", value); }
        public string borderLeftStyle { get => GetStyle("borderLeftStyle"); set => SetStyle("borderLeftStyle", value); }
        object IHTMLStyle.width { get => GetStyle("width"); set => SetStyle("width", value?.ToString()); }
        object IHTMLStyle.height { get => GetStyle("height"); set => SetStyle("height", value?.ToString()); }
        public string styleFloat { get => GetStyle("float"); set => SetStyle("float", value); }
        public string clear { get => GetStyle("clear"); set => SetStyle("clear", value); }
        public string display { get => GetStyle("display"); set => SetStyle("display", value); }
        public string visibility { get => GetStyle("visibility"); set => SetStyle("visibility", value); }
        public string listStyleType { get => GetStyle("listStyleType"); set => SetStyle("listStyleType", value); }
        public string listStylePosition { get => GetStyle("listStylePosition"); set => SetStyle("listStylePosition", value); }
        public string listStyleImage { get => GetStyle("listStyleImage"); set => SetStyle("listStyleImage", value); }
        public string listStyle { get => GetStyle("listStyle"); set => SetStyle("listStyle", value); }
        public string whiteSpace { get => GetStyle("whiteSpace"); set => SetStyle("whiteSpace", value); }
        object IHTMLStyle.top { get => GetStyle("top"); set => SetStyle("top", value?.ToString()); }
        object IHTMLStyle.left { get => GetStyle("left"); set => SetStyle("left", value?.ToString()); }
        public string position => GetStyle("position");
        object IHTMLStyle.zIndex { get => GetStyle("zIndex"); set => SetStyle("zIndex", value?.ToString()); }
        public string overflow { get => GetStyle("overflow"); set => SetStyle("overflow", value); }
        public string pageBreakBefore { get => GetStyle("pageBreakBefore"); set => SetStyle("pageBreakBefore", value); }
        public string pageBreakAfter { get => GetStyle("pageBreakAfter"); set => SetStyle("pageBreakAfter", value); }
        public string cssText { get => _bridge.ExecuteScript<string>("OLW.util.getById(" + WebView2Bridge.JsonEncode(_elementId) + ")?.style.cssText ?? ''"); set => _bridge.ExecuteScript("var el = OLW.util.getById(" + WebView2Bridge.JsonEncode(_elementId) + "); if(el) el.style.cssText = " + WebView2Bridge.JsonEncode(value ?? "")); }
        public string cursor { get => GetStyle("cursor"); set => SetStyle("cursor", value); }
        public string clip { get => GetStyle("clip"); set => SetStyle("clip", value); }
        public string filter { get => GetStyle("filter"); set => SetStyle("filter", value); }

        // Pixel values
        public int pixelTop { get => int.TryParse(GetStyle("top")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("top", value + "px"); }
        public int pixelLeft { get => int.TryParse(GetStyle("left")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("left", value + "px"); }
        public int pixelWidth { get => int.TryParse(GetStyle("width")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("width", value + "px"); }
        public int pixelHeight { get => int.TryParse(GetStyle("height")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("height", value + "px"); }

        // Pos values (float)
        public float posTop { get => float.TryParse(GetStyle("top")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("top", value + "px"); }
        public float posLeft { get => float.TryParse(GetStyle("left")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("left", value + "px"); }
        public float posWidth { get => float.TryParse(GetStyle("width")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("width", value + "px"); }
        public float posHeight { get => float.TryParse(GetStyle("height")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("height", value + "px"); }

        // Methods
        public void setAttribute(string strAttributeName, object AttributeValue, int lFlags = 1)
        {
            SetStyle(strAttributeName, AttributeValue?.ToString());
        }

        public object getAttribute(string strAttributeName, int lFlags = 0)
        {
            return GetStyle(strAttributeName);
        }

        public bool removeAttribute(string strAttributeName, int lFlags = 1)
        {
            SetStyle(strAttributeName, null);
            return true;
        }

        // ===== Convenience properties (not in IHTMLStyle but used in codebase) =====

        public string fontSize { get => GetStyle("fontSize"); set => SetStyle("fontSize", value); }
        public string color { get => GetStyle("color"); set => SetStyle("color", value); }
        public string backgroundColor { get => GetStyle("backgroundColor"); set => SetStyle("backgroundColor", value); }
        public string marginTop { get => GetStyle("marginTop"); set => SetStyle("marginTop", value); }
        public string marginRight { get => GetStyle("marginRight"); set => SetStyle("marginRight", value); }
        public string marginBottom { get => GetStyle("marginBottom"); set => SetStyle("marginBottom", value); }
        public string marginLeft { get => GetStyle("marginLeft"); set => SetStyle("marginLeft", value); }
        public string paddingTop { get => GetStyle("paddingTop"); set => SetStyle("paddingTop", value); }
        public string paddingRight { get => GetStyle("paddingRight"); set => SetStyle("paddingRight", value); }
        public string paddingBottom { get => GetStyle("paddingBottom"); set => SetStyle("paddingBottom", value); }
        public string paddingLeft { get => GetStyle("paddingLeft"); set => SetStyle("paddingLeft", value); }
        public string width { get => GetStyle("width"); set => SetStyle("width", value); }
        public string height { get => GetStyle("height"); set => SetStyle("height", value); }
        public string verticalAlign { get => GetStyle("verticalAlign"); set => SetStyle("verticalAlign", value); }
        public string textIndent { get => GetStyle("textIndent"); set => SetStyle("textIndent", value); }
        public string lineHeight { get => GetStyle("lineHeight"); set => SetStyle("lineHeight", value); }
        public string top { get => GetStyle("top"); set => SetStyle("top", value); }
        public string left { get => GetStyle("left"); set => SetStyle("left", value); }
        public string right { get => GetStyle("right"); set => SetStyle("right", value); }
        public string bottom { get => GetStyle("bottom"); set => SetStyle("bottom", value); }
        public string zIndex { get => GetStyle("zIndex"); set => SetStyle("zIndex", value); }

        string IHTMLStyle.toString() => cssText;

        public override string ToString() => cssText;
    }
}
