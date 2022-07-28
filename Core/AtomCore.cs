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
        [Box("Bandwidth")]
        [Box("Bandwidth/Server")][Label("Byte Rate")][ReadOnly] public string SERVER_REC_BYTES_RATE = "0 Bytes/s";
        [Label("Message Rate")][ReadOnly] public string SERVER_REC_MSG_RATE = "0 Bytes/s";
        [Box("Bandwidth/Client")][Label("Bytes Rate")][ReadOnly] public string CLIENT_REC_BYTES_RATE = "0 Bytes/s";
        [Label("Message Rate")][ReadOnly] public string CLIENT_REC_MSG_RATE = "0 Bytes/s";
        [Box("Settings")]
        public BuildMode Build = BuildMode.Debug;
        public EncodingType Encoding = EncodingType.ASCII;
        [Label("Max Message Size")] public int MaxUdpMessageSize = 8192;
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
            Application.targetFrameRate = 256;
        }

        private void Update()
        {
            NetworkTime = Time.timeAsDouble;
        }

        private void OnValidate()
        {
            bool isSave = Settings.DebugMode != Build.ToString();
            Settings.DebugMode = Build.ToString();
            if (isSave)
                SaveSettingsFile();
        }
    }
}
#endif