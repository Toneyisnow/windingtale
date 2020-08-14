using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Common
{
    public enum MenuId
    {
        ActionMenu = 11,
        ItemsMenu = 12,

        SystemMenu = 21,
        SettingsMenu = 22,
        RecordMenu = 23,

    }

    public enum MenuItemId
    {
        ActionMagic = 110,
        ActionAttack = 111,
        ActionItems = 112,
        ActionRest = 113,

        ItemExchange = 120,
        ItemUse = 121,
        ItemEquip = 122,
        ItemDiscard = 123,

        SystemMatching = 210,
        SystemRecord = 211,
        SystemSettings = 212,
        SystemRestAll = 213,

        SettingsMusic = 220,
        SettingsFight = 221,
        SettingsSound = 222,
        SettingsInfo = 223,

        RecordInfo = 230,
        RecordSave = 231,
        RecordLoad = 232,
        RecordQuit = 233,
    }
}