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

namespace Atom.Core.Attributes
{
#pragma warning disable IDE1006
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class gRPCAttribute : Attribute
#pragma warning restore IDE1006
    {
        internal readonly byte id;
        public gRPCAttribute(byte id)
        {
            this.id = id;
        }
    }
}
#endif