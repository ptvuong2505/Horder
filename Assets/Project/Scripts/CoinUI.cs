using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CoinUI – Hiển thị icon + số coins realtime trên HUD.
/// Gắn vào Canvas > CoinUI object.
/// </summary>
public class CoinUI : MonoBehaviour
{
    [Header("References")]
    public Image coinIcon;             // Icon đồng tiền (Image)
    public TextMeshProUGUI coinText;   // Text hiển thị số coins

    [Header("Animation")]
    public bool punchOnCoin = true;
    private Vector3 originalScale;
    private float punchTimer = 0f;
    private float punchDuration = 0.15f;

    void Start()
    {
        originalScale = transform.localScale;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCoinsChanged += UpdateCoinText;
            // Show real loaded coins immediately (avoid always showing 0 at startup)
            UpdateCoinText(GameManager.Instance.Coins);
        }
        else
        {
            UpdateCoinText(0);
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnCoinsChanged -= UpdateCoinText;
    }

    void Update()
    {
        if (punchTimer > 0f)
        {
            punchTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(punchTimer / punchDuration);
            float scale = Mathf.Lerp(1.3f, 1f, t);
            transform.localScale = originalScale * scale;
        }
    }    void UpdateCoinText(int newCoins)
    {
        if (coinText != null)
            coinText.text = newCoins.ToString();

        if (punchOnCoin)
        {
            punchTimer = punchDuration;
            transform.localScale = originalScale * 1.3f;
        }
    }
}
