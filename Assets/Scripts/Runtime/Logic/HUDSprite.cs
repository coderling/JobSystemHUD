using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace HUD
{
    [System.Serializable]
    public class HUDSprite : HUDComponent
    {
        [SerializeField]
        HUDSpriteGraphic sprite = new HUDSpriteGraphic();
        [SerializeField]
        private uint crc_path;

        public HUDSpriteGraphic graphic { get { return sprite; } }

        protected override void OnAttch()
        {
            AttachGraphic(sprite);
        }

        public void SetSpritePath(string path)
        {
            uint crc = HUDHelper.GetCRC32(path);
            if (crc_path == crc)
                return;

            crc_path = crc;
            Rect uv = HUDManager.Instance.Setting.atlas[crc];
            sprite.uv0_rect[0] = new float4(uv.xMin, uv.yMin, uv.width, uv.height);
            HUDManager.Instance.RebuildGraphic(sprite, DirtyFlag.EQuad);
        }

        public void NativeSize()
        {
            var tex_size = HUDManager.Instance.Setting.atlas.size;
            var uv = sprite.uv0_rect[0];
            tex_size.x = (int)(tex_size.x * uv.z);
            tex_size.y = (int)(tex_size.y * uv.w);
            sprite.sizes[0] = new float2(tex_size.x, tex_size.y);

            HUDManager.Instance.RebuildGraphic(sprite, DirtyFlag.ETransform);
        }
    }
}
