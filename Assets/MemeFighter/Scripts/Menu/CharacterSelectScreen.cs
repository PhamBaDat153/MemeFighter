using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MemeFighter.UI
{
    /// <summary>
    /// Character select for 2P versus and VS Bot.
    /// P1: WASD move, J pick, K undo. P2: arrows move, Num1 pick, Num2 undo.
    /// VS Bot: after P1 picks, P1's keys pick the CPU's fighter. Q / E change CPU level.
    /// Tab switches between P2 and CPU. Mouse clicks pick for whoever is choosing.
    /// </summary>
    public class CharacterSelectScreen : MenuScreen
    {
        public MenuRoster roster;
        public RosterSlotView[] slots;
        public PlayerPanelView p1Panel;
        public PlayerPanelView p2Panel;
        public TMP_Text modeText;
        public UnityEngine.UI.Button backButton;
        public UnityEngine.UI.Button continueButton;
        public UnityEngine.UI.Button cpuToggleButton;
        public UnityEngine.UI.Button[] cpuLevelButtons;

        const int Columns = 4;

        int p1Cursor, p2Cursor;
        int p1Pick = -1, p2Pick = -1;
        bool p1Ready, p2Ready;
        MenuButtonVisual continueVisual;
        MenuButtonVisual[] levelVisuals;

        int FighterCount => roster != null ? roster.fighters.Count : 0;
        int RandomSlot => FighterCount;
        int Rows => Mathf.CeilToInt(slots.Length / (float)Columns);

        bool IsLocked(int i)
        {
            if (i > RandomSlot) return true;
            return i < FighterCount && roster.fighters[i].locked;
        }

        void Awake()
        {
            continueVisual = continueButton.GetComponent<MenuButtonVisual>();
            levelVisuals = new MenuButtonVisual[cpuLevelButtons.Length];
            for (int i = 0; i < cpuLevelButtons.Length; i++)
            {
                int level = i;
                levelVisuals[i] = cpuLevelButtons[i].GetComponent<MenuButtonVisual>();
                cpuLevelButtons[i].onClick.AddListener(() => { GameSession.CpuLevel = (CpuLevel)level; Refresh(); });
            }

            backButton.onClick.AddListener(OnBack);
            continueButton.onClick.AddListener(TryContinue);
            cpuToggleButton.onClick.AddListener(ToggleCpu);
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                slots[i].button.onClick.AddListener(() => OnSlotClicked(index));
            }
        }

        public override void OnShow()
        {
            p1Ready = p2Ready = false;
            p1Cursor = Mathf.Clamp(GameSession.P1Fighter, 0, slots.Length - 1);
            p2Cursor = Mathf.Clamp(GameSession.P2Fighter, 0, slots.Length - 1);
            Refresh();
        }

        public override void OnBack()
        {
            // Esc steps back one pick before leaving the screen.
            if (p2Ready) { p2Ready = false; Refresh(); return; }
            if (p1Ready) { p1Ready = false; Refresh(); return; }
            controller.Show(controller.mainScreen);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.anyKey.wasPressedThisFrame) return;

            Vector2Int wasd = new Vector2Int(
                (kb.dKey.wasPressedThisFrame ? 1 : 0) - (kb.aKey.wasPressedThisFrame ? 1 : 0),
                (kb.sKey.wasPressedThisFrame ? 1 : 0) - (kb.wKey.wasPressedThisFrame ? 1 : 0));
            bool p1Confirm = kb.jKey.wasPressedThisFrame;
            bool p1Undo = kb.kKey.wasPressedThisFrame;

            if (GameSession.P2IsCpu)
            {
                if (!p1Ready)
                {
                    p1Cursor = Move(p1Cursor, wasd);
                    if (p1Confirm) Confirm(true);
                }
                else if (!p2Ready)
                {
                    p2Cursor = Move(p2Cursor, wasd);
                    if (p1Confirm) Confirm(false);
                    else if (p1Undo) p1Ready = false;
                }
                else if (p1Undo) p2Ready = false;
                else if (p1Confirm) TryContinue();

                if (kb.qKey.wasPressedThisFrame) CycleCpuLevel(-1);
                if (kb.eKey.wasPressedThisFrame) CycleCpuLevel(1);
            }
            else
            {
                if (!p1Ready)
                {
                    p1Cursor = Move(p1Cursor, wasd);
                    if (p1Confirm) Confirm(true);
                }
                else if (p1Undo) p1Ready = false;
                else if (p1Confirm && p2Ready) TryContinue();

                Vector2Int arrows = new Vector2Int(
                    (kb.rightArrowKey.wasPressedThisFrame ? 1 : 0) - (kb.leftArrowKey.wasPressedThisFrame ? 1 : 0),
                    (kb.downArrowKey.wasPressedThisFrame ? 1 : 0) - (kb.upArrowKey.wasPressedThisFrame ? 1 : 0));
                bool p2Confirm = kb.numpad1Key.wasPressedThisFrame;
                bool p2Undo = kb.numpad2Key.wasPressedThisFrame;
                if (!p2Ready)
                {
                    p2Cursor = Move(p2Cursor, arrows);
                    if (p2Confirm) Confirm(false);
                }
                else if (p2Undo) p2Ready = false;
                else if (p2Confirm && p1Ready) TryContinue();
            }

            if (kb.tabKey.wasPressedThisFrame) { ToggleCpu(); return; }
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) TryContinue();
            Refresh();
        }

        int Move(int cursor, Vector2Int dir)
        {
            if (dir == Vector2Int.zero) return cursor;
            int col = (cursor % Columns + dir.x + Columns) % Columns;
            int row = (cursor / Columns + dir.y + Rows) % Rows;
            return Mathf.Min(row * Columns + col, slots.Length - 1);
        }

        void Confirm(bool forP1)
        {
            int cursor = forP1 ? p1Cursor : p2Cursor;
            if (IsLocked(cursor)) return;
            int pick = cursor == RandomSlot ? RandomFighter() : cursor;
            if (pick < 0) return;
            if (forP1) { p1Pick = pick; p1Ready = true; }
            else { p2Pick = pick; p2Ready = true; }
        }

        int RandomFighter()
        {
            var open = new List<int>();
            for (int i = 0; i < FighterCount; i++)
                if (!roster.fighters[i].locked) open.Add(i);
            return open.Count == 0 ? -1 : open[Random.Range(0, open.Count)];
        }

        void OnSlotClicked(int index)
        {
            if (!p1Ready) { p1Cursor = index; Confirm(true); }
            else if (!p2Ready) { p2Cursor = index; Confirm(false); }
            Refresh();
        }

        void ToggleCpu()
        {
            GameSession.Mode = GameSession.P2IsCpu ? GameMode.Versus : GameMode.VsBot;
            p2Ready = false;
            Refresh();
        }

        void CycleCpuLevel(int dir)
        {
            GameSession.CpuLevel = (CpuLevel)(((int)GameSession.CpuLevel + dir + 3) % 3);
        }

        void TryContinue()
        {
            if (!p1Ready || !p2Ready) return;
            GameSession.P1Fighter = p1Pick;
            GameSession.P2Fighter = p2Pick;
            controller.Show(controller.mapScreen);
        }

        string SlotName(int i)
        {
            if (i < FighterCount) return roster.fighters[i].displayName;
            return i == RandomSlot ? "RANDOM" : "COMING SOON";
        }

        Sprite Portrait(int i) => i >= 0 && i < FighterCount ? roster.fighters[i].portrait : null;

        void Refresh()
        {
            bool cpu = GameSession.P2IsCpu;
            bool showP2Cursor = !cpu || p1Ready;
            string p2Tag = cpu ? "CPU" : "P2";

            modeText.text = "MODE: <color=#FFFFFF>" + (cpu ? "VS BOT" : "VERSUS 2P") + "</color>";

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].Set(SlotName(i), Portrait(i), IsLocked(i), i == RandomSlot,
                             i == p1Cursor && !p1Ready, i == p2Cursor && showP2Cursor && !p2Ready, p2Tag);
            }

            int p1Shown = p1Ready ? p1Pick : p1Cursor;
            p1Panel.Show("P1", SlotName(p1Shown), Portrait(p1Shown),
                         p1Ready ? "READY" : "CHOOSING...", p1Ready, "WASD + J");

            int p2Shown = p2Ready ? p2Pick : p2Cursor;
            string p2Status = p2Ready ? "READY" : cpu ? (p1Ready ? "P1 PICKS..." : "WAITING") : "CHOOSING...";
            p2Panel.Show(p2Tag, SlotName(p2Shown), Portrait(p2Shown), p2Status, p2Ready,
                         cpu ? "Q / E: level" : "Arrows + Num1");
            p2Panel.SetCpuRow(cpu);
            if (p2Panel.toggleText) p2Panel.toggleText.text = cpu ? "VS P2" : "VS CPU";

            for (int i = 0; i < levelVisuals.Length; i++)
                if (levelVisuals[i]) levelVisuals[i].On = i == (int)GameSession.CpuLevel;

            bool allReady = p1Ready && p2Ready;
            continueButton.interactable = allReady;
            if (continueVisual) continueVisual.On = allReady;
        }
    }
}
