using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MemeFighter.Fight
{
    /// <summary>
    /// Spawns the two fighters picked in the menus (GameSession) and hooks up camera and controls.
    /// Movement-only prototype: no attacks, damage or stocks yet. Esc returns to the main menu.
    /// </summary>
    public class FightBootstrap : MonoBehaviour
    {
        public MenuRoster roster;
        public Transform[] spawnPoints = new Transform[2];
        public FightCamera fightCamera;
        public string menuScene = "MainMenu";

        FighterMotor[] fighters = new FighterMotor[2];
        GUIStyle tagStyle, hintStyle;

        void Start()
        {
            int[] picks = { GameSession.P1Fighter, GameSession.P2Fighter };
            for (int i = 0; i < 2; i++)
            {
                var pos = spawnPoints.Length > i && spawnPoints[i] ? spawnPoints[i].position : new Vector3(i == 0 ? -3f : 3f, 1f, 0f);
                fighters[i] = Spawn(picks[i], pos);
                fighters[i].name = (i == 0 ? "P1_" : (GameSession.P2IsCpu ? "CPU_" : "P2_")) + fighters[i].name;
            }

            fighters[0].control = FighterMotor.Control.P1Keyboard;
            fighters[1].control = GameSession.P2IsCpu ? FighterMotor.Control.Cpu : FighterMotor.Control.P2Keyboard;
            fighters[1].cpuTarget = fighters[0].transform;
            fighters[1].ForceFacing(-1);
            // fighters pass through each other, like in Smash
            Physics.IgnoreCollision(fighters[0].GetComponent<Collider>(), fighters[1].GetComponent<Collider>(), true);

            if (fightCamera)
            {
                fightCamera.targets.Clear();
                foreach (var f in fighters) fightCamera.targets.Add(f.transform);
            }
        }

        FighterMotor Spawn(int fighterIndex, Vector3 pos)
        {
            GameObject prefab = null;
            if (roster && fighterIndex >= 0 && fighterIndex < roster.fighters.Count)
                prefab = roster.fighters[fighterIndex].prefab;

            GameObject go;
            if (prefab)
            {
                go = Instantiate(prefab, pos, Quaternion.identity);
                go.name = prefab.name;
            }
            else
            {
                // Fighter without a model yet: stand-in capsule.
                go = new GameObject("PlaceholderFighter");
                go.transform.position = pos;
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(body.GetComponent<Collider>());
                body.name = "Model";
                body.transform.SetParent(go.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                body.transform.localScale = new Vector3(0.7f, 0.65f, 0.7f);
                var cap = go.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0f, 0.65f, 0f); cap.height = 1.3f; cap.radius = 0.35f;
                go.AddComponent<Rigidbody>();
                go.AddComponent<FighterMotor>().model = body.transform;
            }
            var motor = go.GetComponent<FighterMotor>();
            motor.spawnPoint = pos;
            return motor;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                SceneManager.LoadScene(menuScene);
        }

        void OnGUI()
        {
            if (tagStyle == null)
            {
                tagStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
                hintStyle.normal.textColor = new Color(0.73f, 0.7f, 0.9f);
            }

            var cam = Camera.main;
            if (cam)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (!fighters[i]) continue;
                    Vector3 sp = cam.WorldToScreenPoint(fighters[i].transform.position + Vector3.up * 1.7f);
                    if (sp.z < 0f) continue;
                    string label = i == 0 ? "P1" : GameSession.P2IsCpu ? "CPU" : "P2";
                    tagStyle.normal.textColor = i == 0 ? new Color(1f, 0.42f, 0.21f) : new Color(0.25f, 0.66f, 1f);
                    GUI.Label(new Rect(sp.x - 40, Screen.height - sp.y - 15, 80, 30), label, tagStyle);
                }
            }

            string p2 = GameSession.P2IsCpu ? "CPU follows you" : "P2: Arrows move/jump, Down drop";
            GUI.Label(new Rect(16, 12, 900, 24),
                "PROTOTYPE (movement only)   P1: A/D move, W/Space jump, S drop/fast fall   " + p2 + "   Esc: menu", hintStyle);
        }
    }
}
