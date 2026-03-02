using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UpgradeUI – Hiển thị 3 card upgrade để player chọn.
/// Gắn vào Canvas > UpgradeUI panel.
/// </summary>
public class UpgradeUI : MonoBehaviour
{
    [Header("Card Containers — gán 3 card panels")]
    public GameObject card1;
    public GameObject card2;
    public GameObject card3;

    [Header("Card 1 Elements")]
    public Image card1Icon;
    public TextMeshProUGUI card1Name;
    public TextMeshProUGUI card1Desc;
    public Button card1Button;

    [Header("Card 2 Elements")]
    public Image card2Icon;
    public TextMeshProUGUI card2Name;
    public TextMeshProUGUI card2Desc;
    public Button card2Button;

    [Header("Card 3 Elements")]
    public Image card3Icon;
    public TextMeshProUGUI card3Name;
    public TextMeshProUGUI card3Desc;
    public Button card3Button;

    [Header("Panel")]
    public GameObject upgradePanel;      // Panel cha chứa tất cả cards
    public TextMeshProUGUI titleText;    // "CHOOSE AN UPGRADE"

    private Action<UpgradeData> onSelected;
    private List<UpgradeData> currentOptions;

    void Start()
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }

    // ──────────────────────────────────────────────
    //  Hiển thị 3 cards
    // ──────────────────────────────────────────────
    public void ShowCards(List<UpgradeData> options, Action<UpgradeData> callback)
    {
        currentOptions = options;
        onSelected = callback;

        if (upgradePanel != null)
            upgradePanel.SetActive(true);

        if (titleText != null)
            titleText.text = "CHOOSE AN UPGRADE";

        // Setup card 1
        SetupCard(0, card1, card1Icon, card1Name, card1Desc, card1Button);
        // Setup card 2
        SetupCard(1, card2, card2Icon, card2Name, card2Desc, card2Button);
        // Setup card 3
        SetupCard(2, card3, card3Icon, card3Name, card3Desc, card3Button);
    }

    void SetupCard(int index, GameObject cardObj, Image icon, TextMeshProUGUI nameText, TextMeshProUGUI descText, Button button)
    {
        if (index < currentOptions.Count)
        {
            UpgradeData data = currentOptions[index];

            if (cardObj != null)
                cardObj.SetActive(true);

            if (icon != null && data.icon != null)
            {
                icon.sprite = data.icon;
                icon.enabled = true;
            }
            else if (icon != null)
            {
                icon.enabled = false;
            }

            if (nameText != null)
                nameText.text = data.upgradeName;

            if (descText != null)
                descText.text = data.description;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                int capturedIndex = index; // Capture cho closure
                button.onClick.AddListener(() => OnCardClicked(capturedIndex));
            }
        }
        else
        {
            // Ẩn card nếu không đủ options
            if (cardObj != null)
                cardObj.SetActive(false);
        }
    }

    void OnCardClicked(int index)
    {
        if (index < currentOptions.Count)
        {
            onSelected?.Invoke(currentOptions[index]);
        }
    }

    // ──────────────────────────────────────────────
    //  Ẩn tất cả cards
    // ──────────────────────────────────────────────
    public void HideCards()
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }
}
