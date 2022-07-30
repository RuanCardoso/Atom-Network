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
using Atom.Core.Attributes;
using Atom.Core.Wrappers;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Atom.Core
{
    [DefaultExecutionOrder(-2)]
    public class AtomBehaviour : MonoBehaviour
    {
        internal static readonly Dictionary<byte, Action<AtomStream, bool, int>> gRPCMethods = new();
        private void Awake()
        {
            Type typeOf = this.GetType();
            MethodInfo[] methods = typeOf.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                gRPCAttribute attr = method.GetCustomAttribute<gRPCAttribute>(true);
                if (attr != null)
                {
                    if (method.GetParameters().Length < 3)
                        throw new Exception($"gRPC method with id: {attr.id} -> name: {method.Name} -> requires the (AtomStream, bool, int) parameter in the same order as the method signature.");
                    Action<AtomStream, bool, int> gRPC = method.CreateDelegate(typeof(Action<AtomStream, bool, int>), this) as Action<AtomStream, bool, int>;
                    if (!gRPCMethods.TryAdd(attr.id, gRPC))
                        throw new Exception($"gRPC method with id: {attr.id} -> name: {method.Name} -> already exists. Obs: Don't add this to multi-instance objects, eg: Your player. A unique id is required.");
                }
            }
        }
    }
}
#endif