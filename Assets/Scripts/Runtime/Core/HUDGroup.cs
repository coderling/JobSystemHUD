using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace HUD
{
    public enum DirtyFlag
    {
        ENone = 0,
        ETransform = 1 << 0,
        EQuad = 1 << 1,
    }

    public class HUDGroup 
    {
        private static int unique_id_gen = 0;

         public int unique_id { get; private set; }

        public HUDGroup()
        {
            unique_id = ++unique_id_gen;
        }

        public Mesh mesh;
        
        // 申请够最大顶点
        private MeshInfo _mesh_info;
        public MeshInfo mesh_info
        {
            get
            {
                if(_mesh_info == null)
                {
                    _mesh_info = new MeshInfo(max_quad_count);
                }
                else if(max_quad_count > _mesh_info.quad_count)
                {
                    _mesh_info.Resize(max_quad_count);
                }

                return _mesh_info;
            }
        }

        private List<HUDGraphic> _items = new List<HUDGraphic>();
        public List<HUDGraphic> items { get { return _items; } }
        private int max_quad_count = 0;

        public void ForceRebuild()
        {
            var manager = HUDManager.Instance;
            foreach(var g in items)
            {
                manager.RebuildGraphic(g, HUDBatch.all_dirty);
            }
        }

        public void OnComponentAttach(LinkedList<HUDGraphic> graphics)
        {
            foreach(var g in graphics)
            {
                g.group = this;
                items.Add(g);
                HUDManager.Instance.AddGraphic(g);
                max_quad_count += (int)g.size;
            }
        }

        public void OnComponentUnAttach(LinkedList<HUDGraphic> graphics)
        {
            foreach(var g in graphics)
            {
                items.Remove(g);
                max_quad_count -= (int)g.size;
                HUDManager.Instance.RemoveGraphic(g);
                g.group = null;
            }

        }
    }
}
