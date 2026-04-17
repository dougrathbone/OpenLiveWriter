// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenLiveWriter.CoreServices.HTML;

namespace OpenLiveWriter.UnitTest.CoreServices
{
    [TestClass]
    public class LightWeightHTMLThinner2Test
    {
        [TestMethod]
        public void StrictMode_PreservesBrClearAttribute()
        {
            string input = "<p>Hello</p><br clear=\"all\" /><p>World</p>";
            string result = LightWeightHTMLThinner2.Thin(input, true, true);
            Assert.IsTrue(result.Contains("clear=\"all\"") || result.Contains("clear=all"),
                "BR clear attribute should be preserved in strict mode. Got: " + result);
        }

        [TestMethod]
        public void NonStrictMode_PreservesBrClearAttribute()
        {
            string input = "<p>Hello</p><br clear=\"all\" /><p>World</p>";
            string result = LightWeightHTMLThinner2.Thin(input, true, false);
            Assert.IsTrue(result.Contains("clear=\"all\"") || result.Contains("clear=all"),
                "BR clear attribute should be preserved in non-strict mode. Got: " + result);
        }
    }
}
