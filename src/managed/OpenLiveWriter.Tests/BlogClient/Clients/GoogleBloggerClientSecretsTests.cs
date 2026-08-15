// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using OpenLiveWriter.BlogClient.Clients;

namespace OpenLiveWriter.Tests.BlogClient.Clients
{
    /// <summary>
    /// The Windows GitHub Actions installer shipped with an empty Blogger
    /// OAuth client_id, so Google returned "Missing required parameter:
    /// client_id" on Sign In (issue #1088). The secrets file is gitignored
    /// and synthesized at build time; an empty write from CI must never
    /// make it into the embedded resource.
    /// </summary>
    [TestFixture]
    public class GoogleBloggerClientSecretsTests
    {
        [Test]
        public void LoadClientSecrets_EmptyClientId_Throws()
        {
            using var stream = JsonStream("{ \"installed\": { \"client_id\": \"\", \"client_secret\": \"\" } }");
            var ex = Assert.Throws<InvalidOperationException>(() => GoogleBloggerv3Client.LoadClientSecrets(stream));
            Assert.That(ex.Message, Does.Contain("client_id"));
        }

        [Test]
        public void LoadClientSecrets_MissingClientId_Throws()
        {
            using var stream = JsonStream("{ \"installed\": { \"client_secret\": \"x\" } }");
            Assert.Throws<InvalidOperationException>(() => GoogleBloggerv3Client.LoadClientSecrets(stream));
        }

        [Test]
        public void LoadClientSecrets_ValidInstalledAppJson_ReturnsClientId()
        {
            using var stream = JsonStream("{ \"installed\": { \"client_id\": \"abc.apps.googleusercontent.com\", \"client_secret\": \"secret\" } }");
            var secrets = GoogleBloggerv3Client.LoadClientSecrets(stream);
            Assert.That(secrets.ClientId, Is.EqualTo("abc.apps.googleusercontent.com"));
            Assert.That(secrets.ClientSecret, Is.EqualTo("secret"));
        }

        [Test]
        public void EmbeddedSecrets_HaveNonEmptyClientId()
        {
            var assembly = typeof(GoogleBloggerv3Client).Assembly;
            var name = "OpenLiveWriter.BlogClient.Clients.GoogleBloggerv3Secrets.json";
            using var stream = assembly.GetManifestResourceStream(name);
            Assert.NotNull(stream, $"{name} must be embedded in {assembly.GetName().Name}");

            var secrets = GoogleBloggerv3Client.LoadClientSecrets(stream);
            Assert.That(secrets.ClientId, Is.Not.Null.And.Not.Empty,
                "an empty client_id is what Google reports as 'Missing required parameter: client_id'");
        }

        [Test]
        public void EmbeddedSecretsResource_IsPresentOnTheAssembly()
        {
            var names = typeof(GoogleBloggerv3Client).Assembly.GetManifestResourceNames();
            Assert.That(names, Has.Member("OpenLiveWriter.BlogClient.Clients.GoogleBloggerv3Secrets.json"),
                string.Join(Environment.NewLine, names));
        }

        private static MemoryStream JsonStream(string json)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }
}
