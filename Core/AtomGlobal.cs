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
using System.IO;
using System.Text;
using UnityEngine;

namespace Atom.Core
{
    [MessagePackObject]
    public class AtomGlobal
    {
        private const string _path = "./Assets/atom.json";

        [MessagePackObject]
        public class AtomSettings
        {
            [Key("debug_mode")]
            public string DebugMode = "Debug";
            [Key("encoding")]
            public string Encoding = "ASCII";
            [Key("max_udp_packet_size")]
            public ushort MaxUdpPacketSize = 255;
            [Key("max_players")]
            public ushort MaxPlayers = 512;
        }

        public static string DebugMode { get; } = "Debug";
        public static ArrayPool<byte> ArrayPool { get; } = ArrayPool<byte>.Create();
        public static Encoding Encoding { get; } = Encoding.ASCII;
        public static ushort MaxUdpPacketSize { get; } = 255;
        public static ushort MaxPlayers { get; } = 512;

        static readonly bool _AOT = false;
        static AtomGlobal()
        {
            if (!_AOT)
                _AOT = AtomHelper.AOT();

            CreateSettingsFile();

            if (MaxUdpPacketSize < 1) MaxUdpPacketSize = 1;
            if (MaxUdpPacketSize > 512)
            {
                Debug.LogWarning("Suggestion: Set \"MaxUdpPacketSize\" to 512 or less to avoid packet loss and fragmentation! Occurs when the packet size exceeds the MTU of some router in the path.");
                Debug.LogWarning("Suggestion: Find the best MTU for your route using the \"AtomHelper.GetBestMTU()\" method, send this information to the server to help it find the best packet size that suits you.");
            }
        }

        static void CreateSettingsFile()
        {
            if (!File.Exists(_path))
            {
                using (Stream strFile = File.CreateText(_path).BaseStream)
                {
                    byte[] msgBytes = MessagePackSerializer.Serialize<AtomSettings>(new AtomSettings());
                    strFile.Write(msgBytes, 0, msgBytes.Length);
                }
            }
            else
                LoadSettingsFile();
        }

        static void LoadSettingsFile()
        {
            using (Stream strFile = File.OpenText(_path).BaseStream)
            {
                AtomSettings atomSettings = MessagePackSerializer.Deserialize<AtomSettings>(strFile);
                Debug.Log($"AtomSettings: {atomSettings.DebugMode} {atomSettings.Encoding} {atomSettings.MaxUdpPacketSize} {atomSettings.MaxPlayers}");
            }
        }
    }
}
#endif