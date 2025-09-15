// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    public abstract class EnumInfo
    {
        private protected EnumInfo(Type underlyingType, string[] names, bool isFlags)
        {
            UnderlyingType = underlyingType;
            Names = names;
            HasFlagsAttribute = isFlags;
        }

        internal Type UnderlyingType { get; }
        internal string[] Names { get; }
        internal bool HasFlagsAttribute { get; }
    }

    public sealed class EnumInfo<TStorage> : EnumInfo
        where TStorage : struct, INumber<TStorage>
    {
        public EnumInfo(Type underlyingType, TStorage[] values, string[] names, bool isFlags) :
            base(underlyingType, names, isFlags)
        {
            Debug.Assert(values.Length == names.Length);
            Debug.Assert(Enum.AreSorted(values));

            Values = values;
            ValuesAreSequentialFromZero = Enum.AreSequentialFromZero(values);
        }

        internal TStorage[] Values { get; }
        internal bool ValuesAreSequentialFromZero { get; }

        /// <summary>Create a copy of <see cref="Values"/>.</summary>
        public TResult[] CloneValues<TResult>() where TResult : struct
        {
            TStorage[] storage = Values;
            TResult[] result = new TResult[storage.Length];
            Unsafe.BitCast<ReadOnlySpan<TStorage>, ReadOnlySpan<TResult>>(storage).CopyTo(result);
            return result;
        }
    }
}
