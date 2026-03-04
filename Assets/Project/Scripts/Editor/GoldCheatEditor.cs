using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Project.Scripts.Editor
{
    public class GoldCheatEditor : EditorWindow
    {
        private int goldAmount = 1000;

        [MenuItem("Tools/Gold Cheat")]
        public static void ShowWindow()
        {
            GetWindow<GoldCheatEditor>("Gold Cheat");
        }

        void OnGUI()
        {
            GUILayout.Label("Thêm vàng để test", EditorStyles.boldLabel);

            goldAmount = EditorGUILayout.IntField("Số vàng:", goldAmount);

            if (GUILayout.Button("Thêm vàng"))
            {
                AddGold(goldAmount);
            }

            if (GUILayout.Button("Reset Save Data"))
            {
                ResetSave();
            }

            GUILayout.Space(10);
            GUILayout.Label($"Save path: {Application.persistentDataPath}/save.json", EditorStyles.miniLabel);
        }

        void AddGold(int amount)
        {
            SaveData data = SaveSystem.Load();
            data.gold += amount;
            SaveSystem.Save(data);
            Debug.Log($"[GoldCheat] Đã thêm {amount} vàng. Tổng: {data.gold}");
        }

        void ResetSave()
        {
            SaveData data = new SaveData();
            SaveSystem.Save(data);
            Debug.Log("[GoldCheat] Đã reset save data!");
        }
    }
}
