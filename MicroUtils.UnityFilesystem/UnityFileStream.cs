namespace MicroUtils.UnityFilesystem;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityDataTools.FileSystem;

public class UnityBinaryFileReader : IDisposable
{
    readonly ArrayPool<byte> ArrayPool = ArrayPool<byte>.Shared;

    readonly UnityFileStream stream;
    readonly BinaryReader reader;

    public UnityBinaryFileReader(string path, int bufferSize = 4096)
    {
        this.stream = new(path, bufferSize);
        this.reader = new(this.stream);
    }

    long Seek(long fileOffset) =>
        stream.Seek(fileOffset, System.IO.SeekOrigin.Begin);

    public void ReadArray(long fileOffset, int size, Array dest)
    {
        this.Seek(fileOffset);

        //var buffer = reader.ReadBytes(size);

        var buffer = ArrayPool.Rent(size);

        try
        {
            stream.Read(buffer.AsSpan(0, size));

            Buffer.BlockCopy(buffer, 0, dest, 0, size);
        }
        finally
        {
            ArrayPool.Return(buffer);
        }
    }

    public string ReadString(long fileOffset, int size)
    {
        this.Seek(fileOffset);
        var buffer = reader.ReadBytes(size);

        var s = Encoding.Default.GetString(buffer);

        return s;

    }

    public float ReadFloat(long fileOffset)
    {
        this.Seek(fileOffset);
        var x = reader.ReadSingle();

        return x;
    }

    public double ReadDouble(long fileOffset)
    {
        this.Seek(fileOffset);
        var x = reader.ReadDouble();

        return x;
    }

    public long ReadInt64(long fileOffset)
    {
        this.Seek(fileOffset);
        var x = reader.ReadInt64();

        return x;
    }

    public ulong ReadUInt64(long fileOffset)
    {
        this.Seek(fileOffset);
        var x = reader.ReadUInt64();

        return x;
    }

    public int ReadInt32(long fileOffset)
    {
        this.Seek(fileOffset);
        var x = reader.ReadInt32();

        return x;
    }

    public uint ReadUInt32(long fileOffset)
    {
        this.Seek(fileOffset);
        var x = reader.ReadUInt32();

        return x;
    }

    public short ReadInt16(long fileOffset)
    {
        this.Seek(fileOffset);
        var x = reader.ReadInt16();

        return x;
    }

    public ushort ReadUInt16(long fileOffset)
    {
        this.Seek(fileOffset);
        var x = reader.ReadUInt16();

        return x;
    }

    public sbyte ReadInt8(long fileOffset)
    {
        this.Seek(fileOffset);
        var x = reader.ReadSByte();

        return x;
    }

    public byte ReadUInt8(long fileOffset)
    {
        this.Seek(fileOffset);
        var x = reader.ReadByte();

        return x;
    }


    public void Dispose() => stream.Dispose();
}


/// <summary>
/// Stream reader for UnityFile. Unity Filesystem API seems to expect a fixed buffer for readers(?).
/// Unlike example code in UnityDataTools, this handles request length > buffer size.
/// </summary>
internal class UnityFileStream : Stream
{
    private readonly UnityFile file;
    private readonly byte[] bufferInternal;

    long positionInternal = 0;
    long bufferStartOffset = 0;
    int offsetInBuffer = 0;
    int bytesInBuffer = 0;

    private readonly Lazy<long> length;

    private UnityFileStream(UnityFile file, byte[] bufferInternal)
    {
        this.file = file;
        this.bufferInternal = bufferInternal;
        this.length = new(file.GetSize);

        positionInternal = file.Seek(0, UnityDataTools.FileSystem.SeekOrigin.Begin);
    }

    public UnityFileStream(string path, int bufferSize = 4096) : this(UnityFileSystem.OpenFile(path), new byte[bufferSize]) { }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => length.Value;

    private void CheckState()
    {

#if DEBUG
        if (positionInternal != bufferStartOffset + bytesInBuffer || bufferStartOffset < 0 || bytesInBuffer < 0)
        {
            throw new InvalidDataException();
        }
#endif

        ObjectDisposedException.ThrowIf(disposed, this);

        if (file.m_Handle.IsClosed || file.m_Handle.IsInvalid)
            throw new InvalidOperationException();
    }

    public override long Position
    {
        get
        {
            CheckState();
            return bufferStartOffset + offsetInBuffer;
        }

        set => this.Seek(value, System.IO.SeekOrigin.Begin);
    }

