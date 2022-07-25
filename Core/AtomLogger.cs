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
using System;
using UnityEngine;

namespace Atom.Core
{
    public static class AtomLogger
    {
        public static void Print(string message) => Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", message);
        public static void PrintError(string message) => Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", message);
        public static void PrintWarning(string message) => Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, "{0}", message);
        public static void Log(string message) => Debug.LogFormat(LogType.Log, LogOption.None, null, "{0}", message);
        public static void LogError(string message) => Debug.LogFormat(LogType.Log, LogOption.None, null, "{0}", message);
        public static void LogWarning(string message) => Debug.LogFormat(LogType.Warning, LogOption.None, null, "{0}", message);
        public static void LogStacktrace(Exception message) => Debug.LogException(message);
    }
}
#endif