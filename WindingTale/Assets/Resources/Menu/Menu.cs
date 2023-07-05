using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.UI.Scenes.Game;

public class Menu : MonoBehaviour
{
    public FDMenu FDMenu { get; private set;  }

    /// <summary>
    /// /public FDPosition Position { get; private set; }
    /// </summary>
    /// <param name="menu"></param>
    /// <param name="position"></param>

    public void Init(FDMenu menu)
    {
        this.FDMenu = menu;

        LoadMenuItem(1, menu.Items[0].Id.GetHashCode());
        LoadMenuItem(2, menu.Items[1].Id.GetHashCode());
        LoadMenuItem(3, menu.Items[2].Id.GetHashCode());
        LoadMenuItem(4, menu.Items[3].Id.GetHashCode());
    }

    public void LoadMenuItem(int itemIndex, int menuItemId)
    {
        //// GameObject menuItemPrefab = Resources.Load<GameObject>(string.Format("Menu/Menu_{0}_1", FDMenu.Items[itemIndex - 1].Id));
        GameObject menuItemPrefab = Resources.Load<GameObject>(string.Format("Menu/Menu_{0}_1", menuItemId));
        
        GameObject menuItem = Instantiate(menuItemPrefab, transform.Find(string.Format("Item{0}", itemIndex )));

        menuItem.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.identity);
        menuItem.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);

        GameInterface.Instance.ApplyDefaultMaterial(menuItem.transform.Find("default").gameObject);
    }

}
