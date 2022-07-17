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

#if UNITY_EDITOR
#if UNITY_2021_3_OR_NEWER
using UnityEditor;
using UnityEngine;

namespace Atom.Core
{
    public static class AtomLoad
    {
        [InitializeOnLoadMethod]
        static void Load()
        {
            switch (AtomGlobal.DebugMode)
            {
                case "Debug":
                    AtomHelper.SetDefine(false, "ATOM_RELEASE", "ATOM_DEBUG");
                    break;
                case "Release":
                    AtomHelper.SetDefine(false, "ATOM_DEBUG", "ATOM_RELEASE");
                    break;
                default:
                    Debug.LogError("Atom.Core: Debug mode not found!");
                    break;
            }
        }

        [MenuItem("Atom/Create Settings File")]
        static void CreateSettingsFile() =>
            AtomGlobal.CreateSettingsFile();
    }
}
#endif
#endif