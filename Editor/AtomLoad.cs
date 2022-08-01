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
using MarkupAttributes.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Atom.Core.Editor
{
    public static class AtomLoad
    {
        [InitializeOnLoadMethod]
        public static void Load()
        {
            if (!EditorApplication.isPlaying)
                AtomGlobal.LoadSettingsFile();
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

    class MyCustomBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) =>
            AtomLoad.Load();
    }

    [CustomEditor(typeof(Marked), true)]
    class LoadMarked : MarkedUpEditor
    {

    }
}
#endif
#endif