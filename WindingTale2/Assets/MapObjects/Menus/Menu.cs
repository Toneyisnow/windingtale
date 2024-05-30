using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.MapObjects.GameMap;

public class Menu : MonoBehaviour
{
    public FDMenu FDMenu { get; private set;  }

    public GameObject centerObject;

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
        menuItemObj.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(getItemPosition(itemIndex)), Quaternion.Euler(-90, 0, 0));
        menuItemObj.transform.localScale = new Vector3(0.08f, 0.09f, 0.08f);
        MenuItem menuItem = menuItemObj.AddComponent<MenuItem>();
        menuItem.Init(item);
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
