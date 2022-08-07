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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Atom.Core.AtomGlobal;
using Random = System.Random;

namespace Atom.Core
{
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(AtomNetwork))]
    public class AtomCore : MarkedUp
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
#else
        public const int RELIABLE_SIZE = 6;
        public const int UNRELIABLE_SIZE = 2;
#endif
        public static AtomCore Module { get; private set; }
        public static AtomPooling<AtomStream> Streams { get; private set; }
        public static AtomPooling<AtomStream> StreamsToWaitAck { get; private set; }
        public static double NetworkTime { get; private set; }

#if UNITY_EDITOR
#if ATOM_BANDWIDTH_COUNTER
        [Foldout("Bandwidth Manager")]
        [Label("Timeout")][Range(0.3f, 10f)] public double BandwidthTimeout;
        [Foldout("Bandwidth Manager/Download", true)]
        [Box("Bandwidth Manager/Download/Server")][Label("Byte Rate")][ReadOnly] public string SERVER_REC_BYTES_RATE = "0 Bytes/s";
        [Label("Message Rate")][ReadOnly] public string SERVER_REC_MSG_RATE = "0 Bytes/s";
        [Box("Bandwidth Manager/Download/Client")][Label("Bytes Rate")][ReadOnly] public string CLIENT_REC_BYTES_RATE = "0 Bytes/s";
        [Label("Message Rate")][ReadOnly] public string CLIENT_REC_MSG_RATE = "0 Bytes/s";
        [Foldout("Bandwidth Manager/Upload", true)]
        [Box("Bandwidth Manager/Upload/Server")][Label("Byte Rate")][ReadOnly] public string SERVER_SENT_BYTES_RATE = "0 Bytes/s";
        [Label("Message Rate")][ReadOnly] public string SERVER_SENT_MSG_RATE = "0 Bytes/s";
        [Box("Bandwidth Manager/Upload/Client")][Label("Bytes Rate")][ReadOnly] public string CLIENT_SENT_BYTES_RATE = "0 Bytes/s";
        [Label("Message Rate")][ReadOnly] public string CLIENT_SENT_MSG_RATE = "0 Bytes/s";
#endif
        [Foldout("Settings Manager")]
        [Foldout("Settings Manager/Global")]
        [NaughtyAttributes.InfoBox("IL2CPP: Release mode is extremely slow to build, only use it on release versions!", NaughtyAttributes.EInfoBoxType.Normal)]
        [Foldout("Settings Manager/Global/Others")] public BuildMode Build;
        [NaughtyAttributes.InfoBox("ASCII is recommended for low bandwidth usage!", NaughtyAttributes.EInfoBoxType.Normal)] public EncodingType Encoding;
        public bool BandwidthCounter;
        [Label("GC Incremental")] public bool IncrementalGc;
        [NaughtyAttributes.InfoBox("Some values will directly influence the use of RAM memory!!", NaughtyAttributes.EInfoBoxType.Warning)]
        [NaughtyAttributes.InfoBox("All properties can have different values between server and client!", NaughtyAttributes.EInfoBoxType.Normal)]
        [Foldout("Settings Manager/Global/Socket")][Label("Max Message Size")][Range(1, 1532)][NaughtyAttributes.InfoBox("This value directly influences packet drop!", NaughtyAttributes.EInfoBoxType.Warning)] public int MaxUdpMessageSize;
        [NaughtyAttributes.InfoBox("An inappropriate size can drop packets, even on a localhost!", NaughtyAttributes.EInfoBoxType.Warning)]
        [Label("Receive Size")] public int MaxRecBuffer;
        [NaughtyAttributes.InfoBox("An inappropriate size can delay sending data!", NaughtyAttributes.EInfoBoxType.Warning)]
        [Label("Send Size")] public int MaxSendBuffer;
        public int ReceiveTimeout;
        public int SendTimeout;
        [NaughtyAttributes.InfoBox("Avoid using items from the pool to avoid allocating new objects when there are no items available!", NaughtyAttributes.EInfoBoxType.Warning)]
        [Foldout("Settings Manager/Global/Pools")][HideIf("AutoAllocateStreams")][Label("Unreliable Streams")] public int UnreliableMaxStreamPool;
        [HideIf("AutoAllocateStreams")][Label("Reliable Streams")] public int ReliableMaxStreamPool;
        [NaughtyAttributes.InfoBox("It can significantly affect performance!", NaughtyAttributes.EInfoBoxType.Warning)]
        [Label("Auto Resize Pool")] public bool AutoAllocateStreams;
        [Foldout("Settings Manager/Client")] public string[] Addresses;
        [Range(0.1f, 10f)] public float PingFrequency;
        [Foldout("Settings Manager/Server")][NaughtyAttributes.InfoBox("<= 255 = 1 Byte || > 255 <= 65535 = 2 Byte || 4 Byte", NaughtyAttributes.EInfoBoxType.Normal)] public int MaxPlayers;
