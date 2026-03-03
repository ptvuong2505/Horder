using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BossHealthBar – Thanh HP boss hiện ở trên đầu màn hình.
/// Tự tìm BossEnemy trong scene và subscribe OnHealthChanged.
/// Ẩn khi không có boss, hiện khi boss spawn.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Header("UI References")]
    public Image fillImage;
    public Image backgroundImage;
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI phaseText;
    public CanvasGroup canvasGroup;

    private BossEnemy currentBoss;
    private bool isVisible = false;

    void Start()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0;
    }

    void Update()
    {
        // Tìm boss nếu chưa có
        if (currentBoss == null)
        {
            BossEnemy boss = FindFirstObjectByType<BossEnemy>();
            if (boss != null)
                RegisterBoss(boss);
            else if (isVisible)
                Hide();
        }
    }

    public void RegisterBoss(BossEnemy boss)
    {
        currentBoss = boss;
        currentBoss.OnHealthChanged += UpdateHealthBar;
        currentBoss.OnBossDied += OnBossDied;

        if (bossNameText != null)
            bossNameText.text = "👑 KING SLIME";

        Show();
        UpdateHealthBar(boss.maxHealth, boss.maxHealth);
    }

    void UpdateHealthBar(int current, int max)
    {
        if (fillImage != null)
        {
            float percent = (float)current / max;
            fillImage.fillAmount = percent;

            // Đổi màu theo HP
            if (percent > 0.5f)
                fillImage.color = Color.green;
            else if (percent > 0.25f)
                fillImage.color = Color.yellow;
            else
                fillImage.color = Color.red;
        }

        if (phaseText != null && currentBoss != null)
        {
            if (currentBoss.IsEnraged)
                phaseText.text = "🔥 ENRAGED!";
            else if (currentBoss.CurrentPhase == 2)
                phaseText.text = "⚡ Phase 2";
            else
                phaseText.text = "";
        }
    }

    void OnBossDied()
    {
        if (phaseText != null)
            phaseText.text = "💀 DEFEATED!";

        Invoke(nameof(Hide), 2f);
    }

    void Show()
    {
        isVisible = true;
        if (canvasGroup != null)
            canvasGroup.alpha = 1;
    }

    void Hide()
    {
        isVisible = false;
        currentBoss = null;
        if (canvasGroup != null)
            canvasGroup.alpha = 0;
    }
}
