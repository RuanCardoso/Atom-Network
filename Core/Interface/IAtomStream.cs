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
using System.IO;

namespace Atom.Core.Interface
{
    public interface IAtomStream
    {
        byte[] ToArray();
        byte[] GetBuffer();
        MemoryStream AsStream();
        void SetPosition(int position);
        long GetPosition();
        int GetCapacity();
        bool IsFixedSize();
        void SetCapacity(int size);
        void Reset();
        void Close();
    }
}
#endif