#endif
        [NaughtyAttributes.InfoBox("Alternative, use https://github.com/jagt/clumsy", NaughtyAttributes.EInfoBoxType.Normal)]
        [Foldout("Junk Internet Simulator")][SerializeField] internal bool IsOn = false;
        [SerializeField][ReadOnly] private bool SimulateOnServer = true;
        [SerializeField][ReadOnly] private bool SimulateOnClient = true;
        [Label("Drop(%)")][Range(1, 100)] public int DropPercentage = 1;
        [Label("Lag(ms)")][Range(1, 120)] public int DelayPercentage = 1;

        private void Awake()
        {
            Module = this;
            NetworkTime = Time.timeAsDouble;
            LoadSettingsFile();
            Streams = new(() => new(true, false, false), Conf.UnreliableStreamPool, Conf.AutoAllocStreams, true, "AtomStreamPool");
            StreamsToWaitAck = new(() => new(true, false, false), Conf.ReliableStreamPool, Conf.AutoAllocStreams, true, "AtomStreamPoolToWaitAck");
        }

        private void Start()
        {
            NetworkTime = Time.timeAsDouble;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            /******************************************/
            SimulateOnClient = SimulateOnServer = true;
            SimulateOnServer = SimulateOnClient = true;
        }

        private void Update()
        {
            NetworkTime = Time.timeAsDouble;
        }

        Random random = new();
        public bool Drop() => IsOn && random.Next(1, 101) <= DropPercentage;

#if UNITY_EDITOR
        [ContextMenu("Save Settings")]
        private void Validate()
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
                    || Conf.UnreliableStreamPool != UnreliableMaxStreamPool
#if ATOM_BANDWIDTH_COUNTER
                    || Conf.BandwidthTimeout != BandwidthTimeout
#endif
                    || Conf.BandwidthCounter != BandwidthCounter
                    || Conf.IncrementalGc != IncrementalGc
                    || Conf.ReceiveTimeout != ReceiveTimeout
                    || Conf.SendTimeout != SendTimeout
                    || Conf.PingFrequency != PingFrequency
                    || !Conf.Addresses.SequenceEqual(Addresses)
                    || Conf.ReliableStreamPool != ReliableMaxStreamPool
                    || Conf.AutoAllocStreams != AutoAllocateStreams;
                if (isSave)
                {
                    Conf.DebugMode = Build.ToString();
                    Conf.Encoding = _encoding_;
                    Conf.MaxUdpPacketSize = MaxUdpMessageSize;
                    Conf.MaxPlayers = MaxPlayers;
                    Conf.MaxRecBuffer = MaxRecBuffer;
                    Conf.MaxSendBuffer = MaxSendBuffer;
                    Conf.UnreliableStreamPool = UnreliableMaxStreamPool;
                    Conf.AutoAllocStreams = AutoAllocateStreams;
#if ATOM_BANDWIDTH_COUNTER
                    Conf.BandwidthTimeout = BandwidthTimeout;
#endif
                    Conf.BandwidthCounter = BandwidthCounter;
                    Conf.IncrementalGc = IncrementalGc;
                    Conf.ReceiveTimeout = ReceiveTimeout;
                    Conf.SendTimeout = SendTimeout;
                    Conf.PingFrequency = PingFrequency;
                    Conf.Addresses = Addresses;
                    Conf.ReliableStreamPool = ReliableMaxStreamPool;
                    SaveSettingsFile();
                }
            }
        }

        private void Reset()
        {
            Build = Enum.Parse<BuildMode>(Conf.DebugMode);
            Encoding = Enum.Parse<EncodingType>(Conf.Encoding.Replace("UTF-8", "UTF8").Replace("UTF-16", "UTF16").Replace("UTF-32", "UTF32"));
            MaxUdpMessageSize = Conf.MaxUdpPacketSize;
            MaxPlayers = Conf.MaxPlayers;
            MaxRecBuffer = Conf.MaxRecBuffer;
            MaxSendBuffer = Conf.MaxSendBuffer;
            UnreliableMaxStreamPool = Conf.UnreliableStreamPool;
#if ATOM_BANDWIDTH_COUNTER
            BandwidthTimeout = Conf.BandwidthTimeout;
#endif
            BandwidthCounter = Conf.BandwidthCounter;
            IncrementalGc = Conf.IncrementalGc;
            ReceiveTimeout = Conf.ReceiveTimeout;
            SendTimeout = Conf.SendTimeout;
            PingFrequency = Conf.PingFrequency;
            Addresses = Conf.Addresses;
            ReliableMaxStreamPool = Conf.ReliableStreamPool;
            AutoAllocateStreams = Conf.AutoAllocStreams;
        }
#endif
    }
}
#endif