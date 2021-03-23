using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HUD
{
    public class HUDAtlas : ScriptableObject
    {
        public Texture2D texture;
        public uint[] crc32ids;
        public Rect[] uvs;

        public Vector2Int size { get; private set; }

        Dictionary<uint, int> lookup = new Dictionary<uint, int>();
        public void InitLookUp()
        {
            lookup.Clear();
            for(int i = 0; i < crc32ids.Length; ++i)
            {
                lookup.Add(crc32ids[i], i);
            }

            size = new Vector2Int(texture.width, texture.height);
        }

        public Rect this[uint crc]
        {
            get
            {
                int ind;
                if(!lookup.TryGetValue(crc, out ind))
                {
                    return Rect.zero;
                }
                return uvs[ind];
            }
        }

        public bool QueryUV(uint crc, out Rect uv)
        {
            int ind;
            if (!lookup.TryGetValue(crc, out ind))
            {
                uv = Rect.zero;
                return false;
            }

            uv = uvs[ind];
            return true;
        }
    }
}
