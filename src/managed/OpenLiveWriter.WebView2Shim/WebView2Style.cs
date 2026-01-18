// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using mshtml;

namespace OpenLiveWriter.WebView2Shim
{
    /// <summary>
    /// WebView2 implementation of IHTMLStyle that delegates style property access to JavaScript.
    /// </summary>
    public class WebView2Style : IHTMLStyle, IHTMLStyle2
    {
        private readonly WebView2Bridge _bridge;
        private readonly string _elementId;

        public WebView2Style(WebView2Bridge bridge, string elementId)
        {
            _bridge = bridge;
            _elementId = elementId;
        }

        private string GetStyle(string prop) => _bridge.ElementGetStyleProperty(_elementId, prop);
        private void SetStyle(string prop, string value) => _bridge.ElementSetStyleProperty(_elementId, prop, value);

        // ===== IHTMLStyle Common Properties =====

        public string fontFamily { get => GetStyle("fontFamily"); set => SetStyle("fontFamily", value); }
        public string fontStyle { get => GetStyle("fontStyle"); set => SetStyle("fontStyle", value); }
        public string fontVariant { get => GetStyle("fontVariant"); set => SetStyle("fontVariant", value); }
        public string fontWeight { get => GetStyle("fontWeight"); set => SetStyle("fontWeight", value); }
        public object fontSize { get => GetStyle("fontSize"); set => SetStyle("fontSize", value?.ToString()); }
        public string font { get => GetStyle("font"); set => SetStyle("font", value); }
        
        public string color { get => GetStyle("color"); set => SetStyle("color", value); }
        public string background { get => GetStyle("background"); set => SetStyle("background", value); }
        public string backgroundColor { get => GetStyle("backgroundColor"); set => SetStyle("backgroundColor", value); }
        public string backgroundImage { get => GetStyle("backgroundImage"); set => SetStyle("backgroundImage", value); }
        public string backgroundRepeat { get => GetStyle("backgroundRepeat"); set => SetStyle("backgroundRepeat", value); }
        public string backgroundAttachment { get => GetStyle("backgroundAttachment"); set => SetStyle("backgroundAttachment", value); }
        public string backgroundPosition { get => GetStyle("backgroundPosition"); set => SetStyle("backgroundPosition", value); }
        public object backgroundPositionX { get => GetStyle("backgroundPositionX"); set => SetStyle("backgroundPositionX", value?.ToString()); }
        public object backgroundPositionY { get => GetStyle("backgroundPositionY"); set => SetStyle("backgroundPositionY", value?.ToString()); }

        public object wordSpacing { get => GetStyle("wordSpacing"); set => SetStyle("wordSpacing", value?.ToString()); }
        public object letterSpacing { get => GetStyle("letterSpacing"); set => SetStyle("letterSpacing", value?.ToString()); }
        public string textDecoration { get => GetStyle("textDecoration"); set => SetStyle("textDecoration", value); }
        public bool textDecorationNone { get => GetStyle("textDecoration") == "none"; set { if (value) SetStyle("textDecoration", "none"); } }
        public bool textDecorationUnderline { get => GetStyle("textDecoration")?.Contains("underline") ?? false; set => SetStyle("textDecoration", value ? "underline" : "none"); }
        public bool textDecorationOverline { get => GetStyle("textDecoration")?.Contains("overline") ?? false; set => SetStyle("textDecoration", value ? "overline" : "none"); }
        public bool textDecorationLineThrough { get => GetStyle("textDecoration")?.Contains("line-through") ?? false; set => SetStyle("textDecoration", value ? "line-through" : "none"); }
        public bool textDecorationBlink { get => false; set { } } // blink is deprecated
        public object verticalAlign { get => GetStyle("verticalAlign"); set => SetStyle("verticalAlign", value?.ToString()); }
        public string textTransform { get => GetStyle("textTransform"); set => SetStyle("textTransform", value); }
        public string textAlign { get => GetStyle("textAlign"); set => SetStyle("textAlign", value); }
        public object textIndent { get => GetStyle("textIndent"); set => SetStyle("textIndent", value?.ToString()); }
        public object lineHeight { get => GetStyle("lineHeight"); set => SetStyle("lineHeight", value?.ToString()); }

        public object marginTop { get => GetStyle("marginTop"); set => SetStyle("marginTop", value?.ToString()); }
        public object marginRight { get => GetStyle("marginRight"); set => SetStyle("marginRight", value?.ToString()); }
        public object marginBottom { get => GetStyle("marginBottom"); set => SetStyle("marginBottom", value?.ToString()); }
        public object marginLeft { get => GetStyle("marginLeft"); set => SetStyle("marginLeft", value?.ToString()); }
        public string margin { get => GetStyle("margin"); set => SetStyle("margin", value); }

