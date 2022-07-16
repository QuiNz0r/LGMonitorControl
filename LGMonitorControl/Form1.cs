using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace LGMonitorControl
{
    public partial class Form1 : Form
    {

        private MonitorManager _monitorManager;
        private List<MonitorData> _monitors = new List<MonitorData>();

        public Form1()
        {
            InitializeComponent();

            
            _monitorManager = new MonitorManager();
            _monitorManager.Initialize();

            _monitors = _monitorManager.Monitors
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
        private void LoadSettings()
        {
            Settings.Instance.Load();

            if (_monitorManager.GetCurrentGameMode(_monitors, out LG.GameMode.Modes mode))
            {
                LG.GameMode.currentMode = mode;
            }

            if (Settings.Instance.StartMinimized) this.WindowState = FormWindowState.Minimized;
            checkBox_Minimized.Checked = Settings.Instance.StartMinimized;
            comboBox_Defaultmode.DataSource = Enum.GetValues(typeof(LG.GameMode.Modes));
            comboBox_Defaultmode.SelectedItem = Settings.Instance.DefaultMode;
            //checkBox_Autostart.Checked = Settings.Instance.GetAutostartState();
        }
        
        private void LoadDataGridView()
        {
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.DataSource = Settings.Instance.Applications;
            dataGridView1.EditMode = DataGridViewEditMode.EditOnKeystroke;
            
            WindowName.DataPropertyName = "WindowName";

            Mode.DataSource = Enum.GetValues(typeof(LG.GameMode.Modes));
            Mode.ValueType = typeof(LG.GameMode.Modes);
            Mode.DataPropertyName = "GameMode";


        }

        private void AddNewApplication()
        {
            ApplicationData newApp = new ApplicationData("<Enter Window Name>", LG.GameMode.Modes.SRGB);
            Settings.Instance.Applications.Add(newApp);
        }

        private void RemoveApplication()
        {
            if (Settings.Instance.Applications.Count > 0)
            {
                try
                {
                    Settings.Instance.Applications.RemoveAt(dataGridView1.CurrentCell.RowIndex);
                }
                catch
                {
                    Settings.Instance.Applications.RemoveAt(0);
                }
            }
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
                    
                    if (currentForegroundWindowName != null && _monitors != null && _monitors.Count > 0)
                    {
                        foreach (var app in Settings.Instance.Applications)
                        {
                            if (currentForegroundWindowName.Contains(app.WindowName))
                            {
                                foundWindow = true;

                                _monitorManager.ChangeGameMode(_monitors, app.GameMode);
                                break;
                            }
                        }

                        if (!foundWindow)
                        {
                            _monitorManager.ChangeGameMode(_monitors, Settings.Instance.DefaultMode);
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
                this.Hide();
            }

        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Show();
            this.Activate();
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
            //if (checkBox_Autostart.Checked)
            //{
            //    Settings.Instance.RegisterInStartup(true);
            //}
            //else
            //{
            //    Settings.Instance.RegisterInStartup(false);
            //}
        }

        private void checkBox_Minimized_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance.StartMinimized = checkBox_Minimized.Checked;
            Settings.Instance.Save();
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
                Settings.Instance.Applications[e.RowIndex].GameMode = (LG.GameMode.Modes)cb.Value;
            }
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Instance.Save();
        }

        private void comboBox_Defaultmode_SelectionChangeCommitted(object sender, EventArgs e)
        {
            Settings.Instance.DefaultMode = (LG.GameMode.Modes)comboBox_Defaultmode.SelectedItem;
            Settings.Instance.Save();
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            Settings.Instance.Save();
        }
    }
}
