// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;

namespace OpenLiveWriter.WebView2Shim
{
    /// <summary>
    /// WebView2 style wrapper - standalone class for now.
    /// TODO: Implement IHTMLStyle interface once all members are added.
    /// </summary>
    public class WebView2Style
    {
        private readonly WebView2Bridge _bridge;
        private readonly string _elementId;

        public WebView2Style(WebView2Bridge bridge, string elementId)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _elementId = elementId ?? throw new ArgumentNullException(nameof(elementId));
        }

        // ===== Helper Methods =====

        public string GetStyle(string property)
        {
            return _bridge.ElementGetStyleProperty(_elementId, property);
        }

        public void SetStyle(string property, string value)
        {
            _bridge.ElementSetStyleProperty(_elementId, property, value);
        }

        // ===== Font Properties =====

        public string fontFamily { get => GetStyle("fontFamily"); set => SetStyle("fontFamily", value); }
        public string fontStyle { get => GetStyle("fontStyle"); set => SetStyle("fontStyle", value); }
        public string fontVariant { get => GetStyle("fontVariant"); set => SetStyle("fontVariant", value); }
        public string fontWeight { get => GetStyle("fontWeight"); set => SetStyle("fontWeight", value); }
        public string fontSize { get => GetStyle("fontSize"); set => SetStyle("fontSize", value); }
        public string font { get => GetStyle("font"); set => SetStyle("font", value); }

        // ===== Color Properties =====

        public string color { get => GetStyle("color"); set => SetStyle("color", value); }
        public string background { get => GetStyle("background"); set => SetStyle("background", value); }
        public string backgroundColor { get => GetStyle("backgroundColor"); set => SetStyle("backgroundColor", value); }
        public string backgroundImage { get => GetStyle("backgroundImage"); set => SetStyle("backgroundImage", value); }
        public string backgroundRepeat { get => GetStyle("backgroundRepeat"); set => SetStyle("backgroundRepeat", value); }
        public string backgroundAttachment { get => GetStyle("backgroundAttachment"); set => SetStyle("backgroundAttachment", value); }
        public string backgroundPosition { get => GetStyle("backgroundPosition"); set => SetStyle("backgroundPosition", value); }

        // ===== Text Properties =====

        public string textDecoration { get => GetStyle("textDecoration"); set => SetStyle("textDecoration", value); }
        public string verticalAlign { get => GetStyle("verticalAlign"); set => SetStyle("verticalAlign", value); }
        public string textTransform { get => GetStyle("textTransform"); set => SetStyle("textTransform", value); }
        public string textAlign { get => GetStyle("textAlign"); set => SetStyle("textAlign", value); }
        public string textIndent { get => GetStyle("textIndent"); set => SetStyle("textIndent", value); }
        public string lineHeight { get => GetStyle("lineHeight"); set => SetStyle("lineHeight", value); }

        // ===== Margin & Padding =====

        public string marginTop { get => GetStyle("marginTop"); set => SetStyle("marginTop", value); }
        public string marginRight { get => GetStyle("marginRight"); set => SetStyle("marginRight", value); }
        public string marginBottom { get => GetStyle("marginBottom"); set => SetStyle("marginBottom", value); }
        public string marginLeft { get => GetStyle("marginLeft"); set => SetStyle("marginLeft", value); }
        public string margin { get => GetStyle("margin"); set => SetStyle("margin", value); }

        public string paddingTop { get => GetStyle("paddingTop"); set => SetStyle("paddingTop", value); }
        public string paddingRight { get => GetStyle("paddingRight"); set => SetStyle("paddingRight", value); }
        public string paddingBottom { get => GetStyle("paddingBottom"); set => SetStyle("paddingBottom", value); }
        public string paddingLeft { get => GetStyle("paddingLeft"); set => SetStyle("paddingLeft", value); }
        public string padding { get => GetStyle("padding"); set => SetStyle("padding", value); }

        // ===== Border Properties =====

        public string border { get => GetStyle("border"); set => SetStyle("border", value); }
        public string borderTop { get => GetStyle("borderTop"); set => SetStyle("borderTop", value); }
        public string borderRight { get => GetStyle("borderRight"); set => SetStyle("borderRight", value); }
        public string borderBottom { get => GetStyle("borderBottom"); set => SetStyle("borderBottom", value); }
        public string borderLeft { get => GetStyle("borderLeft"); set => SetStyle("borderLeft", value); }

        public string borderColor { get => GetStyle("borderColor"); set => SetStyle("borderColor", value); }
        public string borderWidth { get => GetStyle("borderWidth"); set => SetStyle("borderWidth", value); }
        public string borderStyle { get => GetStyle("borderStyle"); set => SetStyle("borderStyle", value); }

        // ===== Positioning =====

        public string width { get => GetStyle("width"); set => SetStyle("width", value); }
        public string height { get => GetStyle("height"); set => SetStyle("height", value); }
        public string styleFloat { get => GetStyle("float"); set => SetStyle("float", value); }
        public string clear { get => GetStyle("clear"); set => SetStyle("clear", value); }

        public string display { get => GetStyle("display"); set => SetStyle("display", value); }
        public string visibility { get => GetStyle("visibility"); set => SetStyle("visibility", value); }

        public string top { get => GetStyle("top"); set => SetStyle("top", value); }
        public string left { get => GetStyle("left"); set => SetStyle("left", value); }
        public string right { get => GetStyle("right"); set => SetStyle("right", value); }
        public string bottom { get => GetStyle("bottom"); set => SetStyle("bottom", value); }

        public string position { get => GetStyle("position"); set => SetStyle("position", value); }
        public string zIndex { get => GetStyle("zIndex"); set => SetStyle("zIndex", value); }
        public string overflow { get => GetStyle("overflow"); set => SetStyle("overflow", value); }

        public string whiteSpace { get => GetStyle("whiteSpace"); set => SetStyle("whiteSpace", value); }

        public string cursor { get => GetStyle("cursor"); set => SetStyle("cursor", value); }

        public string cssText 
        { 
            get => _bridge.ExecuteScript<string>("OLW.util.getById(" + WebView2Bridge.JsonEncode(_elementId) + ")?.style.cssText ?? ''");
            set => _bridge.ExecuteScript("var el = OLW.util.getById(" + WebView2Bridge.JsonEncode(_elementId) + "); if(el) el.style.cssText = " + WebView2Bridge.JsonEncode(value ?? ""));
        }

        // ===== Pixel Values =====

        public int pixelTop
        {
            get => int.TryParse(GetStyle("top")?.Replace("px", ""), out var v) ? v : 0;
            set => SetStyle("top", value + "px");
        }

        public int pixelLeft
        {
            get => int.TryParse(GetStyle("left")?.Replace("px", ""), out var v) ? v : 0;
            set => SetStyle("left", value + "px");
        }

        public int pixelWidth
        {
            get => int.TryParse(GetStyle("width")?.Replace("px", ""), out var v) ? v : 0;
            set => SetStyle("width", value + "px");
        }

        public int pixelHeight
        {
            get => int.TryParse(GetStyle("height")?.Replace("px", ""), out var v) ? v : 0;
            set => SetStyle("height", value + "px");
        }

        public override string ToString() => cssText;
    }
}
