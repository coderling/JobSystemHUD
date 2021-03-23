using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HUD
{
    public class HUDProgressBar  : HUDComponent
    {
        [SerializeField]
        private HUDSprite background;
        [SerializeField]
        private HUDSprite progressbar;

        public float Value
        {
            get
            {
                return progressbar.graphic.progress_value;
            }

            set
            {
                if(progressbar.graphic.progress_value != value)
                {
                    progressbar.graphic.progress_value = (Unity.Mathematics.half)value;
                    OnValueChange();
                }
            }
        }

        protected override void OnAttch()
        {
            AttachSubComponent(background);
            AttachSubComponent(progressbar);
        }


        private void OnValueChange()
        {
            progressbar.RebuildGraphics();
        }
    }
}
