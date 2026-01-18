// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

// NUnit 4.x moved classic assertions to NUnit.Framework.Legacy namespace
// This global using alias allows existing tests to use Assert.AreEqual etc. without modification
global using Assert = NUnit.Framework.Legacy.ClassicAssert;
