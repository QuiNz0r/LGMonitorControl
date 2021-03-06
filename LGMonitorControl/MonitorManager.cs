using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace LGMonitorControl
{
    public class PhysicalMonitor
    {
        public IntPtr hPhysicalMonitor;
        public string DeviceName;
        public bool IsEnabled;
        public bool IsPoweredOn;
        public uint BrightnessLevel;
    }
    public class Monitor
    {
        public IntPtr hMonitor;
        public Rect rect;
        public List<PhysicalMonitor> physicalMonitors;
    }

    public class MonitorData
    {

        public MonitorData(PhysicalMonitor physicalMonitor, Rect rect)
        {
            this.Ref = physicalMonitor;
            IsLandspace = (rect.Right - rect.Left) / (float)(rect.Bottom - rect.Top) > 1.0f;
            PositionX = rect.Left;
        }

        public PhysicalMonitor Ref { get; private set; }
        public string DeviceName { get { return Ref.IsEnabled ? Ref.DeviceName : "Generic Monitor (Disabled)"; } }
        public bool IsEnabled { get { return Ref.IsEnabled; } }
        public bool IsPoweredOn { get { return Ref.IsEnabled ? Ref.IsPoweredOn : false; } }
        public uint BrightnessLevel { get { return Ref.IsEnabled ? Ref.BrightnessLevel : 0; } }
        
        public bool IsLandspace { get; private set; }
        public int PositionX { get; private set; }
        public int DisplayWidth { get { return IsLandspace ? 96 : 30; } }
        public string PowerText { get { return IsPoweredOn ? "OFF" : "ON"; } }

        internal void SwitchPower()
        {
            this.Ref.IsPoweredOn = !this.Ref.IsPoweredOn;
            //PropertyChanged(this, new PropertyChangedEventArgs("PowerText"));
        }

        public override string ToString()
        {
            return DeviceName;
        }
    }


    internal class MonitorManager : IDisposable
    {
        #region [Windows API]
        [DllImport("user32.dll", EntryPoint = "MonitorFromWindow")]
        private static extern IntPtr MonitorFromWindow([In] IntPtr hwnd, uint dwFlags);

        [DllImport("dxva2.dll", EntryPoint = "DestroyPhysicalMonitors")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, ref PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", EntryPoint = "GetNumberOfPhysicalMonitorsFromHMONITOR")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, ref uint pdwNumberOfPhysicalMonitors);

        [DllImport("dxva2.dll", EntryPoint = "GetPhysicalMonitorsFromHMONITOR")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize,
            [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", EntryPoint = "GetVCPFeatureAndVCPFeatureReply", SetLastError = true)]
        private static extern Boolean GetVCPFeatureAndVCPFeatureReply([In] IntPtr hPhisicalMonitor, [In] byte bVCPCode,
            IntPtr pvct, ref uint pdwCurrentValue, ref uint pdwMaximumValue);


        private delegate bool MonitorEnumProc(IntPtr hDesktop, IntPtr hdc, ref Rect pRect, int dwData);

        [DllImport("user32")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lpRect, MonitorEnumProc callback, int dwData);

        [DllImport("dxva2.dll", EntryPoint = "SetVCPFeature", SetLastError = true)]
        private static extern bool SetVCPFeature([In] IntPtr hPhisicalMonitor, byte bVCPCode, uint dwNewValue);
        #endregion

        const byte SVC_FEATURE__POWER_MODE = 0xD6; // values use PowerModeEnum
        const byte SVC_FEATURE__BRIGHTNESS = 0x10; // value range is [0-100]

        public enum PowerModeEnum : uint
        {
            PowerOn = 0x01,
            PowerStandby = 0x02,
            PowerSuspend = 0x03,
            PowerOff = 0x04,
            PowerOffButton = 0x05 // Readonly
        }

        List<Monitor> monitors;
        List<PHYSICAL_MONITOR> physicalMonitors;

        public IReadOnlyList<Monitor> Monitors
        {
            get
            {
                return monitors.AsReadOnly();
            }
        }
        public void Initialize()
        {
            monitors = new List<Monitor>();
            physicalMonitors = new List<PHYSICAL_MONITOR>();


            List<(IntPtr monitor, Rect rect)> hMonitors = new List<(IntPtr, Rect)>();

            MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdc, ref Rect prect, int d) =>
            {
                monitors.Add(new Monitor
                {
                    hMonitor = hMonitor,
                    rect = prect,
                });
                return true;
            };


            if (EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, 0))
            {
                foreach (var monitor in monitors)
                {
                    uint mcount = 0;
                    if (!GetNumberOfPhysicalMonitorsFromHMONITOR(monitor.hMonitor, ref mcount))
                    {
                        throw new Exception("Cannot get monitor count!");
                    }

                    PHYSICAL_MONITOR[] physicalMonitors = new PHYSICAL_MONITOR[mcount];

                    if (!GetPhysicalMonitorsFromHMONITOR(monitor.hMonitor, mcount, physicalMonitors))
                    {
                        throw new Exception("Cannot get phisical monitor handle!");
                    }

                    Debug.WriteLine($"PM:{physicalMonitors.Length}) RECT: T:{monitor.rect.Top}/L:{monitor.rect.Left}/R:{monitor.rect.Right}/B:{monitor.rect.Bottom}");


                    this.physicalMonitors.AddRange(physicalMonitors);

                    monitor.physicalMonitors = physicalMonitors.Select(a => new PhysicalMonitor
                    {
                        DeviceName = a.szPhysicalMonitorDescription,
                        hPhysicalMonitor = a.hPhysicalMonitor
                    }).ToList();

                }

                foreach (var p in monitors.SelectMany(a => a.physicalMonitors))
                {
                    uint cv = 0;

                    // power
                    if (GetFeatureValue(p.hPhysicalMonitor, SVC_FEATURE__POWER_MODE, ref cv))
                    {
                        p.IsPoweredOn = (cv == (uint)PowerModeEnum.PowerOn);
                        Debug.WriteLine($"{p.hPhysicalMonitor} + {p.DeviceName} + POWER={cv}");
                        p.IsEnabled = true;
                    }
                    else
                    {
                        string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                        Debug.WriteLine($"ERROR for {p.DeviceName}: `{errorMessage}`");
                    }

                    // BRIG
                    if (GetFeatureValue(p.hPhysicalMonitor, SVC_FEATURE__BRIGHTNESS, ref cv))
                    {
                        p.BrightnessLevel = cv;
                        Debug.WriteLine($"{p.hPhysicalMonitor} + {p.DeviceName} + BRIGHTNESS={cv}");
                    }
                    else
                    {
                        string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                        Debug.WriteLine($"ERROR for {p.DeviceName}: `{errorMessage}`");
                    }
                }
            }
        }


        public bool GetFeatureValue(IntPtr hPhysicalMonitor, byte svc_feature, ref uint currentValue)
        {
            uint mv = 0;
            return GetVCPFeatureAndVCPFeatureReply(hPhysicalMonitor, svc_feature, IntPtr.Zero, ref currentValue, ref mv);
            //string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
        }

        private bool SetFeatureValue(IntPtr hPhysicalMonitor, byte svc_feature, uint newVurrent)
        {
            return SetVCPFeature(hPhysicalMonitor, svc_feature, newVurrent);
            //string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
        }

        public bool ChangeGameMode(IntPtr hPhysicalMonitor, LG.GameMode.Modes mode)
        {
            if (LG.GameMode.currentMode != mode)
            {
                LG.GameMode.currentMode = mode;
                return SetVCPFeature(hPhysicalMonitor, LG.GameMode.VCP, (uint)mode);
            }
            return false;
            //string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
        }

        public bool ChangeGameMode(List<MonitorData> monitors, LG.GameMode.Modes mode)
        {
            if (LG.GameMode.currentMode != mode)
            {
                LG.GameMode.currentMode = mode;
                bool success = false;
                foreach (MonitorData monitor in monitors)
                {
                    success = SetVCPFeature(monitor.Ref.hPhysicalMonitor, LG.GameMode.VCP, (uint)mode);
                }
                return success;
            }
            return false;
            //string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
        }

        public bool GetCurrentGameMode(IntPtr hPhysicalMonitor, out LG.GameMode.Modes currentMode)
        {
            uint mode = 0;
            bool success = GetFeatureValue(hPhysicalMonitor, LG.GameMode.VCP, ref mode);

            currentMode = (LG.GameMode.Modes)mode;
            return success;
        }
        public bool GetCurrentGameMode(List<MonitorData> monitors, out LG.GameMode.Modes currentMode)
        {
            uint mode = 0;
            bool success = false;

            foreach (MonitorData monitor in monitors)
            {
                success = GetFeatureValue(monitor.Ref.hPhysicalMonitor, LG.GameMode.VCP, ref mode);
                if (success) break;
            }

            currentMode = (LG.GameMode.Modes)mode;
            return success;
        }

        public void ChangeBrightness(IntPtr hPhysicalMonitor, uint brightness)
        {
            SetFeatureValue(hPhysicalMonitor, SVC_FEATURE__BRIGHTNESS, brightness);
            // TODO:  use HighLevel API to set brightness on non VESA monitors
            // https://docs.microsoft.com/en-us/windows/win32/api/highlevelmonitorconfigurationapi/nf-highlevelmonitorconfigurationapi-setmonitorbrightness
        }

        public void ChangePower(IntPtr hPhysicalMonitor, bool PowerOn)
        {
            SetFeatureValue(hPhysicalMonitor, SVC_FEATURE__POWER_MODE, PowerOn ? (uint)PowerModeEnum.PowerOn : (uint)PowerModeEnum.PowerOff);
        }


        public void Dispose()
        {
            GC.SuppressFinalize(this);

            PHYSICAL_MONITOR[] toDestroy = physicalMonitors.ToArray();
            DestroyPhysicalMonitors((uint)toDestroy.Length, ref toDestroy);
        }


    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct PHYSICAL_MONITOR
    {
        public IntPtr hPhysicalMonitor;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }



    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }



}