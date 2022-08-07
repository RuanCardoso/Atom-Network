/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

#if UNITY_2021_3_OR_NEWER
using System;
using System.Collections.Concurrent;
using System.IO;
using UnityEngine;
using static Atom.Core.AtomGlobal;

namespace Atom.Core.Wrappers
{
    public class AtomStream : IDisposable
    {
        internal ConcurrentDictionary<int, int> PlayersToRelay { get; } = new();
        private int _bytesWritten;
        private readonly MemoryStream _memoryStream;
        private readonly byte[] _buffer;
        private readonly byte[] _memoryBuffer;
        private readonly bool _reuse;
        private readonly int _size;
        private readonly bool _fixedSize;
        private readonly bool _fixedAutoSize;
        private readonly bool _readOnly;
        private readonly bool _writerOnly;
        private DateTime _lastTime;

        public long Position
        {
            get => _memoryStream.Position;
            set => _memoryStream.Position = value;
        }

        public bool FixedSize => _fixedSize;
        public int Length => _size;
        public int BytesWritten => _bytesWritten;
        public int BytesRemaining => (int)(BytesWritten - Position);
        public DateTime LastTime { get => _lastTime; set => _lastTime = value; }

        public AtomStream()
        {
            _fixedSize = false;
            _fixedAutoSize = true;
            _size = Conf.MaxUdpPacketSize;
            _memoryBuffer = new byte[_size];
            _memoryStream = new(_memoryBuffer, 0, _size);
            _buffer = new byte[_size];
            _reuse = false;
        }

        public AtomStream(bool reuse = false, bool readOnly = false, bool writerOnly = false)
        {
            _fixedSize = false;
            _fixedAutoSize = false;
            _size = Conf.MaxUdpPacketSize;
            _memoryBuffer = new byte[_size];
            _memoryStream = new(_memoryBuffer, 0, _size);
            _buffer = new byte[_size];
            _reuse = reuse;
            _readOnly = readOnly;
            _writerOnly = writerOnly;
        }

        public AtomStream(int size)
        {
            _fixedSize = true;
            _fixedAutoSize = false;
            _size = size;
            _memoryBuffer = new byte[_size];
            _memoryStream = new(_memoryBuffer, 0, _size);
            _buffer = new byte[_size];
            _reuse = false;
        }

        public void Write(Vector3 vec3)
        {
            Write(vec3.x);
            Write(vec3.y);
            Write(vec3.z);
        }

        public void Write(Vector2 vec2)
        {
            Write(vec2.x);
            Write(vec2.y);
        }

        public void Write(Quaternion quat)
        {
            Write(quat.x);
            Write(quat.y);
            Write(quat.z);
            Write(quat.w);
        }

        public void Write(Color color)
        {
            Write(color.r);
            Write(color.g);
            Write(color.b);
            Write(color.a);
        }

        public Vector3 ReadVector3()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();
            return new Vector3(x, y, z);
        }

