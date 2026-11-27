namespace MemeFighter
{
    public enum GameMode { Versus, VsBot }
    public enum CpuLevel { Easy, Normal, Hard }

    /// <summary>
    /// Choices made in the menus, read by the fight scene when it loads.
    /// Fighter and stage values are indexes into the MenuRoster asset.
    /// </summary>
    public static class GameSession
    {
        public static GameMode Mode = GameMode.Versus;
        public static int P1Fighter = 0;
        public static int P2Fighter = 3;
        public static CpuLevel CpuLevel = CpuLevel.Normal;
        public static int StageIndex = 0;
        public static int Stocks = 3;
        /// <summary>Match length in seconds. 0 means no time limit.</summary>
        public static int TimeLimitSeconds = 480;

        public static bool P2IsCpu => Mode == GameMode.VsBot;
    }
}