        public object paddingTop { get => GetStyle("paddingTop"); set => SetStyle("paddingTop", value?.ToString()); }
        public object paddingRight { get => GetStyle("paddingRight"); set => SetStyle("paddingRight", value?.ToString()); }
        public object paddingBottom { get => GetStyle("paddingBottom"); set => SetStyle("paddingBottom", value?.ToString()); }
        public object paddingLeft { get => GetStyle("paddingLeft"); set => SetStyle("paddingLeft", value?.ToString()); }
        public string padding { get => GetStyle("padding"); set => SetStyle("padding", value); }

        public string border { get => GetStyle("border"); set => SetStyle("border", value); }
        public string borderTop { get => GetStyle("borderTop"); set => SetStyle("borderTop", value); }
        public string borderRight { get => GetStyle("borderRight"); set => SetStyle("borderRight", value); }
        public string borderBottom { get => GetStyle("borderBottom"); set => SetStyle("borderBottom", value); }
        public string borderLeft { get => GetStyle("borderLeft"); set => SetStyle("borderLeft", value); }
        public string borderColor { get => GetStyle("borderColor"); set => SetStyle("borderColor", value); }
        public string borderTopColor { get => GetStyle("borderTopColor"); set => SetStyle("borderTopColor", value); }
        public string borderRightColor { get => GetStyle("borderRightColor"); set => SetStyle("borderRightColor", value); }
        public string borderBottomColor { get => GetStyle("borderBottomColor"); set => SetStyle("borderBottomColor", value); }
        public string borderLeftColor { get => GetStyle("borderLeftColor"); set => SetStyle("borderLeftColor", value); }
        public string borderWidth { get => GetStyle("borderWidth"); set => SetStyle("borderWidth", value); }
        public object borderTopWidth { get => GetStyle("borderTopWidth"); set => SetStyle("borderTopWidth", value?.ToString()); }
        public object borderRightWidth { get => GetStyle("borderRightWidth"); set => SetStyle("borderRightWidth", value?.ToString()); }
        public object borderBottomWidth { get => GetStyle("borderBottomWidth"); set => SetStyle("borderBottomWidth", value?.ToString()); }
        public object borderLeftWidth { get => GetStyle("borderLeftWidth"); set => SetStyle("borderLeftWidth", value?.ToString()); }
        public string borderStyle { get => GetStyle("borderStyle"); set => SetStyle("borderStyle", value); }
        public string borderTopStyle { get => GetStyle("borderTopStyle"); set => SetStyle("borderTopStyle", value); }
        public string borderRightStyle { get => GetStyle("borderRightStyle"); set => SetStyle("borderRightStyle", value); }
        public string borderBottomStyle { get => GetStyle("borderBottomStyle"); set => SetStyle("borderBottomStyle", value); }
        public string borderLeftStyle { get => GetStyle("borderLeftStyle"); set => SetStyle("borderLeftStyle", value); }

        public object width { get => GetStyle("width"); set => SetStyle("width", value?.ToString()); }
        public object height { get => GetStyle("height"); set => SetStyle("height", value?.ToString()); }
        public string styleFloat { get => GetStyle("cssFloat"); set => SetStyle("cssFloat", value); }
        public string clear { get => GetStyle("clear"); set => SetStyle("clear", value); }
        public string display { get => GetStyle("display"); set => SetStyle("display", value); }
        public string visibility { get => GetStyle("visibility"); set => SetStyle("visibility", value); }
        public string listStyleType { get => GetStyle("listStyleType"); set => SetStyle("listStyleType", value); }
        public string listStylePosition { get => GetStyle("listStylePosition"); set => SetStyle("listStylePosition", value); }
        public string listStyleImage { get => GetStyle("listStyleImage"); set => SetStyle("listStyleImage", value); }
        public string listStyle { get => GetStyle("listStyle"); set => SetStyle("listStyle", value); }
        public string whiteSpace { get => GetStyle("whiteSpace"); set => SetStyle("whiteSpace", value); }

