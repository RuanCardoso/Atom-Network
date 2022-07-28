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
using System;
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

            if (!string.IsNullOrEmpty(except))
            {
                var _except = except.Split(';').ToList();
                _defines.RemoveAll(x => _except.Contains(x));
            }

            for (int i = 0; i < defines.Length; i++)
            {
                string def = defines[i];
                if (remove) _defines.Remove(def);
                else if (!_defines.Contains(def)) _defines.Add(def);
            }

            string _defines_ = string.Join(";", _defines.ToArray());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, _defines_);
            PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Server, _defines_);
        }
#endif
        public static bool AOT()
        {
            StaticCompositeResolver.Instance.Register(GeneratedResolver.Instance, StandardResolver.Instance);
            MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(StaticCompositeResolver.Instance);
            return true;
        }

        public static bool Interval(ref double lastTime, double interval)
        {
            if (AtomTime.LocalTime >= lastTime + interval)
            {
                lastTime = AtomTime.LocalTime;
                return true;
            }
            else
                return false;
        }

        public static string ToBin(int value) => $"Bin: {Convert.ToString(value, 2).PadLeft(8, '0')}";
        public static string ToHex(int value) => $"0x{value:X} | {value}";
        public static int TwoThePowerOfN(int bits) => (int)Math.Pow(2, bits) - 1;
    }
}
#endif