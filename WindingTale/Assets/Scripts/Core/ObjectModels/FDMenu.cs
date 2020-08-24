using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.ObjectModels
{
    public class FDMenu
    {
        public static MenuItemId[] GetMenuItems(MenuId menuId)
        {
            switch(menuId)
            {
                case MenuId.ActionMenu:
                    return new MenuItemId[4] { MenuItemId.ActionMagic, MenuItemId.ActionAttack, MenuItemId.ActionItems, MenuItemId.ActionRest };
                case MenuId.ItemsMenu:
                    return new MenuItemId[4] { MenuItemId.ItemExchange, MenuItemId.ItemUse, MenuItemId.ItemEquip, MenuItemId.ItemDiscard };
                case MenuId.SystemMenu:
                    return new MenuItemId[4] { MenuItemId.SystemSettings, MenuItemId.SystemMatching, MenuItemId.SystemRecord, MenuItemId.SystemRestAll };
                case MenuId.RecordMenu:
                    return new MenuItemId[4] { MenuItemId.RecordSave, MenuItemId.RecordInfo, MenuItemId.RecordLoad, MenuItemId.RecordQuit };
                case MenuId.SettingsMenu:
                    return new MenuItemId[4] { MenuItemId.SettingsSound, MenuItemId.SettingsMusic, MenuItemId.SettingsFight, MenuItemId.SettingsInfo };
                default:
                    return null;
            }
        }
    }
}