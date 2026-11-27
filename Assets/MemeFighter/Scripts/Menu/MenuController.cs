using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MemeFighter.UI
{
    /// <summary>Switches between menu screens and handles the Back key.</summary>
    public class MenuController : MonoBehaviour
    {
        public MenuScreen mainScreen;
        public MenuScreen characterScreen;
        public MenuScreen mapScreen;
        public MenuScreen settingsScreen;
        public MenuScreen quitScreen;
        public TMP_Text footerText;

        MenuScreen current;
        MenuScreen underOverlay;
        MenuScreen[] all;

        void Awake()
        {
            GameSettings.Load();
            GameSettings.Apply();
            all = new[] { mainScreen, characterScreen, mapScreen, settingsScreen, quitScreen };
            foreach (var s in all)
            {
                if (s == null) continue;
                s.controller = this;
                s.gameObject.SetActive(false);
            }
        }

        void Start()
        {
            Show(mainScreen);
        }

        public void Show(MenuScreen screen)
        {
            if (screen == null) return;

            if (screen.isOverlay)
            {
                underOverlay = current;
            }
            else
            {
                underOverlay = null;
                foreach (var s in all)
                {
                    if (s == null || s == screen || !s.gameObject.activeSelf) continue;
                    s.OnHide();
                    s.gameObject.SetActive(false);
                }
            }

            screen.gameObject.SetActive(true);
            current = screen;
            screen.OnShow();
            if (footerText) footerText.text = screen.footerHint;
            Select(screen.firstSelected);
        }

        /// <summary>Closes an overlay and gives focus back to the screen under it.</summary>
        public void CloseOverlay(MenuScreen overlay)
        {
            overlay.OnHide();
            overlay.gameObject.SetActive(false);
            current = underOverlay != null ? underOverlay : mainScreen;
            underOverlay = null;
            if (footerText) footerText.text = current.footerHint;
            Select(current.firstSelected);
        }

        public void Select(UnityEngine.UI.Selectable selectable)
        {
            var es = EventSystem.current;
            if (es == null) return;
            es.SetSelectedGameObject(null);
            if (selectable != null) es.SetSelectedGameObject(selectable.gameObject);
        }

        void Update()
        {
            if (current == null) return;
            bool back = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            if (!back && Gamepad.current != null) back = Gamepad.current.buttonEast.wasPressedThisFrame;
            if (back) current.OnBack();
        }
    }
}
