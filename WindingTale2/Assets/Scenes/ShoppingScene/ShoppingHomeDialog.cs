using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Core.Files;

/// <summary>
/// The first thing shown when the player walks into a shop: a one-line greeting and four
/// action buttons. Which greeting and which four buttons depend on the shop -- the item
/// shop sells items, the church revives the fallen, the bar saves and loads -- and that
/// is decided by the shopIndex handed in from the village (see ShoppingScene / the spot
/// the cursor entered on).
///
/// Two greetings are looked up per shop: the init one, shown the first time the home
/// dialog opens, and the update one, shown when the player backs out of one of the deeper
/// dialogs (Buy, Sell, ...) and lands here again. Those deeper dialogs are not built yet;
/// the buttons currently route through OnActionSelected, which a caller may subscribe to.
/// </summary>
public class ShoppingHomeDialog : MonoBehaviour
{
    /// <summary>Which shop this is, matching the village spot index the player entered on.</summary>
    public enum ShopType
    {
        ItemShop = 1,
        ChurchShop = 2,
        BarShop = 3,
        AmorShop = 4,
        SecretShop = 5,
    }

    /// <summary>
    /// The actions a shop can offer. Each shop picks four of these for its buttons; the
    /// button face is the icon mapped in ActionSprites.
    /// </summary>
    public enum ShopAction
    {
        BuyItem,
        BuyAmor,
        BuySecret,
        SellAny,
        Equip,
        Give,
        ShowInfo,
        SaveRecord,
        LoadRecord,
        QuitGame,
        Revive,
        Transfer,
    }

    public GameObject MessageText;
    
    public GameObject Button1;

    public GameObject Button2;

    public GameObject Button3;

    public GameObject Button4;

    /// <summary>
    /// Raised when the player picks one of the four actions, by key or by click. The deeper
    /// dialogs each action opens are not built yet, so this is where they will hook in.
    /// </summary>
    public Action<ShopAction> OnActionSelected = null;

    // The selected button bounces to mark the selection, the same idiom the field dialog's
    // Yes / No buttons use (see TalkDialog): Abs(Sin) lifts it off its resting line and back,
    // over and over, while the other three sit still. Measured from each button's captured
    // resting Y so the bounce never walks the button up the dialog.
    private const float ButtonBounceDepth = 8f;   // how far it lifts, in pixels
    private const float ButtonBounceSpeed = 4f;   // radians per second

    /// <summary>Every action's button face lives under Resources/Shops. The three "buy"
    /// variants share the one buy icon -- what differs is what they sell, not the face.</summary>
    private static readonly Dictionary<ShopAction, string> ActionSprites = new Dictionary<ShopAction, string>
    {
        { ShopAction.BuyItem,    "Shop_Buy_0" },
        { ShopAction.BuyAmor,    "Shop_Buy_0" },
        { ShopAction.BuySecret,  "Shop_Buy_0" },
        { ShopAction.SellAny,    "Shop_Sell_0" },
        { ShopAction.Equip,      "Shop_Equip_0" },
        { ShopAction.Give,       "Shop_Give_0" },
        { ShopAction.ShowInfo,   "Shop_Info_0" },
        { ShopAction.SaveRecord, "Shop_Save_0" },
        { ShopAction.LoadRecord, "Shop_Load_0" },
        { ShopAction.QuitGame,   "Shop_Exit_0" },
        { ShopAction.Revive,     "Shop_Revive_0" },
        { ShopAction.Transfer,   "Shop_Trans_0" },
    };

    private int chapterId = 0;

    private ShopType shopType = ShopType.ItemShop;

    private GameRecord gameRecord = null;

    // The message shown on open, and the one shown on returning here from a deeper dialog.
    private int initMessageId = 0;
    private int updateMessageId = 0;

    // The four actions this shop offers, in Button1..Button4 order, and the buttons
    // themselves gathered once so the selection code can walk them by index.
    private ShopAction[] actions = new ShopAction[4];
    private GameObject[] buttons = null;

    // Each button's resting Y, captured once in Init: every bounce offset is measured from
    // here rather than from the live position, so an in-flight bounce leaves no drift behind.
    private float[] buttonBaseY = null;
    private float bounceElapsed = 0f;

    // Which button a keyboard confirm would trigger, 0..3. Meaningful once Init has run.
    private int selectedIndex = 0;

    private bool initialized = false;

    /// <summary>
    /// Sets the shop up: picks the greeting pair and the four buttons for this shop kind,
    /// paints the button faces, and shows the init greeting. chapterId and gameRecord are
    /// kept for the deeper dialogs (prices, the party, the purse) that build on this one.
    /// </summary>
    public void Init(int chapterId, int shopIndex, GameRecord gameRecord)
    {
        this.chapterId = chapterId;
        this.shopType = (ShopType)shopIndex;
        this.gameRecord = gameRecord;

        buttons = new GameObject[] { Button1, Button2, Button3, Button4 };
        CaptureButtonBaseY();

        ConfigureForShop(this.shopType);
        ApplyButtonSprites();
        WireButtonClicks();

        selectedIndex = 0;
        bounceElapsed = 0f;

        initialized = true;

        ShowMessage(initMessageId);
    }

    /// <summary>
    /// Swaps the greeting back to the update line, for when the player returns to the home
    /// dialog from one of the deeper dialogs. The buttons stay as they were.
    /// </summary>
    public void ShowUpdateMessage()
    {
        ShowMessage(updateMessageId);
    }

    void Update()
    {
        if (!initialized)
        {
            return;
        }

        // Left / Right walk the selection across the four buttons; Space / Enter fires the
        // selected one. Esc is left alone -- ShoppingScene reads it to back out of the shop.
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveSelection(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveSelection(1);
        }
        else if (Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SelectAction(selectedIndex);
        }

        AnimateSelectedButton();
    }

