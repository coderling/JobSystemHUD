using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HUD
{
    public class HUDTextDrawer : HUDComponentDrawer 
    {
        SerializedObject target;

        SerializedProperty content;
        SerializedProperty text;
        SerializedProperty font_size;
        SerializedProperty color;
        SerializedProperty spacing;

        SerializedProperty position;
        SerializedProperty scale;

        public void Init(SerializedObject t)
        {
            target = t;
            text = t.FindProperty("text");
            content = t.FindProperty("_content");
            font_size = text.FindPropertyRelative("gscale");
            color = text.FindPropertyRelative("color");
            spacing = text.FindPropertyRelative("spacing");
            position = text.FindPropertyRelative("local_position");
            scale = text.FindPropertyRelative("local_scale");
        }

        public bool Render()
        {
            target.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(position, new GUIContent("position"));
            //EditorGUILayout.PropertyField(scale, new GUIContent("scale"));
            
            EditorGUILayout.PropertyField(color,new GUIContent("color"));
            EditorGUILayout.PropertyField(spacing,new GUIContent("font spacing"));
            var old_content = content.stringValue;
            content.stringValue = EditorGUILayout.TextField("content", content.stringValue);
            font_size.floatValue = EditorGUILayout.FloatField("font size", font_size.floatValue);
            if(EditorGUI.EndChangeCheck())
            {
                target.ApplyModifiedProperties();
                (target.targetObject as HUDText).ForceRebuild();
                return true;
            }
            return false;
        }
    }
}
