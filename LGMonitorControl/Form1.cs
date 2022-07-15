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
        
        private List<ApplicationData> _applications = new List<ApplicationData>();
        private MonitorManager _monitorManager;
        private MonitorData _selectedMonitor;


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
            LoadDataGridView();
            
            backgroundWorker1.RunWorkerAsync();
        }

        private void LoadDataGridView()
        {
            
            _applications.Add(new ApplicationData() { WindowName = "EscapeFromTarkov", GameMode = LG.GameMode.Modes.FPS });
            _applications.Add(new ApplicationData() { WindowName = "Overwatch", GameMode = LG.GameMode.Modes.READER });




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
                    string currentForegroundWindowName = WindowScanner.GetActiveWindowTitle();
                    bool foundWindow = false;
                    
                    if (currentForegroundWindowName != null)
                    {
                        foreach (var app in _applications)
                        {
                            if (currentForegroundWindowName.Contains(app.WindowName))
                            {
                                foundWindow = true;
                                _monitorManager.ChangeGameMode(_selectedMonitor.Ref.hPhysicalMonitor, app.GameMode);
                                break;
                            }
                        }

                        if (!foundWindow)
                        {
                            _monitorManager.ChangeGameMode(_selectedMonitor.Ref.hPhysicalMonitor, Settings.DefaultMode);
                        }
                    }
                }
                System.Threading.Thread.Sleep(100);
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                notifyIcon1.Visible = true;
                this.ShowInTaskbar = false;
            }

        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            notifyIcon1.Visible = false;
        }

    }
}
