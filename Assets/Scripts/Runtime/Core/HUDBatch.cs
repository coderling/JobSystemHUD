using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace HUD
{
    public struct BuildTransformJobData
    {
        public byte is_text;
        public byte is_active;
        public DirtyFlag flag;
        public int index;
        public int per_quad_index;
        public int valid_quad;
        public float spacing;
        public float gscale;
        public float3 local_position;
        public float3 local_scale;

        // 扩展参数，x: 进度条进度
        public half4 extend;
    }

    public struct BuildPerQuadData
    {
        public float2 size;
        public float4 uv0;
        public float4 tparams;
        public Color32 color;
    }

    [Unity.Burst.BurstCompile]
    public struct GraphicRebuildJob : IJobParallelFor
    {
        public int quad_count;
        public float font_uv_padding;
        public int fontAltas_width;
        public int fontAltas_height;

        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeSlice<Vertex> vertices;

        [NativeDisableContainerSafetyRestriction]
        public NativeSlice<BuildTransformJobData> build_trans_data;
        [ReadOnly]
        [NativeDisableContainerSafetyRestriction]
        public NativeSlice<BuildPerQuadData> build_quad_data;
        [ReadOnly]
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> dirty_indices;

        public void Execute(int index)
        {
            index = dirty_indices[index];

            var data = build_trans_data[index];
            // 顶点索引偏移
            int valid_quad_count = data.valid_quad;
            int base_index = data.index * quad_count * 4;
            int per_quad_index = data.per_quad_index;
            float3 position = data.local_position;
            bool is_text = data.is_text == 1;
            float is_text_flag = is_text ? 1 : 0;
            half progress = data.extend.x;
            var gscale = data.gscale;
            var scale = data.local_scale * HUDBatch.default_unit / 2 * gscale;
            float2 sdf_scale = new float2(is_text_flag, scale.y);

            float left_x = position.x;
            for (int i = 0; i < valid_quad_count; ++i)
            {
                int vertex_offset = base_index + i * 4;
                var quad_data = build_quad_data[per_quad_index + i];
                var half_size = quad_data.size / 2;

                Vertex v0 = vertices[vertex_offset + 0];
                Vertex v1 = vertices[vertex_offset + 1];
                Vertex v2 = vertices[vertex_offset + 2];
                Vertex v3 = vertices[vertex_offset + 3];

                if((data.flag & DirtyFlag.ETransform) != DirtyFlag.ENone)
                {
                    var p = position;
                    p.z = 0;

                    var tparams = quad_data.tparams;
                    if(is_text)
                    {
                        // lt
                        p.x = position.x + (tparams.z - font_uv_padding) * scale.x;
                        p.y = position.y + (tparams.w + font_uv_padding) * scale.y;
                        v1.position = p;

                        // record top y
                        float ty = p.y;

                        // lb
                        //p.x = p.x;
                        p.y = p.y - ((tparams.y + font_uv_padding * 2) * scale.y);
                        v0.position = p;

                        // record bottom y
                        float by = p.y;

                        // rt
                        p.x = p.x + ((tparams.x + font_uv_padding * 2) * scale.x);
                        p.y = ty;
                        v2.position = p;

                        // rb
                        //p.x = p.x;
                        p.y = by;
                        v3.position = p;
                        
                        position.x = p.x + data.spacing;

                    }
                    else
                    {
                        float lx = position.x;
                        float rx = position.x + scale.x * half_size.x * 2;

                        //lb
                        p.x = lx;
                        p.y = position.y - scale.y * half_size.y;
                        v0.position = p;

                        //lt
                        p.y = position.y + scale.y * half_size.y;
                        v1.position = p;

                        p.x = lx + (rx - lx) * progress;
                        //rt
                        p.y = position.y + scale.y * half_size.y;
                        v2.position = p;

                        //rb
                        p.y = position.y - scale.y * half_size.y;
                        v3.position = p;
                        
                        position.x += rx + data.spacing;
                    }

                    v1.uv1 = v2.uv1 = v3.uv1 = v0.uv1 = sdf_scale;
                }

                if ((data.flag & DirtyFlag.EQuad) != DirtyFlag.ENone)
                {
                    v0.color = v1.color = v2.color = v3.color = quad_data.color;

                    // uv0 // uv1 pack 到uv0
                    float2 uv_0 = float2.zero;
                    float4 rect = quad_data.uv0;
                    float xmin, xmax, ymin, ymax;
                    if(is_text)
                    {
                        xmin = (rect.x - font_uv_padding) / fontAltas_width;
                        xmax = (rect.x + font_uv_padding + rect.z) / fontAltas_width;
                        ymin = (rect.y - font_uv_padding) / fontAltas_height;
                        ymax = (rect.y + font_uv_padding + rect.w) / fontAltas_height;
                    }
                    else
                    {
                        xmin = (rect.x);
                        xmax = (rect.x + rect.z);
                        ymin = (rect.y);
                        ymax = (rect.y + rect.w);
                    }

                    uv_0.x = xmin;
                    uv_0.y = ymin;
                    v0.uv0 = uv_0;

                    uv_0.y = ymax;
                    v1.uv0 = uv_0;

                    uv_0.x = xmin + (xmax - xmin) * progress;
                    v2.uv0 = uv_0;

                    uv_0.y = ymin;
                    v3.uv0 = uv_0;

                }
                
                 vertices[vertex_offset + 0] = v0;
                 vertices[vertex_offset + 1] = v1;
                 vertices[vertex_offset + 2] = v2;
                 vertices[vertex_offset + 3] = v3;
            }

            float h_size = (position.x - left_x - data.spacing) / 2;
            for(int i = 0; i < valid_quad_count; ++i)
            {
                int vertex_offset = base_index + i * 4;
                Vertex v0 = vertices[vertex_offset + 0];
                Vertex v1 = vertices[vertex_offset + 1];
                Vertex v2 = vertices[vertex_offset + 2];
                Vertex v3 = vertices[vertex_offset + 3];
                v0.position.x -= h_size;
                v1.position.x -= h_size;
                v2.position.x -= h_size;
                v3.position.x -= h_size;
                 vertices[vertex_offset + 0] = v0;
                 vertices[vertex_offset + 1] = v1;
                 vertices[vertex_offset + 2] = v2;
                 vertices[vertex_offset + 3] = v3;
            }
            
            data.flag = DirtyFlag.ENone;
            build_trans_data[index] = data;
        }
    }

    public class HUDBatch
    {
        public enum OperationType
        {
            None = 0,
            TransformChange = 1 << 0,
            VertexProperty = 1 << 1,
            Add = 1 << 2,
            Remove = 1 << 3,
            Active = 1 << 4,
            DeActive = 1 << 5
        }

        public struct Operation
        {
            public OperationType opt;
            public HUDGraphic item;
            public float4x4 world;
        }

        public const float default_unit = 0.01f;
        public const DirtyFlag all_dirty = DirtyFlag.EQuad | DirtyFlag.ETransform;
        public BufferSlice buffer_info { get; private set; }
        
        private NativeBuffer<int> dirty_indices;
        private HashSet<int> dirtyset = new HashSet<int>();

        private Queue<Operation> operator_queue = new Queue<Operation>();
        private Dictionary<HUDGraphic, OperationType> need_rebuild_graphics = new Dictionary<HUDGraphic, OperationType>();

        public void InitNew(HUDBatchData buffer_data, BufferSlice buffer_info)
        {
            this.buffer_info = buffer_info;
            dirty_indices = new NativeBuffer<int>(30, Allocator.Persistent);
        }

        public void Dispose()
        {
            dirty_indices.Dispose();
        }

        public JobHandle BuildJob(out bool valid)
        {

            valid = false;
            QueryDirtyData();
            if(dirtyset.Count > 0)
            {
                current_job = BuildJob();
                valid = true;
            }

            return current_job;
        }

        private JobHandle BuildJob()
        {
            GraphicRebuildJob job = new GraphicRebuildJob()
            {
                quad_count = buffer_info.info.quad_count,
                font_uv_padding = HUDManager.Instance.font_uv_padding,
                fontAltas_width = HUDManager.Instance.font_altas_width,
                fontAltas_height = HUDManager.Instance.font_altas_height,
                vertices = buffer_info.vertex_datas.ToNativeSlice(),
                build_trans_data = buffer_info.transform_job_datas.ToNativeSlice(),
                build_quad_data = buffer_info.quad_job_datas.ToNativeSlice(),
                dirty_indices =dirty_indices
            };

            var job_handle = job.Schedule(dirty_indices.Length, 16);
            return job_handle;
        }

        public void PushOperation(HUDGraphic graphic, OperationType opt)
        {
            OperationType current;
            if(need_rebuild_graphics.TryGetValue(graphic, out current))
            {
                if(CheckOpt(opt, OperationType.Add))
                {
                    current &= ~OperationType.Remove;
                }

                if(CheckOpt(current, OperationType.Remove))
                {
                    current &= ~OperationType.Add;
                }
                
                if(CheckOpt(opt, OperationType.Active))
                {
                    current &= ~OperationType.DeActive;
                }

                if(CheckOpt(current, OperationType.DeActive))
                {
                    current &= ~OperationType.Active;
                }


                current |= opt;
                need_rebuild_graphics[graphic] = current;
            }
            else
            {
                need_rebuild_graphics.Add(graphic, opt);
            }

        }

        private bool CheckOpt(OperationType state, OperationType opt)
        {
            return (state & opt) != OperationType.None;
        }

        private void QueryDirtyData()
        {
            dirty_indices.Clear();
            dirtyset.Clear();
            foreach(var op in need_rebuild_graphics)
            {
                var g = op.Key;
                var opt = op.Value;
                if(CheckOpt(opt, OperationType.Add))
                {
                    InnerAddItem(g);
                    continue;
                }
                
                if(CheckOpt(opt, OperationType.Remove))
                {
                    InnerRemoveItem(g);
                    continue;
                }

                if(CheckOpt(opt, OperationType.TransformChange))
                {
                    InnerOnItemTransformChange(g);
                }

                if(CheckOpt(opt, OperationType.VertexProperty))
                {
                    InnerOnVertexPropertyChange(g);
                }
                
                if(CheckOpt(opt, OperationType.Active))
                {
                    InnerActive(g);
                }
                
                if(CheckOpt(opt, OperationType.DeActive))
                {
                    InnerDeActive(g);
                }

            }

            need_rebuild_graphics.Clear();
        }

        JobHandle current_job;
        bool is_job_valid = false;
        public void CheckJob()
        {
            if(is_job_valid)
            {
                if(!current_job.IsCompleted)
                {
                    current_job.Complete();
                }
            }
        }

        public void Tick()
        {
            QueryDirtyData();
            if(dirtyset.Count > 0)
            {
                current_job = BuildJob();
                is_job_valid = true;
                JobHandle.ScheduleBatchedJobs();
            }
        }

        Dictionary<int, HUDGraphic> item_dic = new Dictionary<int, HUDGraphic>();
        private static BuildTransformJobData tmp_job_data = new BuildTransformJobData();
        private static BuildPerQuadData tmp_quad_data = new BuildPerQuadData();

        private bool CheckBufferOverFlow()
        {
            if(buffer_info.IsUseOut())
            {
                Debug.LogError("buffer use out!");
                return false;
            }

            return true;
        }

        private void InnerAddItem(HUDGraphic item)
        {
            int index = -1;
            int per_quad_index = -1;
            if(item.build_data_index < 0)
            {
                if(!CheckBufferOverFlow())
                {
                    return;
                }
                var data_index = buffer_info.Add();
                index = data_index.x;
                per_quad_index = data_index.y;

                item_dic.Add(index, item);
            }
            else
            {
                index = item.build_data_index;
                per_quad_index = buffer_info.transform_job_datas[index].per_quad_index;
            }
            
            item.build_data_index = index;
            item.batch = this;

            tmp_job_data.index = index;
            tmp_job_data.is_active = (byte)1;
            tmp_job_data.per_quad_index = per_quad_index;
            tmp_job_data.is_text = item.is_text ? (byte)1 : (byte)0;
            tmp_job_data.spacing = item.spacing;
            tmp_job_data.local_position = item.local_position;
            tmp_job_data.local_scale = item.local_scale;
            tmp_job_data.gscale = item.gscale;
            tmp_job_data.valid_quad = item.valid_quad;
            tmp_job_data.extend.x = (half)item.progress_value;
            tmp_job_data.flag = DirtyFlag.ETransform | DirtyFlag.EQuad;

            int count = math.min(item.uv0_rect.Length, buffer_info.info.quad_count);
            count = math.min(count, item.valid_quad);
            for(int i = 0; i < count; ++i)
            {
                int offset = per_quad_index + i;
                tmp_quad_data.uv0 = item.uv0_rect[i];
                tmp_quad_data.tparams = item.tparams[i];
                tmp_quad_data.color = item.color;
                tmp_quad_data.size = item.sizes[i];
                buffer_info.quad_job_datas[offset] = tmp_quad_data;
            }

            buffer_info.transform_job_datas[index] = tmp_job_data;
            AddIndexToDirty(index);
        }

        private void AddIndexToDirty(int index)
        {
            if(!dirtyset.Contains(index))
            {
                dirtyset.Add(index);
                dirty_indices.Add(index);
            }
        }

        private void InnerRemoveItem(HUDGraphic item)
        {
            if (item.build_data_index < 0)
                return;

            int last = buffer_info.Length - 1;
            HUDGraphic last_item = item_dic[last];
            int index = item.build_data_index;

            buffer_info.RemoveSwapAtBack(index);
            
            item_dic[index] = last_item;
            last_item.build_data_index = index;
            item_dic.Remove(last);
            item.build_data_index = -1;
            item.batch = null;
        }

        private void InnerActive(HUDGraphic item)
        {
            int index = -1;
            if(item.build_data_index < 0)
            {
                return;
            }

            index = item.build_data_index;

            tmp_job_data = buffer_info.transform_job_datas[index];
            if(tmp_job_data.is_active == 0)
            {
                tmp_job_data.is_active = (byte)1;
            }
        }
        
        private void InnerDeActive(HUDGraphic item)
        {
            int index = -1;
            if(item.build_data_index < 0)
            {
                return;
            }

            index = item.build_data_index;

            tmp_job_data = buffer_info.transform_job_datas[index];
            tmp_job_data.is_active = (byte)0;
        }

        public void OnItemTransformChange(HUDGraphic item)
        {
            var operation = new Operation() { opt = OperationType.TransformChange, item = item};
            operator_queue.Enqueue(operation);
        }

        private void InnerOnItemTransformChange(HUDGraphic item)
        {
            int index = -1;
            if(item.build_data_index < 0)
            {
                return;
            }

            index = item.build_data_index;

            tmp_job_data = buffer_info.transform_job_datas[index];
            tmp_job_data.spacing = item.spacing;
            tmp_job_data.local_position = item.local_position;
            tmp_job_data.local_scale = item.local_scale;
            tmp_job_data.gscale = item.gscale;
            tmp_job_data.valid_quad = item.valid_quad;
            tmp_job_data.extend.x = (half)item.progress_value;
            tmp_job_data.flag |= DirtyFlag.ETransform;

            buffer_info.transform_job_datas[index] = tmp_job_data;

            AddIndexToDirty(index);
        }

        public void OnVertexPropertyChange(HUDGraphic item)
        {
            var operation = new Operation() { opt = OperationType.VertexProperty, item = item };
            operator_queue.Enqueue(operation);
        }

        private void InnerOnVertexPropertyChange(HUDGraphic item)
        {
            int index = -1;
            int per_quad_index = -1;
            if(item.build_data_index < 0)
            {
                return;
            }
            
            index = item.build_data_index;

            tmp_job_data = buffer_info.transform_job_datas[index];
            tmp_job_data.flag |= DirtyFlag.EQuad;
            per_quad_index = tmp_job_data.per_quad_index;

            int count = math.min(item.uv0_rect.Length, buffer_info.info.quad_count);
            count = math.min(count, item.valid_quad);
            for (int i = 0; i < count; ++i)
            {
                int offset = per_quad_index + i;
                tmp_quad_data.uv0 = item.uv0_rect[i];
                tmp_quad_data.tparams = item.tparams[i];
                tmp_quad_data.color = item.color;
                tmp_quad_data.size = item.sizes[i];
                buffer_info.quad_job_datas[offset] = tmp_quad_data;
            }

            buffer_info.transform_job_datas[index] = tmp_job_data;

            AddIndexToDirty(index);
        }
    }
}
