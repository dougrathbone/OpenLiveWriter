// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using NUnit.Framework;
using OpenLiveWriter.BlogClient.Clients;
using OpenLiveWriter.BlogClient.Providers;
using OpenLiveWriter.CoreServices;
using OpenLiveWriter.Extensibility.BlogClient;

namespace OpenLiveWriter.UnitTest.BlogClient
{
    /// <summary>
    /// Regression tests for the scheduled-post timezone bugs: issue #262 (post
    /// date shifted because picker-local time was sent as UTC) and issue #1084
    /// (WordPress.com displaying the UTC value because dateCreated was not sent
    /// in blog-local time).
    /// </summary>
    [TestFixture]
    public class MetaweblogClientPostDateTests
    {
        private const string DateFormat = "yyyyMMdd'T'HH':'mm':'ss";

        // 18:00 "wall clock" time, exactly what the UI date picker produces.
        private static readonly DateTime PickedLocalTime = new DateTime(2026, 3, 14, 18, 0, 0);

        private sealed class TestMetaweblogClient : MetaweblogClient
        {
            public TestMetaweblogClient(bool useLocalTime)
                : base(new Uri("http://example.com/xmlrpc.php"), null)
            {
                OverrideOptions(new BlogClientOptions
                {
                    SupportsCustomDate = true,
                    UseLocalTime = useLocalTime,
                    PostDateFormat = DateFormat
                });
            }

            public string GetPostDateField(BlogPost post, string name)
            {
                ArrayList members = new ArrayList();
                GeneratePostDateFields(post, members);
                foreach (XmlRpcMember member in members)
                    if (member.Name == name)
                        return SerializeDate(member.Value);
                return null;
            }

            private static string SerializeDate(XmlRpcValue value)
            {
                StringBuilder sb = new StringBuilder();
                XmlWriterSettings settings = new XmlWriterSettings { OmitXmlDeclaration = true };
                using (XmlWriter writer = XmlWriter.Create(sb, settings))
                    value.Write(writer);
                string xml = sb.ToString();
                const string open = "<dateTime.iso8601>";
                const string close = "</dateTime.iso8601>";
                int start = xml.IndexOf(open, StringComparison.Ordinal) + open.Length;
                int end = xml.IndexOf(close, StringComparison.Ordinal);
                return xml.Substring(start, end - start);
            }
        }

        private static BlogPost PostWithPublishOverride(DateTime date)
        {
            BlogPost post = new BlogPost();
            post.DatePublishedOverride = date;
            return post;
        }

        private static string Format(DateTime date)
        {
            return date.ToString(DateFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The UTC instant the user meant, computed independently of the
        /// production code (DateTimeHelper.LocalToUtc) under test.
        /// </summary>
        private static DateTime ExpectedUtc(DateTime localTime)
        {
            return localTime - TimeZoneInfo.Local.GetUtcOffset(localTime);
        }

        [Test]
        public void UseLocalTime_SendsLocalDateCreatedAndTrueUtcGmt()
        {
            TestMetaweblogClient client = new TestMetaweblogClient(useLocalTime: true);
            BlogPost post = PostWithPublishOverride(PickedLocalTime);

            // WordPress displays dateCreated, so it must be the picker time verbatim (#1084).
            Assert.AreEqual(Format(PickedLocalTime), client.GetPostDateField(post, "dateCreated"));
            // date_created_gmt must be the true UTC equivalent, not the local time relabelled (#262).
            Assert.AreEqual(Format(ExpectedUtc(PickedLocalTime)), client.GetPostDateField(post, "date_created_gmt"));
        }

        [Test]
        public void UseUtcTime_SendsUtcInBothFields()
        {
            TestMetaweblogClient client = new TestMetaweblogClient(useLocalTime: false);
            BlogPost post = PostWithPublishOverride(PickedLocalTime);

            Assert.AreEqual(Format(ExpectedUtc(PickedLocalTime)), client.GetPostDateField(post, "dateCreated"));
            Assert.AreEqual(Format(ExpectedUtc(PickedLocalTime)), client.GetPostDateField(post, "date_created_gmt"));
        }

        [Test]
        public void GmtFieldDiffersFromLocalFieldByExactlyTheLocalUtcOffset()
        {
            TestMetaweblogClient client = new TestMetaweblogClient(useLocalTime: true);
            BlogPost post = PostWithPublishOverride(PickedLocalTime);

            DateTime dateCreated = DateTime.ParseExact(client.GetPostDateField(post, "dateCreated"), DateFormat, CultureInfo.InvariantCulture);
            DateTime dateCreatedGmt = DateTime.ParseExact(client.GetPostDateField(post, "date_created_gmt"), DateFormat, CultureInfo.InvariantCulture);

            Assert.AreEqual(TimeZoneInfo.Local.GetUtcOffset(PickedLocalTime), dateCreated - dateCreatedGmt);
        }

        [Test]
        public void NoDateOverride_NoDateFields()
        {
            TestMetaweblogClient client = new TestMetaweblogClient(useLocalTime: true);

            Assert.IsNull(client.GetPostDateField(new BlogPost(), "dateCreated"));
            Assert.IsNull(client.GetPostDateField(new BlogPost(), "date_created_gmt"));
        }

        /// <summary>
        /// Guards the provider configuration half of the #1084 fix: WordPress
        /// expects dateCreated in blog-local time, so every WordPress provider
        /// in the live provider list must opt into useLocalTime.
        /// </summary>
        [Test]
        public void WordPressProvidersOptIntoLocalTime()
        {
            var assembly = typeof(BlogProvider).Assembly;
            string resourceName = null;
            foreach (string name in assembly.GetManifestResourceNames())
                if (name.EndsWith("BlogProvidersB5.xml", StringComparison.Ordinal))
                    resourceName = name;
            Assert.NotNull(resourceName, "BlogProvidersB5.xml embedded resource not found");

            XmlDocument doc = new XmlDocument();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse }))
                doc.Load(reader);

            XmlNodeList providers = doc.SelectNodes("//provider");
            Assert.NotNull(providers);
            int wordPressProviders = 0;
            foreach (XmlNode provider in providers)
            {
                string name = provider.SelectSingleNode("name")?.InnerText;
                if (name == null || !name.StartsWith("WordPress", StringComparison.OrdinalIgnoreCase))
                    continue;
                wordPressProviders++;
                XmlNode useLocalTime = provider.SelectSingleNode("options/useLocalTime");
                Assert.NotNull(useLocalTime, name + " is missing <useLocalTime>");
                Assert.AreEqual("Yes", useLocalTime.InnerText, name + " must set <useLocalTime>Yes</useLocalTime>");
            }
            Assert.GreaterOrEqual(wordPressProviders, 1, "no WordPress providers found in BlogProvidersB5.xml");
        }
    }
}
