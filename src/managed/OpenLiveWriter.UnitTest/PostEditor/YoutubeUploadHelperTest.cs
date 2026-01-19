// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using NUnit.Framework;
using OpenLiveWriter.PostEditor.Video.YouTube;

namespace OpenLiveWriter.UnitTest.PostEditor
{
    [TestFixture]
    public class YoutubeUploadHelperTest
    {
        [Test]
        public void TestAddSimpleHeaderSetsAuthorizationHeader()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
            YouTubeUploadRequestHelper.AddSimpleHeader(request, "Bearer test_token");

            Assert.That(request.Headers.Contains("Authorization"), Is.True);
            Assert.That(request.Headers.GetValues("Authorization"), Does.Contain("Bearer test_token"));
        }

        [Test]
        public void TestAddSimpleHeaderSetsGDataKey()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
            YouTubeUploadRequestHelper.AddSimpleHeader(request, "Bearer test_token");

            Assert.That(request.Headers.Contains("X-GData-Key"), Is.True);
        }

        [Test]
        public void TestCreateMultipartContentReturnsMultipartContent()
        {
            // Create a temporary test file
            string tempFile = Path.Combine(Path.GetTempPath(), "test_video.mp4");
            try
            {
                File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x01, 0x02 });

                var content = YouTubeUploadRequestHelper.CreateMultipartContent(
                    "Test Title",
                    "Test Description",
                    "test,video",
                    "22", // People & Blogs
                    "0", // Public
                    tempFile);

                Assert.That(content, Is.Not.Null);
                Assert.That(content, Is.InstanceOf<MultipartContent>());

                // Check content type contains multipart/related
                var contentType = content.Headers.ContentType;
                Assert.That(contentType, Is.Not.Null);
                Assert.That(contentType.MediaType, Is.EqualTo("multipart/related"));

                content.Dispose();
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Test]
        public void TestCreateMultipartContentWithPrivateVideo()
        {
            // Create a temporary test file
            string tempFile = Path.Combine(Path.GetTempPath(), "test_private_video.mp4");
            try
            {
                File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x01, 0x02 });

                var content = YouTubeUploadRequestHelper.CreateMultipartContent(
                    "Private Video",
                    "This is private",
                    "private,test",
                    "22",
                    "1", // Private
                    tempFile);

                Assert.That(content, Is.Not.Null);
                content.Dispose();
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Test]
        public void TestCreateMultipartContentIncludesXmlPart()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), "test_xml_video.mp4");
            try
            {
                File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x01 });

                using var content = YouTubeUploadRequestHelper.CreateMultipartContent(
                    "XML Test",
                    "Description",
                    "tags",
                    "22",
                    "0",
                    tempFile);

                // Read the content to string to verify XML is present
                var task = content.ReadAsStringAsync();
                task.Wait();
                string contentString = task.Result;

                // Should contain Atom XML
                Assert.That(contentString, Does.Contain("http://www.w3.org/2005/Atom"));
                Assert.That(contentString, Does.Contain("media:title"));
                Assert.That(contentString, Does.Contain("XML Test"));
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Test]
        public void TestCreateMultipartContentWithSpecialCharacters()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), "test_special_video.mp4");
            try
            {
                File.WriteAllBytes(tempFile, new byte[] { 0x00 });

                using var content = YouTubeUploadRequestHelper.CreateMultipartContent(
                    "Title with <special> & \"characters\"",
                    "Description with <xml> characters & more",
                    "tag1, tag2 <special>",
                    "22",
                    "0",
                    tempFile);

                Assert.That(content, Is.Not.Null);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Test]
        public void TestCreateMultipartContentWithDifferentFileExtensions()
        {
            foreach (string ext in new[] { ".mp4", ".avi", ".mov", ".wmv" })
            {
                string tempFile = Path.Combine(Path.GetTempPath(), $"test_video{ext}");
                try
                {
                    File.WriteAllBytes(tempFile, new byte[] { 0x00 });

                    using var content = YouTubeUploadRequestHelper.CreateMultipartContent(
                        "Test",
                        "Desc",
                        "tags",
                        "22",
                        "0",
                        tempFile);

                    Assert.That(content, Is.Not.Null, $"Failed for extension {ext}");
                }
                finally
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
            }
        }
    }
}
