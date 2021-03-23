using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HUD
{
    public class HUDEditorWindow : EditorWindow
    {
        [MenuItem("HUD/Editor")]
        public  static void Open()
        {
            UnityEditor.Compilation.CompilationPipeline.assemblyCompilationStarted += CompileStart;
            win = EditorWindow.GetWindow<HUDEditorWindow>();
        }

        private static HUDEditorWindow win;
        private static void CompileStart(string aaa)
        {
            if(win != null && win.preview != null)
            {
                win.preview.Cleanup();
            }
        }

        PreviewRenderUtility preview;
        PreviewRenderUtility previewUtility
        {
            get
            {
                if(preview == null)
                {
                    preview = new PreviewRenderUtility();
                    preview.cameraFieldOfView = 60f;
                    preview.camera.transform.position = new Vector3(0, 0, -10);
                    preview.camera.nearClipPlane = 0.1f;
                    preview.camera.farClipPlane = 100f;
                    preview.camera.orthographic = true;
                }

                return preview;
            }
        }

        private void OnDestroy()
        {
            previewUtility.Cleanup();
        }

        bool need_tick = false;
        private void OnEnable()
        {  
            var setting = AssetDatabase.LoadAssetAtPath<HUDSetting>(HUDSetting.setting_path);
            HUDManager.Instance.Init(setting);
            EditorApplication.update += TickManager;
            need_tick = true;
        }

        private void OnDisable()
        {
            HUDManager.DestoryManager();
            need_tick = false;
        }

        private void TickManager()
        {
            if (!need_tick)
                return;
            HUDManager.Instance.Update();
        }

        HUDTransform trans;
        HUDTransform edit_trans;
        Transform component_node;
        SerializedObject serializedObject;
        SerializedProperty components_serialized;
        SerializedProperty fieldnames_serialized;
        HUDComponent expand_component;
        HUDComponent delete_component;

        Dictionary<HUDComponent, HUDComponentDrawer> drawers = new Dictionary<HUDComponent, HUDComponentDrawer>();

        private HUDComponentDrawer GetDrawer(HUDComponent com)
        {
            HUDComponentDrawer drawer = null;
            if(!drawers.TryGetValue(com , out drawer))
            {
                if(com is HUDSprite)
                {
                    drawer = new HUDSpriteDrawer();
                    drawer.Init(new SerializedObject(com as HUDSprite));
                    drawers.Add(com, drawer);
                }
                else if(com is HUDText)
                {
                    drawer = new HUDTextDrawer();
                    drawer.Init(new SerializedObject(com as HUDText));
                    drawers.Add(com, drawer);
                }
                else if(com is HUDProgressBar)
                {
                    drawer = new HUDProgressBarDrawer();
                    drawer.Init(new SerializedObject(com as HUDProgressBar));
                    drawers.Add(com, drawer);
                }
            }

            return drawer;
        }

        private const int left_width = 300;
        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(left_width));
            DrawTarget();
            DrawHUDComponents();
            GUILayout.EndVertical();
            DrawPreview();
            GUILayout.BeginHorizontal();
            if(edit_trans != null)
            {
                HUDEditorHelper.InnerDepthSort(edit_trans.Group);
                edit_trans.Group.ForceRebuild();
            }
        }

        private void DrawTarget()
        {
            var current = EditorGUILayout.ObjectField(trans, typeof(HUDTransform), false) as HUDTransform;
            if(current != trans)
            {
                if (edit_trans != null)
                {
                    HUDManager.Instance.DeActiveHUDGroup(edit_trans.Group);
                    Object.DestroyImmediate(edit_trans.gameObject);
                }
                trans = current;
                if(current != null)
                {
                    var gb = previewUtility.InstantiatePrefabInScene(current.gameObject);
                    edit_trans = gb.GetComponent<HUDTransform>();
                    serializedObject = new SerializedObject(edit_trans);
                    fieldnames_serialized = serializedObject.FindProperty("field_names");
                    components_serialized = serializedObject.FindProperty("hud_components");
                    var node = gb.transform.Find("components");
                    if(node == null)
                    {
                        var c_gb = new GameObject("components");
                        node = c_gb.transform;
                        node.SetParent(edit_trans.transform, false);
                    }

                    component_node = node;

                    foreach (var com in edit_trans.hud_components)
                    {
                        com.Attach(edit_trans.Group);
                    }
                    HUDManager.Instance.ActiveHUDGroup(edit_trans.Group);
                    CheckMeshRender();
                    previewUtility.AddSingleGO(edit_trans.gameObject);
                }

            }
            if(serializedObject != null)
            {
                serializedObject.Update();
            }

            DrawOptions();
        }

        private void DrawPreview()
        {
            var trect = EditorGUILayout.GetControlRect(false, 512, GUILayout.Width(this.position.width - left_width));
            Rect rect = new Rect(0, 0, 1920.0f / 2, 1080.0f / 2);
            var rrect = rect;
            rrect.width *= 2;
            rrect.height *= 2;
            previewUtility.BeginPreview(rrect, (GUIStyle)"PreBackground");
            previewUtility.camera.Render();
            preview.EndAndDrawPreview(trect);
        }

        private bool DrawAComponents(int index, HUDComponent component)
        {
            bool l_state = component.enabled;
            component.enabled = EditorGUILayout.Toggle("显示/隐藏", component.enabled);
            if(l_state != component.enabled)
            {
                component.OnValidate();
            }

            bool is_expand = component == expand_component;
            var old_name = GetFieldName(index);
            var new_name = EditorGUILayout.TextField(old_name);
            if(new_name != old_name)
            {
                SetFieldNames(index, new_name);
            }
            string content = " type: " + component.GetType().Name;
            is_expand = EditorGUILayout.Foldout(is_expand, content, true);
            if(is_expand)
            {
                expand_component = component;
                EditorGUI.indentLevel++;
                var drawer = GetDrawer(component);
               if( drawer.Render())
                {
                }
                EditorGUI.indentLevel--;
            }
            else if(component == expand_component)
            {
                expand_component = null;
            }

            if(GUILayout.Button("delete"))
            {
                delete_component = component;
                return true;
            }
            return false;
        }

        string add_name;
        int add_index = -1;
        private void DrawOptions()
        {
            add_name = EditorGUILayout.TextField(add_name);
            add_index = EditorGUILayout.Popup(add_index, HUDEditorHelper.HUDComponentNamesArray());
            if(add_index >= 0)
            {
                var ty = HUDEditorHelper.GetComponentType(add_index);
                add_index = -1;
                AddNewComponent(ty);
            }

            if(GUILayout.Button("Save"))
            {
                bool suc = false;
                CheckMeshRender();
                PrefabUtility.SaveAsPrefabAssetAndConnect(edit_trans.gameObject, AssetDatabase.GetAssetPath(trans), InteractionMode.AutomatedAction, out suc);
            }
        }

        private void AddNewComponent(System.Type t)
        {
            var component = component_node.gameObject.AddComponent(t) as HUDComponent;
            AddComponent(add_name, component);
            if(component is HUDProgressBar)
            {
                var bg = component_node.gameObject.AddComponent<HUDSprite>();
                var bar = component_node.gameObject.AddComponent<HUDSprite>();
                SerializedObject obj = new SerializedObject(component);
                obj.FindProperty("background").objectReferenceValue = bg;
                obj.FindProperty("progressbar").objectReferenceValue = bar;
                obj.ApplyModifiedPropertiesWithoutUndo();
            }



            component.Attach(edit_trans.Group);
            HUDManager.Instance.RegisterHUDGroupRebuildMesh(edit_trans.Group);
        }

        private void OnRemoveComponent(HUDComponent component)
        {
            if(component is HUDProgressBar)
            {
                SerializedObject obj = new SerializedObject(component);
                var bg = obj.FindProperty("background").objectReferenceValue;
                var bar = obj.FindProperty("progressbar").objectReferenceValue;
                
                if(bg != null)
                {
                    Object.DestroyImmediate(bg);
                }

                if(bar != null)
                {
                    Object.DestroyImmediate(bar);
                }
            }
               
        }

        private void CheckMeshRender()
        {
            var mesh_filter = edit_trans.GetComponent<MeshFilter>();
            if (mesh_filter == null)
            {
                mesh_filter = edit_trans.gameObject.AddComponent<MeshFilter>();
            }

            mesh_filter.mesh = edit_trans.Group.mesh;

            var mesh_render = edit_trans.GetComponent<MeshRenderer>();
            if (mesh_render == null)
            {
                mesh_render = edit_trans.gameObject.AddComponent<MeshRenderer>();
            }

            mesh_render.material = HUDManager.Instance.Setting.atlas_material;
            mesh_render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mesh_render.receiveShadows = false;
            mesh_render.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mesh_render.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            mesh_render.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
            mesh_render.allowOcclusionWhenDynamic = false;
        }

        Vector2 com_scroll;
        private void DrawHUDComponents()
        {
            if (edit_trans == null)
                return;
            com_scroll = GUILayout.BeginScrollView(com_scroll);
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(left_width - 20));
            int ind = 0;
            foreach (var com in edit_trans.hud_components)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(left_width - 25));
                DrawAComponents(ind, com);
                GUILayout.EndVertical();
                GUILayout.Space(5);
                ++ind;
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            HandleDelete();
            serializedObject.ApplyModifiedProperties();
        }

        List<HUDComponent> tmp_list = new List<HUDComponent>();
        private void HandleDelete()
        {
            if(delete_component == null)
            {
                return;
            }

            var index = System.Array.IndexOf(edit_trans.hud_components, delete_component);
            delete_component.UnAttach();
            OnRemoveComponent(delete_component);
            Object.DestroyImmediate(delete_component);
            List<string> names = new List<string>();
            List<HUDComponent> coms = new List<HUDComponent>();
            for(int i = 0; i < edit_trans.hud_components.Length; ++i)
            {
                var c = edit_trans.hud_components[i];
                if(c != null)
                {
                    coms.Add(c);
                    names.Add(edit_trans.field_names[i]);
                }
            }

            edit_trans.field_names = names.ToArray();
            edit_trans.hud_components = coms.ToArray();
            delete_component = null;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetFieldNames(int index, string name)
        {
            fieldnames_serialized.FindPropertyRelative(string.Format("Array.data[{0}]", index)).stringValue = name;
        }

        private string GetFieldName(int index)
        {
            return fieldnames_serialized.FindPropertyRelative(string.Format("Array.data[{0}]", index)).stringValue;
        }

        private void SetComponent(int index, HUDComponent component)
        {
            components_serialized.FindPropertyRelative(string.Format("Array.data[{0}]", index)).objectReferenceValue = component;
        }

        private void AddComponent(string name, HUDComponent component)
        {
            fieldnames_serialized.arraySize = fieldnames_serialized.arraySize + 1;
            components_serialized.arraySize = components_serialized.arraySize + 1;
            int index = fieldnames_serialized.arraySize - 1;
            SetFieldNames(index, name);
            SetComponent(index, component);
            serializedObject.ApplyModifiedProperties();
        }
    }
}


///////// todo
/// 1. 检查，删除操作
/// 2. 清空时异常处理
/// 3. 修改测试，分离batch是否可行
