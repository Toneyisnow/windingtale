using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Files;
using WindingTale.Core.Objects;
using WindingTale.UI.Dialogs;
using WindingTale.UI.Utils;

/// <summary>
/// A single shop, reached from the village by walking the cursor onto its spot and
/// pressing space. The village pulls its camera in on that spot and hands the shop over
/// on black (VillageScene.EnterShopRoutine), so the shop opens by fading its picture up
/// out of that black, and closes the same way: on Esc it fades back to black and hands
/// the village back, which pulls its camera back out to where it started.
///
/// Which shop this is comes in as an index -- the village spot the cursor entered on --
/// and picks the picture to show. The party is carried through untouched so the village
/// has it again on the way back.
///
/// What the player does inside a shop runs through a stack of dialogs. The home dialog
/// (greeting + action buttons) is pushed on entry and sits at the bottom; each action
/// that needs more -- the bar's Save and Load -- pushes a dialog over it, and backing out
/// pops back down. Only the top dialog is active, so only it takes input; the ones beneath
/// are hidden until they surface again. See PushDialog / PopDialog.
/// </summary>
public class ShoppingScene : MonoBehaviour
{
    /// <summary>The GlobalVariables key the village hands the entered spot's index over on.</summary>
    public const string ShopIndexVariableName = "ShopIndex";

    /// <summary>The GlobalVariables key the party is carried through on, the village's own key.</summary>
    public const string RecordVariableName = "GameRecord";

    /// <summary>The CommonStrings "Message-57" line shown after a save: "记录存储完毕！".</summary>
    private const int SaveDoneMessageId = 57;

    /// <summary>The CommonStrings "Confirm-52" line shown before a load: "确定要读取游戏吗？".</summary>
    private const int LoadConfirmId = 52;

    /// <summary>CommonStrings "Message-63" shown when the purse cannot cover an item: "钱不够！".</summary>
    private const int NotEnoughMoneyMessageId = 63;

    /// <summary>CommonStrings "Message-62" shown when a creature's pack is full: "{名字}带不动了！".</summary>
    private const int ItemsFullMessageId = 62;

    /// <summary>CommonStrings "Confirm-54" asked before a buy: "这个{名字}，#{价格}元，要不要啊？".</summary>
    private const int BuyConfirmId = 54;

    /// <summary>CommonStrings "Confirm-56" asked after buying an equippable item: "要装备上去吗？".</summary>
    private const int EquipConfirmId = 56;

    /// <summary>How long the bought-item flourish holds before the shop carries on, in seconds.</summary>
    private const float BoughtAnimationSeconds = 2f;

    /// <summary>How many shop pictures exist to be shown, VillageShop-01-1 up to this.</summary>
    private const int ShopPictureCount = 5;

    /// <summary>Time the shop takes to fade its picture up, and back down on the way out.</summary>
    public float fadeDuration = 0.3f;

    /// <summary>
    /// The home dialog prefab (greeting + four action buttons). Assigned in the inspector;
    /// instantiated once on entry and handed the shop kind so it picks its own greeting and
    /// buttons. See ShoppingHomeDialog.
    /// </summary>
    public GameObject shoppingHomeDialogPrefab = null;

    /// <summary>
    /// The save / load slot picker, pushed over the home dialog when the player chooses
    /// Save or Load at the bar. See ShoppingRecordDialog.
    /// </summary>
    public GameObject shoppingRecordDialogPrefab = null;

    /// <summary>
    /// The party creature picker, pushed over the home dialog when the player chooses Sell.
    /// See ShoppingCreaturesDialog.
    /// </summary>
    public GameObject shoppingCreaturesDialogPrefab = null;

    /// <summary>
    /// The item picker (Buy), pushed over the home dialog when the player chooses one of the
    /// Buy actions at an item / armor / secret shop. See ShoppingItemsDialog.
    /// </summary>
    public GameObject shoppingItemsDialogPrefab = null;

    /// <summary>The one-line notice shown after a save. See ShoppingMessageDialog.</summary>
    public GameObject shoppingMessageDialogPrefab = null;

    /// <summary>The yes / no question shown before a load. See ShoppingConfirmDialog.</summary>
    public GameObject shoppingConfirmDialogPrefab = null;

