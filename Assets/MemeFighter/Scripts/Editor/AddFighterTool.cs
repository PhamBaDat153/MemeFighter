using System.Collections.Generic;
using System.Linq;
using MemeFighter.Fight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemeFighter.EditorTools
{
    /// <summary>
    /// Menu: Meme Fighter > Import Animal Roster
    /// Replaces the character select roster with the six animal models in
    /// Assets/MemeFighter/Characters/{Corgi, Croc, Dino, Golden, Husky, Otter}:
    ///  - URP material per character (from its baked _Albedo texture)
    ///  - fighter prefab (Rigidbody + capsule sized to the model + FighterMotor)
    ///  - portrait sprite for character select
    ///  - Triple Platform test arena linked to that stage
    /// Safe to run again. To add a character later, put its files in a new folder and add a line to Roster.
    /// </summary>
    public static class AddFighterTool
    {
        // folder/file name, name shown in character select
        static readonly (string id, string display)[] Roster =
        {
            ("Corgi", "CORGI"),
            ("Croc", "CROC"),
            ("Dino", "DINO"),
            ("Golden", "GOLDEN"),
            ("Husky", "HUSKY"),
            ("Otter", "OTTER"),
        };

        const string CharDir = "Assets/MemeFighter/Characters";
        const string RosterPath = "Assets/MemeFighter/Data/MenuRoster.asset";
        const string ArenaDir = "Assets/MemeFighter/Arenas";
        const string ArenaScene = "Arena_TriplePlatform";
        const string ArenaPath = "Assets/MemeFighter/Scenes/" + ArenaScene + ".unity";

        [MenuItem("Meme Fighter/Import Animal Roster")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var roster = LoadRoster();
            if (roster == null)
            {
                EditorUtility.DisplayDialog("Meme Fighter", "No MenuRoster yet. Run Meme Fighter > Build Main Menu Scene first.", "OK");
                return;
            }

            var fighters = new List<MenuRoster.Fighter>();
            var missing = new List<string>();
            try
            {
                for (int i = 0; i < Roster.Length; i++)
                {
                    var (id, display) = Roster[i];
                    EditorUtility.DisplayProgressBar("Meme Fighter", "Setting up " + display, i / (float)Roster.Length);
                    var f = SetUpCharacter(id, display);
                    if (f == null) { missing.Add(id); continue; }
                    fighters.Add(f);
                }

                // Re-imports and scene changes can unload the roster, so reload it before every use
                // and save it before switching scenes.
                roster = LoadRoster();
                roster.fighters = fighters;
                if (roster.stages.Count > 0) roster.stages[0].sceneName = ArenaScene;
                EditorUtility.SetDirty(roster);
                AssetDatabase.SaveAssets();

                BuildArena();

                roster = LoadRoster();
                if (roster.fighters.Count != fighters.Count)   // safety net: write again if it was lost
                {
                    roster.fighters = fighters;
                    if (roster.stages.Count > 0) roster.stages[0].sceneName = ArenaScene;
                    EditorUtility.SetDirty(roster);
                    AssetDatabase.SaveAssets();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.DisplayDialog("Meme Fighter",
                $"Roster updated: {fighters.Count} fighters.\n" +
                (missing.Count > 0 ? "Missing files for: " + string.Join(", ", missing) + "\n" : "") +
                "\nOpen MainMenu, press Play, pick fighters, pick TRIPLE PLATFORM, Start Fight!",
                "Let's go");
        }

        static MenuRoster LoadRoster() => AssetDatabase.LoadAssetAtPath<MenuRoster>(RosterPath);

        static MenuRoster.Fighter SetUpCharacter(string id, string display)
        {
            string dir = $"{CharDir}/{id}";
            string fbx = $"{dir}/{id}.fbx";
            string albedo = $"{dir}/{id}_Albedo.png";
            string portraitPath = $"{dir}/{id}_Portrait.png";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(fbx) == null) return null;

            // model import: static mesh (no rig yet), we assign our own material
            var mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
            mi.animationType = ModelImporterAnimationType.None;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importCameras = false;
            mi.importLights = false;
            mi.SaveAndReimport();

            // texture + material
            var ti = (TextureImporter)AssetImporter.GetAtPath(albedo);
            if (ti != null)
            {
                ti.maxTextureSize = 2048;
                ti.sRGBTexture = true;
                ti.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(albedo);
            var mat = Mat($"{dir}/M_{id}.mat", tex, Color.white, 0.18f);

            // portrait
            Sprite portrait = null;
            var pi = (TextureImporter)AssetImporter.GetAtPath(portraitPath);
            if (pi != null)
            {
                pi.textureType = TextureImporterType.Sprite;
                pi.spriteImportMode = SpriteImportMode.Single;
                pi.alphaIsTransparency = true;
                pi.mipmapEnabled = false;
                pi.SaveAndReimport();
                portrait = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
            }

            var prefab = BuildPrefab(id, fbx, mat, $"{dir}/Fighter_{id}.prefab");
            return new MenuRoster.Fighter { displayName = display, portrait = portrait, prefab = prefab, locked = false };
        }

        static Material Mat(string path, Texture2D tex, Color color, float smoothness)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            if (isNew) m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (tex) m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            if (isNew) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
            return m;
        }

        static GameObject BuildPrefab(string id, string fbxPath, Material mat, string prefabPath)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var root = new GameObject("Fighter_" + id);

            var pivot = new GameObject("Model").transform;
            pivot.SetParent(root.transform, false);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            inst.transform.SetParent(pivot, false);

            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool first = true;
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
            }

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 1f;
            var cap = root.AddComponent<CapsuleCollider>();
            float h = Mathf.Max(0.8f, bounds.size.y * 0.95f);
            cap.height = h;
            cap.radius = Mathf.Clamp(bounds.size.z * 0.42f, 0.28f, 0.45f);
            cap.center = new Vector3(0f, h * 0.5f, 0f);
            cap.sharedMaterial = NoFriction();

            var motor = root.AddComponent<FighterMotor>();
            motor.model = pivot;

            var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        static PhysicsMaterial NoFriction()
        {
            const string path = "Assets/MemeFighter/Data/PM_FighterNoFriction.asset";
            var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (pm != null) return pm;
            pm = new PhysicsMaterial("FighterNoFriction")
            {
                dynamicFriction = 0f, staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum, bounciness = 0f
            };
            AssetDatabase.CreateAsset(pm, path);
            return pm;
        }

        // ------------------------------------------------------------------ arena

        static void BuildArena()
        {
            if (!AssetDatabase.IsValidFolder(ArenaDir)) AssetDatabase.CreateFolder("Assets/MemeFighter", "Arenas");
            var mainMat = Mat(ArenaDir + "/M_Arena_Main.mat", null, Hex("#7A5CFF"), 0.2f);
            var platMat = Mat(ArenaDir + "/M_Arena_Platform.mat", null, Hex("#F7F2E8"), 0.2f);
            var backMat = Mat(ArenaDir + "/M_Arena_Backdrop.mat", null, Hex("#3B6FD0"), 0f);
            var trimMat = Mat(ArenaDir + "/M_Arena_Trim.mat", null, Hex("#FFD23F"), 0.3f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.68f);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("#2E6FD8");
            cam.fieldOfView = 40f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 3f, -14f);
            var fightCam = camGo.AddComponent<FightCamera>();

            var stage = new GameObject("Stage").transform;
            Block("MainStage", stage, new Vector3(0f, -0.6f, 0f), new Vector3(14f, 1.2f, 4f), mainMat);
            Block("MainStageTrim", stage, new Vector3(0f, -0.03f, -2.02f), new Vector3(14.05f, 0.12f, 0.08f), trimMat, false);
            Block("Underside", stage, new Vector3(0f, -1.8f, 0f), new Vector3(10f, 1.4f, 3.4f), mainMat, false);
            Platform("PlatformLeft", stage, new Vector3(-3.6f, 2.3f, 0f), platMat);
            Platform("PlatformRight", stage, new Vector3(3.6f, 2.3f, 0f), platMat);
            Platform("PlatformTop", stage, new Vector3(0f, 4.4f, 0f), platMat);
            Block("Backdrop", stage, new Vector3(0f, 4f, 14f), new Vector3(80f, 40f, 1f), backMat, false);

            var spawns = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                spawns[i] = new GameObject("Spawn" + (i + 1)).transform;
                spawns[i].position = new Vector3(i == 0 ? -3.5f : 3.5f, 0.05f, 0f);
            }

            var boot = new GameObject("FightBootstrap").AddComponent<FightBootstrap>();
            boot.roster = LoadRoster();   // load after NewScene so the reference is live
            boot.spawnPoints = spawns;
            boot.fightCamera = fightCam;

            EditorSceneManager.SaveScene(scene, ArenaPath);
            var list = EditorBuildSettings.scenes.ToList();
            if (!list.Any(s => s.path == ArenaPath)) list.Add(new EditorBuildSettingsScene(ArenaPath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static GameObject Block(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collide = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collide) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void Platform(string name, Transform parent, Vector3 pos, Material mat)
        {
            var go = Block(name, parent, pos, new Vector3(3.2f, 0.3f, 3f), mat);
            go.AddComponent<OneWayPlatform>();
        }

        static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta;
    }
}
