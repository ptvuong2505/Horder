using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Project.Scripts
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player")]
    public class PlayerData : ScriptableObject
    {
        public string playerID;
        public string playerName;

        [TextArea(2, 3)]
        public string description;

        public Sprite portrait;

        public int maxHealth;
        public float speed;

        [Header("Unlock")]
        public int unlockCost;      // 0 = mở khóa mặc định
        public bool unlockedByDefault;

        public RuntimeAnimatorController animator;
        public GameObject playerPrefab;
    }
}
