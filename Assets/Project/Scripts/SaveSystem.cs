using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Project.Scripts
{
    using System.IO;
    using UnityEngine;

    public static class SaveSystem
    {
        static string path = Application.persistentDataPath + "/save.json";

        public static void Save(SaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
        }

        public static SaveData Load()
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<SaveData>(json);
            }

            return new SaveData();
        }
    }
}
