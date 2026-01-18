// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using OpenLiveWriter.CoreServices;
using OpenLiveWriter.Extensibility.BlogClient;
using OpenLiveWriter.BlogClient.Providers;

namespace OpenLiveWriter.BlogClient.Clients
{
    /// <summary>
    /// Client for SixApart Atom API (TypePad, Vox, etc.)
    /// Uses WSSE authentication with Atom 0.3 protocol.
    /// </summary>
    [BlogClient("SixApartAtom", "Atom")]
    public class SixApartAtomClient : AtomClient
    {
        public SixApartAtomClient(Uri postApiUrl, IBlogCredentialsAccessor credentials)
            : base(AtomProtocolVersion.V03, postApiUrl, credentials)
        {
        }

        protected override void ConfigureClientOptions(BlogClientOptions clientOptions)
        {
            base.ConfigureClientOptions(clientOptions);
            clientOptions.SupportsCategories = true;
            clientOptions.SupportsMultipleCategories = true;
        }

        protected override void VerifyCredentials(TransientCredentials tc)
        {
            // WSSE authentication is handled in the request filter
            // No additional verification needed
        }

        public override BlogPostCategory[] GetCategories(string blogId)
        {
            // TODO: Implement category retrieval
            // The introspection doc at http://www.typepad.com/t/atom/weblog contains category URI
            throw new NotImplementedException("Category retrieval not implemented for SixApart Atom");
        }

        protected override HttpRequestFilter RequestFilter
        {
            get
            {
                return new HttpRequestFilter(WsseFilter);
            }
        }

        /// <summary>
        /// Applies WSSE (Web Services Security) authentication to the request.
        /// </summary>
        private void WsseFilter(HttpWebRequest request)
        {
            // Get credentials from the base class
            TransientCredentials tc = Login();
            string username = tc?.Username ?? string.Empty;
            string password = tc?.Password ?? string.Empty;

            string nonce = Guid.NewGuid().ToString("d");
            string created = DateTimeHelper.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", DateTimeFormatInfo.InvariantInfo);
            byte[] stringToHash = Encoding.UTF8.GetBytes(nonce + created + password);
            byte[] bytes = SHA1.Create().ComputeHash(stringToHash);
            string digest = Convert.ToBase64String(bytes);

            string headerValue = string.Format("UsernameToken Username=\"{0}\", PasswordDigest=\"{1}\", Created=\"{2}\", Nonce=\"{3}\"",
                username,
                digest,
                created,
                nonce);
            if (headerValue.IndexOfAny(new char[] { '\r', '\n' }) >= 0)
                throw new BlogClientAuthenticationException("ProtocolViolation", "Protocol violation, EOL characters are not allowed in WSSE headers");
            request.Headers.Add("X-WSSE", headerValue);
        }

        public override BlogInfo[] GetUsersBlogs()
        {
            // TODO: Implement blog discovery
            throw new NotImplementedException("Blog discovery not implemented for SixApart Atom");
        }
    }
}
