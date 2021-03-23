using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace HUD.Test
{
    public class TestSampleSprite : MonoBehaviour
    {
        public HUDManager manager;
        
        List<HUDGroup> graphics = new List<HUDGroup>();
        Dictionary<HUDGroup, Transform> delegate_transform = new Dictionary<HUDGroup, Transform>();
        public HUDSetting setting;

        private void Awake()
        {
            manager = HUDManager.Instance;
            manager.Init(setting);
        }

        private void OnDestroy()
        {
            HUDManager.DestoryManager();
        }

        bool is_add = false;
        private void Update()
        {
            UpdateGraphics();

            HUDManager.Instance.Update();
            if (is_add)
                return;

            is_add = true;
            AddSampleSprite();

        }

        Dictionary<HUDGraphic, Transform> com_transform = new Dictionary<HUDGraphic, Transform>();
        private void AddItem(HUDGraphic com, HUDGroup g)
        {
            var parent = delegate_transform[g];
            Transform t = null;
            if(!com_transform.TryGetValue(com, out t))
            {
                var gb = new GameObject(com.GetType().Name);
                t = gb.transform;
                t.SetParent(parent, false);
                com_transform.Add(com, t);
            }
        }

        private void  UpdateGraphics()
        {
            foreach(var trans in com_transform)
            {
                trans.Key.local_position = trans.Value.localPosition;
            }
        }

        private void AddSampleSprite()
        {
            var go = new GameObject("hud_sprite");
            go.transform.SetParent(this.transform, false);
            var group = new HUDGroup();
            var qua = go.transform.localRotation;
            delegate_transform.Add(group, go.transform);
            graphics.Add(group);

            /*
            var item = go.AddComponent<HUDSprite>();
            item.Attach(group);
            item.SetSpritePath("Assets/Test/Textures/mask1.png");
            item.NativeSize();
            */

            var text = go.AddComponent<HUDText>();
            text.Attach(group);
            text.content = "你大爷";

            foreach(var gr in group.items)
            {
                AddItem(gr, group);
            }
            manager.ActiveHUDGroup(group);
        }
    }
}
