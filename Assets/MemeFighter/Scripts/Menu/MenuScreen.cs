using UnityEngine;

namespace MemeFighter.UI
{
    /// <summary>Base class for one menu screen (main, character select, map select...).</summary>
    public abstract class MenuScreen : MonoBehaviour
    {
        [HideInInspector] public MenuController controller;
        [Tooltip("Button that gets keyboard focus when the screen opens. Can be empty.")]
        public UnityEngine.UI.Selectable firstSelected;
        [Tooltip("Text shown in the bottom bar (rich text allowed).")]
        [TextArea] public string footerHint;
        [Tooltip("Overlays (like the quit popup) open on top of the current screen.")]
        public bool isOverlay;

        public virtual void OnShow() { }
        public virtual void OnHide() { }
        /// <summary>Called for Esc / gamepad B.</summary>
        public abstract void OnBack();
    }
}
