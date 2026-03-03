using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Project.Scripts
{
    [System.Serializable]
    public class SaveData
    {
        public int gold;

        public List<string> unlockedPlayers = new List<string>();
        public List<string> unlockedWeapons = new List<string>();

        public string selectedPlayerId;
        public List<string> selectedWeaponId;
    }
}
