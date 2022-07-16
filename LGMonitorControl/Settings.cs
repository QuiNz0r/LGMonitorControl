using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LGMonitorControl
{
    public static class Settings
    {
        public static LG.GameMode.Modes DefaultMode { get; set; } = LG.GameMode.Modes.SRGB;
        
        public static void RegisterInStartup(bool isChecked)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (isChecked)
            {
                registryKey.SetValue("LGMonitorControl", Application.ExecutablePath);
            }
            else
            {
                registryKey.DeleteValue("LGMonitorControl");
            }
        }

        public static bool GetAutostartState()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            return registryKey.GetValue("LGMonitorControl") != null;


        }
    }

    public class ApplicationData
    {
        public string WindowName { get; set; }

        public LG.GameMode.Modes GameMode { get; set; }

        public ApplicationData(string windowName, LG.GameMode.Modes gameMode)
        {
            WindowName = windowName;
            GameMode = gameMode;
        }
    }
}
