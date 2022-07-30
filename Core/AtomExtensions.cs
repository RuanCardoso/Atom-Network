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
using Atom.Core.Wrappers;

namespace Atom.Core
{
    public static class AtomExtensions
    {
        public static void Write(this byte[] value, AtomStream writer, int offset, int countBytes) =>
            writer.Write(value, offset, countBytes);
        public static void Write(this int value, AtomStream atomStream) =>
            atomStream.Write(value);
        public static void Write(this uint value, AtomStream atomStream) =>
           atomStream.Write(value);
        public static void Write(this short value, AtomStream atomStream) =>
            atomStream.Write(value);
        public static void Write(this ushort value, AtomStream atomStream) =>
            atomStream.Write(value);
        public static void Write(this byte value, AtomStream atomStream) =>
            atomStream.Write(value);
        public static void Write(this float value, AtomStream atomStream) =>
            atomStream.Write(value);
        public static void Write(this double value, AtomStream atomStream) =>
            atomStream.Write(value);
        public static void Write(this string value, AtomStream atomStream) =>
            atomStream.Write(value);
        public static void Read(this ref int value, AtomStream atomStream) =>
            value = atomStream.ReadInt();
        public static void Read(this ref uint value, AtomStream atomStream) =>
            value = atomStream.ReadUInt();
        public static void Read(this ref short value, AtomStream atomStream) =>
            value = atomStream.ReadShort();
        public static void Read(this ref ushort value, AtomStream atomStream) =>
           value = atomStream.ReadUShort();
        public static void Read(this ref byte value, AtomStream atomStream) =>
           value = atomStream.ReadByte();
        public static void Read(this ref float value, AtomStream atomStream) =>
           value = atomStream.ReadFloat();
        public static void Read(this ref double value, AtomStream atomStream) =>
           value = atomStream.ReadDouble();
        public static string Read(this string _, AtomStream atomStream) =>
            atomStream.ReadString();
    }
}
#endif