using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HUD.Test
{
    public class HUDPlayer 
    {
        HUDText player_name;
        HUDSprite title;
        HUDText guild_name;
        HUDSprite guild_icon;


        HUDTransform _transform;
        public HUDTransform transform { get { return _transform; } }
        public void Set(HUDTransform transform)
        {
            this._transform = transform;
            player_name = transform.GetHUDComponent<HUDText>("name");
            title = transform.GetHUDComponent<HUDSprite>("title");
            guild_name = transform.GetHUDComponent<HUDText>("guild_name");
            guild_icon = transform.GetHUDComponent<HUDSprite>("guild_icon");
        }

        public void Destroy()
        {
            Object.Destroy(_transform.gameObject);
        }
    }
}