        public object top { get => GetStyle("top"); set => SetStyle("top", value?.ToString()); }
        public object left { get => GetStyle("left"); set => SetStyle("left", value?.ToString()); }
        public string position { get => GetStyle("position"); set => SetStyle("position", value); }
        public object zIndex { get => GetStyle("zIndex"); set => SetStyle("zIndex", value?.ToString()); }
        public string overflow { get => GetStyle("overflow"); set => SetStyle("overflow", value); }
        public string pageBreakBefore { get => GetStyle("pageBreakBefore"); set => SetStyle("pageBreakBefore", value); }
        public string pageBreakAfter { get => GetStyle("pageBreakAfter"); set => SetStyle("pageBreakAfter", value); }
        public string cssText { get => GetStyle("cssText"); set => SetStyle("cssText", value); }
        public int pixelTop { get => int.TryParse(GetStyle("top")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("top", value + "px"); }
        public int pixelLeft { get => int.TryParse(GetStyle("left")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("left", value + "px"); }
        public int pixelWidth { get => int.TryParse(GetStyle("width")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("width", value + "px"); }
        public int pixelHeight { get => int.TryParse(GetStyle("height")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("height", value + "px"); }
        public float posTop { get => float.TryParse(GetStyle("top")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("top", value + "px"); }
        public float posLeft { get => float.TryParse(GetStyle("left")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("left", value + "px"); }
        public float posWidth { get => float.TryParse(GetStyle("width")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("width", value + "px"); }
        public float posHeight { get => float.TryParse(GetStyle("height")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("height", value + "px"); }
        public string cursor { get => GetStyle("cursor"); set => SetStyle("cursor", value); }
        public string clip { get => GetStyle("clip"); set => SetStyle("clip", value); }
        public string filter { get => GetStyle("filter"); set => SetStyle("filter", value); }

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

        public override string ToString() => cssText;

        // ===== IHTMLStyle2 Properties =====

        public string tableLayout { get => GetStyle("tableLayout"); set => SetStyle("tableLayout", value); }
        public string borderCollapse { get => GetStyle("borderCollapse"); set => SetStyle("borderCollapse", value); }
        public string direction { get => GetStyle("direction"); set => SetStyle("direction", value); }
        public string behavior { get => GetStyle("behavior"); set => SetStyle("behavior", value); }

        public void setExpression(string propname, string expression, string language = "") { /* Not supported */ }
        public string getExpression(string propname) => null;
        public bool removeExpression(string propname) => false;

        public string msInterpolationMode { get => GetStyle("imageRendering"); set => SetStyle("imageRendering", value); }
        public string unicodeBidi { get => GetStyle("unicodeBidi"); set => SetStyle("unicodeBidi", value); }
        public object bottom { get => GetStyle("bottom"); set => SetStyle("bottom", value?.ToString()); }
        public object right { get => GetStyle("right"); set => SetStyle("right", value?.ToString()); }
        public int pixelBottom { get => int.TryParse(GetStyle("bottom")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("bottom", value + "px"); }
        public int pixelRight { get => int.TryParse(GetStyle("right")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("right", value + "px"); }
        public float posBottom { get => float.TryParse(GetStyle("bottom")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("bottom", value + "px"); }
        public float posRight { get => float.TryParse(GetStyle("right")?.Replace("px", ""), out var v) ? v : 0; set => SetStyle("right", value + "px"); }
        public string imeMode { get => GetStyle("imeMode"); set => SetStyle("imeMode", value); }
        public string rubyAlign { get => GetStyle("rubyAlign"); set => SetStyle("rubyAlign", value); }
        public string rubyPosition { get => GetStyle("rubyPosition"); set => SetStyle("rubyPosition", value); }
        public string rubyOverhang { get => GetStyle("rubyOverhang"); set => SetStyle("rubyOverhang", value); }
        public string layoutGridChar { get => GetStyle("layoutGridChar"); set => SetStyle("layoutGridChar", value); }
        public string layoutGridLine { get => GetStyle("layoutGridLine"); set => SetStyle("layoutGridLine", value); }
        public string layoutGridMode { get => GetStyle("layoutGridMode"); set => SetStyle("layoutGridMode", value); }
        public string layoutGridType { get => GetStyle("layoutGridType"); set => SetStyle("layoutGridType", value); }
        public string layoutGrid { get => GetStyle("layoutGrid"); set => SetStyle("layoutGrid", value); }
        public string wordBreak { get => GetStyle("wordBreak"); set => SetStyle("wordBreak", value); }
        public string lineBreak { get => GetStyle("lineBreak"); set => SetStyle("lineBreak", value); }
        public string textJustify { get => GetStyle("textJustify"); set => SetStyle("textJustify", value); }
        public string textJustifyTrim { get => GetStyle("textJustifyTrim"); set => SetStyle("textJustifyTrim", value); }
        public string textKashida { get => GetStyle("textKashida"); set => SetStyle("textKashida", value); }
        public string textAutospace { get => GetStyle("textAutospace"); set => SetStyle("textAutospace", value); }
        public string overflowX { get => GetStyle("overflowX"); set => SetStyle("overflowX", value); }
        public string overflowY { get => GetStyle("overflowY"); set => SetStyle("overflowY", value); }
        public string accelerator { get => GetStyle("accelerator"); set => SetStyle("accelerator", value); }
    }
}
