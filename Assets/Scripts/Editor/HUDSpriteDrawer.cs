using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental;

namespace HUD
{
    public interface HUDComponentDrawer
    {
        void Init(SerializedObject t);
        bool Render();
    }

    public class HUDSpriteDrawer : HUDComponentDrawer
    {
        private SerializedObject target;
        private SerializedProperty sprite;
        private SerializedProperty color;
        private SerializedProperty position;
        private SerializedProperty scale;
        private SerializedProperty size;
        private SerializedProperty crc;
        private bool lock_radio = true;

        public void Init(SerializedObject t)
        {
            target = t;
            sprite = t.FindProperty("sprite");
            position = sprite.FindPropertyRelative("local_position");
            scale = sprite.FindPropertyRelative("local_scale");
            size = sprite.FindPropertyRelative("sizes").FindPropertyRelative("Array.data[0]");
            color = sprite.FindPropertyRelative("color");
            crc = target.FindProperty("crc_path");
        }

        public bool Render()
        {
            target.Update();
            EditorGUI.BeginChangeCheck();
            var tex = EditorGUILayout.ObjectField("Sprite", null, typeof(Texture2D), false);
            if(tex != null)
            {
                var assetpath = AssetDatabase.GetAssetPath(tex);
                var new_crc = HUDHelper.GetCRC32(assetpath);
                Rect uv_rect;

                if(HUDManager.Instance.Setting.atlas.QueryUV(new_crc, out uv_rect))
                {
                    var uv_property = sprite.FindPropertyRelative("uv0_rect").FindPropertyRelative("Array.data[0]");
                    uv_property.FindPropertyRelative("x").floatValue = uv_rect.xMin;
                    uv_property.FindPropertyRelative("y").floatValue = uv_rect.yMin;
                    uv_property.FindPropertyRelative("z").floatValue = uv_rect.width;
                    uv_property.FindPropertyRelative("w").floatValue = uv_rect.height;
                }
                else
                {
                    var w = EditorWindow.mouseOverWindow;
                    w.ShowNotification(new GUIContent("不在图集中", EditorGUIUtility.Load(EditorResources.iconsPath + "console.erroricon.png") as Texture2D));
                }
            }
            EditorGUILayout.PropertyField(position, new GUIContent("position"));
            EditorGUILayout.PropertyField(scale, new GUIContent("scale"));
            EditorGUILayout.PropertyField(size, new GUIContent("size"));
            EditorGUILayout.PropertyField(color,new GUIContent("color"));


            if(GUILayout.Button("NativeSize"))
            {
                var tex_size = HUDManager.Instance.Setting.atlas.size;
                var uv_property = sprite.FindPropertyRelative("uv0_rect").FindPropertyRelative("Array.data[0]");
                float width = uv_property.FindPropertyRelative("z").floatValue;
                float height = uv_property.FindPropertyRelative("w").floatValue;
                float n_width = Mathf.Floor(tex_size.x * width);
                float n_height = Mathf.Floor(tex_size.y * height);
                size.FindPropertyRelative("x").floatValue = n_width;
                size.FindPropertyRelative("y").floatValue = n_height;
            }

            if(EditorGUI.EndChangeCheck())
            {
                target.ApplyModifiedProperties();
                return true;
            }

            return false;
        }
    }
}