    public override void Close()
    {
        base.Close();

        if (!disposed)
            this.Dispose();
    }

    //public override void CopyTo(Stream destination, int bufferSize) => base.CopyTo(destination, bufferSize);

    //public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => base.CopyToAsync(destination, bufferSize, cancellationToken);

    //public override ValueTask DisposeAsync() => base.DisposeAsync();

    public override void Flush() => throw new NotSupportedException();

    private int ReadFromFile()
    {
        bufferStartOffset = positionInternal;
        offsetInBuffer = 0;
        
        int bytesRead;

        bytesRead = (int)file.Read(bufferInternal.Length, bufferInternal);

        bytesInBuffer = bytesRead;

        positionInternal += bytesRead;

        return bytesRead;
    }

    public override int Read(byte[] buffer, int offset, int count) => this.Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        CheckState();

        var offset = 0;
        var count = buffer.Length;
        var bytesRead = 0;

        while (count > 0)
        {
            if (offsetInBuffer >= bytesInBuffer)
            {
#if DEBUG
                if (positionInternal != bufferStartOffset + bytesInBuffer)
                {
                    throw new Exception($"positionInternal != bufferStartOffset + bytesInBuffer : {positionInternal} != {bufferStartOffset + bytesInBuffer}");
                }
#endif
                var readCount = ReadFromFile();
#if DEBUG
                if (positionInternal != bufferStartOffset + bytesInBuffer)
                {
                    throw new Exception($"positionInternal != bufferStartOffset + bytesInBuffer : {positionInternal} != {bufferStartOffset + bytesInBuffer}");
                }

                if (bytesInBuffer <= 0)
                {
                    Console.Error.WriteLine($"Read {readCount} bytes, bytes in buffer = {bytesInBuffer}");
                    throw new Exception($"bytesInBuffer <= 0 : {bytesInBuffer}");
                }
#endif
            }

            int copyCount = count;

            if (count + offsetInBuffer > bytesInBuffer)
                copyCount = bytesInBuffer - offsetInBuffer;

            bufferInternal.AsSpan(offsetInBuffer, copyCount).CopyTo(buffer[offset..]);

            offsetInBuffer += copyCount;

            offset += copyCount;
            count -= copyCount;

            bytesRead += copyCount;
#if DEBUG
            if (count < 0)
            {
                throw new Exception($"count < 0 : {count}");
            }
#endif
        }

        return bytesRead;
    }

    //public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => base.ReadAsync(buffer, offset, count, cancellationToken);
    //public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => base.ReadAsync(buffer, cancellationToken);

    public override int ReadByte()
    {
        if (Position >= Length)
            return -1;

        var buffer = new byte[1];

        var bytesRead = this.Read(buffer);

        if (bytesRead <= 0)
            return -1;

        return buffer[0];
    }

    public override long Seek(long offset, System.IO.SeekOrigin origin)
    {
        CheckState();

        // Why does UnityFileSystem have its own SeekOrigin enum? I don't care enough to find out, but it is annoying
        var (fileOffset, seekOrigin) = origin switch
        {
            System.IO.SeekOrigin.Begin => (offset, UnityDataTools.FileSystem.SeekOrigin.Begin),
            System.IO.SeekOrigin.Current => (this.Position + offset, UnityDataTools.FileSystem.SeekOrigin.Current),
            System.IO.SeekOrigin.End => (this.Length - offset, UnityDataTools.FileSystem.SeekOrigin.End),
            _ => throw new ArgumentException(null, nameof(origin))
        };

        var newOffset = fileOffset;

        // Is target inside the current buffer?
        if (fileOffset >= bufferStartOffset && fileOffset < (bufferStartOffset + bytesInBuffer))
        {
            offsetInBuffer = (int)(fileOffset - bufferStartOffset);
        }
        else
        {
            newOffset = file.Seek(offset, seekOrigin);

            bufferStartOffset = positionInternal = newOffset;
            offsetInBuffer = bytesInBuffer = 0;
        }

#if DEBUG
        if (newOffset < 0)
        {
            throw new Exception($"newOffset < 0 {newOffset}");
        }

        if (newOffset != fileOffset)
        {
            throw new Exception($"newOffset != fileOffset : {newOffset} != {fileOffset}");
        }
#endif

        return newOffset;
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    bool disposed = false;
    protected override void Dispose(bool disposing)
    {
        if (!disposed)
        {
            this.disposed = true;

            file.Dispose();
        }

        base.Dispose(disposing);
    }
}
