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
using System.Buffers;
using System.Text;
using UnityEngine;

namespace Atom.Core
{
    public class AtomGlobal
    {
        public static ArrayPool<byte> ArrayPool { get; } = ArrayPool<byte>.Create();
        public static Encoding Encoding { get; } = Encoding.ASCII;
        public static ushort MaxUdpPacketSize { get; } = 512;
        public static ushort MaxPlayers = 512;

        static AtomGlobal()
        {
            if (MaxUdpPacketSize > 512)
            {
                Debug.LogWarning("Suggestion: Set \"MaxUdpPacketSize\" to 512 or less to avoid packet loss and fragmentation! Occurs when the packet size exceeds the MTU of some router in the path.");
                Debug.LogWarning("Suggestion: Find the best MTU for your route using the \"AtomHelper.GetBestMTU()\" method, send this information to the server to help it find the best packet size that suits you.");
            }
        }
    }
}
#endif