        public Vector2 ReadVector2()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            return new Vector2(x, y);
        }

        public Quaternion ReadQuaternion()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();
            float w = ReadFloat();
            return new Quaternion(x, y, z, w);
        }

        public Color ReadColor()
        {
            float r = ReadFloat();
            float g = ReadFloat();
            float b = ReadFloat();
            float a = ReadFloat();
            return new Color(r, g, b, a);
        }

        public void Write(byte value)
        {
            _memoryStream.WriteByte(value);
            _bytesWritten += 1;
        }

        public byte ReadByte() => (byte)_memoryStream.ReadByte();
        public void Write(int value)
        {
            int count = sizeof(int);
#if ATOM_DEBUG
            if (_size < count)
                throw new Exception($"_size is less than {count}, can't write int!");
#endif
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            Write(_buffer, 0, count);
        }

        public int ReadInt()
        {
            Read(4);
            return _buffer[0] | _buffer[1] << 8 | _buffer[2] << 16 | _buffer[3] << 24;
        }

        public void Write(uint value) =>
            Write((int)value);
        public uint ReadUInt()
        {
            return (uint)ReadInt();
        }

        public void Write(short value)
        {
            int count = sizeof(short);
#if ATOM_DEBUG
            if (_size < count)
                throw new Exception($"_size is less than {count}, can't write short!");
#endif
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            Write(_buffer, 0, count);
        }

        public short ReadShort()
        {
            Read(2);
            return (short)(_buffer[0] | _buffer[1] << 8);
        }

        public void Write(ushort value) =>
            Write((short)value);
        public ushort ReadUShort()
        {
            return (ushort)ReadShort();
        }

        public unsafe void Write(float value)
        {
            int count = sizeof(float);
#if ATOM_DEBUG
            if (_size < count)
                throw new Exception($"_size is less than {count}, can't write float!");
#endif
            uint TmpValue = *(uint*)&value;
            _buffer[0] = (byte)TmpValue;
            _buffer[1] = (byte)(TmpValue >> 8);
            _buffer[2] = (byte)(TmpValue >> 16);
            _buffer[3] = (byte)(TmpValue >> 24);
            Write(_buffer, 0, count);
        }

        public unsafe float ReadFloat()
        {
            Read(4);
            uint TmpValue = (uint)(_buffer[0] | _buffer[1] << 8 | _buffer[2] << 16 | _buffer[3] << 24);
            return *(float*)&TmpValue;
        }

        public unsafe virtual void Write(double value)
        {
            int count = sizeof(double);
#if ATOM_DEBUG
            if (_size < count)
                throw new Exception($"_size is less than {count}, can't write double!");
#endif
            ulong TmpValue = *(ulong*)&value;
            _buffer[0] = (byte)TmpValue;
            _buffer[1] = (byte)(TmpValue >> 8);
            _buffer[2] = (byte)(TmpValue >> 16);
            _buffer[3] = (byte)(TmpValue >> 24);
            _buffer[4] = (byte)(TmpValue >> 32);
            _buffer[5] = (byte)(TmpValue >> 40);
            _buffer[6] = (byte)(TmpValue >> 48);
            _buffer[7] = (byte)(TmpValue >> 56);
            Write(_buffer, 0, count);
        }

        public unsafe virtual double ReadDouble()
        {
            Read(8);
            uint lo = (uint)(_buffer[0] | _buffer[1] << 8 |
               _buffer[2] << 16 | _buffer[3] << 24);
            uint hi = (uint)(_buffer[4] | _buffer[5] << 8 |
                _buffer[6] << 16 | _buffer[7] << 24);

            ulong tmpBuffer = ((ulong)hi) << 32 | lo;
            return *(double*)&tmpBuffer;
        }

        public void Write7BitEncodedInt(int value)
        {
            // Write out an int 7 bits at a time.  The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            uint v = (uint)value;   // support negative numbers
            while (v >= 0x80)
            {
                Write((byte)(v | 0x80));
                v >>= 7;
            }
            Write((byte)v);
        }

        public int Read7BitEncodedInt()
        {
            // Read out an Int32 7 bits at a time.  The high bit
            // of the byte when on means to continue reading more bytes.
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
#if ATOM_DEBUG
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                    throw new FormatException("corrupted stream.");
#endif
                // ReadByte handles end of stream cases for us.
                b = (byte)_memoryStream.ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }

        public void Write(string value, bool inStackAlloc = false)
        {
            int getByteCount = Encoding.GetByteCount(value);
#if ATOM_DEBUG
            if (_size < getByteCount)
                throw new Exception($"_size is less than {getByteCount}, can't write string!");
#endif
            byte[] rentBytes = !inStackAlloc ? ArrayPool.Rent(getByteCount) : null;
            Span<byte> _rentBytes = !inStackAlloc ? rentBytes : stackalloc byte[getByteCount];
            int bytesCount = Encoding.GetBytes(value, _rentBytes);
            Write7BitEncodedInt(bytesCount);
            Write(_rentBytes[..bytesCount]);
            if (!inStackAlloc)
                ArrayPool.Return(rentBytes);
        }

        public string ReadString()
        {
            ReadOnlySpan<byte> _bytes = _buffer;
            int length = Read7BitEncodedInt();
            Read(length);
            return Encoding.GetString(_bytes[..length]); // String.FastAllocateString(): Garbage is created here, how to avoid?
        }

        public ReadOnlySpan<byte> ReadNextAsReadOnlySpan()
        {
            int bytesRemaining = BytesRemaining;
            Read(bytesRemaining);
            ReadOnlySpan<byte> _bytes = _buffer;
            return _bytes[..bytesRemaining];
        }

        public byte[] ReadNext(out int bytesRemaining)
        {
            bytesRemaining = BytesRemaining;
            Read(bytesRemaining);
            return _buffer;
        }

        public byte[] ReadNext(int size)
        {
            Read(size);
            return _buffer;
        }

        private void Read(int count, int offset = 0)
        {
#if ATOM_DEBUG
            if (_writerOnly)
                throw new Exception("Can't read from a writer only stream!");
            if (count + offset > _size)
                throw new Exception($"_size is less than {count + offset}, can't read!");
#endif
            while (offset < count)
            {
                int bytesRemaining = count - offset;
                int bytesRead = _memoryStream.Read(_buffer, offset, bytesRemaining);
                if (bytesRead > 0)
                    offset += bytesRead;
#if ATOM_DEBUG
                else
                    throw new Exception("AtomStream: Read: End of stream reached, there is no data to read.");
#endif
            }
        }

        public void Write(byte[] buffer, int offset, int count)
        {
#if ATOM_DEBUG
            if (_readOnly)
                throw new Exception("AtomStream: Write: Stream is read only!");
#endif
#if ATOM_DEBUG
            try
            {
#endif
                _memoryStream.Write(buffer, offset, count);
                _bytesWritten += count;
#if ATOM_DEBUG
            }
            catch (NotSupportedException ex)
            {
                switch (ex.Message)
                {
                    case "Memory stream is not expandable.":
                        throw new NotSupportedException("AtomStream: Write: There is not enough space in the buffer to write the data.");
                }
            }
#endif
        }

        public void Write(ReadOnlySpan<byte> buffer)
        {
#if ATOM_DEBUG
            try
            {
#endif
                _memoryStream.Write(buffer);
                _bytesWritten += buffer.Length;
#if ATOM_DEBUG
            }
            catch (NotSupportedException ex)
            {
                switch (ex.Message)
                {
                    case "Memory stream is not expandable.":
                        throw new NotSupportedException($"AtomStream: Write: There is not enough space in the buffer to write the data -> {buffer.Length}");
                }
            }
#endif
        }

        public void SetBuffer(ReadOnlySpan<byte> buffer)
        {
            Write(buffer);
            _memoryStream.Position = 0;
        }

        public void SetBuffer(byte[] buffer, int offset, int count)
        {
            Write(buffer, offset, count);
            _memoryStream.Position = 0;
        }

        public byte[] ToArray()
        {
            if (!_fixedSize) // GC Alloc Here
            {
                ReadOnlySpan<byte> _buffer = _memoryBuffer;
                return _buffer[.._bytesWritten].ToArray();
            }
            else
                return _memoryBuffer;
        }

        public byte[] GetBuffer() =>
            _memoryBuffer;
        public ReadOnlySpan<byte> GetBufferAsReadOnlySpan()
        {
            ReadOnlySpan<byte> _buffer = _memoryBuffer;
            return _buffer[.._bytesWritten];
        }

        public void Reset(int pos = 0, int countBytes = 0)
        {
            _memoryStream.Position = pos;
            _bytesWritten = countBytes;
        }

        public void Seek(long offset, SeekOrigin seekOrigin)
        {
            _memoryStream.Seek(offset, seekOrigin);
        }

        bool _disposable;
        public void Dispose()
        {
            if (!_reuse)
            {
                if (!_fixedSize && !_fixedAutoSize)
                {
                    if (!_disposable)
                    {
                        _memoryStream.Dispose();
                        _disposable = true;
                    }
                    else
                        throw new Exception("AtomStream: Dispose: Already disposed!");
                }
                else
                    Reset();
            }
            else
            {
                Reset();
                AtomCore.Streams.Push(this);
            }
        }

        public static AtomStream Get()
        {
            var atomStream = AtomCore.Streams.Pull();
#if ATOM_DEBUG
            return atomStream._bytesWritten != 0
                ? throw new Exception("AtomStream: A item has been modified while it was in the pool!")
                : atomStream;
#else
            return atomStream;
#endif
        }
    }
}
#endif