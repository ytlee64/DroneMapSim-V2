using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PacketUtil
{
    /// <summary>
    /// Helper methods for marshaling structs to byte arrays.
    /// </summary>
    public static class StructMarshal
    {
        /// <summary>
        /// Convert an unmanaged struct to a newly allocated byte array (fast path).
        /// </summary>
        public static byte[] StructToBytesUnmanaged<T>(in T value) where T : unmanaged
        {
            Span<T> s = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in value), 1);
            return MemoryMarshal.AsBytes(s).ToArray();
        }

        /// <summary>
        /// Convert any struct to a byte array. Uses a fast path for "blittable" structs and a Marshal-based fallback for structs that contain references or are non-blittable.
        /// </summary>
        public static byte[] StructToBytes<T>(in T value) where T : struct
        {
            // If T does not contain references, we can treat it as a block and copy bytes directly.
            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Span<T> s = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in value), 1);
                return MemoryMarshal.AsBytes(s).ToArray();
            }

            int size = Marshal.SizeOf<T>();
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                // Box the struct and marshal it to unmanaged memory
                object boxed = value;
                Marshal.StructureToPtr(boxed, ptr, false);
                byte[] arr = new byte[size];
                Marshal.Copy(ptr, arr, 0, size);
                return arr;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
