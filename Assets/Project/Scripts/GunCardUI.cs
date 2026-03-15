using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GunCardUI – Gắn vào prefab card súng trong GunShopUI.
///
/// Hierarchy card gợi ý:
///   GunCard (GunCardUI)
///   ├── GunIcon        (Image)
///   ├── GunNameText    (TMP)
///   ├── DescText       (TMP)
///   ├── CostRow
///   │   ├── CostText   (TMP)
///   │   └── BuyButton  (Button)
///   │       └── BtnLabel (TMP)
///   └── OwnedOverlay   (Image – lớp phủ "ĐÃ SỞ HỮU")
/// </summary>
public class GunCardUI : MonoBehaviour
{
    [Header("References")]
    public Image gunIcon;
    public TextMeshProUGUI gunNameText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI costText;
    public Button buyButton;
    public TextMeshProUGUI buyButtonLabel;
    public GameObject ownedOverlay;       // Hiện khi đã mở khóa

    [Header("Colors")]
    public Color canAffordColor = new Color(0.2f, 0.8f, 0.2f);   // Xanh lá
    public Color cantAffordColor = new Color(0.8f, 0.2f, 0.2f);  // Đỏ
    public Color ownedColor = new Color(0.4f, 0.4f, 0.4f);       // Xám

    // ── Runtime ──────────────────────────────────────────────────────────────
    private GunData data;
    private System.Action onBuyCallback;

    // ────────────────────────────────────────────────────────────────────────
    public void Setup(GunData gunData, System.Action onBuy)
    {
        data = gunData;
        onBuyCallback = onBuy;

        // Fill thông tin static
        if (gunIcon != null && data.gunSprite != null)
        {
            gunIcon.sprite = data.gunSprite;
            gunIcon.enabled = true;
        }

        if (gunNameText != null)
            gunNameText.text = data.gunName;

        if (descText != null)
            descText.text = string.IsNullOrEmpty(data.description) ? "" : data.description;

        if (costText != null)
            costText.text = data.unlockedByDefault ? "Free" : $"{data.unlockCost} Coins";

        // Gán callback cho button
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => onBuyCallback?.Invoke());
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    /// <summary>Cập nhật trạng thái nút mua (gọi mỗi khi coin / trạng thái thay đổi).</summary>
    public void Refresh(bool isUnlocked, int currentCoins)
    {
        if (data == null) return;

        if (isUnlocked)
        {
            // Đã sở hữu
            if (ownedOverlay != null) ownedOverlay.SetActive(true);
            if (buyButton != null)
            {
                buyButton.interactable = false;
                ColorBlock cb = buyButton.colors;
                cb.normalColor = ownedColor;
                buyButton.colors = cb;
            }
            if (buyButtonLabel != null)
                buyButtonLabel.text = "OWNED";
        }
        else
        {
            if (ownedOverlay != null) ownedOverlay.SetActive(false);

            bool canAfford = currentCoins >= data.unlockCost;
            if (buyButton != null)
            {
                buyButton.interactable = canAfford;
                ColorBlock cb = buyButton.colors;
                cb.normalColor = canAfford ? canAffordColor : cantAffordColor;
                buyButton.colors = cb;
            }
            if (buyButtonLabel != null)
                buyButtonLabel.text = canAfford
                    ? $"BUY ({data.unlockCost} Coins)"
                    : "NOT ENOUGH COINS";
        }
    }
}
