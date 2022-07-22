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
        public const byte ChannelMask = 0x3;
        public const byte OperationMask = 0x3;
        public const byte TargetMask = 0x7;
        public const int RealibleSize = 7;
        public const int UnrealibleSize = 3;
        public static AtomPooling<AtomStream> AtomStreamPool { get; } = new(() => new(true), 10, false, true, "AtomStreamPool");
        public static AtomPooling<AtomMessage> AtomMessagePool { get; } = new(() => new(), 10, false, true, "AtomMessagePool");

        private void Awake()
        {
            AtomGlobal.LoadSettingsFile();
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
        }
    }
}
#endif