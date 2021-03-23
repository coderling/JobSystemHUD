using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace HUD
{
    public struct CollectionMeshInfoOffset
    {
        public int group;
        public int offset;
        public int length; 
        public int out_index;

        // 输出
        public int v_index;
        public int i_index;
    }

    public struct TransformIndex
    {
        public int index;
        public int buffer_offset;
        public int quad_buffer_offset;
        public int quad_count;
    }

    [BurstCompile]
    public struct CollectionMeshInfoJob : IJobParallelFor
    {
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeArray<Vertex> vertices;

        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeArray<BuildTransformJobData> transform_datas;
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeArray<BuildPerQuadData> quad_datas;
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeArray<TransformIndex> transform_indices;
        
        public NativeArray<CollectionMeshInfoOffset> offset_info;
        public int mesh_quad_count;
        
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector3> out_poices;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector2> out_uv0;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector2> out_uv1;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Color32> out_colors;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> out_indices;
       
        public void Execute(int index)
        {
            var index_offset = offset_info[index];
            int length = index_offset.length;
            int base_index = index_offset.offset;
            int base_out_index = index_offset.out_index * mesh_quad_count * 4;
            index_offset.v_index = base_out_index;
            int base_out_index_index = index_offset.out_index * mesh_quad_count * 6;
            index_offset.i_index = base_out_index_index;
            int fill_quad_count = 0;

            for(int i = 0; i < length; ++i)
            {
                var tr_index = transform_indices[base_index + i];
                int quad_count = transform_indices[base_index + i].quad_count;
                var data = transform_datas[tr_index.buffer_offset + tr_index.index];
                int valid_len = data.valid_quad;
                int base_vertex_index =  tr_index.quad_buffer_offset * 4 + tr_index.index * quad_count * 4;
                for(int j = 0; j < valid_len; ++j)
                {
                    int quad_vertex_index = base_vertex_index + j * 4;
                    out_indices[base_out_index_index++] = base_out_index + 0;
                    out_indices[base_out_index_index++] = base_out_index + 1;
                    out_indices[base_out_index_index++] = base_out_index + 2;
                    out_indices[base_out_index_index++] = base_out_index + 2;
                    out_indices[base_out_index_index++] = base_out_index + 3;
                    out_indices[base_out_index_index++] = base_out_index + 0;

                    for(int t = 0; t < 4; ++t)
                    {
                        int v_index = quad_vertex_index + t;
                        var v = vertices[v_index];
                        out_poices[base_out_index] = v.position;
                        out_uv0[base_out_index] = v.uv0;
                        out_uv1[base_out_index] = v.uv1;
                        out_colors[base_out_index] = v.color;
                        base_out_index++;
                    }

                    fill_quad_count++;
                }
            }

            index_offset.length = fill_quad_count; // 作为输出，填充了多少个quad
            offset_info[index] = index_offset;
        }
    }

    public class HUDCollectionMeshInfoJob 
    {

        private int max_mesh_quad_count = 0;
        private int output_index = 0;

        NativeBuffer<Vector3> out_poices;
        NativeBuffer<Vector2> out_uv0;
        NativeBuffer<Vector2> out_uv1;
        NativeBuffer<Color32> out_colors;
        NativeBuffer<int> out_indices;

        Dictionary<int, HUDGroup> building_group = new Dictionary<int, HUDGroup>();

        private const int step_capacity = 200;
        NativeBuffer<TransformIndex> transform_indices;
        NativeBuffer<CollectionMeshInfoOffset> indeices_offset;
        private HUDBatchData batch_data;

        public void Init(HUDBatchData data)
        {
            batch_data = data;
            transform_indices = new NativeBuffer<TransformIndex>(step_capacity, Allocator.Persistent);
            indeices_offset = new NativeBuffer<CollectionMeshInfoOffset>(step_capacity / 10, Allocator.Persistent);
            out_poices = new NativeBuffer<Vector3>(step_capacity * 60, Allocator.Persistent);
            out_uv0 = new NativeBuffer<Vector2>(step_capacity * 60, Allocator.Persistent);
            out_uv1 = new NativeBuffer<Vector2>(step_capacity * 60, Allocator.Persistent);
            out_colors = new NativeBuffer<Color32>(step_capacity * 60, Allocator.Persistent);
            out_indices = new NativeBuffer<int>(step_capacity * 60, Allocator.Persistent);
        }

        public void Dispose()
        {
            CompleteJob();
            transform_indices.Dispose();
            indeices_offset.Dispose();
            out_poices.Dispose();
            out_uv0.Dispose();
            out_uv1.Dispose();
            out_colors.Dispose();
            out_indices.Dispose();
        }

        private void CompleteJob()
        {
            if(is_job_valid)
            {
                is_job_valid = false;
                handle.Complete();
            }
        }

        public void FinishUpdateMesh()
        {
            if (!is_job_valid)
                return;
            CompleteJob();

            var a_out_poices = out_poices.AsArray();
            var a_out_uv0 = out_uv0.AsArray();
            var a_out_uv1 = out_uv1.AsArray();
            var a_out_colors = out_colors.AsArray();
            var a_out_indices = out_indices.AsArray();

            int count = indeices_offset.Length;
            for(int i = 0; i < count; ++i)
            {
                var info = indeices_offset[i];
                HUDGroup group = null;
                if(!building_group.TryGetValue(info.group, out group))
                {
                    continue;
                }

                var mesh = group.mesh;
                if(mesh == null)
                {
                    // 说明group 被销毁或隐藏了
                    continue;
                }

                int out_quad_count = info.length;
                var mesh_info = group.mesh_info;
                // 检查下大小
                if(out_quad_count > mesh_info.quad_count)
                {
                    mesh_info.Resize(out_quad_count);
                }

                int v_count = out_quad_count * 4;
                int i_count = out_quad_count * 6;
                mesh.Clear();
#if UNITY_2019_3_OR_NEWER
                mesh.SetVertices(a_out_poices, info.v_index, v_count);
                mesh.SetUVs(0, a_out_uv0, info.v_index, v_count);
                mesh.SetUVs(1, a_out_uv1, info.v_index, v_count);
                mesh.SetColors(a_out_colors, info.v_index, v_count);
                mesh.SetIndices(a_out_indices, info.i_index, i_count, MeshTopology.Triangles, 0);
#else
                NativeArray<Vector3>.Copy(a_out_poices, info.v_index, mesh_info.poices, 0, v_count);
                NativeArray<Vector2>.Copy(a_out_uv0, info.v_index, mesh_info.uv0, 0, v_count);
                NativeArray<Vector2>.Copy(a_out_uv1, info.v_index, mesh_info.uv1, 0, v_count);
                NativeArray<Color32>.Copy(a_out_colors, info.v_index, mesh_info.colors, 0, v_count);
                NativeArray<int>.Copy(a_out_indices, info.i_index, mesh_info.indices, 0, i_count);

                int index_length = mesh_info.indices.Length;
                for(int t = i_count; t < index_length; ++t)
                {
                    mesh_info.indices[t] = 0;
                }

                mesh.vertices = mesh_info.poices;
                mesh.uv = mesh_info.uv0;
                mesh.uv2 = mesh_info.uv1;
                mesh.colors32 = mesh_info.colors;
                mesh.triangles = mesh_info.indices;
#endif
            }
        }

        public void BeginCollectionMeshInfo()
        {
            transform_indices.Clear();
            indeices_offset.Clear();
            building_group.Clear();
            max_mesh_quad_count = 0;
            output_index = 0;
        }

        public void CollectionMeshfInfo(HUDGroup group)
        {
            CheckCapacity(group.items.Count);
            CollectionMeshInfoOffset offset = new CollectionMeshInfoOffset() { group = group.unique_id };
            int count = 0;
            int mesh_quad_count = 0;
            offset.offset = transform_indices.Length;
            foreach(var itm in group.items)
            {
                if (itm.build_data_index < 0 || itm.batch == null)
                    continue;

                if (itm.batch.buffer_info.transform_job_datas[itm.build_data_index].is_active == 0)
                    continue;

                transform_indices.Add(new TransformIndex() { 
                    index = itm.build_data_index,
                    quad_count = (int)itm.size,
                    buffer_offset = itm.batch.buffer_info.info.offset,
                    quad_buffer_offset = itm.batch.buffer_info.info.quad_offset
                });
                count++;
                mesh_quad_count += itm.valid_quad;
            }

            offset.length = count;
            //if(offset.length > 0)
            {
                indeices_offset.Add(offset);
                offset.out_index = output_index;
                output_index++;
                building_group.Add(group.unique_id, group);
            }
            if(mesh_quad_count > max_mesh_quad_count)
            {
                max_mesh_quad_count = mesh_quad_count;
            }
        }

        private void CheckCapacity(int add_count)
        {
            if(transform_indices.Capacity - transform_indices.Length < add_count)
            {
                transform_indices.Capacity += step_capacity;
            }

            if(indeices_offset.Capacity == indeices_offset.Length)
            {
                indeices_offset.Capacity += step_capacity / 10;
            }
        }

        JobHandle handle;
        bool is_job_valid = false;
        public void EndCollectionMeshInfo(JobHandle batchs_handle)
        {
            int out_buffer_length = max_mesh_quad_count;
            int need_add = out_buffer_length -  out_poices.Length;
            if(need_add > 0)
            {
                int v_add = need_add * 4;
                out_poices.AddLength(v_add);
                out_uv0.AddLength(v_add);
                out_uv1.AddLength(v_add);
                out_colors.AddLength(v_add);
                out_indices.AddLength(need_add * 6);
            }

            var job = new CollectionMeshInfoJob()
            {
                vertices = batch_data.vertex_datas,
                transform_datas = batch_data.build_transform_job_datas,
                quad_datas = batch_data.build_quad_datas,

                transform_indices = transform_indices,
                offset_info = indeices_offset,
                mesh_quad_count = max_mesh_quad_count,

                out_poices = out_poices,
                out_uv0 = out_uv0,
                out_uv1 = out_uv1,
                out_colors = out_colors,
                out_indices = out_indices
            };

            //batchs_handle.Complete();
            handle = job.Schedule(indeices_offset.Length, 1, batchs_handle);
            is_job_valid = true;
            JobHandle.ScheduleBatchedJobs();
        }
    }
}
