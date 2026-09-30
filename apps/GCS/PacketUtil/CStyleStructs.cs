using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace PacketUtil
{
    /// <summary>
    /// C언어 스타일로 바이트 프레임을 슬라이스하고 unmanaged struct로 읽어오는 간단한 헬퍼들입니다.
    /// - ProcessStructSlices: 시작 오프셋 목록으로 프레임을 구간으로 나누어 각 구간을 처리합니다.
    /// - ReadStruct: 버퍼와 오프셋으로 unmanaged struct를 읽습니다 (C의 *(T*)(buf+off)과 유사).
    /// - ProcessStructsAs: 동일한 unmanaged 타입 T로 각 구간을 읽어 콜백을 호출합니다.
    /// </summary>
    public static class CStyleStructs
    {
        public static void ProcessStructSlices(ReadOnlyMemory<byte> frameBytes, int[] offsets, Action<int, ReadOnlyMemory<byte>> parseStruct)
        {
            if (offsets == null || offsets.Length == 0) return;

            var sorted = offsets.Distinct().OrderBy(o => o).ToArray();
            for (int i = 0; i < sorted.Length; i++)
            {
                int start = sorted[i];
                if (start < 0 || start >= frameBytes.Length) throw new ArgumentOutOfRangeException(nameof(offsets), $"Offset {start} out of range.");

                int end = (i + 1 < sorted.Length) ? sorted[i + 1] : frameBytes.Length;
                if (end <= start) throw new InvalidOperationException($"Invalid offsets: next offset {end} <= start {start}.");

                var slice = frameBytes.Slice(start, end - start);
                parseStruct(i, slice);
            }
        }

        public static T ReadStruct<T>(ReadOnlyMemory<byte> buffer, int offset) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            if (offset < 0 || offset + size > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset), $"Requested struct at {offset} exceeds buffer length.");
            return MemoryMarshal.Read<T>(buffer.Span.Slice(offset, size));
        }

        public static void ProcessStructsAs<T>(ReadOnlyMemory<byte> frameBytes, int[] offsets, Action<int, T> callback) where T : unmanaged
        {
            ProcessStructSlices(frameBytes, offsets, (idx, mem) =>
            {
                int size = Marshal.SizeOf<T>();
                if (mem.Length < size) throw new InvalidOperationException($"Slice {idx} too small for type {typeof(T)} (need {size}, have {mem.Length}).");
                var value = MemoryMarshal.Read<T>(mem.Span);
                callback(idx, value);
            });
        }
    }
}
