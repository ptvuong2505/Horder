using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemyManager (Singleton)
/// Mục tiêu:
/// - Giữ danh sách enemy còn sống để các hệ thống khác query (alive count, tổng kill...).
/// - Về lâu dài nên dùng EnemyManager thay cho GameObject.FindGameObjectsWithTag("Enemy")
///   để giảm chi phí mỗi frame.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    private List<GameObject> aliveEnemies = new List<GameObject>();

    // Event: gọi khi tất cả enemy đã chết (để GameManager chuyển wave)
    public event Action OnAllEnemiesDead;

    // Tổng số enemy đã bị tiêu diệt (dùng tính điểm)
    public int TotalKilled { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // ──────────────────────────────────────────────
    public void RegisterEnemy(GameObject enemy)
    {
        if (!aliveEnemies.Contains(enemy))
            aliveEnemies.Add(enemy);
    }

    public void UnregisterEnemy(GameObject enemy)
    {
        aliveEnemies.Remove(enemy);
        TotalKilled++;

        // Nếu hết enemy VÀ spawner đã hết (spawner tự báo GameManager)
        // → chỉ fire event nếu danh sách trống
        if (aliveEnemies.Count == 0)
        {
            OnAllEnemiesDead?.Invoke();
        }
    }

    public int GetAliveCount() => aliveEnemies.Count;

    // Xóa toàn bộ danh sách (khi bắt đầu wave mới)
    public void ResetWave()
    {
        aliveEnemies.Clear();
    }
}

