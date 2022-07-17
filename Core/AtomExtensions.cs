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
namespace Atom.Core
{
    public static class AtomExtensions
    {
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
    }
}
#endif