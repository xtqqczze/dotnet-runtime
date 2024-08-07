// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        internal static void ThrowKeyNullException() => throw new ArgumentNullException("key");

        [DoesNotReturn]
        internal static void ThrowValueNullException() => throw new ArgumentException(SR.ConcurrentDictionary_TypeOfValueIncorrect);

        [DoesNotReturn]
        internal static void ThrowOutOfMemoryException() => throw new OutOfMemoryException();

        [DoesNotReturn]
        internal static void ThrowIncompatibleComparer() => throw new InvalidOperationException(SR.InvalidOperation_IncompatibleComparer);
    }
}
