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
using Atom.Core.Attribute;
using UnityEditor;
using UnityEngine;

namespace Atom.Core.Editor
{
    [CustomPropertyDrawer(typeof(LabelAttribute))]
    public class LabelDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LabelAttribute attr = attribute as LabelAttribute;
            label.text = attr.label;
            EditorGUI.PropertyField(position, property, label);
        }
    }
}
#endif
#endif
