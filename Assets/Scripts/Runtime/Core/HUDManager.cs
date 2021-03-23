using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace HUD
{

    public class HUDManager 
    {
        private static HUDManager _instance;
        public static HUDManager Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new HUDManager();
                }
                return _instance;
            }
        }

        public static void DestoryManager()
        {
            if(_instance != null)
            {
                _instance.OnDestroy();
                _instance = null;
            }
        }

        HashSet<HUDGroup> group_rebuild_mesh = new HashSet<HUDGroup>();
        Dictionary<HUDGraphic, HUDBatch.OperationType> need_rebuild_graphics = new Dictionary<HUDGraphic, HUDBatch.OperationType>();

        private HUDBatch sprite_batch;
        private HUDBatch text_batch;
        private List<HUDBatch> batches = new List<HUDBatch>();
        private HUDBatchData batch_data; 

        private HUDCollectionMeshInfoJob collection_mesh_job;

        private HUDSetting setting;
        public HUDSetting Setting { get { return setting; } }
        public float font_uv_padding { get; private set; }
        public int font_altas_width { get; private set; }
        public int font_altas_height { get; private set; }

        public void Init(HUDSetting setting)
        {
            this.setting = setting;
            setting.atlas.InitLookUp();

            batch_data = new HUDBatchData();
            batch_data.BeginRequestSpace();            
            var sprite_batch_info = batch_data.RequestSpace(500, (int)EGraphicSize.ESmall);
            var text_batch_info = batch_data.RequestSpace(500, (int)EGraphicSize.ELarge);
            batch_data.EndRequestSpace();
            
            sprite_batch = new HUDBatch();
            BufferSlice slice = new BufferSlice();
            slice.Init(batch_data, ref sprite_batch_info);
            sprite_batch.InitNew(batch_data, slice);
            batches.Add(sprite_batch);

            text_batch = new HUDBatch();
            BufferSlice slice_text = new BufferSlice();
            slice_text.Init(batch_data, ref text_batch_info);
            text_batch.InitNew(batch_data, slice_text);
            batches.Add(text_batch);

            font_uv_padding = TMPro.ShaderUtilities.GetPadding(setting.atlas_material, false, false);
            font_altas_width = setting.font.atlasWidth;
            font_altas_height = setting.font.atlasHeight;
            collection_mesh_job = new HUDCollectionMeshInfoJob();
            collection_mesh_job.Init(batch_data);
        }

        public void Update()
        {
            collection_mesh_job.FinishUpdateMesh();

            NativeBuffer<JobHandle> handles = new NativeBuffer<JobHandle>(batches.Count, Allocator.Temp);
            bool has_job = false;
            foreach(var b in batches)
            {
                var handle = b.BuildJob(out has_job);
                if(has_job)
                {
                    handles.Add(handle);
                }
            }

            //if(handles.Length > 0)
            {
                collection_mesh_job.BeginCollectionMeshInfo();
                foreach (var g in group_rebuild_mesh)
                {
                    if (g.mesh == null)
                        continue;

                    collection_mesh_job.CollectionMeshfInfo(g);
                }
                collection_mesh_job.EndCollectionMeshInfo(JobHandle.CombineDependencies(handles));
                group_rebuild_mesh.Clear();
            }
            handles.Dispose();
        }

        private void OnDestroy()
        {
            foreach(var b in batches)
            {
                b.Dispose();
            }
            batches.Clear();
            collection_mesh_job.Dispose();
            batch_data.Dispose();
        }

        public void RegisterHUDGroupRebuildMesh(HUDGroup group)
        {
            if (group_rebuild_mesh.Contains(group))
                return;

            group_rebuild_mesh.Add(group);
        }

        public void AddGraphic(HUDGraphic graphic)
        {
            if (graphic.batch != null)
                return;

            if(graphic.size == EGraphicSize.ESmall)
            {
                graphic.batch = sprite_batch;
            }
            else if(graphic.size == EGraphicSize.ELarge)
            {
                graphic.batch = text_batch;
            }

            graphic.batch.PushOperation(graphic, HUDBatch.OperationType.Add);
            RegisterHUDGroupRebuildMesh(graphic.group);
        }

        public void RemoveGraphic(HUDGraphic graphic)
        {
            if (graphic.batch == null)
                return;

            graphic.batch.PushOperation(graphic, HUDBatch.OperationType.Remove);
            RegisterHUDGroupRebuildMesh(graphic.group);
        }

        public void ActiveGraphic(HUDGraphic graphic)
        {
            AddGraphic(graphic);
        }

        public void DeActiveGraphic(HUDGraphic graphic)
        {
            RemoveGraphic(graphic);
        }


        public void RebuildGraphic(HUDGraphic graphic, DirtyFlag flag)
        {
            if (graphic.batch == null)
                return;

            switch(flag)
            {
                case DirtyFlag.ETransform:
                    graphic.batch.PushOperation(graphic, HUDBatch.OperationType.TransformChange);
                    break;
                case DirtyFlag.EQuad:
                    graphic.batch.PushOperation(graphic, HUDBatch.OperationType.VertexProperty);
                    break;
            }
            RegisterHUDGroupRebuildMesh(graphic.group);
        }

        Stack<Mesh> pool = new Stack<Mesh>();
        public void ActiveHUDGroup(HUDGroup group)
        {
            if(group.mesh == null)
            {
                if(pool.Count == 0)
                {
                    group.mesh = new Mesh();
                    group.mesh.MarkDynamic();
                }
                else
                {
                    group.mesh = pool.Pop();
                }
            }
        }
        
        public void DeActiveHUDGroup(HUDGroup group)
        {
            if(group.mesh != null)
            {
                pool.Push(group.mesh);
                group.mesh = null;
            }
        }
    }
}
