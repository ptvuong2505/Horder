using TMPro;
using UnityEngine;

/// <summary>
/// KillCountUI – Hiển thị tổng số enemy đã tiêu diệt.
/// Gắn vào Canvas > KillCountUI object.
/// </summary>
public class KillCountUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI killText;   // "Kills: 0"

    private int killCount = 0;

    void Start()
    {
        UpdateText();

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.OnAllEnemiesDead += OnWaveClear;
    }

    void OnDestroy()
    {
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.OnAllEnemiesDead -= OnWaveClear;
    }

    void Update()
    {
        // Đọc tổng kill realtime từ EnemyManager
        if (EnemyManager.Instance != null && killCount != EnemyManager.Instance.TotalKilled)
        {
            killCount = EnemyManager.Instance.TotalKilled;
            UpdateText();
        }
    }

    void OnWaveClear()
    {
        // Refresh thêm 1 lần khi wave vừa clear
        if (EnemyManager.Instance != null)
            killCount = EnemyManager.Instance.TotalKilled;
        UpdateText();
    }

    void UpdateText()
    {
        if (killText != null)
            killText.text = $"Kills: {killCount}";
    }
}
