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
using MessagePack;
using System.Buffers;
using System.Text;
using UnityEngine;

namespace Atom.Core
{
    [MessagePackObject]
    public class AtomGlobal
    {
        [Key("test")]
        public static string test { get; } = "test";
        [Key("test2")]
        string test2 = "test2";

        [Key("debug_mode")]
        public static string DebugMode { get; } = "Debug";
        public static ArrayPool<byte> ArrayPool { get; } = ArrayPool<byte>.Create();
        public static Encoding Encoding { get; } = Encoding.ASCII;
        public static ushort MaxUdpPacketSize { get; } = 255;
        public static ushort MaxPlayers { get; } = 512;

        static readonly bool _AOT = false;
        static AtomGlobal()
        {
            if (MaxUdpPacketSize < 1) MaxUdpPacketSize = 1;
            if (MaxUdpPacketSize > 512)
            {
                Debug.LogWarning("Suggestion: Set \"MaxUdpPacketSize\" to 512 or less to avoid packet loss and fragmentation! Occurs when the packet size exceeds the MTU of some router in the path.");
                Debug.LogWarning("Suggestion: Find the best MTU for your route using the \"AtomHelper.GetBestMTU()\" method, send this information to the server to help it find the best packet size that suits you.");
            }

            if (!_AOT)
            {
                AtomHelper.AOT();
                _AOT = true;
            }
        }

        public static void CreateSettingsFile()
        {
            Debug.LogError(MessagePackSerializer.SerializeToJson(new AtomGlobal()));
        }
    }
}
#endif