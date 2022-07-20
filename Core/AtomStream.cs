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
using System.IO;
using static Atom.Core.AtomGlobal;

namespace Atom.Core
{
    public class AtomStream : IDisposable
    {
        public static readonly AtomStream None = new();
        private readonly MemoryStream _memoryStream;
        private readonly byte[] _buffer;
        private readonly byte[] _memoryBuffer;
        private int _countBytes;
        public long Position { get => _memoryStream.Position; set => _memoryStream.Position = value; }

        public AtomStream()
        {
            _memoryBuffer = new byte[MaxUdpPacketSize];
            _memoryStream = new(_memoryBuffer, 0, MaxUdpPacketSize);
            _buffer = new byte[MaxUdpPacketSize];
        }

        public void Write(byte value)
        {
            _memoryStream.WriteByte(value);
            _countBytes += 1;
        }

        public void Read(out byte value) =>
            value = (byte)_memoryStream.ReadByte();
        public void Write(int value)
        {
            int count = sizeof(int);
#if ATOM_DEBUG
            if (MaxUdpPacketSize < count)
                throw new Exception($"MaxUdpPacketSize is less than {count}, can't write int!");
#endif
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            Write(_buffer, 0, count);
        }

        public void Read(out int value)
        {
            Read(4);
            value = _buffer[0] | _buffer[1] << 8 | _buffer[2] << 16 | _buffer[3] << 24;
        }

        public void Write(uint value) =>
            Write((int)value);
        public void Read(out uint value)
        {
            Read(out int unsignedValue);
            value = (uint)unsignedValue;
        }

        public void Write(short value)
        {
            int count = sizeof(short);
#if ATOM_DEBUG
            if (MaxUdpPacketSize < count)
                throw new Exception($"MaxUdpPacketSize is less than {count}, can't write short!");
#endif
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            Write(_buffer, 0, count);
        }

        public void Read(out short value)
        {
            Read(2);
            value = (short)(_buffer[0] | _buffer[1] << 8);
        }

        public void Write(ushort value) =>
            Write((short)value);
        public void Read(out ushort value)
        {
            Read(out short unsignedValue);
            value = (ushort)unsignedValue;
        }

        public unsafe void Write(float value)
        {
            int count = sizeof(float);
#if ATOM_DEBUG
            if (MaxUdpPacketSize < count)
                throw new Exception($"MaxUdpPacketSize is less than {count}, can't write float!");
#endif
            uint TmpValue = *(uint*)&value;
            _buffer[0] = (byte)TmpValue;
            _buffer[1] = (byte)(TmpValue >> 8);
            _buffer[2] = (byte)(TmpValue >> 16);
            _buffer[3] = (byte)(TmpValue >> 24);
            Write(_buffer, 0, count);
        }

        public unsafe void Read(out float value)
        {
            Read(4);
            uint TmpValue = (uint)(_buffer[0] | _buffer[1] << 8 | _buffer[2] << 16 | _buffer[3] << 24);
            value = *(float*)&TmpValue;
        }

        public unsafe virtual void Write(double value)
        {
            int count = sizeof(double);
#if ATOM_DEBUG
            if (MaxUdpPacketSize < count)
                throw new Exception($"MaxUdpPacketSize is less than {count}, can't write double!");
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

        public unsafe virtual void Read(out double value)
        {
            Read(8);
            uint lo = (uint)(_buffer[0] | _buffer[1] << 8 |
               _buffer[2] << 16 | _buffer[3] << 24);
            uint hi = (uint)(_buffer[4] | _buffer[5] << 8 |
                _buffer[6] << 16 | _buffer[7] << 24);

            ulong tmpBuffer = ((ulong)hi) << 32 | lo;
            value = *(double*)&tmpBuffer;
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
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                    throw new FormatException("corrupted stream.");

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
            if (MaxUdpPacketSize < getByteCount)
                throw new Exception($"MaxUdpPacketSize is less than {getByteCount}, can't write string!");

            byte[] rentBytes = !inStackAlloc ? ArrayPool.Rent(getByteCount) : null;
            Span<byte> _rentBytes = !inStackAlloc ? rentBytes : stackalloc byte[getByteCount];
            int bytesCount = Encoding.GetBytes(value, _rentBytes);
            Write7BitEncodedInt(bytesCount);
            Write(_rentBytes[..bytesCount]);
            if (!inStackAlloc)
                ArrayPool.Return(rentBytes);
        }

        public void Read(out string value)
        {
            ReadOnlySpan<byte> _bytes = _buffer;
            int length = Read7BitEncodedInt();
            Read(length);
            value = Encoding.GetString(_bytes[..length]); // String.FastAllocateString(): Garbage is created here, how to avoid?
        }

        private void Read(int count, int offset = 0)
        {
#if ATOM_DEBUG
            if (count + offset > MaxUdpPacketSize)
                throw new Exception($"MaxUdpPacketSize is less than {count + offset}, can't read!");
#endif
            while (offset < count)
            {
                int bytesRemaining = count - offset;
                int bytesRead = _memoryStream.Read(_buffer, offset, bytesRemaining);
                if (bytesRead > 0)
                    offset += bytesRead;
                else
                    throw new Exception("AtomStream: Read: End of stream reached, there is no data to read.");
            }
        }

        public byte[] ReadNext()
        {
            int bytesRemaining = (int)(_countBytes - _memoryStream.Position);
            Read(bytesRemaining);
            return _buffer[..bytesRemaining];
        }

        public void Write(byte[] buffer, int offset, int count)
        {
#if ATOM_DEBUG
            try
            {
#endif
                _memoryStream.Write(buffer, offset, count);
                _countBytes += count;
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
                _countBytes += buffer.Length;
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

        public void SetBuffer(ReadOnlySpan<byte> buffer)
        {
            Write(buffer);
            _memoryStream.Position = 0;
        }

        public ReadOnlySpan<byte> GetBuffer()
        {
            ReadOnlySpan<byte> _buffer = _memoryBuffer;
            return _buffer[.._countBytes];
        }

        public byte[] GetBufferAsCopy()
        {
            ReadOnlySpan<byte> _buffer = _memoryBuffer;
            return _buffer[.._countBytes].ToArray();
        }

        bool _disposable;
        public void Dispose()
        {
            if (!_disposable)
            {
                _memoryStream.Dispose();
                _disposable = true;
            }
            else
                throw new Exception("AtomStream: Dispose: Already disposed!");
        }
    }
}
#endif