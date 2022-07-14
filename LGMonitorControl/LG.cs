using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGMonitorControl
{
    public static class LG
    {
        public static class VCPCodes
        {
            public static class GameMode
            {
                public const byte VCP = 0x15;

                public static int currentMode = -1;
                public const int SRGB = 15;
                public const int FPS = 30;

                
            }



        }
    }
}