    /// <summary>
    /// Picks the greeting pair and the four actions for the shop kind. The item, armor and
    /// secret shops share a layout, differing only in what their first button buys; the bar
    /// and the church have their own.
    /// </summary>
    private void ConfigureForShop(ShopType type)
    {
        switch (type)
        {
            case ShopType.ItemShop:
                initMessageId = 51;
                updateMessageId = 52;
                actions = new[] { ShopAction.BuyItem, ShopAction.SellAny, ShopAction.Equip, ShopAction.Give };
                break;

            case ShopType.AmorShop:
                initMessageId = 51;
                updateMessageId = 52;
                actions = new[] { ShopAction.BuyAmor, ShopAction.SellAny, ShopAction.Equip, ShopAction.Give };
                break;

            case ShopType.SecretShop:
                initMessageId = 51;
                updateMessageId = 52;
                actions = new[] { ShopAction.BuySecret, ShopAction.SellAny, ShopAction.Equip, ShopAction.Give };
                break;

            case ShopType.BarShop:
                initMessageId = 53;
                updateMessageId = 54;
                actions = new[] { ShopAction.ShowInfo, ShopAction.SaveRecord, ShopAction.LoadRecord, ShopAction.QuitGame };
                break;

            case ShopType.ChurchShop:
                initMessageId = 55;
                updateMessageId = 56;
                actions = new[] { ShopAction.ShowInfo, ShopAction.Revive, ShopAction.Transfer, ShopAction.Give };
                break;

            default:
                Debug.LogWarning("ShoppingHomeDialog: unknown shop type " + type);
                initMessageId = 51;
                updateMessageId = 52;
                actions = new[] { ShopAction.BuyItem, ShopAction.SellAny, ShopAction.Equip, ShopAction.Give };
                break;
        }
    }

    /// <summary>Paints each button with the icon for the action it now stands for.</summary>
    private void ApplyButtonSprites()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            Image image = buttons[i] != null ? buttons[i].GetComponent<Image>() : null;
            if (image == null)
            {
                continue;
            }

            string spriteName;
            if (!ActionSprites.TryGetValue(actions[i], out spriteName))
            {
                continue;
            }

            Sprite sprite = Resources.Load<Sprite>("Shops/" + spriteName);
            if (sprite == null)
            {
                Debug.LogWarning("ShoppingHomeDialog: cannot load button sprite Shops/" + spriteName);
                continue;
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }
    }

    /// <summary>Routes each button's click to the same handler the keyboard uses.</summary>
    private void WireButtonClicks()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i] != null ? buttons[i].GetComponent<Button>() : null;
            if (button == null)
            {
                continue;
            }

            int index = i; // capture per-iteration, not the shared loop variable
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                selectedIndex = index;
                SelectAction(index);
            });
        }
    }

    private void MoveSelection(int delta)
    {
        selectedIndex = (selectedIndex + delta + buttons.Length) % buttons.Length;
    }

    /// <summary>Records each button's resting Y so the bounce can be measured off it.</summary>
    private void CaptureButtonBaseY()
    {
        buttonBaseY = new float[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            RectTransform rt = buttons[i] != null ? buttons[i].GetComponent<RectTransform>() : null;
            buttonBaseY[i] = rt != null ? rt.anchoredPosition.y : 0f;
        }
    }

    /// <summary>
    /// Bounces the selected button off its resting line and back, holding the other three
    /// still. Same Abs(Sin) curve as TalkDialog's Yes / No buttons, so the two read alike.
    /// </summary>
    private void AnimateSelectedButton()
    {
        if (buttonBaseY == null)
        {
            return;
        }

        bounceElapsed += Time.deltaTime;
        float lift = Mathf.Abs(Mathf.Sin(bounceElapsed * ButtonBounceSpeed)) * ButtonBounceDepth;

        for (int i = 0; i < buttons.Length; i++)
        {
            RectTransform rt = buttons[i] != null ? buttons[i].GetComponent<RectTransform>() : null;
            if (rt == null)
            {
                continue;
            }

            Vector2 pos = rt.anchoredPosition;
            pos.y = buttonBaseY[i] + (i == selectedIndex ? lift : 0f);
            rt.anchoredPosition = pos;
        }
    }

    private void SelectAction(int index)
    {
        if (index < 0 || index >= actions.Length)
        {
            return;
        }

        ShopAction action = actions[index];
        Debug.Log(string.Format("ShoppingHomeDialog: selected {0} in {1}", action, shopType));

        if (OnActionSelected != null)
        {
            OnActionSelected(action);
        }
    }

    /// <summary>Resolves the CommonStrings "Message-NN" line and drops it onto the greeting.</summary>
    private void ShowMessage(int messageId)
    {
        TextMeshProUGUI textMesh = MessageText != null ? MessageText.GetComponent<TextMeshProUGUI>() : null;
        if (textMesh == null)
        {
            return;
        }

        // The message font: the FZB_Message font asset, glyph table and matching atlas
        // material both, the same one the field dialog's messages use. The prefab ships
        // with LiberationSans, which carries no Chinese glyphs -- overriding only the
        // material left that table in place and the line came out as tofu, so assign the
        // whole font asset, which pulls its own material with it.
        // TMP_FontAsset messageFont = Resources.Load<TMP_FontAsset>(@"Fonts/FontAssets/zh/FZB_Message");
        // if (messageFont != null)
        // {
        //     textMesh.font = messageFont;
        //     textMesh.ForceMeshUpdate();
        // }

        // '#' is the source line-break marker, same as the field dialogs use.
        string text = LocalizationManager.GetMessageString(messageId).GetLocalizedString().Replace("#", "\n");
        textMesh.text = text;
    }
}
