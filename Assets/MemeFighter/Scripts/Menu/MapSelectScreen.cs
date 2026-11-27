using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemeFighter.UI
{
    /// <summary>Stage list, match rules (stocks, time) and the Start Fight button.</summary>
    public class MapSelectScreen : MenuScreen
    {
        public MenuRoster roster;
        public UnityEngine.UI.Button backButton;
        public UnityEngine.UI.Button startButton;
        public UnityEngine.UI.Button lessStockButton;
        public UnityEngine.UI.Button moreStockButton;
        public UnityEngine.UI.Button[] stageButtons;
        public UnityEngine.UI.Button[] timeButtons;
        public UnityEngine.UI.Image previewFill;
        public UnityEngine.UI.Image previewImage;
        public RectTransform[] platforms;
        public UnityEngine.UI.Image[] platformFills;
        public TMP_Text stageNameText;
        public TMP_Text stageDescText;
        public TMP_Text stocksText;
        public TMP_Text p1Pill;
        public TMP_Text p2Pill;

        static readonly int[] TimeOptions = { 300, 480, 0 };
        const int MinStocks = 1, MaxStocks = 9;

        int stage;
        MenuButtonVisual[] stageVisuals;
        MenuButtonVisual[] timeVisuals;

        int StageCount => roster != null ? Mathf.Min(roster.stages.Count, stageButtons.Length) : 0;

        void Awake()
        {
            stageVisuals = new MenuButtonVisual[stageButtons.Length];
            for (int i = 0; i < stageButtons.Length; i++)
            {
                int index = i;
                stageVisuals[i] = stageButtons[i].GetComponent<MenuButtonVisual>();
                stageButtons[i].onClick.AddListener(() => SelectStage(index));
            }

            timeVisuals = new MenuButtonVisual[timeButtons.Length];
            for (int i = 0; i < timeButtons.Length; i++)
            {
                int index = i;
                timeVisuals[i] = timeButtons[i].GetComponent<MenuButtonVisual>();
                timeButtons[i].onClick.AddListener(() => { GameSession.TimeLimitSeconds = TimeOptions[index]; Refresh(); });
            }

            lessStockButton.onClick.AddListener(() => { GameSession.Stocks = Mathf.Max(MinStocks, GameSession.Stocks - 1); Refresh(); });
            moreStockButton.onClick.AddListener(() => { GameSession.Stocks = Mathf.Min(MaxStocks, GameSession.Stocks + 1); Refresh(); });
            backButton.onClick.AddListener(OnBack);
            startButton.onClick.AddListener(StartFight);
        }

        public override void OnShow()
        {
            stage = Mathf.Clamp(GameSession.StageIndex, 0, Mathf.Max(0, StageCount - 1));
            for (int i = 0; i < stageButtons.Length; i++)
            {
                bool used = i < StageCount;
                stageButtons[i].gameObject.SetActive(used);
                if (used && stageVisuals[i] && stageVisuals[i].label)
                    stageVisuals[i].label.text = roster.stages[i].displayName;
            }
            if (StageCount > 0) firstSelected = stageButtons[stage];

            p1Pill.text = "P1 · " + FighterName(GameSession.P1Fighter);
            p2Pill.text = (GameSession.P2IsCpu ? "CPU · " : "P2 · ") + FighterName(GameSession.P2Fighter);
            Refresh();
        }

        public override void OnBack()
        {
            controller.Show(controller.characterScreen);
        }

        string FighterName(int i)
        {
            if (roster == null || i < 0 || i >= roster.fighters.Count) return "?";
            return roster.fighters[i].displayName;
        }

        void SelectStage(int index)
        {
            stage = index;
            GameSession.StageIndex = index;
            Refresh();
        }

        void Refresh()
        {
            if (StageCount == 0) return;
            var s = roster.stages[stage];

            stageNameText.text = s.displayName;
            stageDescText.text = s.description;
            stageDescText.color = UiColors.Muted;
            previewFill.color = s.previewColor;
            previewImage.sprite = s.preview;
            previewImage.enabled = s.preview != null;

            for (int i = 0; i < platforms.Length; i++)
            {
                bool show = s.preview == null && s.previewPlatforms != null && i < s.previewPlatforms.Length;
                platforms[i].gameObject.SetActive(show);
                if (!show) continue;
                Rect r = s.previewPlatforms[i];
                platforms[i].anchoredPosition = new Vector2(r.x, -r.y);
                platforms[i].sizeDelta = new Vector2(r.width, r.height);
                platformFills[i].color = s.platformColor;
            }

            for (int i = 0; i < StageCount; i++)
                if (stageVisuals[i]) stageVisuals[i].On = i == stage;

            stocksText.text = GameSession.Stocks.ToString();
            for (int i = 0; i < timeVisuals.Length; i++)
                if (timeVisuals[i]) timeVisuals[i].On = TimeOptions[i] == GameSession.TimeLimitSeconds;
        }

        void StartFight()
        {
            int index = stage;
            if (roster.stages[index].isRandom)
            {
                var real = new List<int>();
                for (int i = 0; i < roster.stages.Count; i++)
                    if (!roster.stages[i].isRandom) real.Add(i);
                if (real.Count > 0) index = real[Random.Range(0, real.Count)];
            }

            GameSession.StageIndex = index;
            string scene = roster.stages[index].sceneName;
            if (!string.IsNullOrEmpty(scene) && Application.CanStreamedLevelBeLoaded(scene))
            {
                SceneManager.LoadScene(scene);
                return;
            }

            string shown = string.IsNullOrEmpty(scene) ? "(no scene set)" : scene;
            stageDescText.text = "Stage scene " + shown + " isn't in the Build Profile scene list yet.";
            stageDescText.color = UiColors.P1;
            Debug.LogWarning($"[Meme Fighter] Can't start: stage '{roster.stages[index].displayName}' scene {shown} is not set up. " +
                             "Set Scene Name in Assets/MemeFighter/Data/MenuRoster.asset and add the scene to the build.");
        }
    }
}
