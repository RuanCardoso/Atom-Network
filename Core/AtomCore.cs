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
        public const int RELIABLE_SIZE = 7;
        public const int UNRELIABLE_SIZE = 3;
        public static AtomPooling<AtomStream> StreamPool { get; private set; }

        private void Awake()
        {
            AtomGlobal.LoadSettingsFile();
            StreamPool = new(() => new(true, false, false), AtomGlobal.Settings.MaxStreamPool, false, true, "AtomStreamPool");
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
        }
    }
}
#endif