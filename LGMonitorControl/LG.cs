namespace LGMonitorControl
{
    public static class LG
    {
        public static class GameMode
        {
            public const byte VCP = 0x15;
            public static Modes currentMode;
            public enum Modes
            {
                Gamer1 = 45,
                Gamer2 = 46,
                FPS = 30,
                RTS = 31,
                VIVID = 49,
                READER = 1,
                HDR = 39,
                SRGB = 15,
            }
        }
    }
}
