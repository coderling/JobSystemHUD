using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

namespace HUD
{
    public struct Vertex
    {
        public float3 position;
        public float2 uv0;
        public float2 uv1;
        public Color32 color;
    }

    public struct BufferInfo
    {
        public int offset;
        public int length;
        public int quad_offset;
        public int quad_count;
    }
    
    public class NativeBufferSlice<T> where T: struct
    {
        private int offset;
        private int length;
        private NativeBuffer<T> buffer;
        public int used { get; private set; }
        private int stride;


        public NativeBufferSlice(NativeBuffer<T> data, int offset, int length, int stride)
        {
            this.offset = offset;
            this.length = length;
            this.buffer = data;
            this.used = 0;
            this.stride = stride;
        }

        public T this[int index]
        {
            get
            {
                return buffer[offset + index];
            }

            set
            {
                buffer[offset + index] = value;
            }
        }

        public bool IsUseout()
        {
            return used >= length;
        }

        public int Add()
        {
            int ret = used;
            used += stride;
            return ret;
        }

        public void RemoveSwawAtBack(int index)
        {
            buffer.RangeSwapBackTargetIndex(offset + index, stride, offset + used);
            used -= stride;
        }

        public NativeSlice<T> ToNativeSlice()
        {
            return new NativeSlice<T>(buffer, offset, length);
        }
    }

    public class BufferSlice
    {
        public BufferInfo info;
        public HUDBatchData buffer;

        public NativeBufferSlice<BuildTransformJobData> transform_job_datas { get; private set; }
        public NativeBufferSlice<BuildPerQuadData> quad_job_datas { get; private set; }
        public NativeBufferSlice<Vertex> vertex_datas { get; private set; }

        public void Init(HUDBatchData data, ref BufferInfo info)
        {
            buffer = data;
            this.info = info;
            transform_job_datas = new NativeBufferSlice<BuildTransformJobData>(buffer.build_transform_job_datas, info.offset, info.length, 1);
            quad_job_datas = new NativeBufferSlice<BuildPerQuadData>(buffer.build_quad_datas, info.quad_offset, info.quad_count * info.length, info.quad_count);
            vertex_datas = new NativeBufferSlice<Vertex>(buffer.vertex_datas, info.quad_offset * 4, info.quad_count * info.length * 4, info.quad_count * 4);
        }

        static Vector2Int ret;
        public Vector2Int Add()
        {
            ret.x = transform_job_datas.Add();
            ret.y = quad_job_datas.Add();
            vertex_datas.Add();
            return ret;
        }

        public int Length { get { return transform_job_datas.used; } }

        public void RemoveSwapAtBack(int index)
        {
            var tdata = transform_job_datas[index];

            transform_job_datas.RemoveSwawAtBack(index);
            quad_job_datas.RemoveSwawAtBack(tdata.per_quad_index);
            vertex_datas.RemoveSwawAtBack(tdata.per_quad_index * 4);

            var n_tdata = transform_job_datas[index];
            if (index != n_tdata.index)
            {
                n_tdata.per_quad_index = tdata.per_quad_index;
                n_tdata.index = index;
                transform_job_datas[index] = n_tdata;
            }
        }

        public bool IsUseOut()
        {
            return transform_job_datas.IsUseout();
        }
    }

    public struct SubBufferData
    {
        public NativeSlice<BuildTransformJobData> transform_job_datas;
        public NativeSlice<BuildPerQuadData> quad_job_datas;
        public NativeSlice<Vertex> vertex_datas;
    }


    public class HUDBatchData 
    {
        public  NativeBuffer<Vertex> vertex_datas;
        public NativeBuffer<BuildTransformJobData> build_transform_job_datas;
        public NativeBuffer<BuildPerQuadData> build_quad_datas;

        private Dictionary<int, BufferInfo> buffer_infos = new Dictionary<int, BufferInfo>();

        public void Reset()
        {
            buffer_infos.Clear();
            if (build_transform_job_datas.IsCreated)
                build_transform_job_datas.Clear();

            if (build_quad_datas.IsCreated)
                build_quad_datas.Clear();

            if (vertex_datas.IsCreated)
                vertex_datas.Clear();
        }


        private int r_capacity = 0;
        private int r_quad_count = 0;
        private List<BufferInfo> pending_infos = new List<BufferInfo>();
        public void BeginRequestSpace()
        {
            pending_infos.Clear();
            r_capacity = 0;
            r_quad_count = 0;
        }

        public BufferInfo RequestSpace(int capacity, int quad_count)
        {
            int offset = build_transform_job_datas.IsCreated ? build_transform_job_datas.Length : 0;
            offset += r_capacity;
            
            int quad_offset = build_quad_datas.IsCreated ? build_quad_datas.Length : 0;
            quad_offset += r_quad_count;

            BufferInfo info = new BufferInfo();
            info.offset = offset;
            info.quad_offset = quad_offset;
            info.length = capacity;
            info.quad_count = quad_count;
            pending_infos.Add(info);
            r_capacity += capacity;
            r_quad_count += capacity * quad_count;

            return info;
        }

        public void EndRequestSpace()
        {
            int vertex_count = r_quad_count * 4;
            if (!build_transform_job_datas.IsCreated)
            {
                build_transform_job_datas = new NativeBuffer<BuildTransformJobData>(r_capacity, Allocator.Persistent);
                build_transform_job_datas.AddLength(r_capacity);
                build_quad_datas = new NativeBuffer<BuildPerQuadData>(r_quad_count, Allocator.Persistent);
                build_quad_datas.AddLength(r_quad_count);
                vertex_datas = new NativeBuffer<Vertex>(vertex_count, Allocator.Persistent);
                vertex_datas.AddLength(vertex_count);
            }
            else
            {
                build_transform_job_datas.AddLength(r_capacity);
                build_quad_datas.AddLength(r_quad_count);
                vertex_datas.AddLength(vertex_count);
            }

            foreach(var info in pending_infos)
            {
                buffer_infos.Add(info.offset, info);
            }

            pending_infos.Clear();
        }

        public SubBufferData GetSubBuildTransJobData(BufferInfo info)
        {
            SubBufferData data = new SubBufferData()
            {
                transform_job_datas = new NativeSlice<BuildTransformJobData>(build_transform_job_datas, info.offset, info.length),
                quad_job_datas = new NativeSlice<BuildPerQuadData>(build_quad_datas, info.quad_offset, info.length * info.quad_count),
                vertex_datas = new NativeSlice<Vertex>(vertex_datas, info.quad_offset * 4, info.length * info.quad_count * 4)
            };

            return data;
        }

        public void Dispose()
        {
            build_transform_job_datas.Dispose();
            build_quad_datas.Dispose();
            vertex_datas.Dispose();
        }
    }
}

