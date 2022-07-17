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
using MessagePack;
using MessagePack.Resolvers;
using System.Linq;
using UnityEditor;

namespace Atom.Core
{
    public static class AtomHelper
    {
#if UNITY_EDITOR
        public static void SetDefine(bool remove = false, string except = "", params string[] defines)
        {
            BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(activeBuildTarget);
            var _defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';').ToList();

            var _except = except.Split(';').ToList();
            _defines.RemoveAll(x => _except.Contains(x));

            for (int i = 0; i < defines.Length; i++)
            {
                string def = defines[i];
                if (!_defines.Contains(def)) _defines.Add(def);
                else if (remove) _defines.Remove(def);
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", _defines.ToArray()));
        }
#endif
        public static void AOT()
        {
            StaticCompositeResolver.Instance.Register(
                GeneratedResolver.Instance,
                StandardResolver.Instance);
            MessagePackSerializer.DefaultOptions =
                MessagePackSerializerOptions.Standard.WithResolver(StaticCompositeResolver.Instance);
        }
    }
}
#endif