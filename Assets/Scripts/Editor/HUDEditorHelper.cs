using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace HUD
{
    public static class HUDEditorHelper
    {
        [MenuItem("HUD/Atlas")]
        public static void CreateTest()
        {
            var guids = Selection.assetGUIDs;
            List<string> paths = new List<string>();
            foreach(var id in guids)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(id));
            }

            CreateAtlats(paths.ToArray(), HUDSetting.altas_path);
        }

        public static void CreateAtlats(string[] textures, string path)
        {
            HUDAtlas atlas = AssetDatabase.LoadAssetAtPath<HUDAtlas>(path);
            if(atlas == null)
            {
                atlas = ScriptableObject.CreateInstance<HUDAtlas>();
                AssetDatabase.CreateAsset(atlas, path);
                AssetDatabase.Refresh();
            }

            List<Texture2D> texture2ds = new List<Texture2D>();
            List<uint> crcids = new List<uint>();
            foreach(var p in textures)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                if (tex != null)
                {
                    var u_tex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false, false);
                     RenderTexture tmp = RenderTexture.GetTemporary(tex.width, tex.height,0,RenderTextureFormat.Default,RenderTextureReadWrite.Default);
                    Graphics.Blit(tex, tmp);
                    RenderTexture.active = tmp;
                    u_tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                    u_tex.Apply();
                    texture2ds.Add(u_tex);
                    crcids.Add(HUDHelper.GetCRC32(p));
                }
            }

            var alta_tex = new Texture2D(1, 1, TextureFormat.RGBA32, false, false);
            var uvs = alta_tex.PackTextures(texture2ds.ToArray(), 4, 2048, false);
            if(uvs == null)
            {
                Debug.LogError("package error");
                return;
            }
            var datas = alta_tex.EncodeToPNG();
            var atlas_tex_path = System.IO.Path.GetDirectoryName(path) + "/" + System.IO.Path.GetFileNameWithoutExtension(path) + ".png";
            System.IO.File.WriteAllBytes(atlas_tex_path, datas);
            AssetDatabase.ImportAsset(atlas_tex_path);
            atlas.crc32ids = crcids.ToArray();
            atlas.uvs = uvs;
            atlas.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlas_tex_path);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("HUD/create_setting")]
        private static void CreateSetting()
        {
            var setting = ScriptableObject.CreateInstance<HUDSetting>();
            setting.atlas = AssetDatabase.LoadAssetAtPath<HUDAtlas>(HUDSetting.altas_path);

            AssetDatabase.CreateAsset(setting, HUDSetting.setting_path);
        }


        private static MethodInfo open_method_info = null;
        public static GameObject OpenPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                return null;

            if(open_method_info == null)
            {
                var ty = typeof(UnityEditor.Experimental.SceneManagement.PrefabStageUtility);
                open_method_info = ty.GetMethod("OpenPrefab", BindingFlags.NonPublic | BindingFlags.Static, null, new System.Type[] { typeof(string) }, null);
            }

            open_method_info.Invoke(null, new string[] { path });

            return UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
        }

        private static MethodInfo save_prefab_method_info;
        public static bool SavePrefabStage(GameObject obj)
        {
            if(save_prefab_method_info == null)
            {
                var ty = typeof(UnityEditor.Experimental.SceneManagement.PrefabStage);
                save_prefab_method_info = ty.GetMethod("SavePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            var stage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetPrefabStage(obj);
            if(stage != null)
            {
                return (bool)save_prefab_method_info.Invoke(stage, null);
            }
            return false;
        }

        static string[] all_component_names;
        static System.Type[] all_component_types;
        public static string[] HUDComponentNamesArray()
        {
            if (all_component_names != null)
                return all_component_names;

            var types = Assembly.GetAssembly(typeof(HUDComponent)).GetTypes();
            List<string> names = new List<string>();
            List<System.Type> tys = new List<System.Type>();
            foreach(var t in types)
            {
                if(t.IsSubclassOf(typeof(HUDComponent)))
                {
                    tys.Add(t);
                    names.Add(t.Name);
                }
            }

            all_component_names = names.ToArray();
            all_component_types = tys.ToArray();
            return all_component_names;
        }

        public static System.Type GetComponentType(int index)
        {
            return all_component_types[index];
        }

        public static void InnerDepthSort(HUDGroup group)
        {
            group.items.Sort((lg, rg) => { return lg.local_position.z.CompareTo(rg.local_position.z); });
        }
    }
}
