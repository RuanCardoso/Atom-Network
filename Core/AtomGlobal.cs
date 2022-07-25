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
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Atom.Core
{
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
        [Key("max_stream_pool")]
        public int MaxStreamPool = 10;
        [Key("bandwidth_counter")]
        public bool BandwidthCounter = true;
    }

    public class AtomGlobal
    {
        private const string FILE_NAME = "atom";
        private const string RES_PATH = "./Assets/Resources";
        private const string PATH = RES_PATH + "/" + FILE_NAME + ".json";
        private const int MIN_MTU = 512;
        private const int MAX_PLAYERS = ushort.MaxValue;
        private static readonly bool _AOT_;
        private static bool _init_;

        public static ArrayPool<byte> ArrayPool { get; } = ArrayPool<byte>.Create();
        public static Encoding Encoding;
        public static AtomSettings Settings;

        static AtomGlobal()
        {
            if (!_AOT_)
            {
                _AOT_ = AtomHelper.AOT();

#if UNITY_EDITOR
                CreateFileWatcher(RES_PATH);
                CreateSettingsFile();
#else
                _init_ = true;
#endif
            }
        }

        static void CreateSettingsFile()
        {
            if (!Directory.Exists(RES_PATH)) Directory.CreateDirectory(RES_PATH);
            if (!File.Exists(PATH))
            {
                using (TextWriter strFile = File.CreateText(PATH))
                {
                    MessagePackSerializer.SerializeToJson(strFile, new AtomSettings());
                }
            }
            else
                _init_ = true;
        }

        public static void LoadSettingsFile()
        {
            if (_init_)
            {
                var asset = Resources.Load<TextAsset>(FILE_NAME);
                if (asset != null)
                {
                    byte[] msgBytes = MessagePackSerializer.ConvertFromJson(asset.text);
                    Settings = MessagePackSerializer.Deserialize<AtomSettings>(msgBytes);
                    if (Settings != null)
                    {
                        if (Settings.MaxUdpPacketSize < 1) Settings.MaxUdpPacketSize = 1;
                        if (Settings.MaxUdpPacketSize > MIN_MTU)
                        {
                            Debug.LogWarning($"Suggestion: Set \"MaxUdpPacketSize\" to {MIN_MTU} or less to avoid packet loss and fragmentation! Occurs when the packet size exceeds the MTU of some router in the path.");
                            Debug.LogWarning("Suggestion: Find the best MTU for your route using the \"AtomHelper.GetBestMTU()\" method, send this information to the server to help it find the best packet size that suits you.");
                        }

                        if (Settings.MaxPlayers > MAX_PLAYERS)
                            throw new System.Exception($"Max players reached! -> Only {MAX_PLAYERS} players are supported!");
#if UNITY_EDITOR
                        //AtomHelper.SetDefine(Settings.BandwidthCounter, "", "ATOM_BANDWIDTH_COUNTER");
                        switch (Settings.DebugMode)
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
                            Encoding = Encoding.GetEncoding(Settings.Encoding);
                        }
                        catch
                        {
                            Debug.LogWarning("Atom: Encoding not found! Using default encoding (ASCII).");
                            Encoding = Encoding.ASCII;
                        }
                    }
                    else
                        Debug.LogWarning("Atom: Deserialization error! Settings not found!");
                }
                else
                    Debug.LogWarning("Atom: Settings file not loaded! Please, check if the file \"atom.json\" is in the \"Resources\" folder.");
            }
        }

#if UNITY_EDITOR
        public static void CreateFileWatcher(string path)
        {
            // Create a new FileSystemWatcher and set its properties.
            FileSystemWatcher watcher = new()
            {
                Path = path,
                /* Watch for changes in LastAccess and LastWrite times, and 
                   the renaming of files or directories. */
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                // Only watch text files.
                Filter = "*.txt"
            };

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            Debug.Log($"File: {e.FullPath} {e.ChangeType}");
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {

        }
#endif
    }
}
#endif