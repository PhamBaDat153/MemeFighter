using TMPro;
using UnityEngine;

namespace MemeFighter.UI
{
    /// <summary>The big P1 / P2 panel on the character select screen.</summary>
    public class PlayerPanelView : MonoBehaviour
    {
        public TMP_Text headerText;
        public TMP_Text nameText;
        public TMP_Text hintText;
        public TMP_Text statusText;
        public UnityEngine.UI.Image statusFill;
        public UnityEngine.UI.Image portrait;
        public GameObject silhouette;
        public GameObject cpuRow;
        public TMP_Text toggleText;

        public void Show(string header, string fighterName, Sprite portraitSprite, string status, bool ready, string hint)
        {
            headerText.text = header;
            nameText.text = fighterName;
            hintText.text = hint;
            statusText.text = status;
            statusFill.color = ready ? UiColors.Green : UiColors.PanelHi;
            statusText.color = ready ? UiColors.Outline : UiColors.White;
            portrait.sprite = portraitSprite;
            portrait.enabled = portraitSprite != null;
            silhouette.SetActive(portraitSprite == null);
        }

        public void SetCpuRow(bool visible)
        {
            if (cpuRow) cpuRow.SetActive(visible);
        }
    }
}
