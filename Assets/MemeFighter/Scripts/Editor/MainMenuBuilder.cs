using System.Collections.Generic;
using System.IO;
using System.Linq;
using MemeFighter.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Navigation = UnityEngine.UI.Navigation;
using Selectable = UnityEngine.UI.Selectable;
using Slider = UnityEngine.UI.Slider;
using Toggle = UnityEngine.UI.Toggle;

namespace MemeFighter.EditorTools
{
    /// <summary>
    /// Builds Assets/MemeFighter/Scenes/MainMenu.unity from code, matching the
    /// "Meme Fighter – Menu Screens" design. Run: menu bar > Meme Fighter > Build Main Menu Scene.
    /// Safe to run again: it regenerates the scene (your MenuRoster asset is kept).
    /// </summary>
    public static class MainMenuBuilder
    {
        const string Root = "Assets/MemeFighter";
        const string GenDir = Root + "/UI/Generated";
        const string FontDir = Root + "/Fonts";
        const string SceneDir = Root + "/Scenes";
        const string ScenePath = SceneDir + "/MainMenu.unity";
        const string DataDir = Root + "/Data";
        const string RosterPath = DataDir + "/MenuRoster.asset";
        const string ActionsPath = "Assets/InputSystem_Actions.inputactions";
        const int UiLayer = 5;

        static Sprite rounded, circle, dots;
        static TMP_FontAsset display, body;
        static bool usingFallbackFonts;

        struct Box { public RectTransform root; public Image outline; public Image fill; }

        struct Cols
        {
            public Color off, focus, on, offText, focusText, onText;
            public Cols(Color off, Color focus, Color on, Color offText, Color focusText, Color onText)
            { this.off = off; this.focus = focus; this.on = on; this.offText = offText; this.focusText = focusText; this.onText = onText; }
        }

        // Button color sets
        static Cols MainCols => new Cols(UiColors.Panel, UiColors.Yellow, UiColors.Yellow, UiColors.White, UiColors.Outline, UiColors.Outline);
        static Cols ListCols => new Cols(UiColors.Panel, UiColors.Focus, UiColors.Yellow, UiColors.White, UiColors.White, UiColors.Outline);
        static Cols SmallCols => new Cols(UiColors.PanelHi, UiColors.Focus, UiColors.Yellow, UiColors.White, UiColors.White, UiColors.Outline);
        static Cols PrimaryCols => new Cols(UiColors.Yellow, UiColors.White, UiColors.Yellow, UiColors.Outline, UiColors.Outline, UiColors.Outline);

