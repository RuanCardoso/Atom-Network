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
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Atom.Core
{
    public static class AtomLoad
    {
        [InitializeOnLoadMethod]
        static void Load()
        {
            if (!EditorApplication.isPlaying)
                AtomGlobal.LoadSettingsFile();

            switch (AtomGlobal.DebugMode)
            {
                case "Debug":
                case "debug":
                    AtomHelper.SetDefine(false, "ATOM_RELEASE", "ATOM_DEBUG");
                    break;
                case "Release":
                case "release":
                    AtomHelper.SetDefine(false, "ATOM_DEBUG", "ATOM_RELEASE");
                    break;
                default:
                    Debug.LogError("Atom.Core: Debug mode not found!");
                    break;
            }
        }

        [MenuItem("Atom/Setup", priority = -10)]
        static void Setup()
        {
            if (Object.FindObjectOfType(typeof(AtomCore)) is null)
            {
                GameObject go = new("Atom Core");
                go.AddComponent<AtomCore>();
                EditorUtility.SetDirty(go);
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            }
        }
    }
}
#endif
#endif