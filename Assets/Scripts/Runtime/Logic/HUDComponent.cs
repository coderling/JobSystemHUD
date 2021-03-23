using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace HUD
{
    [System.Serializable]
    public class HUDComponent  : MonoBehaviour 
    {
        private HUDGroup target_group;
        private LinkedList<HUDGraphic> self_graphics = new LinkedList<HUDGraphic>();
        
        public void Attach(HUDGroup group)
        {
            target_group = group;
            OnAttch();
            target_group.OnComponentAttach(self_graphics);
        }

        public void UnAttach()
        {
            target_group.OnComponentUnAttach(self_graphics);
        }

        protected virtual void OnAttch() { }
        
        protected void AttachGraphic(HUDGraphic graphic)
        {
            if (self_graphics.Contains(graphic))
                return;

            self_graphics.AddLast(graphic);
        }

        protected void AttachSubComponent(HUDComponent component)
        {
            component.Attach(target_group);
        }

        public void RebuildGraphics()
        {
            var manager = HUDManager.Instance;
            foreach(var g in self_graphics)
            {
                manager.RebuildGraphic(g, HUDBatch.all_dirty);
            }
        }

#if UNITY_EDITOR
        public void OnValidate()
        {
            if(enabled)
            {
                OnEnable();
            }
            else
            {
                OnDisable();
            }
        }
#endif

        private void OnEnable()
        {
            foreach(var g in self_graphics)
            {
                if(!g.IsActive)
                {
                    HUDManager.Instance.ActiveGraphic(g);
                }
            }
        }

        private void OnDisable()
        {
            foreach(var g in self_graphics)
            {
                if(g.IsActive)
                {
                    HUDManager.Instance.DeActiveGraphic(g);
                }
            }
        }

        private void OnDestroy()
        {
        }
    }
}
