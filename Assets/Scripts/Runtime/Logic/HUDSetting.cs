using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HUD
{
    public class HUDSetting : ScriptableObject
    {
        public const string altas_path = "Assets/Res/hud_atlas.asset";
        public const string setting_path = "Assets/Res/hud.asset";
        public HUDAtlas atlas;
        public Material atlas_material;

        public TMPro.TMP_FontAsset font;
    }
}
