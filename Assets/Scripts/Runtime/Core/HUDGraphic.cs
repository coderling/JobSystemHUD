using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

namespace HUD
{
    // 枚举值表示quad数量
    public enum EGraphicSize
    {
        ESmall = 1,
        ELarge = 16
    }
    
    [System.Serializable]
    public class HUDTextGraphic : HUDGraphic
    {
        public HUDTextGraphic()
        {
            is_text = true;
        }
        public override EGraphicSize size => EGraphicSize.ELarge;
    }

    [System.Serializable]
    public class HUDSpriteGraphic: HUDGraphic
    {
        public HUDSpriteGraphic()
        {
            is_text = false;
        }
        public override EGraphicSize size => EGraphicSize.ESmall;
    }


    [System.Serializable]
    public abstract class HUDGraphic
    {
        public bool is_text = false;
        public float3 local_position;
        public float3 local_scale = new float3(1, 1, 1);
        public float gscale = 1;
        public float spacing;
        public float4[] uv0_rect;
        public float4[] tparams;
        public float2[] sizes;
        public Color32 color = Color.white;
        public int valid_quad = 1;
        public float progress_value = 1;

        [System.NonSerialized]
        public int build_data_index = -1;
        [System.NonSerialized]
        public HUDBatch batch;
        [System.NonSerialized]
        public HUDGroup group;

        public bool IsActive { get { return build_data_index >= 0; } }

        public HUDGraphic()
        {
            int len = (int)size;
            valid_quad = len;
            uv0_rect = new float4[len];
            tparams = new float4[len];
            sizes = new float2[len];
            var one = new float2(1, 1);
            for(int i = 0; i < len; ++i)
            {
                sizes[i] = one;
            }
        }

        [System.NonSerialized]
        public HUDBatch.OperationType flag = HUDBatch.OperationType.None;
        public abstract EGraphicSize size { get; }
    }


    public class MeshInfo
    {
        public int quad_count { get; private set; }

        public Vector3[] poices;
        public Vector2[] uv0;
        public Vector2[] uv1;
        public Color32[] colors;
        public int[] indices;

        public MeshInfo(int quad_count)
        {
            this.quad_count = quad_count;
            int size = quad_count * 4;
            poices = new Vector3[size];
            uv0 = new Vector2[size];
            uv1 = new Vector2[size];
            colors = new Color32[size];
            indices = new int[quad_count * 6];
        }

        public void Resize(int new_quad_count)
        {
            if (this.quad_count == new_quad_count)
                return;
            this.quad_count = new_quad_count;

            int new_size = quad_count * 4;
            System.Array.Resize(ref poices, new_size);
            System.Array.Resize(ref uv0, new_size);
            System.Array.Resize(ref uv1, new_size);
            System.Array.Resize(ref colors, new_size);
            System.Array.Resize(ref indices, quad_count * 6);
        }
    }
}
