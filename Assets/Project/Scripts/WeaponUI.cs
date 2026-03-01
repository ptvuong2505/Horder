using TMPro;
using UnityEngine;

/// <summary>
/// WeaponUI – Hiển thị 4 slot súng ở góc màn hình.
/// Gắn vào Canvas > WeaponUI object.
/// </summary>
public class WeaponUI : MonoBehaviour
{
    [Header("Slot Icons (4 Image hoặc UI element tương ứng 4 súng)")]
    public UnityEngine.UI.Image[] slotIcons;       // 4 icon slot súng
    public UnityEngine.UI.Image[] slotHighlights;  // 4 viền highlight slot đang chọn
    public TextMeshProUGUI[] slotKeyTexts;          // Hiển thị "1","2","3","4"
    public TextMeshProUGUI currentGunNameText;      // Tên súng đang dùng

    [Header("Colors")]
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;

    private WeaponManager weaponManager;

    void Start()
    {
        weaponManager = FindFirstObjectByType<WeaponManager>();

        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged += UpdateUI;

            // Khởi tạo UI với dữ liệu ban đầu
            InitSlots();
            UpdateUI(weaponManager.CurrentIndex, weaponManager.CurrentGunData);
        }
    }

    void OnDestroy()
    {
        if (weaponManager != null)
            weaponManager.OnWeaponChanged -= UpdateUI;
    }

    // ──────────────────────────────────────────────
    //  Khởi tạo icon và phím cho từng slot
    // ──────────────────────────────────────────────
    void InitSlots()
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            // Gán phím số
            if (i < slotKeyTexts.Length && slotKeyTexts[i] != null)
                slotKeyTexts[i].text = (i + 1).ToString();

            // Gán sprite icon súng
            if (i < weaponManager.gunDataList.Count && weaponManager.gunDataList[i] != null)
            {
                GunData data = weaponManager.gunDataList[i];
                if (slotIcons[i] != null && data.gunSprite != null)
                {
                    slotIcons[i].sprite = data.gunSprite;
                    slotIcons[i].enabled = true;
                }
            }
        }
    }

    // ──────────────────────────────────────────────
    //  Cập nhật highlight slot đang chọn
    // ──────────────────────────────────────────────
    void UpdateUI(int selectedIndex, GunData data)
    {
        for (int i = 0; i < slotHighlights.Length; i++)
        {
            if (slotHighlights[i] != null)
                slotHighlights[i].color = (i == selectedIndex) ? selectedColor : normalColor;
        }

        if (currentGunNameText != null && data != null)
            currentGunNameText.text = data.gunName;
    }
}
