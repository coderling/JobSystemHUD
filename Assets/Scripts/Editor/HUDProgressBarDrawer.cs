using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HUD
{
    public class HUDProgressBarDrawer : HUDComponentDrawer
    {
        SerializedObject taret;
        SerializedObject s_bar;
        SerializedProperty value;

        HUDSpriteDrawer bg_drawer;
        HUDSpriteDrawer bar_drawer;


        public void Init(SerializedObject t)
        {
            taret = t;
            taret.Update();
            var bg = t.FindProperty("background");
            bg_drawer = new HUDSpriteDrawer();
            bg_drawer.Init(new SerializedObject(bg.objectReferenceValue));
            var bar = t.FindProperty("progressbar");
            bar_drawer = new HUDSpriteDrawer();
            s_bar = new SerializedObject(bar.objectReferenceValue);
            bar_drawer.Init(s_bar);

            SerializedProperty sp = s_bar.FindProperty("sprite");
            value = sp.FindPropertyRelative("progress_value");
        }

        public bool Render()
        {
            taret.Update();
            s_bar.Update();
            bool ret = false;
            if (bg_drawer.Render())
                ret = true;
            if (bar_drawer.Render())
                ret = true;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(value, 0, 1, "progress");
            
            if (EditorGUI.EndChangeCheck())
            {
                s_bar.ApplyModifiedProperties();
                ret = true;
            }

            return ret;
        }
    }
}
