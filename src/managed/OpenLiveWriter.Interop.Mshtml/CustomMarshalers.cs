// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices.CustomMarshalers
{
    /// <summary>
    /// Compatibility shim for EnumeratorToEnumVariantMarshaler which was removed in .NET Core.
    /// This marshaler is used for COM interop with IEnumVARIANT.
    /// 
    /// Note: This is a minimal implementation for compatibility. Full functionality
    /// may require additional work in Phase 2 of the migration.
    /// </summary>
    public class EnumeratorToEnumVariantMarshaler : ICustomMarshaler
    {
        private static readonly EnumeratorToEnumVariantMarshaler _instance = new EnumeratorToEnumVariantMarshaler();

        public static ICustomMarshaler GetInstance(string cookie)
        {
            return _instance;
        }

        public void CleanUpManagedData(object ManagedObj)
        {
            // No cleanup needed for managed objects
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            if (pNativeData != IntPtr.Zero)
            {
                Marshal.Release(pNativeData);
            }
        }

        public int GetNativeDataSize()
        {
            return -1; // Variable size
        }

        public IntPtr MarshalManagedToNative(object ManagedObj)
        {
            if (ManagedObj == null)
            {
                return IntPtr.Zero;
            }

            if (ManagedObj is IEnumerator enumerator)
            {
                // Create an EnumeratorViewOfEnumVariant wrapper
                // For full implementation, this would wrap the IEnumerator in a COM IEnumVARIANT
                throw new NotImplementedException(
                    "MarshalManagedToNative is not implemented. " +
                    "This will be addressed in Phase 2 of the .NET 10 migration.");
            }

            throw new ArgumentException("ManagedObj must be an IEnumerator", nameof(ManagedObj));
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            if (pNativeData == IntPtr.Zero)
            {
                return null;
            }

            // The native data is an IEnumVARIANT pointer
            // We need to wrap it in a managed IEnumerator
            object enumVariant = Marshal.GetObjectForIUnknown(pNativeData);
            
            // For IEnumVARIANT COM objects, we can use the IEnumerable pattern
            if (enumVariant is IEnumerable enumerable)
            {
                return enumerable.GetEnumerator();
            }

            // Return a basic enumerator wrapper
            return new EnumVariantEnumerator(pNativeData);
        }

        /// <summary>
        /// Basic enumerator wrapper for IEnumVARIANT
        /// </summary>
        private class EnumVariantEnumerator : IEnumerator, IDisposable
        {
            private IntPtr _enumVariant;
#pragma warning disable CS0649 // Field is never assigned - stub implementation
            private object _current;
#pragma warning restore CS0649
            private bool _disposed;

            public EnumVariantEnumerator(IntPtr enumVariant)
            {
                _enumVariant = enumVariant;
                if (_enumVariant != IntPtr.Zero)
                {
                    Marshal.AddRef(_enumVariant);
                }
            }

            public object Current => _current;

            public bool MoveNext()
            {
                // Minimal implementation - returns false to indicate end of enumeration
                // Full implementation would call IEnumVARIANT::Next
                return false;
            }

            public void Reset()
            {
                // Full implementation would call IEnumVARIANT::Reset
            }

            public void Dispose()
            {
                if (!_disposed && _enumVariant != IntPtr.Zero)
                {
                    Marshal.Release(_enumVariant);
                    _enumVariant = IntPtr.Zero;
                    _disposed = true;
                }
            }

            ~EnumVariantEnumerator()
            {
                Dispose();
            }
        }
    }
}
