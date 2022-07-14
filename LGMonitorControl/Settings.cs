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
    }
}