    private GameRecord record = null;

    /// <summary>
    /// The purse read-out ("$00000000"), a persistent HUD placed in the scene as an inactive
    /// root. Not on the dialog stack: it stays up while the pickers come and go, shown once the
    /// shop has faded up and hidden again on the way out. Left unset it is found by name at
    /// fade-in; a serialized reference (wired in the scene) skips that lookup.
    /// </summary>
    public GameObject moneyBarObject = null;

    private MoneyBar moneyBar = null;

    private ShoppingHomeDialog homeDialog = null;

    private int shopIndex = 0;

    private ScreenFader fader = null;

    /// <summary>True once the shop is fading out to the village; input is shut off from there.</summary>
    private bool leaving = false;

    /// <summary>
    /// The dialogs currently on screen, home dialog at the bottom. Only the top is active
    /// and taking input; PushDialog and PopDialog keep that so.
    /// </summary>
    private readonly Stack<GameObject> dialogStack = new Stack<GameObject>();

    /// <summary>
    /// The frame a push or pop last happened on. The scene skips its own Esc handling on
    /// that frame so the key press a dialog just acted on (a slot picker cancelled with
    /// Esc, say) does not also read through here and leave the shop in the same frame.
    /// </summary>
    private int stackChangedFrame = -1;

    /// <summary>The slot the load flow is waiting on the player to confirm reading from.</summary>
    private int pendingLoadSlot = -1;

    /// <summary>
    /// The item the Buy flow is in the middle of purchasing -- carried from the item picker,
    /// through the "buy it?" confirm, to the creature picker that finally receives it.
    /// </summary>
    private ItemDefinition pendingBuyItem = null;

    /// <summary>
    /// The creature the pending item is about to be bought for, carried across the "equip it?"
    /// confirm. The purchase itself is deferred until that answer comes back, so it can buy
    /// (No) or buy-and-equip (Yes) in one step -- see OnEquipConfirmed / ExecutePurchase.
    /// </summary>
    private CreatureMapRecord pendingBuyFriend = null;

    void Start()
    {
        shopIndex = GlobalVariables.Get<int>(ShopIndexVariableName);
        record = GlobalVariables.Get<GameRecord>(RecordVariableName);

        ShowBackground(shopIndex);
        PlayShopMusic(shopIndex);

        // The shop picture fades up out of the black the village handed over on; the home
        // dialog and the money bar are only put up once that has finished, so they appear on
        // the shop rather than fading in through the curtain with it.
        fader = ScreenFader.Create(1.0f);
        fader.FadeTo(0.0f, fadeDuration, () =>
        {
            ShowHomeDialog(shopIndex);
            ShowMoneyBar();
        });
    }

    /// <summary>
    /// Switches on the purse read-out for the first time and fills it with the current money.
    /// The bar lives in the scene as an inactive root; it is resolved once (a wired reference,
    /// or found by name), a MoneyBar controller is attached if the prefab carries none, and it
    /// is then left on for the life of the shop until LeaveShop hides it.
    /// </summary>
    private void ShowMoneyBar()
    {
        if (moneyBar == null)
        {
            GameObject barObject = ResolveMoneyBarObject();
            if (barObject == null)
            {
                Debug.LogWarning("Shopping scene has no MoneyBar in the scene to show.");
                return;
            }

            moneyBar = barObject.GetComponent<MoneyBar>();
            if (moneyBar == null)
            {
                moneyBar = barObject.AddComponent<MoneyBar>();
            }

            moneyBarObject = barObject;
        }

        RefreshMoneyBar();
        moneyBarObject.SetActive(true);
    }

    /// <summary>
    /// The scene's MoneyBar root: the wired reference if there is one, else the inactive root
    /// GameObject named "MoneyBar" the scene ships with. GameObject.Find would miss it -- it
    /// only sees active objects -- so the scene's roots (which include the inactive ones) are
    /// walked instead.
    /// </summary>
    private GameObject ResolveMoneyBarObject()
    {
        if (moneyBarObject != null)
        {
            return moneyBarObject;
        }

        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            if (root.name == "MoneyBar")
            {
                return root;
            }
        }

