using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LGMonitorControl
{
    public partial class Form1 : Form
    {
        public List<MonitorData> monitors = null;

        private MonitorManager _monitorManager;
        private MonitorData _selectedMonitor;
        private string _appNameToCheck = "EscapeFromTarkov";


        public Form1()
        {
            InitializeComponent();

            
            _monitorManager = new MonitorManager();
            _monitorManager.Initialize();

            monitors = _monitorManager.Monitors
                .SelectMany(a => a.physicalMonitors.Select(b => new MonitorData(b, a.rect)))
                .OrderBy(r => r.PositionX)
                .ToList();

            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadSettings();

            backgroundWorker1.RunWorkerAsync();
            
        }

        

        private void LoadSettings()
        {
            Settings.RegisterInStartup(true);
            
            
            foreach (MonitorData monitor in monitors)
            {
                comboBox_Monitors.Items.Add(monitor);
            }

            comboBox_Monitors.SelectedIndex = 1;

            
            this.WindowState = FormWindowState.Minimized;
            
        }

        private void comboBox_Monitors_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_Monitors.SelectedItem == null) return;

            _selectedMonitor = (MonitorData)comboBox_Monitors.SelectedItem;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            while (true)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    bool running = WindowScanner.CheckForWindowName(_appNameToCheck);

                    if (running)
                    {
                        if (LG.VCPCodes.GameMode.currentMode != LG.VCPCodes.GameMode.FPS)
                        {
                            _monitorManager.SetFeatureValue(_selectedMonitor.Ref.hPhysicalMonitor, LG.VCPCodes.GameMode.VCP, LG.VCPCodes.GameMode.FPS);
                            LG.VCPCodes.GameMode.currentMode = LG.VCPCodes.GameMode.FPS;
                        }
                    }
                    else
                    {
                        if (LG.VCPCodes.GameMode.currentMode != LG.VCPCodes.GameMode.SRGB)
                        {
                            _monitorManager.SetFeatureValue(_selectedMonitor.Ref.hPhysicalMonitor, LG.VCPCodes.GameMode.VCP, LG.VCPCodes.GameMode.SRGB);
                            LG.VCPCodes.GameMode.currentMode = LG.VCPCodes.GameMode.SRGB;
                        }
                    }

                    System.Threading.Thread.Sleep(100);
                }
            }

            
            
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                //Hide();
                notifyIcon1.Visible = true;
                this.ShowInTaskbar = false;
            }

        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            //Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            notifyIcon1.Visible = false;

        }
    }
}
