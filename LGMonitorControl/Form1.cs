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
        
        private BindingList<ApplicationData> _applications = new BindingList<ApplicationData>();
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
            _applications.Add(new ApplicationData("EscapeFromTarkov", LG.GameMode.Modes.FPS));
            _applications.Add(new ApplicationData("Overwatch", LG.GameMode.Modes.VIVID));
            _applications.Add(new ApplicationData("Notepad", LG.GameMode.Modes.READER));

            comboBox_Monitors.SelectionLength = 0;

            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.DataSource = _applications;
            dataGridView1.EditMode = DataGridViewEditMode.EditOnKeystroke;
            
            WindowName.DataPropertyName = "WindowName";

            Mode.DataSource = Enum.GetValues(typeof(LG.GameMode.Modes));
            Mode.ValueType = typeof(LG.GameMode.Modes);
            Mode.DataPropertyName = "GameMode";


            comboBox_Defaultmode.DataSource = Enum.GetValues(typeof(LG.GameMode.Modes));
        }

        private void AddNewApplication()
        {
            ApplicationData newApp = new ApplicationData("<Enter Window Name>", LG.GameMode.Modes.SRGB);
            _applications.Add(newApp);
        }

        private void RemoveApplication()
        {
            if (_applications.Count > 0)
            {
                try
                {
                    _applications.RemoveAt(dataGridView1.CurrentCell.RowIndex);
                }
                catch
                {
                    _applications.RemoveAt(0);
                }
            }
        }

        private void LoadSettings()
        {
            checkBox_Autostart.Checked = Settings.GetAutostartState();

            
            foreach (MonitorData monitor in monitors)
            {
                comboBox_Monitors.Items.Add(monitor);
            }
            
            comboBox_Monitors.SelectedIndex = 1;

            if (_selectedMonitor != null)
            {
                if(_monitorManager.GetCurrentGameMode(_selectedMonitor.Ref.hPhysicalMonitor, out LG.GameMode.Modes mode))
                {
                    LG.GameMode.currentMode = mode;
                }
                
            }

            if (checkBox_Minimized.Checked) this.WindowState = FormWindowState.Minimized;
            
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
                    
                    if (currentForegroundWindowName != null && _selectedMonitor != null)
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
                //notifyIcon1.Visible = true;
                this.ShowInTaskbar = false;
            }

        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            //notifyIcon1.Visible = false;
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            AddNewApplication();
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            RemoveApplication();
        }

        private void checkBox_Autostart_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Autostart.Checked)
            {
                Settings.RegisterInStartup(true);
            }
            else
            {
                Settings.RegisterInStartup(false);
            }
        }

        private void checkBox_Minimized_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell is DataGridViewComboBoxCell)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
                dataGridView1.EndEdit();
            }

            if (dataGridView1.IsCurrentCellDirty)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void dataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            
            if (dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex] is DataGridViewComboBoxCell)
            {
                dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
                dataGridView1.BeginEdit(true);
                ((ComboBox)dataGridView1.EditingControl).DroppedDown = true; // Tell combobox to expand
            }
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            
            DataGridViewComboBoxCell cb = (DataGridViewComboBoxCell)dataGridView1.Rows[e.RowIndex].Cells[1];
            if (cb.Value != null)
            {
                dataGridView1.Invalidate();
                Console.WriteLine(cb.Value);
                _applications[e.RowIndex].GameMode = (LG.GameMode.Modes)cb.Value;
            }
        }
    }
}
