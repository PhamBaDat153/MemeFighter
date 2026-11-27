using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MemeFighter.UI
{
    /// <summary>
    /// Colors a menu button for three states: off, focused (keyboard/mouse) and on
    /// (selected option, e.g. the active tab). Hovering with the mouse focuses it.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Selectable))]
    public class MenuButtonVisual : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        public UnityEngine.UI.Image fill;
        public TMP_Text label;
        public TMP_Text subLabel;
        public Color offFill, focusFill, onFill;
        public Color offText = Color.white, focusText = Color.white, onText = Color.white;
        [Tooltip("Pixels the button slides right while focused (0 = off).")]
        public float nudge;

        bool focused, on, hasBase;
        Vector2 basePos;
        RectTransform rt;
        UnityEngine.UI.Selectable selectable;

        public bool On
        {
            get => on;
            set { on = value; Refresh(); }
        }

        void Awake()
        {
            rt = (RectTransform)transform;
            selectable = GetComponent<UnityEngine.UI.Selectable>();
            basePos = rt.anchoredPosition;
            hasBase = true;
            Refresh();
        }

        void OnDisable()
        {
            focused = false;
            Refresh();
        }

        public void OnSelect(BaseEventData e) { focused = true; Refresh(); }
        public void OnDeselect(BaseEventData e) { focused = false; Refresh(); }

        public void OnPointerEnter(PointerEventData e)
        {
            if (selectable != null && selectable.IsInteractable() && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void Refresh()
        {
            Color f = on ? onFill : focused ? focusFill : offFill;
            Color t = on ? onText : focused ? focusText : offText;
            if (fill) fill.color = f;
            if (label) label.color = t;
            if (subLabel) subLabel.color = t;
            if (hasBase && nudge != 0f)
                rt.anchoredPosition = basePos + (focused ? new Vector2(nudge, 0f) : Vector2.zero);
        }
    }
}
