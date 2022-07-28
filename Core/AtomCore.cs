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
using Atom.Core.Attribute;
using Atom.Core.Wrappers;
using MarkupAttributes;
using System;
using UnityEngine;
using static Atom.Core.AtomGlobal;

namespace Atom.Core
{
    [DefaultExecutionOrder(-1)]
    public class AtomCore : MonoBehaviour
    {
        public const byte CHANNEL_MASK = 0x3;
        public const byte OPERATION_MASK = 0x3;
        public const byte TARGET_MASK = 0x7;
#if ATOM_BYTE_PLAYER_ID
        public const int RELIABLE_SIZE = 6;
        public const int UNRELIABLE_SIZE = 2;
#elif ATOM_USHORT_PLAYER_ID
        public const int RELIABLE_SIZE = 7;
        public const int UNRELIABLE_SIZE = 3;
#elif ATOM_INT_PLAYER_ID
        public const int RELIABLE_SIZE = 9;
        public const int UNRELIABLE_SIZE = 5;
#endif
        public static AtomCore Module { get; private set; }
        public static AtomPooling<AtomStream> Streams { get; private set; }
        public static double NetworkTime { get; private set; }

#if UNITY_EDITOR
#if ATOM_BANDWIDTH_COUNTER
        [Box("Bandwidth")]
        [Label("Timeout")] public int BandwidthTimeout;
        [Box("Bandwidth/Server")][Label("Byte Rate")][ReadOnly] public string SERVER_REC_BYTES_RATE = "0 Bytes/s";
        [Label("Message Rate")][ReadOnly] public string SERVER_REC_MSG_RATE = "0 Bytes/s";
        [Box("Bandwidth/Client")][Label("Bytes Rate")][ReadOnly] public string CLIENT_REC_BYTES_RATE = "0 Bytes/s";
        [Label("Message Rate")][ReadOnly] public string CLIENT_REC_MSG_RATE = "0 Bytes/s";
#endif
        [Box("Settings")]
        public BuildMode Build;
        public EncodingType Encoding;
        [Label("Max Message Size")][Range(1, 1532)][NaughtyAttributes.InfoBox("This value directly influences packet drop!", NaughtyAttributes.EInfoBoxType.Warning)] public int MaxUdpMessageSize;
        [Range(1, MAX_PLAYERS)][NaughtyAttributes.InfoBox("<= 255 = 1 Byte || > 255 <= 65535 = 2 Byte || 4 Byte", NaughtyAttributes.EInfoBoxType.Normal)][Label("Receive Size")] public int MaxPlayers;
        [NaughtyAttributes.InfoBox("An inappropriate size can drop packets, even on a localhost!", NaughtyAttributes.EInfoBoxType.Warning)][Label("Receive Size")] public int MaxRecBuffer;
        [NaughtyAttributes.InfoBox("An inappropriate size can delay sending data!", NaughtyAttributes.EInfoBoxType.Warning)][Label("Send Size")] public int MaxSendBuffer;
        [Range(1, 128)] public int MaxStreamPool;
        public bool BandwidthCounter;
        [Label("GC Incremental")] public bool IncrementalGc;
#endif
        private void Awake()
        {
            Module = this;
            NetworkTime = Time.timeAsDouble;
            LoadSettingsFile();
            Streams = new(() => new(true, false, false), Settings.MaxStreamPool, false, true, "AtomStreamPool");
        }

        private void Start()
        {
            NetworkTime = Time.timeAsDouble;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        private void Update()
        {
            NetworkTime = Time.timeAsDouble;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                string _encoding_ = Encoding.ToString().Replace("UTF8", "UTF-8").Replace("UTF16", "UTF-16").Replace("UTF32", "UTF-32");
                bool isSave = Settings.DebugMode != Build.ToString()
                    || Settings.Encoding != _encoding_
                    || Settings.MaxUdpPacketSize != MaxUdpMessageSize
                    || Settings.MaxPlayers != MaxPlayers
                    || Settings.MaxRecBuffer != MaxRecBuffer
                    || Settings.MaxSendBuffer != MaxSendBuffer
                    || Settings.MaxStreamPool != MaxStreamPool
                    || Settings.BandwidthCounter != BandwidthCounter
                    || Settings.BandwidthCounter != BandwidthCounter
                    || Settings.IncrementalGc != IncrementalGc;
                if (isSave)
                {
                    AtomLogger.Print("Wait for save settings... 3 seconds.....Don't play!");
                    Settings.DebugMode = Build.ToString();
                    Settings.Encoding = _encoding_;
                    Settings.MaxUdpPacketSize = MaxUdpMessageSize;
                    Settings.MaxPlayers = MaxPlayers;
                    Settings.MaxRecBuffer = MaxRecBuffer;
                    Settings.MaxSendBuffer = MaxSendBuffer;
                    Settings.MaxStreamPool = MaxStreamPool;
                    Settings.BandwidthTimeout = BandwidthTimeout;
                    Settings.BandwidthCounter = BandwidthCounter;
                    Settings.IncrementalGc = IncrementalGc;
                    SaveSettingsFile();
                }
            }
        }

        private void Reset()
        {
            if (!Application.isPlaying)
                SaveSettingsFile();

            AtomLogger.Print("Wait for save settings... 3 seconds.....Don't play!");
            Build = Enum.Parse<BuildMode>(Settings.DebugMode);
            Encoding = Enum.Parse<EncodingType>(Settings.Encoding.Replace("UTF-8", "UTF8").Replace("UTF-16", "UTF16").Replace("UTF-32", "UTF32"));
            MaxUdpMessageSize = Settings.MaxUdpPacketSize;
            MaxPlayers = Settings.MaxPlayers;
            MaxRecBuffer = Settings.MaxRecBuffer;
            MaxSendBuffer = Settings.MaxSendBuffer;
            MaxStreamPool = Settings.MaxStreamPool;
            BandwidthTimeout = Settings.BandwidthTimeout;
            BandwidthCounter = Settings.BandwidthCounter;
            IncrementalGc = Settings.IncrementalGc;
        }
    }
#endif
}
#endif