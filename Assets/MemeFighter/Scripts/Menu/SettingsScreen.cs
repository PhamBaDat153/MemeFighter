using TMPro;
using UnityEngine;

namespace MemeFighter.UI
{
    /// <summary>Audio, Display and Controls tabs. Save &amp; Back keeps changes, Esc throws them away.</summary>
    public class SettingsScreen : MenuScreen
    {
        public UnityEngine.UI.Button backButton;
        public UnityEngine.UI.Button resetButton;
        public UnityEngine.UI.Button saveButton;
        public UnityEngine.UI.Button[] tabButtons;
        public GameObject[] tabPanels;

        [Header("Audio")]
        public UnityEngine.UI.Slider masterSlider;
        public UnityEngine.UI.Slider musicSlider;
        public UnityEngine.UI.Slider sfxSlider;
        public UnityEngine.UI.Slider voiceSlider;
        public TMP_Text masterValue, musicValue, sfxValue, voiceValue;

        [Header("Display")]
        public UnityEngine.UI.Button resolutionPrev;
        public UnityEngine.UI.Button resolutionNext;
        public TMP_Text resolutionText;
        public UnityEngine.UI.Toggle fullscreenToggle;
        public UnityEngine.UI.Toggle vsyncToggle;
        public UnityEngine.UI.Toggle shakeToggle;

        MenuButtonVisual[] tabVisuals;

        void Awake()
        {
            tabVisuals = new MenuButtonVisual[tabButtons.Length];
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int index = i;
                tabVisuals[i] = tabButtons[i].GetComponent<MenuButtonVisual>();
                tabButtons[i].onClick.AddListener(() => ShowTab(index));
            }

            masterSlider.onValueChanged.AddListener(v => { GameSettings.Master = v; AudioListener.volume = v; UpdateLabels(); });
            musicSlider.onValueChanged.AddListener(v => { GameSettings.Music = v; UpdateLabels(); });
            sfxSlider.onValueChanged.AddListener(v => { GameSettings.Sfx = v; UpdateLabels(); });
            voiceSlider.onValueChanged.AddListener(v => { GameSettings.Voice = v; UpdateLabels(); });

            resolutionPrev.onClick.AddListener(() => StepResolution(-1));
            resolutionNext.onClick.AddListener(() => StepResolution(1));
            fullscreenToggle.onValueChanged.AddListener(v => GameSettings.Fullscreen = v);
            vsyncToggle.onValueChanged.AddListener(v => GameSettings.VSync = v);
            shakeToggle.onValueChanged.AddListener(v => GameSettings.ScreenShake = v);

            resetButton.onClick.AddListener(() => { GameSettings.ResetDefaults(); PushToUi(); AudioListener.volume = GameSettings.Master; });
            saveButton.onClick.AddListener(() =>
            {
                GameSettings.Save();
                GameSettings.Apply();
                controller.Show(controller.mainScreen);
            });
            backButton.onClick.AddListener(OnBack);
        }

        public override void OnShow()
        {
            GameSettings.Load();
            PushToUi();
            ShowTab(0);
        }

        public override void OnBack()
        {
            GameSettings.Load();   // discard unsaved changes
            GameSettings.Apply();
            controller.Show(controller.mainScreen);
        }

        void ShowTab(int index)
        {
            for (int i = 0; i < tabPanels.Length; i++)
                tabPanels[i].SetActive(i == index);
            for (int i = 0; i < tabVisuals.Length; i++)
                if (tabVisuals[i]) tabVisuals[i].On = i == index;
        }

        void PushToUi()
        {
            masterSlider.SetValueWithoutNotify(GameSettings.Master);
            musicSlider.SetValueWithoutNotify(GameSettings.Music);
            sfxSlider.SetValueWithoutNotify(GameSettings.Sfx);
            voiceSlider.SetValueWithoutNotify(GameSettings.Voice);
            SetToggle(fullscreenToggle, GameSettings.Fullscreen);
            SetToggle(vsyncToggle, GameSettings.VSync);
            SetToggle(shakeToggle, GameSettings.ScreenShake);
            UpdateLabels();
        }

        static void SetToggle(UnityEngine.UI.Toggle t, bool value)
        {
            t.SetIsOnWithoutNotify(value);
            var sw = t.GetComponent<SwitchToggle>();
            if (sw) sw.Refresh();
        }

        void StepResolution(int dir)
        {
            var res = GameSettings.Resolutions;
            if (res.Length == 0) return;
            int i = GameSettings.CurrentResolutionIndex();
            GameSettings.ResolutionIndex = (i + dir + res.Length) % res.Length;
            UpdateLabels();
        }

        void UpdateLabels()
        {
            masterValue.text = Percent(GameSettings.Master);
            musicValue.text = Percent(GameSettings.Music);
            sfxValue.text = Percent(GameSettings.Sfx);
            voiceValue.text = Percent(GameSettings.Voice);

            var res = GameSettings.Resolutions;
            int r = GameSettings.CurrentResolutionIndex();
            resolutionText.text = r >= 0 && r < res.Length ? $"{res[r].width} × {res[r].height}" : $"{Screen.width} × {Screen.height}";
        }

        static string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";
    }
}
