using System.Collections.Generic;
using UnityEngine;

namespace MemeFighter
{
    /// <summary>
    /// The fighters and stages shown in the menus. Edit the asset at
    /// Assets/MemeFighter/Data/MenuRoster.asset to rename fighters, add portraits,
    /// link prefabs and point stages at their scenes.
    /// </summary>
    [CreateAssetMenu(menuName = "Meme Fighter/Menu Roster", fileName = "MenuRoster")]
    public class MenuRoster : ScriptableObject
    {
        [System.Serializable]
        public class Fighter
        {
            public string displayName = "FIGHTER";
            public Sprite portrait;
            public GameObject prefab;
            public bool locked;
        }

        [System.Serializable]
        public class Stage
        {
            public string displayName = "STAGE";
            [TextArea] public string description;
            [Tooltip("Scene to load. It must be added to File > Build Profiles > Scene List.")]
            public string sceneName;
            public bool isRandom;
            public Sprite preview;
            public Color previewColor = new Color(0.18f, 0.44f, 0.85f);
            public Color platformColor = Color.white;
            [Tooltip("Platform boxes drawn when there is no preview sprite. x, y from top-left of a 768x512 preview.")]
            public Rect[] previewPlatforms;
        }

        public List<Fighter> fighters = new List<Fighter>();
        public List<Stage> stages = new List<Stage>();
    }
}
