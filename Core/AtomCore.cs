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
using Atom.Core.Attributes;
using Atom.Core.Wrappers;
using MarkupAttributes;
using System;
using System.Linq;
using UnityEngine;
using static Atom.Core.AtomGlobal;

namespace Atom.Core
{
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(AtomNetwork))]
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
        [Label("Timeout")][Range(1, 10)] public double BandwidthTimeout;
        [Box("Bandwidth/Server")][Label("Byte Rate")][ReadOnly] public string SERVER_REC_BYTES_RATE = "0 Bytes/s";
        [Label("Message Rate")][ReadOnly] public string SERVER_REC_MSG_RATE = "0 Bytes/s";
        [Box("Bandwidth/Client")][Label("Bytes Rate")][ReadOnly] public string CLIENT_REC_BYTES_RATE = "0 Bytes/s";
        [Label("Message Rate")][ReadOnly] public string CLIENT_REC_MSG_RATE = "0 Bytes/s";
#endif
        [Box("Settings")]
        public string[] Addresses;
        [NaughtyAttributes.InfoBox("Release mode is extremely slow, only use it on release builds!", NaughtyAttributes.EInfoBoxType.Normal)] public BuildMode Build;
        [NaughtyAttributes.InfoBox("ASCII is more bandwidth efficient!", NaughtyAttributes.EInfoBoxType.Normal)] public EncodingType Encoding;
        [Label("Max Message Size")][Range(1, 1532)][NaughtyAttributes.InfoBox("This value directly influences packet drop!", NaughtyAttributes.EInfoBoxType.Warning)] public int MaxUdpMessageSize;
        [Range(1, MAX_PLAYERS)][NaughtyAttributes.InfoBox("<= 255 = 1 Byte || > 255 <= 65535 = 2 Byte || 4 Byte", NaughtyAttributes.EInfoBoxType.Normal)][Label("Receive Size")] public int MaxPlayers;
        [NaughtyAttributes.InfoBox("An inappropriate size can drop packets, even on a localhost!", NaughtyAttributes.EInfoBoxType.Warning)][Label("Receive Size")] public int MaxRecBuffer;
        [NaughtyAttributes.InfoBox("An inappropriate size can delay sending data!", NaughtyAttributes.EInfoBoxType.Warning)][Label("Send Size")] public int MaxSendBuffer;
        public int ReceiveTimeout;
        public int SendTimeout;
        [Range(0.3f, 5f)] public float PingFrequency;
        [Range(1, 128)] public int MaxStreamPool;
        public bool BandwidthCounter;
        [Label("GC Incremental")] public bool IncrementalGc;
#endif
        private void Awake()
        {
            Module = this;
            NetworkTime = Time.timeAsDouble;
            LoadSettingsFile();
            Streams = new(() => new(true, false, false), Conf.MaxStreamPool, false, true, "AtomStreamPool");
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
                bool isSave = Conf.DebugMode != Build.ToString()
                    || Conf.Encoding != _encoding_
                    || Conf.MaxUdpPacketSize != MaxUdpMessageSize
                    || Conf.MaxPlayers != MaxPlayers
                    || Conf.MaxRecBuffer != MaxRecBuffer
                    || Conf.MaxSendBuffer != MaxSendBuffer
                    || Conf.MaxStreamPool != MaxStreamPool
                    || Conf.BandwidthTimeout != BandwidthTimeout
                    || Conf.BandwidthCounter != BandwidthCounter
                    || Conf.IncrementalGc != IncrementalGc
                    || Conf.ReceiveTimeout != ReceiveTimeout
                    || Conf.SendTimeout != SendTimeout
                    || Conf.PingFrequency != PingFrequency
                    || !Conf.Addresses.SequenceEqual(Addresses);
                if (isSave)
                {
                    AtomLogger.Print("Wait for save settings... 3 seconds.....Don't play!");
                    Conf.DebugMode = Build.ToString();
                    Conf.Encoding = _encoding_;
                    Conf.MaxUdpPacketSize = MaxUdpMessageSize;
                    Conf.MaxPlayers = MaxPlayers;
                    Conf.MaxRecBuffer = MaxRecBuffer;
                    Conf.MaxSendBuffer = MaxSendBuffer;
                    Conf.MaxStreamPool = MaxStreamPool;
                    Conf.BandwidthTimeout = BandwidthTimeout;
                    Conf.BandwidthCounter = BandwidthCounter;
                    Conf.IncrementalGc = IncrementalGc;
                    Conf.ReceiveTimeout = ReceiveTimeout;
                    Conf.SendTimeout = SendTimeout;
                    Conf.PingFrequency = PingFrequency;
                    Conf.Addresses = Addresses;
                    SaveSettingsFile();
                }
            }
        }

        private void Reset()
        {
            if (!Application.isPlaying)
                SaveSettingsFile();

            AtomLogger.Print("Wait for save settings... 3 seconds.....Don't play!");
            Build = Enum.Parse<BuildMode>(Conf.DebugMode);
            Encoding = Enum.Parse<EncodingType>(Conf.Encoding.Replace("UTF-8", "UTF8").Replace("UTF-16", "UTF16").Replace("UTF-32", "UTF32"));
            MaxUdpMessageSize = Conf.MaxUdpPacketSize;
            MaxPlayers = Conf.MaxPlayers;
            MaxRecBuffer = Conf.MaxRecBuffer;
            MaxSendBuffer = Conf.MaxSendBuffer;
            MaxStreamPool = Conf.MaxStreamPool;
            BandwidthTimeout = Conf.BandwidthTimeout;
            BandwidthCounter = Conf.BandwidthCounter;
            IncrementalGc = Conf.IncrementalGc;
            ReceiveTimeout = Conf.ReceiveTimeout;
            SendTimeout = Conf.SendTimeout;
            PingFrequency = Conf.PingFrequency;
            Addresses = Conf.Addresses;
        }
#endif
    }
}
#endif