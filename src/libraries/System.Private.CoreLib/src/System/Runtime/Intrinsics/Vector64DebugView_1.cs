// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Intrinsics
{
    internal readonly struct Vector64DebugView<T>
    {
        private readonly Vector64<T> _value;

        public Vector64DebugView(Vector64<T> value)
        {
            _value = value;
        }

        public byte[] ByteView
        {
            get
            {
                var items = new byte[Vector64<byte>.Count];
                _value.As<T, byte>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public double[] DoubleView
        {
            get
            {
                var items = new double[Vector64<double>.Count];
                _value.As<T, double>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public short[] Int16View
        {
            get
            {
                var items = new short[Vector64<short>.Count];
                _value.As<T, short>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public int[] Int32View
        {
            get
            {
                var items = new int[Vector64<int>.Count];
                _value.As<T, int>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public long[] Int64View
        {
            get
            {
                var items = new long[Vector64<long>.Count];
                _value.As<T, long>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public nint[] NIntView
        {
            get
            {
                var items = new nint[Vector64<nint>.Count];
                _value.As<T, nint>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public nuint[] NUIntView
        {
            get
            {
                var items = new nuint[Vector64<nuint>.Count];
                _value.As<T, nuint>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public sbyte[] SByteView
        {
            get
            {
                var items = new sbyte[Vector64<sbyte>.Count];
                _value.As<T, sbyte>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public float[] SingleView
        {
            get
            {
                var items = new float[Vector64<float>.Count];
                _value.As<T, float>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public ushort[] UInt16View
        {
            get
            {
                var items = new ushort[Vector64<ushort>.Count];
                _value.As<T, ushort>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public uint[] UInt32View
        {
            get
            {
                var items = new uint[Vector64<uint>.Count];
                _value.As<T, uint>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public ulong[] UInt64View
        {
            get
            {
                var items = new ulong[Vector64<ulong>.Count];
                _value.As<T, ulong>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }
    }
}
