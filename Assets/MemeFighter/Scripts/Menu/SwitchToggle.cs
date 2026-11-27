using UnityEngine;

namespace MemeFighter.UI
{
    /// <summary>On/off switch look for a Toggle: yellow track and knob on the right when on.</summary>
    [RequireComponent(typeof(UnityEngine.UI.Toggle))]
    public class SwitchToggle : MonoBehaviour
    {
        public UnityEngine.UI.Toggle toggle;
        public UnityEngine.UI.Image track;
        public RectTransform knob;
        public UnityEngine.UI.Image knobImage;
        public float offX = 3f;
        public float travel = 30f;

        void Awake()
        {
            if (!toggle) toggle = GetComponent<UnityEngine.UI.Toggle>();
            toggle.onValueChanged.AddListener(_ => Refresh());
            Refresh();
        }

        public void Refresh()
        {
            bool on = toggle.isOn;
            track.color = on ? UiColors.Yellow : UiColors.PanelHi;
            knobImage.color = on ? UiColors.Outline : UiColors.Muted;
            knob.anchoredPosition = new Vector2(on ? offX + travel : offX, knob.anchoredPosition.y);
        }
    }
}