        [MenuItem("Meme Fighter/Build Main Menu Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EnsureTmpEssentials()) return;

            EnsureFolder(GenDir);
            EnsureFolder(FontDir);
            EnsureFolder(SceneDir);
            EnsureFolder(DataDir);

            rounded = MakeSprite("RoundedRect", 64, RoundedAlpha(64, 16f), new Vector4(20, 20, 20, 20), false);
            circle = MakeSprite("Circle", 64, CircleAlpha(64), Vector4.zero, false);
            dots = MakeSprite("Dots", 22, DotAlpha(22, 2f), Vector4.zero, true);
            LoadFonts();
            var roster = LoadOrCreateRoster();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildScene(roster);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild();
            AssetDatabase.SaveAssets();

            string msg = "MainMenu scene built at " + ScenePath + ".\n\nPress Play to try it.";
            if (usingFallbackFonts)
                msg += "\n\nFonts: Bungee / Inter weren't found, so the default TMP font is used. " +
                       "Put Bungee-Regular.ttf and an Inter .ttf (e.g. Inter-SemiBold.ttf) in " + FontDir +
                       " and run this again.";
            EditorUtility.DisplayDialog("Meme Fighter", msg, "Nice");
        }

        // ------------------------------------------------------------------ setup

        static bool EnsureTmpEssentials()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return true;
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            EditorUtility.DisplayDialog("Meme Fighter",
                "TextMeshPro Essential Resources are being imported.\n\nWhen the import finishes, run Meme Fighter > Build Main Menu Scene again.",
                "OK");
            return false;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static System.Func<int, int, float> RoundedAlpha(int size, float r)
        {
            float half = size / 2f;
            return (x, y) =>
            {
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - r), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - r), 0f);
                return Mathf.Clamp01(0.5f - (Mathf.Sqrt(dx * dx + dy * dy) - r));
            };
        }

        static System.Func<int, int, float> CircleAlpha(int size)
        {
            float half = size / 2f;
            return (x, y) => Mathf.Clamp01(0.5f - (Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) - (half - 0.5f)));
        }

        static System.Func<int, int, float> DotAlpha(int size, float r)
        {
            float half = size / 2f;
            return (x, y) => Mathf.Clamp01(0.5f - (Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) - r));
        }

        static Sprite MakeSprite(string name, int size, System.Func<int, int, float> alpha, Vector4 border, bool repeat)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha(x, y) * 255f));
            tex.SetPixels32(px);
            tex.Apply();
            string path = GenDir + "/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.spritePixelsPerUnit = 100;
            imp.spriteBorder = border;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void LoadFonts()
        {
            var fallback = TMP_Settings.defaultFontAsset;
            display = FindFont("Bungee", new[] { "Regular" });
            body = FindFont("Inter", new[] { "SemiBold", "Bold", "Medium", "Regular" });
            usingFallbackFonts = display == null || body == null;
            if (display == null) display = fallback;
            if (body == null) body = fallback;
        }

        static TMP_FontAsset FindFont(string family, string[] preferredStyles)
        {
            string full = Path.GetFullPath(FontDir);
            if (!Directory.Exists(full)) return null;
            var files = Directory.GetFiles(full, "*.*", SearchOption.AllDirectories)
                .Where(f => (f.EndsWith(".ttf", System.StringComparison.OrdinalIgnoreCase) ||
                             f.EndsWith(".otf", System.StringComparison.OrdinalIgnoreCase)) &&
                            Path.GetFileName(f).StartsWith(family, System.StringComparison.OrdinalIgnoreCase))
                .Select(f => FontDir + "/" + Path.GetRelativePath(full, f).Replace('\\', '/'))
                .ToList();
            if (files.Count == 0) return null;

            string pick = files[0];
            foreach (var style in preferredStyles)
            {
                var match = files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                    .EndsWith("-" + style, System.StringComparison.OrdinalIgnoreCase));
                if (match != null) { pick = match; break; }
            }
            return GetFontAsset(pick);
        }

        static TMP_FontAsset GetFontAsset(string fontPath)
        {
            string assetPath = Path.ChangeExtension(fontPath, null) + " SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null) return null;
            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fa == null) return null;
            fa.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(fa, assetPath);
            if (fa.atlasTextures != null)
                foreach (var t in fa.atlasTextures)
                    if (t != null) { t.name = fa.name + " Atlas"; AssetDatabase.AddObjectToAsset(t, fa); }
            if (fa.material != null) { fa.material.name = fa.name + " Material"; AssetDatabase.AddObjectToAsset(fa.material, fa); }
            AssetDatabase.SaveAssets();
            return fa;
        }

        static MenuRoster LoadOrCreateRoster()
        {
            var r = AssetDatabase.LoadAssetAtPath<MenuRoster>(RosterPath);
            if (r != null) return r;

            r = ScriptableObject.CreateInstance<MenuRoster>();
            for (int i = 1; i <= 8; i++)
                r.fighters.Add(new MenuRoster.Fighter { displayName = $"FIGHTER {i:00}" });

            r.stages.Add(new MenuRoster.Stage
            {
                displayName = "TRIPLE PLATFORM", description = "Main stage + 3 pass-through platforms",
                previewColor = UiColors.Hex("#2E6FD8"), platformColor = UiColors.OffWhite,
                previewPlatforms = new[] { new Rect(150, 330, 470, 80), new Rect(180, 230, 150, 18), new Rect(440, 230, 150, 18), new Rect(310, 140, 150, 18) }
            });
            r.stages.Add(new MenuRoster.Stage
            {
                displayName = "FLAT ARENA", description = "One big floor, no platforms. Pure skill.",
                previewColor = UiColors.Hex("#D9534F"), platformColor = UiColors.Yellow,
                previewPlatforms = new[] { new Rect(90, 320, 590, 90) }
            });
            r.stages.Add(new MenuRoster.Stage
            {
                displayName = "SKY ISLANDS", description = "Two floating islands with a gap in the middle",
                previewColor = UiColors.Hex("#1F9E89"), platformColor = UiColors.Green,
                previewPlatforms = new[] { new Rect(80, 300, 260, 70), new Rect(430, 300, 260, 70), new Rect(300, 170, 170, 18) }
            });
            r.stages.Add(new MenuRoster.Stage
            {
                displayName = "RANDOM", description = "Let the meme gods decide", isRandom = true,
                previewColor = UiColors.Silhouette, platformColor = UiColors.PanelHi,
                previewPlatforms = new[] { new Rect(260, 300, 250, 70) }
            });
            AssetDatabase.CreateAsset(r, RosterPath);
            AssetDatabase.SaveAssets();
            return r;
        }

        static void AddSceneToBuild()
        {
            var list = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static void AssignUiActions(InputSystemUIInputModule module)
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (asset == null || asset.FindActionMap("UI") == null) return; // keep the module's defaults
            var refs = AssetDatabase.LoadAllAssetsAtPath(ActionsPath).OfType<InputActionReference>().ToArray();
            InputActionReference R(string action) => refs.FirstOrDefault(r =>
                r.action != null && r.action.actionMap != null && r.action.actionMap.name == "UI" && r.action.name == action);

            module.actionsAsset = asset;
            module.move = R("Navigate") ?? module.move;
            module.submit = R("Submit") ?? module.submit;
            module.cancel = R("Cancel") ?? module.cancel;
            module.point = R("Point") ?? module.point;
            module.leftClick = R("Click") ?? module.leftClick;
            module.rightClick = R("RightClick") ?? module.rightClick;
            module.middleClick = R("MiddleClick") ?? module.middleClick;
            module.scrollWheel = R("ScrollWheel") ?? module.scrollWheel;
            module.trackedDevicePosition = R("TrackedDevicePosition") ?? module.trackedDevicePosition;
            module.trackedDeviceOrientation = R("TrackedDeviceOrientation") ?? module.trackedDeviceOrientation;
        }

        // ------------------------------------------------------------------ scene

        static void BuildScene(MenuRoster roster)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiColors.Night;
            camGo.transform.position = new Vector3(0f, 1f, -10f);
            camGo.AddComponent<AudioListener>();

            var esGo = new GameObject("EventSystem", typeof(EventSystem));
            AssignUiActions(esGo.AddComponent<InputSystemUIInputModule>());

            var canvasGo = new GameObject("MenuCanvas", typeof(RectTransform)) { layer = UiLayer };
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var canvasRt = (RectTransform)canvasGo.transform;

            Img(Stretch("Background", canvasRt), UiColors.Night);
            var dotsImg = Img(Stretch("Dots", canvasRt), UiColors.Dots, dots);
            dotsImg.type = Image.Type.Tiled;

            var frameGo = new GameObject("Frame1280x720", typeof(RectTransform)) { layer = UiLayer };
            var frame = (RectTransform)frameGo.transform;
            frame.SetParent(canvasRt, false);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f);
            frame.sizeDelta = new Vector2(1280, 720);

            var controller = canvasGo.AddComponent<MenuController>();

            // Footer bar sits behind the screens; screens put their own buttons on top of it.
            Img(Rect("FooterBar", frame, 0, 656, 1280, 64), UiColors.Outline);
            var footer = Txt("FooterHint", frame, 40, 656, 920, 64, "", body, 14, UiColors.Muted, TextAlignmentOptions.MidlineLeft, true);
            controller.footerText = footer;

            controller.mainScreen = BuildMain(frame);
            controller.characterScreen = BuildCharacterSelect(frame, roster);
            controller.mapScreen = BuildMapSelect(frame, roster);
            controller.settingsScreen = BuildSettings(frame);
            controller.quitScreen = BuildQuit(frame);
        }

        // ------------------------------------------------------------------ screens

        static MainMenuScreen BuildMain(RectTransform frame)
        {
            var root = Rect("MainScreen", frame, 0, 0, 1280, 720);
            var screen = root.gameObject.AddComponent<MainMenuScreen>();
            screen.footerHint = Hint("W / S") + " Move     " + Hint("Enter") + " Select     " + Hint("Esc") + " Quit";

            var logo = Rect("Logo", root, 72, 56, 560, 250);
            logo.localEulerAngles = new Vector3(0, 0, 4f);
            ShadowTxt("Meme", logo, 0, 0, 560, 92, "MEME", display, 96, UiColors.Yellow, 6);
            ShadowTxt("Fighter", logo, 0, 86, 560, 92, "FIGHTER", display, 96, UiColors.White, 6);
            var tag = Panel("Tagline", logo, 0, 196, 330, 38, UiColors.P1, null, 3, null, null, 2f);
            TxtFill("Text", tag.fill.transform, "MUCH FIGHT. VERY CHAOS.", body, 15, UiColors.Outline, TextAlignmentOptions.Center, true);

            screen.versusButton = MainButton(root, "VersusButton", 330, 64, "VERSUS", 26, "2 PLAYERS");
            screen.vsBotButton = MainButton(root, "VsBotButton", 408, 56, "VS BOT", 22, "1P VS CPU");
            screen.settingsButton = MainButton(root, "SettingsButton", 478, 56, "SETTINGS", 22, null);
            screen.quitButton = MainButton(root, "QuitButton", 548, 56, "QUIT", 22, null);
            screen.firstSelected = screen.versusButton;

            ShowcaseCard(root, "P1Card", 670, 126, 3f, UiColors.P1, "[P1 fighter render]");
            ShowcaseCard(root, "P2Card", 940, 96, -4f, UiColors.P2, "[P2 fighter render]");
            var vs = Rect("VS", root, 860, 250, 200, 90);
            vs.localEulerAngles = new Vector3(0, 0, 8f);
            ShadowTxt("Text", vs, 0, 0, 200, 90, "VS", display, 64, UiColors.Yellow, 5, TextAlignmentOptions.Center);
            return screen;
        }

        static Button MainButton(Transform parent, string name, float y, float h, string label, float size, string sub)
        {
            var b = Btn(name, parent, 72, y, 400, h, label, display, size, MainCols, TextAlignmentOptions.MidlineLeft, 28f);
            var v = b.GetComponent<MenuButtonVisual>();
            v.nudge = 6f;
            if (sub != null)
            {
                var s = TxtFill("Sub", v.fill.transform, sub, body, 14, UiColors.White, TextAlignmentOptions.MidlineRight, true, 0f, 24f);
                v.subLabel = s;
            }
            return b;
        }

        static void ShowcaseCard(Transform parent, string name, float x, float y, float angle, Color accent, string note)
        {
            var card = Panel(name, parent, x, y, 270, 420, UiColors.Panel, null, 4, new Vector2(10, -10), accent);
            card.root.localEulerAngles = new Vector3(0, 0, angle);
            Silhouette(card.fill.transform, 131, 360, 1.3f);
            var portrait = Img(Stretch("Portrait", card.fill.rectTransform, 8), Color.white);
            portrait.preserveAspect = true;
            portrait.enabled = false; // drop a render sprite in here later
            Txt("Note", card.fill.transform, 0, 370, 262, 30, note, body, 14, UiColors.Muted, TextAlignmentOptions.Center, true);
        }

        static CharacterSelectScreen BuildCharacterSelect(RectTransform frame, MenuRoster roster)
        {
            var root = Rect("CharacterSelectScreen", frame, 0, 0, 1280, 720);
            var screen = root.gameObject.AddComponent<CharacterSelectScreen>();
            screen.roster = roster;
            screen.footerHint = "<color=#FF6B35>P1</color> WASD + J pick, K undo     <color=#3FA9FF>P2</color> Arrows + Num1 pick, Num2 undo     " +
                                Hint("Tab") + " CPU on/off";

            screen.backButton = Header(root, "CHOOSE YOUR FIGHTER");
            NavNone(screen.backButton);

            var pill = Rect("ModePill", root, 1010, 26, 230, 36);
            Img(pill, UiColors.Outline, rounded);
            screen.modeText = TxtFill("Text", pill, "MODE: VERSUS 2P", body, 14, UiColors.Muted, TextAlignmentOptions.Center, true);

            screen.p1Panel = PlayerPanel(root, "P1Panel", 40, UiColors.P1, false, out _, out _);
            screen.p2Panel = PlayerPanel(root, "P2Panel", 940, UiColors.P2, true, out var toggle, out var levels);
            screen.cpuToggleButton = toggle;
            screen.cpuLevelButtons = levels;

            const int count = 12;
            const float cellW = 129, cellH = 124, gap = 12;
            screen.slots = new RosterSlotView[count];
            for (int i = 0; i < count; i++)
            {
                int col = i % 4, row = i / 4;
                screen.slots[i] = Slot(root, "Slot" + i.ToString("00"), 364 + col * (cellW + gap), 88 + row * (cellH + gap), cellW, cellH);
            }

            var cont = Btn("ContinueButton", root, 980, 666, 260, 44, "PICK MAP", display, 18,
                new Cols(UiColors.Locked, UiColors.Locked, UiColors.Yellow, UiColors.LockedText, UiColors.LockedText, UiColors.Outline),
                TextAlignmentOptions.Center, 0f, 0f, UiColors.White, 3f);
            NavNone(cont);
            screen.continueButton = cont;
            screen.firstSelected = null; // players use their own keys here
            return screen;
        }

        static PlayerPanelView PlayerPanel(Transform parent, string name, float x, Color accent, bool isP2, out Button toggle, out Button[] levels)
        {
            var box = Panel(name, parent, x, 88, 300, 520, UiColors.Panel, null, 4, new Vector2(0, -8), accent);
            var view = box.root.gameObject.AddComponent<PlayerPanelView>();
            var f = box.fill.transform;

            var header = Rect("Header", f, 0, 0, 292, 40);
            Img(header, accent, rounded, false, 1.5f);
            view.headerText = Txt("Tag", header, 16, 0, 120, 40, isP2 ? "P2" : "P1", display, 22, UiColors.Outline, TextAlignmentOptions.MidlineLeft);
            toggle = null;
            if (isP2)
            {
                toggle = Btn("CpuToggle", header, 170, 6, 112, 28, "VS CPU", body, 12,
                    new Cols(UiColors.Outline, UiColors.Night, UiColors.Outline, UiColors.White, UiColors.Yellow, UiColors.White),
                    TextAlignmentOptions.Center, 0f, 0f, null, 0f, 2f);
                NavNone(toggle);
                view.toggleText = toggle.GetComponent<MenuButtonVisual>().label;
            }
            else
            {
                Txt("Device", header, 150, 0, 126, 40, "KEYBOARD", body, 13, UiColors.Outline, TextAlignmentOptions.MidlineRight, true);
            }

            var area = Rect("PortraitArea", f, 8, 48, 276, 330);
            Img(area, UiColors.PanelHi, rounded);
            view.silhouette = Silhouette(area, 138, 330, 1.6f).gameObject;
            view.portrait = Img(Stretch("Portrait", area, 6), Color.white);
            view.portrait.preserveAspect = true;
            view.portrait.enabled = false;

            view.nameText = Txt("Name", f, 16, 388, 260, 36, "FIGHTER", display, 26, UiColors.White, TextAlignmentOptions.MidlineLeft);

            levels = new Button[0];
            if (isP2)
            {
                var row = Rect("CpuLevelRow", f, 16, 428, 260, 30);
                Txt("Label", row, 0, 0, 60, 30, "LEVEL", body, 12, UiColors.Muted, TextAlignmentOptions.MidlineLeft, true);
                string[] names = { "EASY", "NORMAL", "HARD" };
                levels = new Button[3];
                for (int i = 0; i < 3; i++)
                {
                    levels[i] = Btn("Level" + names[i], row, 60 + i * 66, 1, 62, 28, names[i], body, 11, SmallCols,
                        TextAlignmentOptions.Center, 0f, 0f, null, 0f, 2.5f);
                    NavNone(levels[i]);
                }
                view.cpuRow = row.gameObject;
            }

            view.hintText = Txt("Hint", f, 16, 468, 150, 30, "", body, 13, UiColors.Muted, TextAlignmentOptions.MidlineLeft, true);
            var badge = Rect("Status", f, 168, 468, 108, 30);
            view.statusFill = Img(badge, UiColors.PanelHi, rounded, false, 2.5f);
            view.statusText = TxtFill("Text", badge, "CHOOSING...", body, 13, UiColors.White, TextAlignmentOptions.Center, true);
            return view;
        }

        static RosterSlotView Slot(Transform parent, string name, float x, float y, float w, float h)
        {
            var box = Panel(name, parent, x, y, w, h, UiColors.Panel, null, 4, new Vector2(0, -5));
            box.outline.raycastTarget = true;
            var view = box.root.gameObject.AddComponent<RosterSlotView>();
            var button = box.root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = box.outline;
            NavNone(button);
            view.button = button;
            view.outline = box.outline;
            view.fill = box.fill;

            float iw = w - 8, ih = h - 8;
            var sil = Silhouette(box.fill.transform, iw / 2f, ih - 24, 0.55f);
            view.silhouetteParts = sil.GetComponentsInChildren<Image>();
            view.portrait = Img(Rect("Portrait", box.fill.transform, 0, 0, iw, ih - 24), Color.white);
            view.portrait.preserveAspect = true;
            view.portrait.enabled = false;

            var bar = Rect("NameBar", box.fill.transform, 0, ih - 24, iw, 24);
            Img(bar, UiColors.Outline);
            view.nameText = TxtFill("Name", bar, "FIGHTER", body, 11, UiColors.White, TextAlignmentOptions.Center, true);

            var p1 = Rect("P1Badge", box.fill.transform, 6, 6, 32, 20);
            Img(p1, UiColors.P1, rounded, false, 3f);
            TxtFill("Text", p1, "P1", body, 11, UiColors.Outline, TextAlignmentOptions.Center, true);
            view.p1Badge = p1.gameObject;

            var p2 = Rect("P2Badge", box.fill.transform, iw - 44, 6, 38, 20);
            Img(p2, UiColors.P2, rounded, false, 3f);
            view.p2BadgeText = TxtFill("Text", p2, "P2", body, 11, UiColors.Outline, TextAlignmentOptions.Center, true);
            view.p2Badge = p2.gameObject;
            return view;
        }

        static MapSelectScreen BuildMapSelect(RectTransform frame, MenuRoster roster)
        {
            var root = Rect("MapSelectScreen", frame, 0, 0, 1280, 720);
            var screen = root.gameObject.AddComponent<MapSelectScreen>();
            screen.roster = roster;
            screen.footerHint = Hint("Arrows / WASD") + " Move     " + Hint("Enter") + " Select     " + Hint("Esc") + " Back";

            screen.backButton = Header(root, "PICK A STAGE");

            var p1 = Rect("P1Pill", root, 830, 26, 200, 36);
            Img(p1, UiColors.P1, rounded);
            screen.p1Pill = TxtFill("Text", p1, "P1 · FIGHTER 01", body, 14, UiColors.Outline, TextAlignmentOptions.Center, true);
            var p2 = Rect("P2Pill", root, 1040, 26, 200, 36);
            Img(p2, UiColors.P2, rounded);
            screen.p2Pill = TxtFill("Text", p2, "P2 · FIGHTER 04", body, 14, UiColors.Outline, TextAlignmentOptions.Center, true);

            // Preview
            var preview = Panel("Preview", root, 40, 88, 776, 520, UiColors.Hex("#2E6FD8"), null, 4, new Vector2(0, -8), UiColors.Yellow);
            screen.previewFill = preview.fill;
            var ground = Img(Rect("Ground", preview.fill.transform, 0, 422, 768, 90), new Color(UiColors.Outline.r, UiColors.Outline.g, UiColors.Outline.b, 0.35f), rounded);
            ground.name = "Ground";
            screen.platforms = new RectTransform[5];
            screen.platformFills = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var plat = Panel("Platform" + i, preview.fill.transform, 0, 0, 100, 20, UiColors.OffWhite, null, 4, null, null, 2f);
                screen.platforms[i] = plat.root;
                screen.platformFills[i] = plat.fill;
            }
            screen.previewImage = Img(Stretch("PreviewImage", preview.fill.rectTransform), Color.white);
            screen.previewImage.preserveAspect = false;
            screen.previewImage.enabled = false;

            var info = Panel("StageInfo", preview.fill.transform, 20, 18, 460, 96, UiColors.Outline, UiColors.Outline, 0, null, null, 1.3f);
            screen.stageNameText = Txt("Name", info.fill.transform, 16, 8, 430, 38, "STAGE", display, 28, UiColors.White, TextAlignmentOptions.MidlineLeft);
            screen.stageDescText = Txt("Desc", info.fill.transform, 16, 46, 430, 44, "", body, 14, UiColors.Muted, TextAlignmentOptions.TopLeft, true);
            screen.stageDescText.textWrappingMode = TextWrappingModes.Normal;

            // Stage list
            int n = Mathf.Clamp(roster.stages.Count, 1, 6);
            screen.stageButtons = new Button[n];
            for (int i = 0; i < n; i++)
            {
                var b = Btn("Stage" + i, root, 840, 88 + i * 66, 400, 56, roster.stages[Mathf.Min(i, roster.stages.Count - 1)].displayName,
                    display, 18, ListCols, TextAlignmentOptions.MidlineLeft, 56f, 5f);
                var v = b.GetComponent<MenuButtonVisual>();
                v.subLabel = TxtFill("Number", v.fill.transform, (i + 1).ToString("00"), body, 13, UiColors.White, TextAlignmentOptions.MidlineLeft, true, 18f, 0f);
                screen.stageButtons[i] = b;
            }

            // Rules
            float ry = 88 + n * 66 + 8;
            var rules = Panel("Rules", root, 840, ry, 400, 160, UiColors.Panel, null, 4, new Vector2(0, -6));
            var rf = rules.fill.transform;
            Txt("Title", rf, 16, 10, 300, 28, "MATCH RULES", display, 16, UiColors.Yellow, TextAlignmentOptions.MidlineLeft);
            Txt("StocksLabel", rf, 16, 50, 200, 40, "Stocks (lives)", body, 15, UiColors.White, TextAlignmentOptions.MidlineLeft, true);
            screen.lessStockButton = Btn("LessStocks", rf, 250, 52, 36, 36, "-", display, 18, SmallCols, TextAlignmentOptions.Center, 0f, 0f, null, 3f, 2f);
            screen.stocksText = Txt("Stocks", rf, 290, 50, 40, 40, "3", display, 22, UiColors.White, TextAlignmentOptions.Center);
            screen.moreStockButton = Btn("MoreStocks", rf, 334, 52, 36, 36, "+", display, 18, SmallCols, TextAlignmentOptions.Center, 0f, 0f, null, 3f, 2f);
            Txt("TimeLabel", rf, 16, 102, 170, 40, "Time limit", body, 15, UiColors.White, TextAlignmentOptions.MidlineLeft, true);
            string[] times = { "5:00", "8:00", "∞" };
            screen.timeButtons = new Button[3];
            for (int i = 0; i < 3; i++)
                screen.timeButtons[i] = Btn("Time" + i, rf, 196 + i * 62, 105, 56, 34, times[i], body, 14, SmallCols,
                    TextAlignmentOptions.Center, 0f, 0f, null, 3f, 2f);

            screen.startButton = Btn("StartFightButton", root, 1000, 662, 240, 52, "START FIGHT!", display, 22,
                new Cols(UiColors.P1, UiColors.Yellow, UiColors.P1, UiColors.Outline, UiColors.Outline, UiColors.Outline),
                TextAlignmentOptions.Center, 0f, 0f, UiColors.White, 3f);
            screen.firstSelected = screen.stageButtons[0];
            return screen;
        }

        static SettingsScreen BuildSettings(RectTransform frame)
        {
            var root = Rect("SettingsScreen", frame, 0, 0, 1280, 720);
            var screen = root.gameObject.AddComponent<SettingsScreen>();
            screen.footerHint = Hint("Arrows") + " Move     " + Hint("Left / Right") + " Adjust     " + Hint("Enter") + " Select     " + Hint("Esc") + " Back without saving";
            screen.backButton = Header(root, "SETTINGS");

            string[] tabs = { "AUDIO", "DISPLAY", "CONTROLS" };
            screen.tabButtons = new Button[tabs.Length];
            for (int i = 0; i < tabs.Length; i++)
                screen.tabButtons[i] = Btn("Tab" + tabs[i], root, 40, 88 + i * 72, 260, 60, tabs[i], display, 20, ListCols, TextAlignmentOptions.MidlineLeft, 20f);
            screen.firstSelected = screen.tabButtons[0];

            var content = Panel("Content", root, 324, 88, 916, 520, UiColors.Panel, null, 4, new Vector2(0, -6));
            var cf = content.fill.transform;

            // Audio
            var audio = Rect("AudioTab", cf, 0, 0, 908, 430);
            string[] labels = { "Master volume", "Music", "Sound effects", "Meme voice lines" };
            var sliders = new Slider[4];
            var values = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                float y = 30 + i * 68;
                Txt("Label" + i, audio, 36, y, 230, 40, labels[i], body, 17, UiColors.White, TextAlignmentOptions.MidlineLeft, true);
                sliders[i] = MakeSlider("Slider" + i, audio, 270, y + 6, 500, 28);
                values[i] = Txt("Value" + i, audio, 800, y, 90, 40, "100%", display, 18, UiColors.White, TextAlignmentOptions.MidlineLeft);
            }
            screen.masterSlider = sliders[0]; screen.musicSlider = sliders[1]; screen.sfxSlider = sliders[2]; screen.voiceSlider = sliders[3];
            screen.masterValue = values[0]; screen.musicValue = values[1]; screen.sfxValue = values[2]; screen.voiceValue = values[3];

            // Display
            var disp = Rect("DisplayTab", cf, 0, 0, 908, 430);
            Txt("ResolutionLabel", disp, 36, 30, 300, 44, "Resolution", body, 17, UiColors.White, TextAlignmentOptions.MidlineLeft, true);
            screen.resolutionPrev = Btn("ResPrev", disp, 560, 30, 44, 44, "<", display, 20, SmallCols, TextAlignmentOptions.Center, 0f, 0f, null, 3f, 2f);
            screen.resolutionText = Txt("ResText", disp, 612, 30, 180, 44, "1920 × 1080", body, 17, UiColors.White, TextAlignmentOptions.Center, true);
            screen.resolutionNext = Btn("ResNext", disp, 800, 30, 44, 44, ">", display, 20, SmallCols, TextAlignmentOptions.Center, 0f, 0f, null, 3f, 2f);
            string[] toggles = { "Fullscreen", "V-Sync", "Screen shake on big hits" };
            var made = new Toggle[3];
            for (int i = 0; i < 3; i++)
            {
                float y = 30 + (i + 1) * 68;
                Txt("ToggleLabel" + i, disp, 36, y, 400, 44, toggles[i], body, 17, UiColors.White, TextAlignmentOptions.MidlineLeft, true);
                made[i] = MakeSwitch("Switch" + i, disp, 780, y + 5);
            }
            screen.fullscreenToggle = made[0]; screen.vsyncToggle = made[1]; screen.shakeToggle = made[2];

            // Controls
            var ctrl = Rect("ControlsTab", cf, 0, 0, 908, 430);
            Txt("HAction", ctrl, 36, 20, 260, 30, "ACTION", body, 14, UiColors.Muted, TextAlignmentOptions.MidlineLeft, true);
            Txt("HP1", ctrl, 330, 20, 220, 30, "P1 · KEYBOARD", body, 14, UiColors.P1, TextAlignmentOptions.MidlineLeft, true);
            Txt("HP2", ctrl, 580, 20, 220, 30, "P2 · KEYBOARD", body, 14, UiColors.P2, TextAlignmentOptions.MidlineLeft, true);
            string[,] binds =
            {
                { "Move", "A / D", "← / →" },
                { "Jump", "W", "↑" },
                { "Fast fall / drop", "S", "↓" },
                { "Attack", "J", "Num 1" },
                { "Special", "K", "Num 2" },
                { "Shield / dodge", "L", "Num 3" },
                { "Pause", "Esc", "Num 0" },
            };
            for (int i = 0; i < binds.GetLength(0); i++)
            {
                float y = 58 + i * 46;
                Txt("Action" + i, ctrl, 36, y, 280, 36, binds[i, 0], body, 15, UiColors.White, TextAlignmentOptions.MidlineLeft, true);
                var c1 = Panel("P1Key" + i, ctrl, 330, y, 220, 36, UiColors.PanelHi, null, 2, null, null, 2f);
                TxtFill("Text", c1.fill.transform, binds[i, 1], body, 15, UiColors.White, TextAlignmentOptions.Center, true);
                var c2 = Panel("P2Key" + i, ctrl, 580, y, 220, 36, UiColors.PanelHi, null, 2, null, null, 2f);
                TxtFill("Text", c2.fill.transform, binds[i, 2], body, 15, UiColors.White, TextAlignmentOptions.Center, true);
            }
            screen.tabPanels = new[] { audio.gameObject, disp.gameObject, ctrl.gameObject };

            screen.resetButton = Btn("ResetButton", cf, 460, 440, 224, 52, "RESET TO DEFAULT", body, 15, ListCols,
                TextAlignmentOptions.Center, 0f, 0f, UiColors.Muted, 3f);
            screen.saveButton = Btn("SaveButton", cf, 700, 440, 172, 52, "SAVE & BACK", display, 18, PrimaryCols,
                TextAlignmentOptions.Center, 0f, 0f, null, 4f);
            return screen;
        }

        static QuitDialogScreen BuildQuit(RectTransform frame)
        {
            var root = Rect("QuitDialog", frame, 0, 0, 1280, 720);
            var screen = root.gameObject.AddComponent<QuitDialogScreen>();
            screen.isOverlay = true;
            screen.footerHint = Hint("A / D") + " Choose     " + Hint("Enter") + " Confirm     " + Hint("Esc") + " Stay";

            var dim = Stretch("Dimmer", root, -2000f);
            Img(dim, new Color(UiColors.Outline.r, UiColors.Outline.g, UiColors.Outline.b, 0.7f), null, true);

            var dialog = Panel("Dialog", root, 340, 170, 600, 360, UiColors.Panel, null, 4, new Vector2(12, -12), UiColors.P1);
            var df = dialog.fill.transform;
            var face = Rect("Face", df, 248, 22, 96, 96);
            Img(face, UiColors.PanelHi, circle);
            var faceText = TxtFill("Text", face, ":(", display, 40, UiColors.Yellow, TextAlignmentOptions.Center);
            faceText.rectTransform.localEulerAngles = new Vector3(0, 0, -90f);
            ShadowTxt("Title", df, 0, 134, 592, 52, "LEAVING SO SOON?", display, 40, UiColors.Yellow, 4, TextAlignmentOptions.Center);
            Txt("Body", df, 0, 194, 592, 30, "Close Meme Fighter and return to the desktop?", body, 17, UiColors.Muted, TextAlignmentOptions.Center, true);

            var stay = Btn("StayButton", df, 66, 248, 220, 60, "STAY", display, 22, MainCols, TextAlignmentOptions.Center);
            var quit = Btn("RageQuitButton", df, 306, 248, 220, 60, "RAGE QUIT", display, 22,
                new Cols(UiColors.Night, UiColors.P1, UiColors.P1, UiColors.White, UiColors.Outline, UiColors.Outline),
                TextAlignmentOptions.Center, 0f, 6f, UiColors.P1);
            var ns = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = quit };
            stay.navigation = ns;
            var nq = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = stay };
            quit.navigation = nq;

            screen.stayButton = stay;
            screen.quitButton = quit;
            screen.firstSelected = stay;
            return screen;
        }

        // ------------------------------------------------------------------ widgets

        static Button Header(Transform parent, string title)
        {
            var back = Btn("BackButton", parent, 40, 20, 48, 48, "<", display, 22, ListCols, TextAlignmentOptions.Center, 0f, 0f, null, 3f);
            ShadowTxt("Title", parent, 108, 18, 820, 52, title, display, 40, UiColors.Yellow, 4);
            return back;
        }

        static Slider MakeSlider(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = UnityEngine.UI.DefaultControls.CreateSlider(new UnityEngine.UI.DefaultControls.Resources());
            go.name = name;
            SetLayer(go);
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Place(rt, x, y, w, h);

            var s = go.GetComponent<Slider>();
            s.minValue = 0f;
            s.maxValue = 1f;
            s.value = 0.8f;
            var cb = s.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = UiColors.Yellow;
            cb.selectedColor = UiColors.Yellow;
            cb.pressedColor = UiColors.Yellow;
            s.colors = cb;

            var bg = go.transform.Find("Background").GetComponent<Image>();
            bg.sprite = rounded; bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 2.5f; bg.color = UiColors.Outline;
            var fill = go.transform.Find("Fill Area/Fill").GetComponent<Image>();
            fill.sprite = rounded; fill.type = Image.Type.Sliced; fill.pixelsPerUnitMultiplier = 2.5f; fill.color = UiColors.Yellow;
            var handle = go.transform.Find("Handle Slide Area/Handle").GetComponent<Image>();
            handle.sprite = circle; handle.type = Image.Type.Simple; handle.color = Color.white;
            ((RectTransform)handle.transform).sizeDelta = new Vector2(28f, 0f);
            return s;
        }

        static Toggle MakeSwitch(string name, Transform parent, float x, float y)
        {
            var rt = Rect(name, parent, x, y, 64, 34);
            var outline = Img(rt, Color.white, rounded, true);
            var toggle = rt.gameObject.AddComponent<Toggle>();
            toggle.transition = Selectable.Transition.ColorTint;
            toggle.targetGraphic = outline;
            var cb = toggle.colors;
            cb.normalColor = UiColors.Outline;
            cb.highlightedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.pressedColor = UiColors.Yellow;
            cb.fadeDuration = 0.05f;
            toggle.colors = cb;
            toggle.graphic = null;

            var track = Img(Stretch("Track", rt, 3f), UiColors.PanelHi, rounded);
            var knobRt = new GameObject("Knob", typeof(RectTransform)) { layer = UiLayer }.GetComponent<RectTransform>();
            knobRt.SetParent(track.transform, false);
            knobRt.anchorMin = knobRt.anchorMax = knobRt.pivot = new Vector2(0f, 0.5f);
            knobRt.sizeDelta = new Vector2(22, 22);
            knobRt.anchoredPosition = new Vector2(3, 0);
            var knob = Img(knobRt, UiColors.Muted, circle);

            var sw = rt.gameObject.AddComponent<SwitchToggle>();
            sw.toggle = toggle;
            sw.track = track;
            sw.knob = knobRt;
            sw.knobImage = knob;
            sw.offX = 3f;
            sw.travel = 30f;
            return toggle;
        }

        static RectTransform Silhouette(Transform parent, float centerX, float bottomY, float scale)
        {
            var group = Rect("Silhouette", parent, 0, 0, 0, 0);
            float bodyW = 110 * scale, bodyH = 135 * scale, head = 80 * scale;
            Img(Rect("Body", group, centerX - bodyW / 2f, bottomY - bodyH, bodyW, bodyH), UiColors.Silhouette, rounded, false, Mathf.Max(0.5f, 1.4f / scale));
            Img(Rect("Head", group, centerX - head / 2f, bottomY - bodyH - head - 6 * scale, head, head), UiColors.Silhouette, circle);
            return group;
        }

        static Button Btn(string name, Transform parent, float x, float y, float w, float h, string label, TMP_FontAsset font, float size,
                          Cols c, TextAlignmentOptions align = TextAlignmentOptions.Center, float padLeft = 0f, float shadowY = 6f,
                          Color? outline = null, float border = 4f, float ppu = 1f)
        {
            var box = Panel(name, parent, x, y, w, h, c.off, outline, border,
                            shadowY > 0f ? new Vector2(0f, -shadowY) : (Vector2?)null, null, ppu);
            box.outline.raycastTarget = true;
            var b = box.root.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = box.outline;

            bool isBody = font == body;
            var t = TxtFill("Label", box.fill.transform, label, font, size, c.offText, align, isBody, padLeft, 0f);

            var v = box.root.gameObject.AddComponent<MenuButtonVisual>();
            v.fill = box.fill;
            v.label = t;
            v.offFill = c.off; v.focusFill = c.focus; v.onFill = c.on;
            v.offText = c.offText; v.focusText = c.focusText; v.onText = c.onText;
            return b;
        }

        static Box Panel(string name, Transform parent, float x, float y, float w, float h, Color fill,
                         Color? outline = null, float border = 4f, Vector2? shadow = null, Color? shadowColor = null, float ppu = 1f)
        {
            var rt = Rect(name, parent, x, y, w, h);
            var o = Img(rt, outline ?? UiColors.Outline, rounded, false, ppu);
            if (shadow.HasValue)
            {
                var sh = rt.gameObject.AddComponent<UnityEngine.UI.Shadow>();
                sh.effectColor = shadowColor ?? UiColors.Outline;
                sh.effectDistance = shadow.Value;
                sh.useGraphicAlpha = true;
            }
            var f = Img(Stretch("Fill", rt, border), fill, rounded, false, ppu);
            return new Box { root = rt, outline = o, fill = f };
        }

        static void ShadowTxt(string name, Transform parent, float x, float y, float w, float h, string text, TMP_FontAsset font,
                              float size, Color color, float offset, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var g = Rect(name, parent, x, y, w, h);
            Txt("Shadow", g, offset, offset, w, h, text, font, size, UiColors.Outline, align);
            Txt("Text", g, 0, 0, w, h, text, font, size, color, align);
        }

        static TextMeshProUGUI Txt(string name, Transform parent, float x, float y, float w, float h, string text, TMP_FontAsset font,
                                   float size, Color color, TextAlignmentOptions align, bool bold = false)
        {
            var rt = Rect(name, parent, x, y, w, h);
            return SetupText(rt, text, font, size, color, align, bold);
        }

        static TextMeshProUGUI TxtFill(string name, Transform parent, string text, TMP_FontAsset font, float size, Color color,
                                       TextAlignmentOptions align, bool bold = false, float padLeft = 0f, float padRight = 0f)
        {
            var rt = Stretch(name, (RectTransform)parent);
            rt.offsetMin = new Vector2(padLeft, 0f);
            rt.offsetMax = new Vector2(-padRight, 0f);
            return SetupText(rt, text, font, size, color, align, bold);
        }

        static TextMeshProUGUI SetupText(RectTransform rt, string text, TMP_FontAsset font, float size, Color color,
                                         TextAlignmentOptions align, bool bold)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = true;
            t.raycastTarget = false;
            return t;
        }

        static Image Img(RectTransform rt, Color color, Sprite sprite = null, bool raycast = false, float ppu = 1f)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.pixelsPerUnitMultiplier = ppu;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Place(rt, x, y, w, h);
            return rt;
        }

        /// <summary>Top-left anchored placement, using the same x/y as the design (y grows downward).</summary>
        static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        static RectTransform Stretch(string name, RectTransform parent, float inset = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        static void NavNone(Selectable s)
        {
            if (s == null) return;
            var n = s.navigation;
            n.mode = Navigation.Mode.None;
            s.navigation = n;
        }

        static void SetLayer(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = UiLayer;
        }

        static string Hint(string key) => "<color=#FFD23F>" + key + "</color>";
    }
}
