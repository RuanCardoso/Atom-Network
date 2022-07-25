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
using Atom.Core.Wrappers;
using UnityEngine;

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
        public static AtomPooling<AtomStream> StreamPool { get; private set; }
        public static double NetworkTime { get; private set; }

        private void Awake()
        {
            NetworkTime = Time.timeAsDouble;
            AtomGlobal.LoadSettingsFile();
            {
                StreamPool = new(() => new(true, false, false), AtomGlobal.Settings.MaxStreamPool, false, true, "AtomStreamPool");
            }
        }

        private void Start()
        {
            NetworkTime = Time.timeAsDouble;
            Application.targetFrameRate = 60;
        }

        private void Update()
        {
            NetworkTime = Time.timeAsDouble;
        }
    }
}
#endif