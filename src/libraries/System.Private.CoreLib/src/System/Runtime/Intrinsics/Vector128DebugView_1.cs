// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Intrinsics
{
    internal readonly struct Vector128DebugView<T>
    {
        private readonly Vector128<T> _value;

        public Vector128DebugView(Vector128<T> value)
        {
            _value = value;
        }

        public byte[] ByteView
        {
            get
            {
                var items = new byte[Vector128<byte>.Count];
                _value.As<T, byte>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public double[] DoubleView
        {
            get
            {
                var items = new double[Vector128<double>.Count];
                _value.As<T, double>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public short[] Int16View
        {
            get
            {
                var items = new short[Vector128<short>.Count];
                _value.As<T, short>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public int[] Int32View
        {
            get
            {
                var items = new int[Vector128<int>.Count];
                _value.As<T, int>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public long[] Int64View
        {
            get
            {
                var items = new long[Vector128<long>.Count];
                _value.As<T, long>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public nint[] NIntView
        {
            get
            {
                var items = new nint[Vector128<nint>.Count];
                _value.As<T, nint>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public nuint[] NUIntView
        {
            get
            {
                var items = new nuint[Vector128<nuint>.Count];
                _value.As<T, nuint>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public sbyte[] SByteView
        {
            get
            {
                var items = new sbyte[Vector128<sbyte>.Count];
                _value.As<T, sbyte>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public float[] SingleView
        {
            get
            {
                var items = new float[Vector128<float>.Count];
                _value.As<T, float>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public ushort[] UInt16View
        {
            get
            {
                var items = new ushort[Vector128<ushort>.Count];
                _value.As<T, ushort>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public uint[] UInt32View
        {
            get
            {
                var items = new uint[Vector128<uint>.Count];
                _value.As<T, uint>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public ulong[] UInt64View
        {
            get
            {
                var items = new ulong[Vector128<ulong>.Count];
                _value.As<T, ulong>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }
    }
}
