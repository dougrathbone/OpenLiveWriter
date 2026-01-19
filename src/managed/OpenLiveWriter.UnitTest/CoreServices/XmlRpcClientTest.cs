// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenLiveWriter.CoreServices;

namespace OpenLiveWriter.UnitTest.CoreServices
{
    [TestFixture]
    public class XmlRpcClientTest
    {
        [Test]
        public void TestSimpleConstructor()
        {
            var client = new XmlRpcClient("http://example.com/xmlrpc", "TestAgent");
            Assert.That(client, Is.Not.Null);
            Assert.That(client.UsesHttpClient, Is.False);
        }

        [Test]
        public void TestHttpClientConstructor()
        {
            // Explicitly type the Action to avoid ambiguity with HttpRequestFilter
            Action<HttpRequestMessage> configurator = request => request.Headers.Add("X-Custom", "value");
            var client = new XmlRpcClient(
                "http://example.com/xmlrpc",
                "TestAgent",
                configurator,
                "utf-8");
            Assert.That(client, Is.Not.Null);
            Assert.That(client.UsesHttpClient, Is.True);
        }

        [Test]
        public void TestLegacyConstructorWithFilter()
        {
            HttpRequestFilter filter = request => { request.Headers.Add("X-Custom", "value"); };
            var client = new XmlRpcClient(
                "http://example.com/xmlrpc",
                "TestAgent",
                filter,
                "utf-8");
            Assert.That(client, Is.Not.Null);
            Assert.That(client.UsesHttpClient, Is.False);
        }

        [Test]
        public void TestXmlRpcString()
        {
            var value = new XmlRpcString("test value");
            Assert.That(value, Is.Not.Null);
        }

        [Test]
        public void TestXmlRpcStringWithSuppressLog()
        {
            var value = new XmlRpcString("password", true);
            Assert.That(value, Is.Not.Null);
        }

        [Test]
        public void TestXmlRpcInt()
        {
            var value = new XmlRpcInt(42);
            Assert.That(value, Is.Not.Null);
        }

        [Test]
        public void TestXmlRpcBoolean()
        {
            var value = new XmlRpcBoolean(true);
            Assert.That(value, Is.Not.Null);
        }

        [Test]
        public void TestXmlRpcArray()
        {
            var values = new XmlRpcValue[]
            {
                new XmlRpcString("item1"),
                new XmlRpcInt(2),
                new XmlRpcBoolean(false)
            };
            var array = new XmlRpcArray(values);
            Assert.That(array, Is.Not.Null);
        }

        [Test]
        public void TestXmlRpcStruct()
        {
            var members = new XmlRpcMember[]
            {
                new XmlRpcMember("name", "John"),
                new XmlRpcMember("age", 30),
                new XmlRpcMember("active", true)
            };
            var structValue = new XmlRpcStruct(members);
            Assert.That(structValue, Is.Not.Null);
        }

        [Test]
        public void TestXmlRpcMemberString()
        {
            var member = new XmlRpcMember("key", "value");
            Assert.That(member.Name, Is.EqualTo("key"));
            Assert.That(member.Value, Is.Not.Null);
        }

        [Test]
        public void TestXmlRpcMemberInt()
        {
            var member = new XmlRpcMember("count", 100);
            Assert.That(member.Name, Is.EqualTo("count"));
            Assert.That(member.Value, Is.Not.Null);
        }

        [Test]
        public void TestXmlRpcMemberBool()
        {
            var member = new XmlRpcMember("enabled", true);
            Assert.That(member.Name, Is.EqualTo("enabled"));
            Assert.That(member.Value, Is.Not.Null);
        }

        [Test]
        public void TestXmlRpcMemberNested()
        {
            var nested = new XmlRpcMember[]
            {
                new XmlRpcMember("inner", "value")
            };
            var member = new XmlRpcMember("outer", nested);
            Assert.That(member.Name, Is.EqualTo("outer"));
            Assert.That(member.Value, Is.Not.Null);
        }

        [Test]
        public void TestXmlRpcBase64()
        {
            byte[] data = Encoding.UTF8.GetBytes("Hello World");
            var base64Value = new XmlRpcBase64(data);
            Assert.That(base64Value, Is.Not.Null);
        }

        [Test]
        public void TestXmlRpcFormatTime()
        {
            var dateTime = new DateTime(2024, 1, 15, 10, 30, 0);
            var timeValue = new XmlRpcFormatTime(dateTime, "");
            Assert.That(timeValue, Is.Not.Null);
        }

        [Test]
        public void TestXmlRpcFormatTimeWithCustomFormat()
        {
            var dateTime = new DateTime(2024, 1, 15, 10, 30, 0);
            var timeValue = new XmlRpcFormatTime(dateTime, "yyyy-MM-dd");
            Assert.That(timeValue, Is.Not.Null);
        }

        [Test]
        public void TestBloggerXmlRpcFormatTime()
        {
            var dateTime = new DateTime(2024, 1, 15, 10, 30, 0);
            var timeValue = new BloggerXmlRpcFormatTime(dateTime, "");
            Assert.That(timeValue, Is.Not.Null);
        }

        [Test]
        public void TestXmlRpcClientInvalidResponseException()
        {
            var ex = new XmlRpcClientInvalidResponseException("bad response", new Exception("inner"));
            Assert.That(ex.Response, Is.EqualTo("bad response"));
            Assert.That(ex.InnerException, Is.Not.Null);
            Assert.That(ex.InnerException.Message, Is.EqualTo("inner"));
        }
    }
}
