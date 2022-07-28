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
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using static Atom.Core.AtomLogger;

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
        public int MaxUdpPacketSize = 128;
        [Key("max_players")]
        public int MaxPlayers = 12;
        [Key("max_rec_buffer")]
        public int MaxRecBuffer = 1024;
        [Key("max_send_buffer")]
        public int MaxSendBuffer = 1024;
        [Key("max_stream_pool")]
        public int MaxStreamPool = 12;
        [Key("bandwidth_timeout")]
        public int BandwidthTimeout = 2;
        [Key("bandwidth_counter")]
        public bool BandwidthCounter = true;
        [Key("incremental_gc")]
        public bool IncrementalGc = true;
    }

    public class AtomGlobal
    {
        private const string FILE_NAME = "atom";
        private const string RES_PATH = "./Assets/Resources";
        private const string PATH = RES_PATH + "/" + FILE_NAME + ".json";
        private const int MIN_MTU = 512;
        public const int MAX_PLAYERS = ushort.MaxValue * 4;
        private static readonly bool _AOT_;
        private static bool _INIT_;

        public static ArrayPool<byte> ArrayPool { get; } = ArrayPool<byte>.Create();
        public static Encoding Encoding { get; private set; }
        public static AtomSettings Settings { get; private set; } = new();

        static AtomGlobal()
        {
            if (!_AOT_)
            {
                _AOT_ = AtomHelper.AOT();
#if UNITY_EDITOR
                if (!Directory.Exists(RES_PATH))
                    Directory.CreateDirectory(RES_PATH);
                CreateSettingsFile();
                CreateFileWatcher(RES_PATH);
#else
                _INIT_ = true;
#endif
            }
        }

#if UNITY_EDITOR
        private static void CreateSettingsFile()
        {
            if (!File.Exists(PATH))
                SaveSettingsFile();
            else
                _INIT_ = true;
        }

        public static void SaveSettingsFile()
        {
            if (Directory.Exists(RES_PATH))
            {
                using TextWriter strFile = File.CreateText(PATH);
                MessagePackSerializer.SerializeToJson(strFile, Settings);
            }
        }
#endif

        public static void LoadSettingsFile()
        {
            try
            {
                if (_INIT_)
                {
#if !UNITY_EDITOR
                    var asset = Resources.Load<TextAsset>(FILE_NAME);
                    string strFile = asset.text;
#else
                    var asset = File.ReadAllText(PATH);
                    string strFile = asset;
#endif
                    if (asset != null)
                    {
                        byte[] msgBytes = MessagePackSerializer.ConvertFromJson(strFile);
                        Settings = MessagePackSerializer.Deserialize<AtomSettings>(msgBytes);
                        if (Settings != null)
                        {
                            if (Settings.MaxUdpPacketSize < 1) Settings.MaxUdpPacketSize = 1;
                            if (Settings.MaxUdpPacketSize > MIN_MTU)
                            {
                                PrintWarning($"Suggestion: Set \"MaxUdpPacketSize\" to {MIN_MTU} or less to avoid packet loss and fragmentation! Occurs when the packet size exceeds the MTU of some router in the path.");
                                PrintWarning($"Suggestion: Find the best MTU for your route using the \"AtomHelper.GetBestMTU()\" method, send this information to the server to help it find the best packet size that suits you.");
                            }

                            if (Settings.MaxPlayers > MAX_PLAYERS)
                                throw new Exception($"Max players reached! -> Only {MAX_PLAYERS} players are supported!");
#if UNITY_EDITOR
                            BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
                            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(activeBuildTarget);
                            if (PlayerSettings.GetApiCompatibilityLevel(targetGroup) != ApiCompatibilityLevel.NET_Standard || PlayerSettings.GetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Server) != ApiCompatibilityLevel.NET_Standard)
                                PrintWarning("Suggestion: Set the \"Api Compatibility Level\" to .NET Standard 2.1 or higher to best support Atom!");

                            PlayerSettings.gcIncremental = Settings.IncrementalGc;
                            if (!PlayerSettings.gcIncremental)
                                PrintWarning("Suggestion: Enable \"Incremental GC\" to best performance!");

                            AtomHelper.SetDefine(!Settings.BandwidthCounter, "", "ATOM_BANDWIDTH_COUNTER");
                            switch (Settings.DebugMode.ToLower())
                            {
                                case "debug":
                                    AtomHelper.SetDefine(false, "ATOM_RELEASE", "ATOM_DEBUG");
                                    PlayerSettings.SetIl2CppCompilerConfiguration(targetGroup, Il2CppCompilerConfiguration.Debug);
                                    PlayerSettings.SetIl2CppCompilerConfiguration(UnityEditor.Build.NamedBuildTarget.Server, Il2CppCompilerConfiguration.Debug);
                                    break;
                                case "release":
                                    AtomHelper.SetDefine(false, "ATOM_DEBUG", "ATOM_RELEASE");
                                    PlayerSettings.SetIl2CppCompilerConfiguration(targetGroup, Il2CppCompilerConfiguration.Master);
                                    PlayerSettings.SetIl2CppCompilerConfiguration(UnityEditor.Build.NamedBuildTarget.Server, Il2CppCompilerConfiguration.Master);
                                    break;
                                default:
                                    throw new Exception("Atom.Core: Debug mode not found!");
                            }

                            switch (Settings.MaxPlayers)
                            {
                                case <= byte.MaxValue:
                                    AtomHelper.SetDefine(false, "ATOM_USHORT_PLAYER_ID;ATOM_INT_PLAYER_ID", "ATOM_BYTE_PLAYER_ID");
                                    break;
                                case <= ushort.MaxValue:
                                    AtomHelper.SetDefine(false, "ATOM_INT_PLAYER_ID;ATOM_BYTE_PLAYER_ID", "ATOM_USHORT_PLAYER_ID");
                                    break;
                                case <= int.MaxValue:
                                    AtomHelper.SetDefine(false, "ATOM_BYTE_PLAYER_ID;ATOM_USHORT_PLAYER_ID", "ATOM_INT_PLAYER_ID");
                                    break;
                                default:
                                    throw new Exception("Long player IDs are not supported!");
                            }
#endif
                            try
                            {
                                Encoding = Encoding.GetEncoding(Settings.Encoding);
                            }
                            catch
                            {
                                throw new Exception("Atom.Core: Encoding not found!");
                            }
                        }
                        else
                            throw new Exception("Atom.Core: Settings not found!");
                    }
                    else
                        throw new Exception("Atom.Core: Settings file not found!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

#if UNITY_EDITOR
        public static void CreateFileWatcher(string path)
        {
            FileSystemWatcher watcher = new()
            {
                Path = path,
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = $"{FILE_NAME}.json"
            };

            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;
        }

        private static void OnChanged(object source, FileSystemEventArgs e) => Task.Run(async () =>
        {
            await Task.Delay(3000);
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        });
#endif
    }
}
#endif