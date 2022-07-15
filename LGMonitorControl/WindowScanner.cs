using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LGMonitorControl
{
    public static class WindowScanner
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        public static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }
        
        public static bool CheckForWindowName(string name)
        {
            string activeWindowTitle = GetActiveWindowTitle();
            
            if (activeWindowTitle == null) return false;
            
            if (GetActiveWindowTitle().Equals(name))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        
    }

}
