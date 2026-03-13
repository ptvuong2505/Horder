using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ShopItemCard
/// UI component cho 1 "card" item trong shop.
/// 
/// Mục tiêu:
/// - Hiển thị icon/tên/mô tả/giá.
/// - Disable nút mua nếu không đủ coins.
/// - Khi mua: trừ coins và phát event để hệ thống khác (ShopManager/Inventory) xử lý.
/// 
/// Cách dùng (gợi ý):
/// 1) Tạo 1 Panel/Card prefab trong Canvas.
/// 2) Gán các reference: icon, nameText, descText, priceText, buyButton.
/// 3) Khi mở shop, gọi Bind(...) để set data.
/// </summary>
public class ShopItemCard : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button buyButton;

    [Header("Visual")]
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = new Color(1f, 1f, 1f, 0.5f);

    /// <summary>
    /// Data nhẹ để bind lên UI. (Không phải ScriptableObject để bạn đổi dần theo nhu cầu.)
    /// </summary>
    [Serializable]
    public class ShopItemViewModel
    {
        public string id;              // ID item (để phân biệt loại item)
        public Sprite icon;
        public string title;
        [TextArea(2, 4)] public string description;
        public int price;
    }

    private ShopItemViewModel current;

    /// <summary>
    /// Event khi mua thành công.
    /// Shop UI có thể lắng nghe để add vào inventory / apply effect / remove item khỏi shop.
    /// </summary>
    public event Action<ShopItemViewModel> OnPurchased;

    private void Awake()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(TryPurchase);
        }
    }

    private void OnEnable()
    {
        // Khi mở shop, update lại trạng thái nút theo coins hiện có.
        RefreshAffordability();

        if (GameManager.Instance != null)
            GameManager.Instance.OnCoinsChanged += HandleCoinsChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
    }

    private void HandleCoinsChanged(int _)
    {
        RefreshAffordability();
    }

    /// <summary>
    /// Bind dữ liệu lên UI.
    /// </summary>
    public void Bind(ShopItemViewModel vm)
    {
        current = vm;

        if (icon != null)
        {
            icon.sprite = vm.icon;
            icon.enabled = vm.icon != null;
        }

        if (nameText != null) nameText.text = string.IsNullOrWhiteSpace(vm.title) ? "Item" : vm.title;
        if (descText != null) descText.text = vm.description ?? string.Empty;
        if (priceText != null) priceText.text = vm.price.ToString();

        RefreshAffordability();
    }

    /// <summary>
    /// Kiểm tra người chơi có đủ tiền mua item đang bind hay không.
    /// </summary>
    public bool CanAfford()
    {
        if (current == null) return false;
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.Coins >= current.price;
    }

    /// <summary>
    /// Thử mua item: trừ coins và fire event nếu thành công.
    /// </summary>
    public void TryPurchase()
    {
        if (current == null) return;
        if (GameManager.Instance == null) return;

        if (!CanAfford())
        {
            // Có thể thêm SFX/Animation "not enough coins" sau.
            RefreshAffordability();
            return;
        }

        // Trừ coins (dùng AddCoins số âm để tránh tạo thêm API mới).
        GameManager.Instance.AddCoins(-current.price);

        // SFX (tạm dùng Pickup để nghe có phản hồi; sau có thể thêm clip "Buy")
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPickup();

        OnPurchased?.Invoke(current);

        RefreshAffordability();
    }

    /// <summary>
    /// Update màu/nút mua theo coins.
    /// </summary>
    public void RefreshAffordability()
    {
        bool canBuy = CanAfford();

        if (buyButton != null)
            buyButton.interactable = canBuy;

        // Làm mờ icon/card nếu không đủ tiền
        if (icon != null)
            icon.color = canBuy ? affordableColor : unaffordableColor;
    }
}
