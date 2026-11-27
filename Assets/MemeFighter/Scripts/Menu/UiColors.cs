using UnityEngine;

namespace MemeFighter.UI
{
    /// <summary>Meme Fighter palette (matches the Style Guide board).</summary>
    public static class UiColors
    {
        public static readonly Color Night = Hex("#16123A");      // screen background
        public static readonly Color Panel = Hex("#241E57");      // cards, buttons
        public static readonly Color PanelHi = Hex("#312A73");    // inputs, slots
        public static readonly Color Focus = Hex("#463C9E");      // keyboard focus on list items
        public static readonly Color Outline = Hex("#0B0920");    // borders, shadows, bars
        public static readonly Color Yellow = Hex("#FFD23F");     // primary / selected
        public static readonly Color P1 = Hex("#FF6B35");         // player 1
        public static readonly Color P2 = Hex("#3FA9FF");         // player 2
        public static readonly Color Green = Hex("#9BE564");      // ready
        public static readonly Color Muted = Hex("#B9B3E6");      // hints, secondary text
        public static readonly Color Locked = Hex("#1C1745");
        public static readonly Color LockedText = Hex("#7F79B3");
        public static readonly Color Dots = Hex("#2B2466");
        public static readonly Color Silhouette = Hex("#4A4196");
        public static readonly Color RandomFill = Hex("#3A2F8A");
        public static readonly Color OffWhite = Hex("#F7F2E8");
        public static readonly Color White = Color.white;

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }
    }
}
