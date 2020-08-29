using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;

public class CreatureDialog : CanvasDialog
{
    public enum ShowType
    {
        SelectEquipItem = 1,
        SelectUseItem = 2,
        SelectAllItem = 3,
        SelectMagic = 4,
        ViewItem = 5,
        ViewMagic = 6,
    }


    private FDCreature creature = null;

    private ShowType showType = ShowType.ViewItem;

    public void Initialize(GameObject root, FDCreature creature, ShowType showType)
    {
        base.Initialize(root);
        this.creature = creature;
        this.showType = showType;
    }

    // Start is called before the first frame update
    void Start()
    {
        AddControl(@"Others/CreatureData", new Vector3(-352, 202, 0), new Vector3(30, 1, 30));
        AddControl(@"Others/CreatureDetail", new Vector3(144, 202, 0), new Vector3(30, 1, 30));
        AddControl(@"Others/ContainerBase", new Vector3(-5, -126, 0), new Vector3(37, 1, 37));



    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
