using TMPro;
using UnityEngine;

namespace MemeFighter.UI
{
    /// <summary>One tile in the character select grid.</summary>
    public class RosterSlotView : MonoBehaviour
    {
        public UnityEngine.UI.Button button;
        public UnityEngine.UI.Image outline;
        public UnityEngine.UI.Image fill;
        public UnityEngine.UI.Image portrait;
        public UnityEngine.UI.Image[] silhouetteParts;
        public TMP_Text nameText;
        public GameObject p1Badge;
        public GameObject p2Badge;
        public TMP_Text p2BadgeText;

        public void Set(string displayName, Sprite portraitSprite, bool locked, bool isRandom,
                        bool p1Here, bool p2Here, string p2Tag)
        {
            outline.color = p1Here ? UiColors.P1 : p2Here ? UiColors.P2 : UiColors.Outline;
            fill.color = locked ? UiColors.Locked : isRandom ? UiColors.RandomFill : UiColors.Panel;

            bool hasPortrait = portraitSprite != null && !locked;
            portrait.sprite = portraitSprite;
            portrait.enabled = hasPortrait;
            foreach (var part in silhouetteParts)
            {
                part.enabled = !hasPortrait;
                part.color = locked ? UiColors.Dots : UiColors.Silhouette;
            }

            nameText.text = displayName;
            nameText.color = locked ? UiColors.LockedText : UiColors.White;
            p1Badge.SetActive(p1Here);
            p2Badge.SetActive(p2Here);
            p2BadgeText.text = p2Tag;
        }
    }
}
