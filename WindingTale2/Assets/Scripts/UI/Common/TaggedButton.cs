using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Scenes.GameFieldScene;

public class TaggedButton : MonoBehaviour
{

    private int tag = 0;

    private Action<int> onSelected;

    public void Init(int tag, Action<int> onSelected)
    {
        this.tag = tag;
        this.onSelected = onSelected;
    }

    // Start is called before the first frame update
    void Start()
    {
        var button = this.gameObject.GetComponent<Button>();

        button.onClick.AddListener(delegate {
            this.onSelected(tag);
        });
    }

}
