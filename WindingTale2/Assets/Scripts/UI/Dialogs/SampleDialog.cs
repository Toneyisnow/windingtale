using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Scenes.GameFieldScene;

public class SampleDialog : MonoBehaviour
{

    private Action<int> onSelected = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init(Action<int> onSelected)
    {
        this.onSelected = onSelected;
    }

    public void onConfirm()
    {
        this.onSelected(1);
        GameMain.getDefault().gameCanvas.CloseDialog();
    }


    public void onCancel()
    {
        this.onSelected(-1);
        GameMain.getDefault().gameCanvas.CloseDialog();
    }
}
