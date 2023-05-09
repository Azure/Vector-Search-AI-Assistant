using System.Buffers;
using System.Runtime.InteropServices;

namespace DataCopilot.Vectorize.Utils;

/// <summary>
/// Efficiently treat an array of float as an array of bytes.
/// 
/// Untested code that comes from Stephen Toub.
/// </summary>
public static class CastMemoryExtensions
{
    public static ReadOnlyMemory<byte> AsBytes(this ReadOnlyMemory<float> memory) =>
        new FloatsAsBytesMemoryManager(memory).Memory;

    private sealed unsafe class FloatsAsBytesMemoryManager : MemoryManager<byte>
    {
        private readonly ReadOnlyMemory<float> _source;

        public FloatsAsBytesMemoryManager(ReadOnlyMemory<float> source) => _source = source;

        public override Span<byte> GetSpan() => MemoryMarshal.AsBytes(MemoryMarshal.AsMemory(_source).Span);

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            if (MemoryMarshal.TryGetArray(_source, out ArraySegment<float> array))
            {
                GCHandle pinned = GCHandle.Alloc(array.Array, GCHandleType.Pinned);
                return new MemoryHandle((void*)(pinned.AddrOfPinnedObject() + elementIndex * sizeof(float)), pinned, this);
            }

            if (MemoryMarshal.TryGetMemoryManager(_source, out MemoryManager<float>? manager))
            {
                return manager.Pin(elementIndex);
            }

            throw new InvalidOperationException("This code should be unreachable");
        }

        public override void Unpin() { }

        protected override void Dispose(bool disposing) { }
    }
}