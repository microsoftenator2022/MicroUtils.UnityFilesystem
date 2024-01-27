using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

using MicroUtils.Functional;
using MicroUtils.Linq;

namespace MicroUtils.UnityFileSystem
{
    internal class UnityFileCache
    {
        class Chunk(byte[] data, int pageSize, long baseOffset, Func<long, Memory<byte>, int> readBytes)
        {
            readonly Memory<byte> memory = new(data);
            public int PageSize => pageSize;
            public long BaseOffset => baseOffset;

            readonly BitArray bitmap = new((data.Length / pageSize) + 1);

            int BitIndex(long absOffset)
            {
                var offset = absOffset - baseOffset;

                return (int)(offset / pageSize);
            }
            
            public Chunk(int size, int pageSize, long baseOffset, Func<long, Memory<byte>, int> readBytes) :
                this(new byte[size], pageSize, baseOffset, readBytes) { }

            public Span<byte> GetBytes(long absOffset, int length)
            {
                var offset = (int)(absOffset - baseOffset);
                var slice = memory.Slice(offset, length);

                var allValid = true;

                for (var i = BitIndex(offset); i < Math.Min(bitmap.Length, BitIndex(offset + length) + 1); i++)
                {
                    if (!bitmap[i])
                    {
                        allValid = false;
                        break;
                    }
                }

                if (!allValid)
                {
                    
                }

                return slice.Span;
            }
        }
    }
}
