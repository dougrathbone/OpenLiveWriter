// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using OpenLiveWriter.Interop.Windows;
using OpenLiveWriter.Localization;

namespace OpenLiveWriter.FileDestinations
{
    /// <summary>
    /// Shared static utility methods for web-publishing
    /// </summary>
    public sealed class WebPublishUtils
    {

        /// <summary>
        /// Create the appropriate file destination for the specified settings profile and initial path
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="initialPath"></param>
        /// <returns></returns>
        public static FileDestination CreateFileDestination(WebPublishSettings settings, string initialPath)
        {
            DestinationProfile destProfile = settings.Destination.Profile;
            if (destProfile.Type == DestinationProfile.DestType.WINDOWS)
            {
                return new LocalFileSystemDestination(initialPath);
            }
            else
            {
                return new WinInetFTPFileDestination(destProfile.FtpServer, initialPath, destProfile.UserName, destProfile.Password);
            }
        }

        /// <summary>
        /// Creates a destination that points to the destination's root folder.
        /// </summary>
        /// <returns></returns>
        public static FileDestination CreateRootDestination(WebPublishSettings settings)
        {
            FileDestination dest;
            if (settings.Destination.Profile.Type == DestinationProfile.DestType.FTP)
            {
                if (settings.PublishRootPath.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                    dest = CreateFileDestination(settings, "/");
                else
                    dest = CreateFileDestination(settings, "");
            }
            else
                dest = CreateFileDestination(settings, "");
            return dest;
        }

        /// <summary>
        /// Translate an exception into an error message
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static WebPublishMessage ExceptionToErrorMessage(Exception e)
        {
            // parse out extended error info for use in error message construction
            string message = e.Message;
            if (e is SiteDestinationException siteEx && siteEx.DestinationExtendedMessage != null)
            {
                message = siteEx.DestinationExtendedMessage;
            }
            else if (e.InnerException != null)
            {
                message = e.InnerException.Message;
            }

            // trace for diagnostics
            Trace.WriteLine(e.ToString());

            // Convert exception to appropriate message using pattern matching
            return e switch
            {
                LoginException => new WebPublishMessage(MessageId.LoginFailed),
                NoSuchDirectoryException noSuchDir => new WebPublishMessage(MessageId.NoSuchPublishFolder, noSuchDir.Path),
                SiteDestinationException site when site.DestinationErrorCode == ERROR_INTERNET.NAME_NOT_RESOLVED
                    => new WebPublishMessage(MessageId.InvalidHostname),
                SiteDestinationException site when site.DestinationErrorCode == ERROR_INTERNET.CANNOT_CONNECT
                    => new WebPublishMessage(MessageId.FtpServerUnavailable),
                SiteDestinationException site when site.DestinationErrorCode == ERROR_INTERNET.TIMEOUT
                    => new WebPublishMessage(MessageId.ConnectionTimeout),
                _ => new WebPublishMessage(MessageId.PublishFailed, message)
            };
        }
    }
}
