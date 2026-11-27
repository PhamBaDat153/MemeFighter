using UnityEngine;

namespace MemeFighter.UI
{
    public class MainMenuScreen : MenuScreen
    {
        public UnityEngine.UI.Button versusButton;
        public UnityEngine.UI.Button vsBotButton;
        public UnityEngine.UI.Button settingsButton;
        public UnityEngine.UI.Button quitButton;

        void Awake()
        {
            versusButton.onClick.AddListener(() =>
            {
                GameSession.Mode = GameMode.Versus;
                controller.Show(controller.characterScreen);
            });
            vsBotButton.onClick.AddListener(() =>
            {
                GameSession.Mode = GameMode.VsBot;
                controller.Show(controller.characterScreen);
            });
            settingsButton.onClick.AddListener(() => controller.Show(controller.settingsScreen));
            quitButton.onClick.AddListener(() => controller.Show(controller.quitScreen));
        }

        public override void OnBack()
        {
            controller.Show(controller.quitScreen);
        }
    }
}
