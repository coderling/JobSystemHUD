using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTMP_Font_features : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public bool add_characters = false;
    public bool clear_font = false;
    public TMPro.TMP_FontAsset font;
    // Update is called once per frame
    void Update()
    {
        if(add_characters)
        {
            add_characters = false;
            font.TryAddCharacters("我是个好人，还不行吗");
        }

        if(clear_font)
        {
            clear_font = false;
            font.ClearFontAssetData();
        }
    }
}
