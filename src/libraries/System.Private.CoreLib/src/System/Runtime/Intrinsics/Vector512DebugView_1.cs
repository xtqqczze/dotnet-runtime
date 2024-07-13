// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Intrinsics
{
    internal readonly struct Vector512DebugView<T>
    {
        private readonly Vector512<T> _value;

        public Vector512DebugView(Vector512<T> value)
        {
            _value = value;
        }

        public byte[] ByteView
        {
            get
            {
                var items = new byte[Vector512<byte>.Count];
                _value.As<T, byte>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public double[] DoubleView
        {
            get
            {
                var items = new double[Vector512<double>.Count];
                _value.As<T, double>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public short[] Int16View
        {
            get
            {
                var items = new short[Vector512<short>.Count];
                _value.As<T, short>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public int[] Int32View
        {
            get
            {
                var items = new int[Vector512<int>.Count];
                _value.As<T, int>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public long[] Int64View
        {
            get
            {
                var items = new long[Vector512<long>.Count];
                _value.As<T, long>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public nint[] NIntView
        {
            get
            {
                var items = new nint[Vector512<nint>.Count];
                _value.As<T, nint>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public nuint[] NUIntView
        {
            get
            {
                var items = new nuint[Vector512<nuint>.Count];
                _value.As<T, nuint>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public sbyte[] SByteView
        {
            get
            {
                var items = new sbyte[Vector512<sbyte>.Count];
                _value.As<T, sbyte>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public float[] SingleView
        {
            get
            {
                var items = new float[Vector512<float>.Count];
                _value.As<T, float>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public ushort[] UInt16View
        {
            get
            {
                var items = new ushort[Vector512<ushort>.Count];
                _value.As<T, ushort>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public uint[] UInt32View
        {
            get
            {
                var items = new uint[Vector512<uint>.Count];
                _value.As<T, uint>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }

        public ulong[] UInt64View
        {
            get
            {
                var items = new ulong[Vector512<ulong>.Count];
                _value.As<T, ulong>().StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(items));
                return items;
            }
        }
    }
}
