using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WindingTale.Core.Files;
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

    /// <summary>The one-line notice shown after a save. See ShoppingMessageDialog.</summary>
    public GameObject shoppingMessageDialogPrefab = null;

    /// <summary>The yes / no question shown before a load. See ShoppingConfirmDialog.</summary>
    public GameObject shoppingConfirmDialogPrefab = null;

    private GameRecord record = null;

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

    void Start()
    {
        shopIndex = GlobalVariables.Get<int>(ShopIndexVariableName);
        record = GlobalVariables.Get<GameRecord>(RecordVariableName);

        ShowBackground(shopIndex);

        // The shop picture fades up out of the black the village handed over on; the home
        // dialog is only put up once that has finished, so it appears on the shop rather
        // than fading in through the curtain with it.
        fader = ScreenFader.Create(1.0f);
        fader.FadeTo(0.0f, fadeDuration, () => ShowHomeDialog(shopIndex));
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

    /// <summary>Pushes a one-line notice that any key dismisses, popping back to the home dialog.</summary>
    private void OpenMessageDialog(int messageId)
    {
        if (shoppingMessageDialogPrefab == null)
        {
            Debug.LogWarning("Shopping scene has no message dialog prefab to show.");
            return;
        }

        GameObject dialogObject = Instantiate(shoppingMessageDialogPrefab);
        ShoppingMessageDialog dialog = dialogObject.GetComponent<ShoppingMessageDialog>();
        if (dialog == null)
        {
            Debug.LogWarning("Message dialog prefab has no ShoppingMessageDialog component.");
            Destroy(dialogObject);
            return;
        }

        dialog.Init(messageId, PopDialog);
        PushDialog(dialogObject);
    }

    /// <summary>Pushes a yes / no question, reporting the answer back through onSelected.</summary>
    private void OpenConfirmDialog(int confirmId, System.Action<bool> onSelected)
    {
        if (shoppingConfirmDialogPrefab == null)
        {
            Debug.LogWarning("Shopping scene has no confirm dialog prefab to show.");
            return;
        }

        GameObject dialogObject = Instantiate(shoppingConfirmDialogPrefab);
        ShoppingConfirmDialog dialog = dialogObject.GetComponent<ShoppingConfirmDialog>();
        if (dialog == null)
        {
            Debug.LogWarning("Confirm dialog prefab has no ShoppingConfirmDialog component.");
            Destroy(dialogObject);
            return;
        }

        dialog.Init(confirmId, onSelected);
        PushDialog(dialogObject);
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

    /// <summary>Hides every dialog on the stack, so the shop picture fades out on its own.</summary>
    private void HideDialogs()
    {
        foreach (GameObject dialog in dialogStack)
        {
            if (dialog != null)
            {
                dialog.SetActive(false);
            }
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
