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
    public class AtomGlobal
    {
        private static readonly bool _aot;
        private static bool _init;
        private const string _file_name = "atom";
        private const string _res_path = "./Assets/Resources";
        private const string _path = _res_path + "/" + _file_name + ".json";

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
            [Key("max_rec_buffer")]
            public int MaxRecBuffer = 8192;
            [Key("max_send_buffer")]
            public int MaxSendBuffer = 8192;
        }

        public static ArrayPool<byte> ArrayPool { get; } = ArrayPool<byte>.Create();
        public static string DebugMode { get; private set; } = "Debug";
        public static Encoding Encoding { get; private set; } = Encoding.ASCII;
        public static ushort MaxUdpPacketSize { get; private set; } = 256;
        public static ushort MaxPlayers { get; private set; } = 255;
        public static int MaxRecBuffer { get; private set; } = 8192;
        public static int MaxSendBuffer { get; private set; } = 8192;

        static AtomGlobal()
        {
            if (!_aot)
                _aot = AtomHelper.AOT();

#if UNITY_EDITOR
            CreateSettingsFile();
#else
            _init = true;
#endif

            if (MaxUdpPacketSize < 1) MaxUdpPacketSize = 1;
            if (MaxUdpPacketSize > 512)
            {
                Debug.LogWarning("Suggestion: Set \"MaxUdpPacketSize\" to 512 or less to avoid packet loss and fragmentation! Occurs when the packet size exceeds the MTU of some router in the path.");
                Debug.LogWarning("Suggestion: Find the best MTU for your route using the \"AtomHelper.GetBestMTU()\" method, send this information to the server to help it find the best packet size that suits you.");
            }

            if (MaxPlayers > 12400)
                throw new System.Exception("Max players reached! -> Only 12400 Players are supported!");
        }

        static void CreateSettingsFile()
        {
            if (!Directory.Exists(_res_path)) Directory.CreateDirectory(_res_path);
            if (!File.Exists(_path))
            {
                using (TextWriter strFile = File.CreateText(_path))
                {
                    MessagePackSerializer.SerializeToJson(strFile, new AtomSettings());
                }
            }
            else
                _init = true;
        }

        public static void LoadSettingsFile()
        {
            if (_init)
            {
                string strFile = Resources.Load<TextAsset>(_file_name).text;
                byte[] msgBytes = MessagePackSerializer.ConvertFromJson(strFile);
                AtomSettings atomSettings = MessagePackSerializer.Deserialize<AtomSettings>(msgBytes);
                if (atomSettings != null)
                {
                    DebugMode = atomSettings.DebugMode;
                    MaxUdpPacketSize = atomSettings.MaxUdpPacketSize;
                    MaxPlayers = atomSettings.MaxPlayers;
                    MaxRecBuffer = atomSettings.MaxRecBuffer;
                    MaxSendBuffer = atomSettings.MaxSendBuffer;
#if UNITY_EDITOR
                    switch (DebugMode.ToLower())
                    {
                        case "debug":
                            AtomHelper.SetDefine(false, "ATOM_RELEASE", "ATOM_DEBUG");
                            break;
                        case "release":
                            AtomHelper.SetDefine(false, "ATOM_DEBUG", "ATOM_RELEASE");
                            break;
                        default:
                            throw new System.Exception("Atom.Core: Debug mode not found!");
                    }
#endif
                    try
                    {
                        Encoding = Encoding.GetEncoding(atomSettings.Encoding);
                    }
                    catch
                    {
                        Debug.LogWarning("Atom: Encoding not found! Using default encoding (ASCII).");
                        Encoding = Encoding.ASCII;
                    }
                }
            }
            else
                Debug.LogWarning("Atom: Settings file not loaded! Please, check if the file \"atom.json\" is in the \"Resources\" folder.");
        }
    }
}
#endif