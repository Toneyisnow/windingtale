using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Core.Common;

/// <summary>
/// A yes / no question pushed over the shop's dialog stack -- "确定要读取游戏吗？" before a
/// load, for one. It shows a single CommonStrings "Confirm-NN" line and a Yes / No pair;
/// Left / Right move between them, Space / Enter answers with whichever is selected, and the
/// cancel key (Esc / Backspace) always answers No. The
/// answer goes back through onSelected(true|false); the shop scene decides what it means.
///
/// Some questions carry parameters -- "这个{StrParam1}，#{IntParam1}元，要不要啊？" names the
/// item and its price when buying -- so the FDMessage overload resolves the "Confirm-NN" line
/// with its arguments filled in, the same way the field dialogs do.
/// </summary>
public class ShoppingConfirmDialog : MonoBehaviour
{
    public GameObject MessageText;

    public GameObject YesButton;

    public GameObject NoButton;

    /// <summary>Raised once the player answers: true for Yes, false for No.</summary>
    public Action<bool> OnSelected = null;

    // The selected button bounces, the same Abs(Sin) lift the other shop dialogs use.
    private const float ButtonBounceDepth = 8f;   // how far it lifts, in pixels
    private const float ButtonBounceSpeed = 4f;   // radians per second

    private float yesBaseY = 0f;
    private float noBaseY = 0f;
    private bool baseCaptured = false;
    private float bounceElapsed = 0f;

    // Yes is selected when the question opens; Right moves to No, Left comes back.
    private bool yesSelected = true;

    private bool initialized = false;

    // Draw the question in the dynamic record font instead of the baked FZB_Message atlas.
    private bool useRecordFont = false;

    // Ignore the frame Init ran on: the key that opened this question (the confirm on the
    // slot picker) is still down this frame and would answer it instantly.
    private bool firstFrame = false;

    /// <summary>
    /// Shows CommonStrings "Confirm-<paramref name="confirmId"/>" with Yes selected, and
    /// starts listening. onSelected fires once, on the first Space / Enter after this frame.
    /// </summary>
    public void Init(int confirmId, Action<bool> onSelected)
    {
        Init(LocalizationManager.GetConfirmString(confirmId).GetLocalizedString(), onSelected);
    }

    /// <summary>
    /// Shows a "Confirm-NN" line with its parameters filled in ({StrParam1}, {IntParam1}) and
    /// a Yes / No pair -- for the questions the shop's Buy flow raises, whose text names the
    /// item and its price. onSelected fires once, on the first Space / Enter after this frame.
    /// </summary>
    public void Init(FDMessage message, Action<bool> onSelected)
    {
        Init(LocalizationManager.GetFDMessageString(message).GetLocalizedString(), onSelected);
    }

    /// <summary>
    /// Shows a literal question rather than a CommonStrings line, drawn in the dynamic record
    /// font. For a line whose glyphs the baked FZB_Message atlases do not carry (the
    /// overwrite-a-save question needs 覆, 盖 and 档), which would come out as boxes there.
    /// </summary>
    public void InitLiteral(string messageText, Action<bool> onSelected)
    {
        Init(messageText, onSelected, useRecordFont: true);
    }

    private void Init(string messageText, Action<bool> onSelected, bool useRecordFont = false)
    {
        this.OnSelected = onSelected;
        this.useRecordFont = useRecordFont;

        CaptureButtonBaseY();
        ShowText(messageText);
        WireButtonClicks();

        yesSelected = true;
        bounceElapsed = 0f;
        firstFrame = true;
        initialized = true;
    }

    void Update()
    {
        if (!initialized)
        {
            return;
        }

        if (firstFrame)
        {
            firstFrame = false;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            yesSelected = false;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            yesSelected = true;
        }
        else if (Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Answer(yesSelected);
        }
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        {
            // Cancel is No, for every question the shop asks.
            Answer(false);
        }

        AnimateSelectedButton();
    }

    private void Answer(bool yes)
    {
        initialized = false; // answer once, whatever the callback does next
        if (OnSelected != null)
        {
            OnSelected(yes);
        }
    }

    private void WireButtonClicks()
    {
        WireButtonClick(YesButton, true);
        WireButtonClick(NoButton, false);
    }

    private void WireButtonClick(GameObject buttonObject, bool yes)
    {
        Button button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            yesSelected = yes;
            Answer(yes);
        });
    }

    private void CaptureButtonBaseY()
    {
        RectTransform yesRt = YesButton != null ? YesButton.GetComponent<RectTransform>() : null;
        RectTransform noRt = NoButton != null ? NoButton.GetComponent<RectTransform>() : null;
        if (yesRt != null && noRt != null)
        {
            yesBaseY = yesRt.anchoredPosition.y;
            noBaseY = noRt.anchoredPosition.y;
            baseCaptured = true;
        }
    }

    /// <summary>Lifts whichever of Yes / No is selected, holding the other on its line.</summary>
    private void AnimateSelectedButton()
    {
        if (!baseCaptured)
        {
            return;
        }

        bounceElapsed += Time.deltaTime;
        float lift = Mathf.Abs(Mathf.Sin(bounceElapsed * ButtonBounceSpeed)) * ButtonBounceDepth;

        SetAnchoredY(YesButton, yesBaseY + (yesSelected ? lift : 0f));
        SetAnchoredY(NoButton, noBaseY + (yesSelected ? 0f : lift));
    }

    private static void SetAnchoredY(GameObject obj, float y)
    {
        RectTransform rt = obj != null ? obj.GetComponent<RectTransform>() : null;
        if (rt == null)
        {
            return;
        }

        Vector2 pos = rt.anchoredPosition;
        pos.y = y;
        rt.anchoredPosition = pos;
    }

    /// <summary>
    /// Drops the resolved line on, in the Chinese message font -- the whole FZB_Message font
    /// asset, as the other shop dialogs do, since the prefab ships with LiberationSans and no
    /// Chinese glyphs.
    /// </summary>
    private void ShowText(string text)
    {
        TextMeshProUGUI textMesh = MessageText != null ? MessageText.GetComponent<TextMeshProUGUI>() : null;
        if (textMesh == null)
        {
            return;
        }

        TMP_FontAsset messageFont = useRecordFont
            ? ShoppingRecordDialog.GetRecordFont()
            : Resources.Load<TMP_FontAsset>(@"Fonts/FontAssets/zh/FZB_Message");
        if (messageFont != null)
        {
            textMesh.font = messageFont;
        }

        // The prefab's font colour is a dark grey; the question reads white like the rest of the UI.
        textMesh.color = Color.white;

        // '#' is the source line-break marker, same as the field dialogs use.
        textMesh.text = text.Replace("#", "\n");
        textMesh.ForceMeshUpdate();
    }
}
