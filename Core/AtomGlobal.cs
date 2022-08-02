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
        [Key(0)]
        public string[] Addresses = new[] { "127.0.0.1:5055", "0.0.0.0:0000" };
        [Key(1)]
        public string DebugMode = "Debug";
        [Key(2)]
        public string Encoding = "ASCII";
        [Key(3)]
        public int MaxUdpPacketSize = 128;
        [Key(4)]
        public int MaxPlayers = 12;
        [Key(5)]
        public int MaxRecBuffer = 1024;
        [Key(6)]
        public int MaxSendBuffer = 1024;
        [Key(7)]
        public int ReceiveTimeout = -1;
        [Key(8)]
        public int SendTimeout = -1;
        [Key(9)]
        public int MaxStreamPool = 12;
        [Key(10)]
        public double BandwidthTimeout = 2;
        [Key(11)]
        public float PingFrequency = 1f;
        [Key(12)]
        public bool BandwidthCounter = true;
        [Key(13)]
        public bool IncrementalGc = true;
    }

    public class AtomGlobal
    {
        private const string FILE_NAME = "atom";
        private const string RES_PATH = "./Assets/Resources";
        private const string PATH = RES_PATH + "/" + FILE_NAME + ".json";
        private const int MIN_MTU = 512;
        public const int MAX_PLAYERS = byte.MaxValue * 64;
        private static readonly bool _AOT_;
        private static bool _INIT_;

        public static ArrayPool<byte> ArrayPool { get; } = ArrayPool<byte>.Create();
        public static Encoding Encoding { get; private set; }
        public static AtomSettings Conf { get; private set; } = new();

        static AtomGlobal()
        {
            if (!_AOT_)
            {
                AtomHelper.SetResolver();
#if UNITY_EDITOR
                if (!Directory.Exists(RES_PATH))
                    Directory.CreateDirectory(RES_PATH);
                CreateSettingsFile();
                CreateFileWatcher(RES_PATH);
#else
                _INIT_ = true;
#endif
                _AOT_ = true;
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
                MessagePackSerializer.SerializeToJson(strFile, Conf);
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
                        Conf = MessagePackSerializer.Deserialize<AtomSettings>(msgBytes);
                        if (Conf != null)
                        {
                            if (Conf.MaxUdpPacketSize < 1) Conf.MaxUdpPacketSize = 1;
                            if (Conf.MaxUdpPacketSize > MIN_MTU)
                            {
                                PrintWarning($"Suggestion: Set \"MaxUdpPacketSize\" to {MIN_MTU} or less to avoid packet loss and fragmentation! Occurs when the packet size exceeds the MTU of some router in the path.");
                                PrintWarning($"Suggestion: Find the best MTU for your route using the \"AtomHelper.GetBestMTU()\" method, send this information to the server to help it find the best packet size that suits you.");
                            }

                            if (Conf.MaxPlayers > MAX_PLAYERS)
                                throw new Exception($"Max players reached! -> Only {MAX_PLAYERS} players are supported!");
#if UNITY_EDITOR
                            BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
                            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(activeBuildTarget);
                            if (PlayerSettings.GetApiCompatibilityLevel(targetGroup) != ApiCompatibilityLevel.NET_Standard || PlayerSettings.GetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Server) != ApiCompatibilityLevel.NET_Standard)
                                PrintWarning("Suggestion: Set the \"Api Compatibility Level\" to .NET Standard 2.1 or higher to best support Atom!");

                            PlayerSettings.gcIncremental = Conf.IncrementalGc;
                            if (!PlayerSettings.gcIncremental)
                                PrintWarning("Suggestion: Enable \"Incremental GC\" to best performance!");

                            AtomHelper.SetDefine(!Conf.BandwidthCounter, "", "ATOM_BANDWIDTH_COUNTER");
                            switch (Conf.DebugMode.ToLower())
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

                            switch (Conf.MaxPlayers)
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
                                Encoding = Encoding.GetEncoding(Conf.Encoding);
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

        private static void OnChanged(object source, FileSystemEventArgs e) => UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
#endif
    }
}
#endif