using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.MapObjects.GameMap;

public class Menu : MonoBehaviour
{
    public FDMenu FDMenu { get; private set;  }

    private GameObject parentLayer;

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
        
        GameObject menuItem = Instantiate(menuItemPrefab, this.transform);
        menuItem.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(item.Position), Quaternion.Euler(-90, 0, 0));
        menuItem.transform.localScale = new Vector3(0.08f, 0.09f, 0.08f);
        
    }

}
