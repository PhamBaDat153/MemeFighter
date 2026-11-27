using UnityEngine;

namespace MemeFighter.UI
{
    /// <summary>"Leaving so soon?" popup. STAY is focused first so a stray Enter doesn't close the game.</summary>
    public class QuitDialogScreen : MenuScreen
    {
        public UnityEngine.UI.Button stayButton;
        public UnityEngine.UI.Button quitButton;

        void Awake()
        {
            stayButton.onClick.AddListener(OnBack);
            quitButton.onClick.AddListener(QuitGame);
        }

        public override void OnBack()
        {
            controller.CloseOverlay(this);
        }

        static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
