using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.MapObjects.GameMap;
using WindingTale.MapObjects.Menus;
using WindingTale.UI.Utils;

public class Menu : MonoBehaviour
{
    public FDMenu FDMenu { get; private set;  }

    public GameObject centerObject;

    private GameObject[] menuItemObjs = new GameObject[4];

    /// <summary>
    /// /public FDPosition Position { get; private set; }
    /// </summary>
    /// <param name="menu"></param>
    /// <param name="position"></param>

    public void Init(FDMenu menu)
    {
        this.FDMenu = menu;

        for (int i = 0; i < 4; i++)
        {
            LoadMenuItem(i);
        }
    }

    public void LoadMenuItem(int itemIndex)
    {
        FDMenuItem item = FDMenu.Items[itemIndex];
        int animIndex = item.Enabled ? 1 : 3;

        GameObject menuItemPrefab = Resources.Load<GameObject>(string.Format("Menus/Menu_{0}_{1}", item.Id.GetHashCode(), animIndex));
        
        GameObject menuItemObj = Instantiate(menuItemPrefab, centerObject.transform);
        menuItemObjs[itemIndex] = menuItemObj;

        var centerPos = FDPosition.At(0, 0);
        var itemPos = getItemPosition(itemIndex);

        menuItemObj.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(centerPos), Quaternion.Euler(-90, 0, 0));
        menuItemObj.transform.localScale = new Vector3(0.08f, 0.09f, 0.08f);
        
        
        MenuItem menuItem = menuItemObj.AddComponent<MenuItem>();
        menuItem.Init(item);

        MenuSlidingEffect slidingEffect = menuItemObj.AddComponent<MenuSlidingEffect>();
        slidingEffect.Init(centerPos, itemPos);

    }

    public void CloseMenu()
    {
        for (int i = 0; i < 4; i++)
        {
            CloseMenuItem(i);
        }

        MonoBehaviourUtils.ExecuteWithDelay(this, 0.15f, () =>
        {
            Destroy(gameObject);
        });
    }

    public void CloseMenuItem(int itemIndex)
    {
        var centerPos = FDPosition.At(0, 0);
        var itemPos = getItemPosition(itemIndex);
        
        MenuSlidingEffect slidingEffect = menuItemObjs[itemIndex].AddComponent<MenuSlidingEffect>();
        slidingEffect.Init(itemPos, centerPos, true);
    }

    private FDPosition getItemPosition(int itemIndex)
    {
        switch (itemIndex)
        {
            case 0:
                return FDPosition.At(-1, 0);
            case 1: 
                return FDPosition.At(0, -1);
            case 2:
                return FDPosition.At(1, 0);
            case 3:
                return FDPosition.At(0, 1);
            default: return null;
        }
    }
}
