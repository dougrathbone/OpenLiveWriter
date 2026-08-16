// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using NUnit.Framework;
using OpenLiveWriter.Interop.Com;
using OpenLiveWriter.Mshtml;

namespace OpenLiveWriter.UnitTest.Mshtml
{
    [TestFixture]
    public class ZoomHelperTests
    {
        [Test]
        public void GetNextZoomLevel_ZoomIn_FromDefault_Returns110()
        {
            Assert.AreEqual(110, ZoomHelper.GetNextZoomLevel(100, zoomIn: true));
        }

        [Test]
        public void GetNextZoomLevel_ZoomOut_FromDefault_Returns90()
        {
            Assert.AreEqual(90, ZoomHelper.GetNextZoomLevel(100, zoomIn: false));
        }

        [Test]
        public void GetNextZoomLevel_ZoomIn_FromMinimum_Returns33()
        {
            Assert.AreEqual(33, ZoomHelper.GetNextZoomLevel(25, zoomIn: true));
        }

        [Test]
        public void GetNextZoomLevel_ZoomOut_FromMaximum_Returns400()
        {
            Assert.AreEqual(400, ZoomHelper.GetNextZoomLevel(500, zoomIn: false));
        }

        [Test]
        public void GetNextZoomLevel_ZoomIn_AtMaximum_StaysAtMaximum()
        {
            Assert.AreEqual(500, ZoomHelper.GetNextZoomLevel(500, zoomIn: true));
        }

        [Test]
        public void GetNextZoomLevel_ZoomOut_AtMinimum_StaysAtMinimum()
        {
            Assert.AreEqual(25, ZoomHelper.GetNextZoomLevel(25, zoomIn: false));
        }

        [Test]
        public void GetNextZoomLevel_ZoomIn_FromBetweenLevels_ReturnsNextLevelUp()
        {
            Assert.AreEqual(110, ZoomHelper.GetNextZoomLevel(105, zoomIn: true));
        }

        [Test]
        public void GetNextZoomLevel_ZoomOut_FromBetweenLevels_ReturnsNextLevelDown()
        {
            Assert.AreEqual(100, ZoomHelper.GetNextZoomLevel(105, zoomIn: false));
        }
    }

    /// <summary>
    /// Regression tests for issue #1083: IDM.ZOOMPERCENT and IDM.JUSTIFYFULL both
    /// have the numeric value 50, so registering the zoom command in
    /// MshtmlCoreCommandSet (a Dictionary keyed by IDM value) under its raw IDM
    /// threw ArgumentException ("An item with the same key has already been
    /// added.") at editor startup.
    /// </summary>
    [TestFixture]
    public class MshtmlCoreCommandSetTests
    {
        /// <summary>
        /// Constructing the command set must not throw, and both colliding
        /// commands must be reachable: JUSTIFYFULL under its raw IDM, and
        /// ZOOMPERCENT under the synthetic ZoomPercentKey.
        /// </summary>
        [Test]
        public void Constructor_RegistersJustifyFullAndZoomWithoutCollision()
        {
            MshtmlCoreCommandSet commands = null;
            Assert.DoesNotThrow(() => commands = new MshtmlCoreCommandSet(new StubCommandTarget()));

            Assert.IsTrue(commands.ContainsKey(IDM.JUSTIFYFULL));
            Assert.IsTrue(commands.ContainsKey(MshtmlCoreCommandSet.ZoomPercentKey));
            Assert.AreNotEqual(IDM.JUSTIFYFULL, MshtmlCoreCommandSet.ZoomPercentKey);
        }

        private sealed class StubCommandTarget : IOleCommandTargetWithExecParams, IOleCommandTargetGetCommandValue
        {
            public void QueryStatus(Guid pguidCmdGroup, uint cCmds, ref OLECMD prgCmds, IntPtr pCmdText)
            {
            }

            public void Exec(Guid pguidCmdGroup, uint nCmdID, OLECMDEXECOPT nCmdexecopt, ref object pvaIn, ref object pvaOut)
            {
            }

            public void Exec(Guid pguidCmdGroup, uint nCmdID, OLECMDEXECOPT nCmdexecopt, IntPtr pvaIn, ref object pvaOut)
            {
            }
        }
    }
}
