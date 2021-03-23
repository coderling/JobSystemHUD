using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace HUD
{
    public class HUDTransform : MonoBehaviour
    {
        protected HUDGroup group = new HUDGroup();
        public HUDGroup Group { get { return group; } }
        public string[] field_names;
        public HUDComponent[] hud_components;

        [SerializeField]
        private MeshFilter mesh_filter;
        [SerializeField]
        private MeshRenderer mesh_renderer;

        protected virtual void Awake()
        {
            mesh_renderer.sharedMaterial = HUDManager.Instance.Setting.atlas_material;
            foreach(var com in hud_components)
            {
                com.Attach(group);
            }
            HUDManager.Instance.ActiveHUDGroup(group);
        }

        public T GetHUDComponent<T>(string name) where T : HUDComponent
        {
            int index = System.Array.IndexOf<string>(field_names, name);
            if(index < 0)
            {
                return null;
            }    
            return hud_components[index] as T;
        }

        private void OnEnable()
        {
            HUDManager.Instance.ActiveHUDGroup(group);
            mesh_filter.sharedMesh = group.mesh;
            foreach(var c in hud_components)
            {
                if(!c.enabled)
                {
                    c.enabled = true;
                }
            }
        }

        private void OnDisable()
        {
            HUDManager.Instance.DeActiveHUDGroup(group);
            mesh_filter.sharedMesh = null;
            foreach(var c in hud_components)
            {
                if(c.enabled)
                {
                    c.enabled = false;
                }
            }
        }

        private void OnDestroy()
        {
            HUDManager.Instance.DeActiveHUDGroup(group);
            foreach(var c in hud_components)
            {
                c.UnAttach();
            }
        }
    }
}
