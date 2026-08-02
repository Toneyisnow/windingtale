using TMPro;
using UnityEngine;

/// <summary>
/// The shop's purse read-out: a "$" followed by the money as eight zero-padded digits
/// ("$00000000"). It is a persistent HUD in the ShoppingScene, not a dialog on the stack --
/// so it stays put while the item / creature pickers come and go over it. The scene switches
/// it on once the shop has faded up, refreshes it after every buy or sell, and switches it off
/// again on the way out. See ShoppingScene.ShowMoneyBar / RefreshMoneyBar.
/// </summary>
public class MoneyBar : MonoBehaviour
{
    /// <summary>How many digits the amount is padded to; the format the bar ships showing.</summary>
    private const int DigitCount = 8;

    // The bar carries one TMP label; found the first time it is needed and then reused.
    private TextMeshProUGUI moneyText = null;

    /// <summary>
    /// Puts the purse on the bar as "$" + eight zero-padded digits. A negative amount (which the
    /// buy gate should never let happen) is floored to zero so the "$" is never followed by a
    /// minus sign.
    /// </summary>
    public void SetMoney(int money)
    {
        TextMeshProUGUI text = ResolveText();
        if (text == null)
        {
            return;
        }

        int amount = Mathf.Max(0, money);
        text.text = "$" + amount.ToString("D" + DigitCount);
        text.ForceMeshUpdate();
    }

    private TextMeshProUGUI ResolveText()
    {
        if (moneyText == null)
        {
            // Include inactive: the bar is switched on and off, and the label may be looked up
            // (SetMoney) while the bar is still hidden, just before it is shown.
            moneyText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        return moneyText;
    }
}
