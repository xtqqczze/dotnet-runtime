// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;
using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;

namespace System
{
    /// <summary>
    /// Memory represents a contiguous region of arbitrary memory similar to <see cref="Span{T}"/>.
    /// Unlike <see cref="Span{T}"/>, it is not a byref-like type.
    /// </summary>
    [DebuggerTypeProxy(typeof(MemoryDebugView<>))]
    [DebuggerDisplay("{ToString(),raw}")]
    public readonly struct Memory<T> : IEquatable<Memory<T>>
    {
        // The highest order bit of _index is used to discern whether _object is a pre-pinned array.
        // (_index < 0) => _object is a pre-pinned array, so Pin() will not allocate a new GCHandle
        //       (else) => Pin() needs to allocate a new GCHandle to pin the object.
        private readonly object? _object;
        private readonly int _index;
        private readonly int _length;

        /// <summary>
        /// Creates a new memory over the entirety of the target array.
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory(T[]? array)
        {
            if (array == null)
            {
                this = default;
                return; // returns default
            }
            if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();

            (_index, _length) = (0, array.Length);
            _object = array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Memory(T[]? array, int start)
        {
            if (array == null)
            {
                if (start != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                this = default;
                return; // returns default
            }
            if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();
            if ((uint)start > (uint)array.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            (_index, _length) = (start, array.Length - start);
            _object = array;
        }

        /// <summary>
        /// Creates a new memory over the portion of the target array beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="array">The target array.</param>
        /// <param name="start">The index at which to begin the memory.</param>
        /// <param name="length">The number of items in the memory.</param>
        /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
        /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory(T[]? array, int start, int length)
        {
            if (array == null)
            {
                if (start != 0 || length != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                this = default;
                return; // returns default
            }
            if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
                ThrowHelper.ThrowArrayTypeMismatchException();
#if TARGET_64BIT
            // See comment in Span<T>.Slice for how this works.
            if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)array.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException();
#else
            if ((uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();
#endif

            (_index, _length) = (start, length);
            _object = array;
        }

        /// <summary>
        /// Creates a new memory from a memory manager that provides specific method implementations beginning
        /// at 0 index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="manager">The memory manager.</param>
        /// <param name="length">The number of items in the memory.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="length"/> is negative.
        /// </exception>
        /// <remarks>For internal infrastructure only</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Memory(MemoryManager<T> manager, int length)
        {
            Debug.Assert(manager != null);

            if (length < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            (_index, _length) = (0, length);
            _object = manager;
        }

        /// <summary>
        /// Creates a new memory from a memory manager that provides specific method implementations beginning
        /// at 'start' index and ending at 'end' index (exclusive).
        /// </summary>
        /// <param name="manager">The memory manager.</param>
        /// <param name="start">The index at which to begin the memory.</param>
        /// <param name="length">The number of items in the memory.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or <paramref name="length"/> is negative.
        /// </exception>
        /// <remarks>For internal infrastructure only</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Memory(MemoryManager<T> manager, int start, int length)
        {
            Debug.Assert(manager != null);

            if (length < 0 || start < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            (_index, _length) = (start, length);
            _object = manager;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Memory(object? obj, int start, int length)
        {
            // No validation performed in release builds; caller must provide any necessary validation.

            // 'obj is T[]' below also handles things like int[] <-> uint[] being convertible
            Debug.Assert((obj == null)
                || (typeof(T) == typeof(char) && obj is string)
                || (obj is T[])
                || (obj is MemoryManager<T>));

            (_index, _length) = (start, length);
            _object = obj;
        }

        /// <summary>
        /// Defines an implicit conversion of an array to a <see cref="Memory{T}"/>
        /// </summary>
        public static implicit operator Memory<T>(T[]? array) => new Memory<T>(array);

        /// <summary>
        /// Defines an implicit conversion of a <see cref="ArraySegment{T}"/> to a <see cref="Memory{T}"/>
        /// </summary>
        public static implicit operator Memory<T>(ArraySegment<T> segment) => new Memory<T>(segment.Array, segment.Offset, segment.Count);

        /// <summary>
        /// Defines an implicit conversion of a <see cref="Memory{T}"/> to a <see cref="ReadOnlyMemory{T}"/>
        /// </summary>
        public static implicit operator ReadOnlyMemory<T>(Memory<T> memory) =>
            new ReadOnlyMemory<T>(memory._object, memory._index, memory._length);

        /// <summary>
        /// Returns an empty <see cref="Memory{T}"/>
        /// </summary>
        public static Memory<T> Empty => default;

        /// <summary>
        /// The number of items in the memory.
        /// </summary>
        public int Length => _length;

        /// <summary>
        /// Returns true if Length is 0.
        /// </summary>
        public bool IsEmpty => _length == 0;

        /// <summary>
        /// For <see cref="Memory{Char}"/>, returns a new instance of string that represents the characters pointed to by the memory.
        /// Otherwise, returns a <see cref="string"/> with the name of the type and the number of elements.
        /// </summary>
        public override string ToString()
        {
            (int selfIndex, int selfLength) = (_index, _length);

            if (typeof(T) == typeof(char))
            {
                return (_object is string str) ? str.Substring(selfIndex, selfLength) : Span.ToString();
            }

            return $"System.Memory<{typeof(T).Name}>[{selfLength}]";
        }

        /// <summary>
        /// Forms a slice out of the given memory, beginning at 'start'.
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<T> Slice(int start)
        {
            (int selfIndex, int selfLength) = (_index, _length);

            if ((uint)start > (uint)selfLength)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
            }

            // It is expected for _index + start to be negative if the memory is already pre-pinned.
            return new Memory<T>(_object, selfIndex + start, selfLength - start);
        }

        /// <summary>
        /// Forms a slice out of the given memory, beginning at 'start', of given length
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice (exclusive).</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<T> Slice(int start, int length)
        {
            (int selfIndex, int selfLength) = (_index, _length);

#if TARGET_64BIT
            // See comment in Span<T>.Slice for how this works.
            if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)selfLength)
                ThrowHelper.ThrowArgumentOutOfRangeException();
#else
            if ((uint)start > (uint)selfLength || (uint)length > (uint)(selfLength - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();
#endif

            // It is expected for _index + start to be negative if the memory is already pre-pinned.
            return new Memory<T>(_object, selfIndex + start, length);
        }

        /// <summary>
        /// Returns a span from the memory.
        /// </summary>
        public Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // This property getter has special support for returning a mutable Span<char> that wraps
                // an immutable String instance. This is obviously a dangerous feature and breaks type safety.
                // However, we need to handle the case where a ReadOnlyMemory<char> was created from a string
                // and then cast to a Memory<T>. Such a cast can only be done with unsafe or marshaling code,
                // in which case that's the dangerous operation performed by the dev, and we're just following
                // suit here to make it work as best as possible.

                ref T reference = ref Unsafe.NullRef<T>();
                int length = 0;

                // Copy this field into a local so that it can't change out from under us mid-operation.

                object? selfObject = _object;
                if (selfObject != null)
                {
                    int lengthOfUnderlyingSpan;

                    if (typeof(T) == typeof(char) && selfObject.GetType() == typeof(string))
                    {
                        // Special-case string since it's the most common for ROM<char>.

                        reference = ref Unsafe.As<char, T>(ref ((string)selfObject).GetRawStringData());
                        lengthOfUnderlyingSpan = Unsafe.As<string>(selfObject).Length;
                    }
                    else if (RuntimeHelpers.ObjectHasComponentSize(selfObject))
                    {
                        // We know the object is not null, it's not a string, and it is variable-length. The only
                        // remaining option is for it to be a T[] (or a U[] which is blittable to T[], like int[]
                        // and uint[]). As a special case of this, ROM<T> allows some amount of array variance
                        // that Memory<T> disallows. For example, an array of actual type string[] cannot be turned
                        // into a Memory<object> or a Span<object>, but it can be turned into a ROM/ROS<object>.
                        // We'll assume these checks succeeded because they're performed during Memory<T> construction.
                        // It's always possible for somebody to use private reflection to bypass these checks, but
                        // preventing type safety violations due to misuse of reflection is out of scope of this logic.

                        // 'selfObject is T[]' below also handles things like int[] <-> uint[] being convertible
                        Debug.Assert(selfObject is T[]);

                        reference = ref MemoryMarshal.GetArrayDataReference(Unsafe.As<T[]>(selfObject));
                        lengthOfUnderlyingSpan = Unsafe.As<T[]>(selfObject).Length;
                    }
                    else
                    {
                        // We know the object is not null, and it's not variable-length, so it must be a MemoryManager<T>.
                        // Otherwise somebody used private reflection to set this field, and we're not too worried about
                        // type safety violations at that point. Note that it can't be a MemoryManager<U>, even if U and
                        // T are blittable (e.g., MemoryManager<int> to MemoryManager<uint>), since there exists no
                        // constructor or other public API which would allow such a conversion.

                        Debug.Assert(selfObject is MemoryManager<T>);
                        Span<T> memoryManagerSpan = Unsafe.As<MemoryManager<T>>(selfObject).GetSpan();
                        reference = ref MemoryMarshal.GetReference(memoryManagerSpan);
                        lengthOfUnderlyingSpan = memoryManagerSpan.Length;
                    }

                    // If the Memory<T> or ReadOnlyMemory<T> instance is torn, this property getter has undefined behavior.
                    // We try to detect this condition and throw an exception, but it's possible that a torn struct might
                    // appear to us to be valid, and we'll return an undesired span. Such a span is always guaranteed at
                    // least to be in-bounds when compared with the original Memory<T> instance, so using the span won't
                    // AV the process.

                    (int selfIndex, int selfLength) = (_index, _length);

                    int desiredStartIndex = selfIndex & ReadOnlyMemory<T>.RemoveFlagsBitMask;

                    // Overflow cannot occur here because of the following invariants:
                    // - desiredStartIndex >= 0; as the high order bit of _index has been reset by RemoveFlagsBitMask
                    // - selfLength >= 0; as it is assigned from the _length field which is validated as non-negative by all public constructors
                    // - lengthOfUnderlyingSpan >= 0; as it is assigned from the object's Length property which cannot be negative

                    if (selfLength > lengthOfUnderlyingSpan - desiredStartIndex)
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException();
                    }

                    reference = ref Unsafe.Add(ref reference, (nint)(uint)desiredStartIndex);
                    length = selfLength;
                }

                return new Span<T>(ref reference, length);
            }
        }

        /// <summary>
        /// Copies the contents of the memory into the destination. If the source
        /// and destination overlap, this method behaves as if the original values are in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The Memory to copy items into.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the destination is shorter than the source.
        /// </exception>
        public void CopyTo(Memory<T> destination) => Span.CopyTo(destination.Span);

        /// <summary>
        /// Copies the contents of the memory into the destination. If the source
        /// and destination overlap, this method behaves as if the original values are in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <returns>If the destination is shorter than the source, this method
        /// return false and no data is written to the destination.</returns>
        /// <param name="destination">The span to copy items into.</param>
        public bool TryCopyTo(Memory<T> destination) => Span.TryCopyTo(destination.Span);

        /// <summary>
        /// Creates a handle for the memory.
        /// The GC will not move the memory until the returned <see cref="MemoryHandle"/>
        /// is disposed, enabling taking and using the memory's address.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// An instance with nonprimitive (non-blittable) members cannot be pinned.
        /// </exception>
        public unsafe MemoryHandle Pin()
        {
            // Just like the Span property getter, we have special support for a mutable Memory<char>
            // that wraps an immutable String instance. This might happen if a caller creates an
            // immutable ROM<char> wrapping a String, then uses Unsafe.As to create a mutable M<char>.
            // This needs to work, however, so that code that uses a single Memory<char> field to store either
            // a readable ReadOnlyMemory<char> or a writable Memory<char> can still be pinned and
            // used for interop purposes.

            // It's possible that the below logic could result in an AV if the struct
            // is torn. This is ok since the caller is expecting to use raw pointers,
            // and we're not required to keep this as safe as the other Span-based APIs.

            Memory<T> self = this;
            if (self._object != null)
            {
                if (typeof(T) == typeof(char) && self._object is string s)
                {
                    // Unsafe.AsPointer is safe since the handle pins it
                    GCHandle handle = GCHandle.Alloc(self._object, GCHandleType.Pinned);
                    ref char stringData = ref Unsafe.Add(ref s.GetRawStringData(), (nint)(uint)self._index);
                    return new MemoryHandle(Unsafe.AsPointer(ref stringData), handle);
                }
                else if (RuntimeHelpers.ObjectHasComponentSize(self._object))
                {
                    // 'self._object is T[]' below also handles things like int[] <-> uint[] being convertible
                    Debug.Assert(self._object is T[]);

                    // Array is already pre-pinned
                    if (self._index < 0)
                    {
                        // Unsafe.AsPointer is safe since it's pinned
                        void* pointer = Unsafe.AsPointer(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<T[]>(self._object)), (nint)(uint)(self._index & ReadOnlyMemory<T>.RemoveFlagsBitMask)));
                        return new MemoryHandle(pointer);
                    }
                    else
                    {
                        // Unsafe.AsPointer is safe since the handle pins it
                        GCHandle handle = GCHandle.Alloc(self._object, GCHandleType.Pinned);
                        void* pointer = Unsafe.AsPointer(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<T[]>(self._object)), (nint)(uint)self._index));
                        return new MemoryHandle(pointer, handle);
                    }
                }
                else
                {
                    Debug.Assert(self._object is MemoryManager<T>);
                    return Unsafe.As<MemoryManager<T>>(self._object).Pin(self._index);
                }
            }

            return default;
        }

        /// <summary>
        /// Copies the contents from the memory into a new array.  This heap
        /// allocates, so should generally be avoided, however it is sometimes
        /// necessary to bridge the gap with APIs written in terms of arrays.
        /// </summary>
        public T[] ToArray() => Span.ToArray();

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// Returns true if the object is Memory or ReadOnlyMemory and if both objects point to the same array and have the same length.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is ReadOnlyMemory<T>)
            {
                return ((ReadOnlyMemory<T>)obj).Equals(this);
            }
            else if (obj is Memory<T> memory)
            {
                return Equals(memory);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the memory points to the same array and has the same length.  Note that
        /// this does *not* check to see if the *contents* are equal.
        /// </summary>
        public bool Equals(Memory<T> other)
        {
            Memory<T> self = this;

            return
                self._object == other._object &&
                self._index == other._index &&
                self._length == other._length;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            // We use RuntimeHelpers.GetHashCode instead of Object.GetHashCode because the hash
            // code is based on object identity and referential equality, not deep equality (as common with string).
            object? selfObject = _object;
            return (selfObject != null) ? HashCode.Combine(RuntimeHelpers.GetHashCode(selfObject), _index, _length) : 0;
        }
    }
}
