using UnityEngine;
using WindingTale.Core.Definitions;

/// <summary>
/// The item type -> face mapping the item lists share. An item's icon is one of the sprites
/// under Resources/Dialogs, picked by its ItemType: attack, defend or usable; money and
/// special items have no face of their own and take the usable one. Kept in one place so the
/// shop's Buy list (ShoppingItemsDialog) and a creature's pack (CreatureInfoDialog) show the
/// same face for the same item.
/// </summary>
public static class ItemIconHelper
{
    private const string IconFolder = "Dialogs/";
    private const string AttackIcon = "ItemAttackIcon_0";
    private const string DefendIcon = "ItemDefendIcon_0";
    private const string UsableIcon = "ItemUsableIcon_0";

    /// <summary>The Resources sub-path (under Dialogs) of the face for this item's type.</summary>
    public static string GetIconName(ItemDefinition item)
    {
        if (item == null)
        {
            return UsableIcon;
        }

        switch (item.GetItemType())
        {
            case ItemDefinition.ItemType.Attack:
                return AttackIcon;
            case ItemDefinition.ItemType.Defend:
                return DefendIcon;
            default:
                return UsableIcon;
        }
    }

    /// <summary>Loads the type face sprite for an item, or null (logged) when it is missing.</summary>
    public static Sprite LoadIcon(ItemDefinition item)
    {
        string path = IconFolder + GetIconName(item);
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogWarning("ItemIconHelper: cannot load item icon " + path);
        }
        return sprite;
    }
}
