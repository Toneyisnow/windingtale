using System;
using System.Collections;
using System.Collections.Generic;
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

    /// <summary>
    /// Marks one item as the selected one: it runs a 2-frame animation between its
    /// Menu_XXX_1 and Menu_XXX_2 meshes, while the others show their static normal (1)
    /// or disabled (3) mesh. Purely visual. Disabled items can't be selected.
    /// </summary>
    public void SetActiveItem(int activeIndex)
    {
        for (int i = 0; i < 4; i++)
        {
            FDMenuItem item = FDMenu.Items[i];
            if (item == null || menuItemObjs[i] == null)
            {
                continue;
            }

            MeshFilter filter = GetItemMeshFilter(i);
            if (filter == null)
            {
                continue;
            }

            // Stop any running animation before re-deciding this item's look.
            MenuItemBlink running = menuItemObjs[i].GetComponent<MenuItemBlink>();
            if (running != null)
            {
                Destroy(running);
            }

            if (item.Enabled && i == activeIndex)
            {
                // Selected: animate between frame 1 and frame 2.
                Mesh frame1 = LoadItemMesh(item, 1);
                Mesh frame2 = LoadItemMesh(item, 2);
                if (frame1 != null && frame2 != null)
                {
                    MenuItemBlink blink = menuItemObjs[i].AddComponent<MenuItemBlink>();
                    blink.Init(filter, frame1, frame2);
                }
                else if (frame1 != null)
                {
                    filter.mesh = frame1;
                }
            }
            else
            {
                // Not selected: static normal (1) or disabled (3).
                Mesh staticMesh = LoadItemMesh(item, item.Enabled ? 1 : 3);
                if (staticMesh != null)
                {
                    filter.mesh = staticMesh;
                }
            }
        }
    }

    private MeshFilter GetItemMeshFilter(int itemIndex)
    {
        Transform inner = menuItemObjs[itemIndex].transform.Find("default");
        return inner != null ? inner.GetComponent<MeshFilter>() : null;
    }

    private static Mesh LoadItemMesh(FDMenuItem item, int animIndex)
    {
        GameObject prefab = Resources.Load<GameObject>(
            string.Format("Menus/Menu_{0}_{1}", item.Id.GetHashCode(), animIndex));
        return prefab != null ? FindDefaultMesh(prefab.transform) : null;
    }

    private static Mesh FindDefaultMesh(Transform root)
    {
        Transform inner = root.Find("default");
        if (inner != null)
        {
            MeshFilter filter = inner.GetComponent<MeshFilter>();
            if (filter != null)
            {
                return filter.sharedMesh;
            }
        }

        MeshFilter any = root.GetComponentInChildren<MeshFilter>();
        return any != null ? any.sharedMesh : null;
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
