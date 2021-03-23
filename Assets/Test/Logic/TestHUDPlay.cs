using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HUD.Test
{
    public class TestHUDPlay : MonoBehaviour
    {
        public HUDSetting setting;
        public GameObject prefab;

        private void Awake()
        {
            HUDManager.Instance.Init(setting);
            for(int i = 0; i < 100; ++i)
            {
                Add();
            }
            //HUDManager.Instance.SetCamera(Camera.main);
        }

        public bool force_update_trans = false;
        // Update is called once per frame
        void Update()
        {
            HUDManager.Instance.Update();
            foreach(var p in players)
            {
                //force rebuild for test performance
                p.transform.Group.ForceRebuild();
            }
        }

        private void OnGUI()
        {
            float fLeft = 10.0f;
            float fTop = 10.0f;

            if (GUI.Button(new Rect(fLeft, fTop, 100.0f, 20.0f), "添加"))
            {
                Add();
            }
            fLeft += 110;
            if (GUI.Button(new Rect(fLeft, fTop, 100.0f, 20.0f), "删除"))
            {
                Remove();
            }
            fLeft += 110;
        }


        private List<HUDPlayer> players = new List<HUDPlayer>();
        
        private void Add()
        {
            HUDPlayer play = new HUDPlayer();
            var position = Random.insideUnitSphere * 5;
            var gb = GameObject.Instantiate(prefab);
            gb.transform.position = position;
            play.Set(gb.GetComponent<HUDTransform>());
            players.Add(play);
        }

        private void Remove()
        {
            if(players.Count > 0)
            {
                var p = players[0];
                p.Destroy();
                players.RemoveAt(0);
            }
        }

        private void OnDestroy()
        {
            HUDManager.DestoryManager();
        }
    }
}