        return null;
    }

    /// <summary>Redraws the purse read-out from the current money -- after a buy or a sell.</summary>
    private void RefreshMoneyBar()
    {
        if (moneyBar != null && record != null)
        {
            moneyBar.SetMoney(record.TotalMoney);
        }
    }

    /// <summary>
    /// Puts up the home dialog for this shop and makes it the bottom of the dialog stack.
    /// The village hands the entered spot over as the shop index directly: pos 1-5 are the
    /// shops, matched one-to-one to the shop kinds (1=ItemShop .. 5=SecretShop), so the
    /// index is the shop kind with no offset -- pos 0 is the way on to the next chapter,
    /// never a shop, so a shop index is always 1-5. The chapter comes off the party record
    /// so the deeper dialogs have it.
    /// </summary>
    private void ShowHomeDialog(int shopIndex)
    {
        if (shoppingHomeDialogPrefab == null)
        {
            Debug.LogWarning("Shopping scene has no home dialog prefab to show.");
            return;
        }

        GameObject dialogObject = Instantiate(shoppingHomeDialogPrefab);
        homeDialog = dialogObject.GetComponent<ShoppingHomeDialog>();
        if (homeDialog == null)
        {
            Debug.LogWarning("Shopping home dialog prefab has no ShoppingHomeDialog component.");
            return;
        }

        homeDialog.OnActionSelected = OnHomeAction;

        int chapterId = record != null ? record.ChapterId : 0;
        homeDialog.Init(chapterId, shopIndex, record);

        // The home dialog is the bottom of the stack rather than a pushed dialog, so its
        // Init (which shows the greeting) has already run; just register it.
        dialogStack.Push(dialogObject);
        stackChangedFrame = Time.frameCount;
    }

    void Update()
    {
        if (leaving || fader == null || fader.IsFading)
        {
            return;
        }

        // Esc leaves the shop, but only from the home dialog. While a dialog is pushed over
        // it, Esc belongs to that dialog (the slot picker cancels on it); and the frame a
        // dialog was just closed on is skipped so its own Esc does not leak through to here.
        if (dialogStack.Count > 1 || Time.frameCount == stackChangedFrame)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        {
            LeaveShop();
        }
    }

    /// <summary>
    /// Pushes a freshly instantiated dialog onto the stack: the dialog beneath is hidden
    /// (so it stops taking input) and the new one is shown on top.
    /// </summary>
    public void PushDialog(GameObject dialogObject)
    {
        if (dialogObject == null)
        {
            return;
        }

        if (dialogStack.Count > 0)
        {
            dialogStack.Peek().SetActive(false);
        }

        dialogStack.Push(dialogObject);
        dialogObject.SetActive(true);
        stackChangedFrame = Time.frameCount;
    }

    /// <summary>
    /// Pops and destroys the top dialog, bringing the one beneath back to the surface. The
    /// home dialog at the bottom is never popped -- backing out of it leaves the shop
    /// instead (see Update). The revealed home dialog is switched to its "back again"
    /// greeting.
    /// </summary>
    public void PopDialog()
    {
        if (dialogStack.Count <= 1)
        {
            return;
        }

        GameObject top = dialogStack.Pop();
        Destroy(top);

        GameObject revealed = dialogStack.Peek();
        revealed.SetActive(true);
        stackChangedFrame = Time.frameCount;

        // Coming back to the home dialog shows its "returned from a deeper dialog" line.
        if (homeDialog != null && revealed == homeDialog.gameObject)
        {
            homeDialog.ShowUpdateMessage();
        }
    }

    /// <summary>
    /// The home dialog's action buttons land here. Only the bar's Save and Load open a
    /// further dialog; the rest are not wired up yet.
    /// </summary>
    private void OnHomeAction(ShoppingHomeDialog.ShopAction action)
    {
        switch (action)
        {
            case ShoppingHomeDialog.ShopAction.SaveRecord:
                OpenRecordDialog(isSave: true);
                break;

            case ShoppingHomeDialog.ShopAction.LoadRecord:
                OpenRecordDialog(isSave: false);
                break;

            case ShoppingHomeDialog.ShopAction.SellAny:
                OpenCreaturesDialog();
                break;

            case ShoppingHomeDialog.ShopAction.BuyItem:
            case ShoppingHomeDialog.ShopAction.BuyAmor:
            case ShoppingHomeDialog.ShopAction.BuySecret:
                OpenItemsDialog();
                break;
        }
    }

    /// <summary>
    /// Opens the slot picker for a save or a load. It reports the chosen slot back through
    /// OnRecordSlotSelected, which is where the two flows part.
    /// </summary>
    private void OpenRecordDialog(bool isSave)
    {
        if (shoppingRecordDialogPrefab == null)
        {
            Debug.LogWarning("Shopping scene has no record dialog prefab to show.");
            return;
        }

        GameObject dialogObject = Instantiate(shoppingRecordDialogPrefab);
        ShoppingRecordDialog dialog = dialogObject.GetComponent<ShoppingRecordDialog>();
        if (dialog == null)
        {
            Debug.LogWarning("Record dialog prefab has no ShoppingRecordDialog component.");
            Destroy(dialogObject);
            return;
        }

        // Saving may pick an empty slot (a fresh slot to write into); loading may not, since
        // there is nothing there to read.
        dialog.Init(slotIndex => OnRecordSlotSelected(isSave, slotIndex), allowEmpty: isSave);
        PushDialog(dialogObject);
    }

    /// <summary>
    /// Opens the party creature picker over the home dialog (the Sell action). It shows the
    /// whole party for the info panel; backing out of it (Esc) reports through OnClosed, which
    /// pops it back to the home dialog the same way the record picker's cancel does.
    /// </summary>
    private void OpenCreaturesDialog()
    {
        if (shoppingCreaturesDialogPrefab == null)
        {
            Debug.LogWarning("Shopping scene has no creatures dialog prefab to show.");
            return;
        }

        GameObject dialogObject = Instantiate(shoppingCreaturesDialogPrefab);
        ShoppingCreaturesDialog dialog = dialogObject.GetComponent<ShoppingCreaturesDialog>();
        if (dialog == null)
        {
            Debug.LogWarning("Creatures dialog prefab has no ShoppingCreaturesDialog component.");
            Destroy(dialogObject);
            return;
        }

        dialog.Init(record, ShoppingCreaturesDialog.CreatureSelectType.All, CreatureInfoType.SelectAllItem, PopDialog);
        PushDialog(dialogObject);
    }

    /// <summary>
    /// Opens the item picker (Buy) over the home dialog. The stock shown is the Shop.txt
    /// column for the shop the player is standing in -- see ShopIdForBuy -- and backing out
    /// (Esc) pops back to the home dialog through OnClosed, the same way the other pickers do.
    /// Confirming an item reports through OnItemSelected (OnItemToBuy).
    /// </summary>
    private void OpenItemsDialog()
    {
        if (shoppingItemsDialogPrefab == null)
        {
            Debug.LogWarning("Shopping scene has no items dialog prefab to show.");
            return;
        }

        int shopId = ShopIdForBuy(shopIndex);
        if (shopId < 0)
        {
            Debug.LogWarning("Shopping scene: shop index " + shopIndex + " has no Buy action.");
            return;
        }

        GameObject dialogObject = Instantiate(shoppingItemsDialogPrefab);
        ShoppingItemsDialog dialog = dialogObject.GetComponent<ShoppingItemsDialog>();
        if (dialog == null)
        {
            Debug.LogWarning("Items dialog prefab has no ShoppingItemsDialog component.");
            Destroy(dialogObject);
            return;
        }

        int chapterId = record != null ? record.ChapterId : 0;
        dialog.OnClosed = PopDialog;
        dialog.OnItemSelected = OnItemToBuy;
        dialog.Init(chapterId, shopId);
        PushDialog(dialogObject);
    }

    /// <summary>
    /// The Shop.txt shop column (its ShopDefinition.ShopType id) a village shop buys from,
    /// keyed by the spot the player entered on: the item shop (1) buys the Item column (2),
    /// the armor shop (4) the Amor column (1), the secret shop (5) the Secret column (3). The
    /// church (2) and bar (3) have no Buy action and never reach here, so they map to -1.
    /// </summary>
    private static int ShopIdForBuy(int shopIndex)
    {
        switch (shopIndex)
        {
            case 1: return 2;   // ItemShop   -> Item column
            case 4: return 1;   // AmorShop   -> Amor column
            case 5: return 3;   // SecretShop -> Secret column
            default: return -1; // church / bar: no Buy
        }
    }

    /// <summary>
    /// A player picked an item to buy, from the item picker on top of the stack. With too
    /// little money the "钱不够！" notice is shown over the picker and nothing else happens;
    /// otherwise the "这个 ...，NN 元，要不要啊？" confirm is asked, and a Yes carries the item
    /// on to the creature picker (OnBuyConfirmed). Esc out of the picker backs to the home
    /// dialog through OnClosed as before.
    /// </summary>
    private void OnItemToBuy(int itemId)
    {
        ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);
        if (item == null)
        {
            Debug.LogWarning("ShoppingScene: cannot buy unknown item " + itemId);
            return;
        }

        int money = record != null ? record.TotalMoney : 0;
        if (money < item.Price)
        {
            // "钱不够！" -- shown over the item picker, which any key pops back to.
            OpenMessageDialog(FDMessage.Create(FDMessage.MessageTypes.Information, NotEnoughMoneyMessageId));
            return;
        }

        pendingBuyItem = item;

        // "这个{名字}，NN 元，要不要啊？"
        FDMessage confirm = FDMessage.Create(
            FDMessage.MessageTypes.Confirm, BuyConfirmId, item.Price, 0, item.Name ?? string.Empty);
        OpenConfirmDialog(confirm, OnBuyConfirmed);
    }

    /// <summary>
    /// The "buy it?" confirm has closed. The confirm is popped either way. On No the item
    /// picker comes back for another pick; on Yes the creature picker opens over it, to choose
    /// who receives the item (OnCreatureToBuy).
    /// </summary>
    private void OnBuyConfirmed(bool yes)
    {
        PopDialog();

        if (!yes)
        {
            pendingBuyItem = null;
            return;
        }

        OpenBuyCreaturesDialog();
    }

    /// <summary>
    /// Opens the party creature picker (Buy: "who gets it?") over the item picker. Unlike the
    /// Sell picker it reports the chosen creature back through OnCreatureToBuy rather than
    /// opening the info dialog; Esc backs out through OnClosed, which pops it back to the item
    /// picker.
    /// </summary>
    private void OpenBuyCreaturesDialog()
    {
        if (shoppingCreaturesDialogPrefab == null)
        {
            Debug.LogWarning("Shopping scene has no creatures dialog prefab to show.");
            return;
        }

        GameObject dialogObject = Instantiate(shoppingCreaturesDialogPrefab);
        ShoppingCreaturesDialog dialog = dialogObject.GetComponent<ShoppingCreaturesDialog>();
        if (dialog == null)
        {
            Debug.LogWarning("Creatures dialog prefab has no ShoppingCreaturesDialog component.");
            Destroy(dialogObject);
            return;
        }

        dialog.Init(record, ShoppingCreaturesDialog.CreatureSelectType.All, CreatureInfoType.SelectAllItem, PopDialog, OnCreatureToBuy);
        PushDialog(dialogObject);
    }

    /// <summary>
    /// The player chose which creature receives the pending item. A full pack takes nothing:
    /// the "{名字}带不动了！" notice is shown over the picker (any key pops back to it) and no
    /// purchase is made. Otherwise, for an equippable item the "要装备上去吗？" question is asked
    /// first and the purchase waits on the answer (OnEquipConfirmed) -- Yes buys and equips, No
    /// just buys; a plain item is bought straight away.
    /// </summary>
    private void OnCreatureToBuy(FDCreature creature)
    {
        if (pendingBuyItem == null || creature == null)
        {
            return;
        }

        CreatureMapRecord friend = FindFriendById(creature.Id);
        if (friend == null)
        {
            Debug.LogWarning("ShoppingScene: no party record for creature " + creature.Id);
            return;
        }

        // A record with no pack list yet would leave the live creature's Items null, which
        // AddItem cannot append to; give it an empty list before the live creature shares it.
        if (friend.ItemIds == null)
        {
            friend.ItemIds = new List<int>();
        }

        // Rebuild a live creature to reuse the pack utility for the full check. Nothing is
        // bought yet, so this live creature is only read from here.
        FDCreature live = GameMapRecordManager.CreateCreatureFromRecord(friend);
        if (live.IsItemsFull())
        {
            // "{名字}带不动了！" -- shown over the creature picker, which any key pops back to.
            string name = LocalizationManager.GetCreatureString(friend.DefinitionId).GetLocalizedString();
            OpenMessageDialog(FDMessage.Create(FDMessage.MessageTypes.Information, ItemsFullMessageId, 0, 0, name));
            return;
        }

        if (pendingBuyItem.IsEquipment())
        {
            // "要装备上去吗？" -- the purchase itself waits on the answer, then buys (No) or
            // buys and equips (Yes) in the one step.
            pendingBuyFriend = friend;
            OpenConfirmDialog(EquipConfirmId, OnEquipConfirmed);
            return;
        }

        ExecutePurchase(friend, equip: false);
    }

    /// <summary>
    /// The "equip it?" confirm has closed. The confirm is popped, then the purchase is made at
    /// last: on Yes the item is bought and equipped, on No it is only bought. Either way the
    /// bought flourish plays after.
    /// </summary>
    private void OnEquipConfirmed(bool yes)
    {
        PopDialog();

        CreatureMapRecord friend = pendingBuyFriend;
        pendingBuyFriend = null;
        if (friend == null)
        {
            return;
        }

        ExecutePurchase(friend, equip: yes);
    }

    /// <summary>
    /// Makes the purchase: the pending item drops into the creature's pack and its price leaves
    /// the purse, and -- when <paramref name="equip"/> -- the item just added is equipped. The
    /// pack list is the record's own (CreateCreatureFromRecord shares it) and the moved equip
    /// indices are written back, so both survive the trip back to the village. The bought
    /// flourish plays once it is done.
    /// </summary>
    private void ExecutePurchase(CreatureMapRecord friend, bool equip)
    {
        if (pendingBuyItem == null || friend == null)
        {
            return;
        }

        FDCreature live = GameMapRecordManager.CreateCreatureFromRecord(friend);
        live.AddItem(pendingBuyItem.ItemId);
        if (equip)
        {
            live.EquipItemAt(live.Items.Count - 1);
        }
        WriteBackItems(friend, live);

        record.TotalMoney -= pendingBuyItem.Price;
        RefreshMoneyBar();

        ShowBoughtAnimation();
    }

    /// <summary>
    /// Plays the bought-item flourish (a placeholder for now) over the creature picker for a
    /// couple of seconds, then pops both it and the picker to land back on the item picker so
    /// the player can buy again. The pending item is cleared once it is spent.
    /// </summary>
    private void ShowBoughtAnimation()
    {
        ItemDefinition item = pendingBuyItem;
        pendingBuyItem = null;

        GameObject animationObject = new GameObject("BoughtItemAnimation");
        BoughtItemAnimationDialog animation = animationObject.AddComponent<BoughtItemAnimationDialog>();
        animation.Init(item, BoughtAnimationSeconds, () =>
        {
            PopDialog(); // the animation
            PopDialog(); // the creature picker, back to the item picker
        });
        PushDialog(animationObject);
    }

    /// <summary>The party record for the creature with this id, or null if the party has none.</summary>
    private CreatureMapRecord FindFriendById(int creatureId)
    {
        if (record == null || record.Friends == null)
        {
            return null;
        }

        return record.Friends.Find(friend => friend != null && friend.Id == creatureId);
    }

    /// <summary>
    /// Copies a live creature's pack and equip indices back onto its party record after a buy
    /// or an equip, so the change is carried back to the village. The item list is the record's
    /// own (CreateCreatureFromRecord shares it), but the equip indices are separate ints and
    /// must be written across.
    /// </summary>
    private static void WriteBackItems(CreatureMapRecord friend, FDCreature creature)
    {
        friend.ItemIds = creature.Items;
        friend.AttackItemIndex = creature.AttackItemIndex;
        friend.DefendItemIndex = creature.DefendItemIndex;
    }

    /// <summary>
    /// The slot picker has closed on a choice. It is popped first, whichever way this goes;
    /// a negative slot means the player backed out and nothing more happens. Otherwise the
    /// save flow writes the record and shows the "saved" notice, and the load flow reads the
    /// slot and asks the player to confirm before it takes effect.
    /// </summary>
    private void OnRecordSlotSelected(bool isSave, int slotIndex)
    {
        PopDialog();

        if (slotIndex < 0)
        {
            return;
        }

        if (isSave)
        {
            GameRecordManager.SaveToFile(slotIndex, record);
            OpenMessageDialog(SaveDoneMessageId);
        }
        else
        {
            // Read now to confirm the slot actually holds a save; an empty slot is nothing
            // to load, so the confirm question is skipped and the home dialog stays up.
            GameRecord loaded = GameRecordManager.LoadFromFile(slotIndex);
            if (loaded == null)
            {
                Debug.LogWarning("Load slot " + slotIndex + " is empty; nothing to read.");
                return;
            }

            pendingLoadSlot = slotIndex;
            OpenConfirmDialog(LoadConfirmId, OnLoadConfirmed);
        }
    }

    /// <summary>
    /// The load confirm has closed. On Yes the record is read afresh and carried into the
    /// village, which the load drops the player back into. On No the question is just popped
    /// and the home dialog comes back.
    /// </summary>
    private void OnLoadConfirmed(bool yes)
    {
        PopDialog();

        if (!yes)
        {
            return;
        }

        GameRecord loaded = GameRecordManager.LoadFromFile(pendingLoadSlot);
        if (loaded == null)
        {
            Debug.LogWarning("Load slot " + pendingLoadSlot + " could not be read on confirm.");
            return;
        }

        leaving = true;
        GlobalVariables.Set(RecordVariableName, loaded);
        SceneManager.LoadScene("VillageScene", LoadSceneMode.Single);
    }

    /// <summary>Pushes a one-line notice that any key dismisses, popping back to whatever is beneath.</summary>
    private void OpenMessageDialog(int messageId)
    {
        ShoppingMessageDialog dialog = InstantiateMessageDialog();
        if (dialog == null)
        {
            return;
        }

        dialog.Init(messageId, PopDialog);
        PushDialog(dialog.gameObject);
    }

    /// <summary>As OpenMessageDialog, for a line whose parameters ({名字} and the rest) are filled in.</summary>
    private void OpenMessageDialog(FDMessage message)
    {
        ShoppingMessageDialog dialog = InstantiateMessageDialog();
        if (dialog == null)
        {
            return;
        }

        dialog.Init(message, PopDialog);
        PushDialog(dialog.gameObject);
    }

    private ShoppingMessageDialog InstantiateMessageDialog()
    {
        if (shoppingMessageDialogPrefab == null)
        {
            Debug.LogWarning("Shopping scene has no message dialog prefab to show.");
            return null;
        }

        GameObject dialogObject = Instantiate(shoppingMessageDialogPrefab);
        ShoppingMessageDialog dialog = dialogObject.GetComponent<ShoppingMessageDialog>();
        if (dialog == null)
        {
            Debug.LogWarning("Message dialog prefab has no ShoppingMessageDialog component.");
            Destroy(dialogObject);
            return null;
        }

        return dialog;
    }

    /// <summary>Pushes a yes / no question, reporting the answer back through onSelected.</summary>
    private void OpenConfirmDialog(int confirmId, System.Action<bool> onSelected)
    {
        ShoppingConfirmDialog dialog = InstantiateConfirmDialog();
        if (dialog == null)
        {
            return;
        }

        dialog.Init(confirmId, onSelected);
        PushDialog(dialog.gameObject);
    }

    /// <summary>As OpenConfirmDialog, for a question whose parameters ({名字}, {价格}) are filled in.</summary>
    private void OpenConfirmDialog(FDMessage message, System.Action<bool> onSelected)
    {
        ShoppingConfirmDialog dialog = InstantiateConfirmDialog();
        if (dialog == null)
        {
            return;
        }

        dialog.Init(message, onSelected);
        PushDialog(dialog.gameObject);
    }

    private ShoppingConfirmDialog InstantiateConfirmDialog()
    {
        if (shoppingConfirmDialogPrefab == null)
        {
            Debug.LogWarning("Shopping scene has no confirm dialog prefab to show.");
            return null;
        }

        GameObject dialogObject = Instantiate(shoppingConfirmDialogPrefab);
        ShoppingConfirmDialog dialog = dialogObject.GetComponent<ShoppingConfirmDialog>();
        if (dialog == null)
        {
            Debug.LogWarning("Confirm dialog prefab has no ShoppingConfirmDialog component.");
            Destroy(dialogObject);
            return null;
        }

        return dialog;
    }

    /// <summary>
    /// Leaves for the village: the picture fades to black and the village is loaded on
    /// it. The party and the return marker are handed back untouched -- the village put
    /// the marker up when it entered the shop -- so the village knows to back its camera
    /// out of this shop rather than fade in fresh.
    /// </summary>
    private void LeaveShop()
    {
        leaving = true;

        // Take the dialogs down before the curtain comes across, so only the shop picture
        // is left to fade out -- the dialog does not linger on screen through the fade.
        HideDialogs();

        GlobalVariables.Set(RecordVariableName, record);

        fader.FadeTo(1.0f, fadeDuration, () =>
        {
            SceneManager.LoadScene("VillageScene", LoadSceneMode.Single);
        });
    }

    /// <summary>
    /// Hides every dialog on the stack and the money bar with them, so only the shop picture
    /// is left to fade out on the way to the village -- the read-out does not linger on screen
    /// through the fade.
    /// </summary>
    private void HideDialogs()
    {
        foreach (GameObject dialog in dialogStack)
        {
            if (dialog != null)
            {
                dialog.SetActive(false);
            }
        }

        if (moneyBarObject != null)
        {
            moneyBarObject.SetActive(false);
        }
    }

    /// <summary>
    /// Starts this shop's music, faded in, chosen by the spot the village entered on: the
    /// armor and secret shops share the Amor track, the item shop the Item track, and the
    /// bar and church each their own. Pos 0 (the way on to the next chapter) never opens a
    /// shop, so it has no music and is left silent.
    /// </summary>
    private void PlayShopMusic(int shopIndex)
    {
        string clipName = ShopMusicName(shopIndex);
        string path = string.IsNullOrEmpty(clipName) ? null : "Audios/" + clipName;
        BackgroundMusic.GetOrCreate().PlayClipByName(path);
    }

    /// <summary>
    /// The Resources/Audios clip name for each shop, keyed by the spot the cursor entered
    /// on: item shop (1), church (2), bar (3), armor shop (4), secret shop (5). The armor
    /// and secret shops share the Amor track. Anything else has no music.
    /// </summary>
    private static string ShopMusicName(int shopIndex)
    {
        switch (shopIndex)
        {
            case 1: return "Shop_Item_HD_Final_V1"; // ItemShop
            case 2: return "Shop_Church_HD_Final";  // Church
            case 3: return "Shop_Bar_HD_Final";     // Bar
            case 4: return "Shop_Amor_HD_Final";    // AmorShop
            case 5: return "Shop_Amor_HD_Final";    // SecretShop -> Amor track
            default: return null;
        }
    }

    /// <summary>
    /// Puts the shop picture up full-screen, chosen by the spot the village entered on.
    /// Rendered through the camera rather than as an overlay so the fader's black
    /// curtain, which is an overlay, still lands on top of it.
    /// </summary>
    private void ShowBackground(int shopIndex)
    {
        Image image = FindBackgroundImage();
        if (image == null)
        {
            Debug.LogWarning("Shopping scene has no background image to draw the shop on.");
            return;
        }

        //// The shop index is the picture number directly: pos 1-5 show VillageShop-01-1
        //// through -5. Clamped to the pictures that exist.
        int pictureNo = Mathf.Clamp(shopIndex, 1, ShopPictureCount);
        string spritePath = string.Format("Shops/VillageShop-01-{0}", pictureNo);
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogWarning("Cannot load shop background: " + spritePath);
            return;
        }

        image.sprite = sprite;

        //// Fill the screen whatever the resolution turns out to be.
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Canvas canvas = image.canvas;
        if (canvas != null && Camera.main != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 32.0f;
        }
    }

    private Image FindBackgroundImage()
    {
        GameObject background = GameObject.Find("Background");
        if (background != null)
        {
            Image image = background.GetComponentInChildren<Image>(true);
            if (image != null)
            {
                return image;
            }
        }

        return FindFirstObjectByType<Image>();
    }
}
