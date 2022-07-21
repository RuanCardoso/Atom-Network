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
        public const int RealibleSize = 9;
        public const int UnrealibleSize = 5;
        public static AtomPooling<AtomStream> AtomStreamPool { get; } = new(() => new(true), 10, false, true, "AtomStreamPool");
        private void Awake()
        {
            AtomGlobal.LoadSettingsFile();
        }
    }
}
#endif