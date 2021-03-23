using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Mathematics;

namespace HUD
{
    [System.Serializable]
    public class HUDText : HUDComponent
    {
        [SerializeField]
        private HUDTextGraphic text = new HUDTextGraphic();
        [SerializeField]
        private string _content = "嘿";

        public string content
        {
            get
            {
                return _content;
            }
            set
            {
                if (_content != value)
                {
                    _content = value;
                    FillGraphic();
                }
            }
        }

        public Color color
        {
            get
            {
                return text.color;
            }
            set
            {
                text.color = value;
                HUDManager.Instance.RebuildGraphic(text, DirtyFlag.EQuad);
            }
        }

        public float font_size
        {
            get
            {
                return text.gscale;
            }
            set
            {
                if(text.gscale != value)
                {
                    text.gscale = value;
                    HUDManager.Instance.RebuildGraphic(text, DirtyFlag.ETransform);
                }
            }
        }

        private TMP_FontAsset font;

        protected override void OnAttch()
        {
            AttachGraphic(text);
            font = HUDManager.Instance.Setting.font;
            FillGraphic();
        }

        public void ForceRebuild()
        {
            FillGraphic();
        }

        private void FillGraphic()
        {
            bool ret = font.TryAddCharacters(_content);

            int count = Mathf.Min(_content.Length, (int)text.size);
            text.valid_quad = count;
            var one = new float2(1, 1);
            for(int i = 0; i < count; ++i)
            {
                uint unicode = _content[i];
                var character = font.characterLookupTable[unicode];
                var glyph = character.glyph;
                var glyphmetrices = glyph.metrics;
                var rect = glyph.glyphRect;
                text.uv0_rect[i] = new float4(rect.x, rect.y, rect.width, rect.height);
                text.tparams[i] = new float4(glyphmetrices.width, glyphmetrices.height, glyphmetrices.horizontalBearingX, glyphmetrices.horizontalBearingY);
                text.sizes[i] = new float2(glyphmetrices.horizontalAdvance, 0);
            }

            HUDManager.Instance.RebuildGraphic(text, HUDBatch.all_dirty);
        }
    }
}
