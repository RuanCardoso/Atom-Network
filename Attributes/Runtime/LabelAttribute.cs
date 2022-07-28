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
using UnityEngine;

namespace Atom.Core.Attribute
{
    public class LabelAttribute : PropertyAttribute
    {
        internal readonly string label;
        public LabelAttribute(string label)
        {
            this.label = label;
        }
    }
}
#endif
#endif