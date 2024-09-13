using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TaskRecorder.Core;
using TaskRecorder.WindowsApp.Tools;

namespace TaskRecorder.WindowsApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static Mutex? _mutex;

        private WorkingManager _workingManager;

        private ContextMenuStrip _menu;
        private NotifyIcon _notifyIcon;

        private ToolStripMenuItem _tasksMenu;

        private DispatcherTimer _emptyCheckTimer;
        private DispatcherTimer _statusCheckTimer;

        private ToolMenuInfo? _toolMenuInfo;


        public string ApplicationName
        {
            get;
            private set;
        }

        public string TrayIconTip
        {
            get;
            private set;
        }

        public string RepositoryPath
        {
            get;
            private set;
        }

        public string TaskStorePath
        {
            get;
            private set;
        }

        public string ToolDirectoryPath
        {
            get;
            private set;
        }

        public string Location
        {
            get;
            private set;
        }

        public App()
        {
            this._menu = new ContextMenuStrip();
            this._notifyIcon = new NotifyIcon();
            this._tasksMenu = new ToolStripMenuItem("業務の切り替え(&C)");

            this.Location = Environment.ProcessPath ?? throw new Exception();

            this.ApplicationName = "Task Recorder";
            this.TrayIconTip = "業務履歴を記録します。";
            this.RepositoryPath = Path.Combine(Path.GetDirectoryName(this.Location) ?? "", "TaskLogs");
            this.TaskStorePath = Path.Combine(Path.GetDirectoryName(this.Location) ?? "", "TaskStore");
            this.ToolDirectoryPath = Path.Combine(Path.GetDirectoryName(this.Location) ?? "", "ReportTools");

            this._loadAppConfig();

            if (Directory.Exists(this.RepositoryPath) == false)
                Directory.CreateDirectory(this.RepositoryPath);

            if (Directory.Exists(this.TaskStorePath) == false)
                Directory.CreateDirectory(this.TaskStorePath);

            if (Directory.Exists(this.ToolDirectoryPath) == false)
                Directory.CreateDirectory(this.ToolDirectoryPath);

            this._workingManager = new WorkingManager(this.RepositoryPath);
            this._workingManager.LogFileTimeFormat = "yyyyMMdd-HHmmss-fff_";
            
            this._emptyCheckTimer = new DispatcherTimer();
            this._statusCheckTimer = new DispatcherTimer();

            this._loadTasks();
            this._updateTasksMenu();
            this._loadTools();
        }

        private void _loadAppConfig()
        {
            var appConfigPath = Path.Combine(Path.GetDirectoryName(this.Location) ?? throw new Exception(), "AppConfig.json");
            if (File.Exists(appConfigPath))
            {
                var getPropertyOrNull = (JsonElement elem, string key) =>
                {
                    if (elem.TryGetProperty(key, out var value))
                    {
                        return value.GetString();
                    }

                    return null;
                };

                using (var fs = File.OpenRead(appConfigPath))
                using (var document = JsonDocument.Parse(fs))
                {
                    var root = document.RootElement;
                    var application = root.GetProperty("Application");

                    this.ApplicationName = getPropertyOrNull(application, "Name") ?? this.ApplicationName;
                    this.TrayIconTip = getPropertyOrNull(application, "TrayIconTip") ?? this.TrayIconTip;
                    this.RepositoryPath = getPropertyOrNull(application, "RepositoryPath") ?? this.RepositoryPath;
                    this.TaskStorePath = getPropertyOrNull(application, "TaskStorePath") ?? this.TaskStorePath;
                    this.TaskStorePath = getPropertyOrNull(application, "ToolDirectoryPath") ?? this.TaskStorePath;
                }
            }
        }

        private void _loadTasks()
        {
            var taskDefJsonFiles = Directory.GetFiles(this.TaskStorePath, "*.json", SearchOption.TopDirectoryOnly);
            if (taskDefJsonFiles == null)
                throw new Exception();
            
            if (taskDefJsonFiles.Length == 0)
            {
                for (var i = 1; i <= 3; i++)
                {
                    var exampleTask = new WorkingTask()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Example task " + i.ToString("000"),
                        Code = "example-task-" + i.ToString("000"),
                        Description = "Task description",
                    };

                    using (var fs = File.OpenWrite(Path.Combine(this.TaskStorePath, $"exampletask_{i.ToString("000")}.json")))
                    {
                        JsonSerializer.Serialize(fs, exampleTask, new JsonSerializerOptions()
                        {
                            WriteIndented = true,
                        });
                    }
                }

                taskDefJsonFiles = Directory.GetFiles(this.TaskStorePath, "*.json", SearchOption.TopDirectoryOnly);
            }

            var tasks = new List<WorkingTask>();
            foreach (var taskDef in taskDefJsonFiles)
            {
                using (var fs = File.OpenRead(taskDef))
                {
                    tasks.Add(JsonSerializer.Deserialize<WorkingTask>(fs) ?? throw new Exception());
                }
            }

            this._workingManager.WorkingTasks.Clear();
            this._workingManager.WorkingTasks.AddRange(tasks);
        }

        private void _updateTasksMenu()
        {
            this._tasksMenu.DropDownItems.Clear();
            foreach (var wtask in this._workingManager.WorkingTasks)
            {
                var item = new ToolStripMenuItem(wtask.Name);
                item.Tag = wtask;
                item.Click += this._workingTaskMenuItem_Click;

                this._tasksMenu.DropDownItems.Add(item);
            }
        }

        private void _loadTools()
        {
            var defJsonPath = Path.Combine(this.ToolDirectoryPath, "tools.json");
            if (!File.Exists(defJsonPath))
                return;

            using (var fs = File.OpenRead(defJsonPath))
            {
                this._toolMenuInfo = JsonSerializer.Deserialize<ToolMenuInfo>(fs);
            }
        }

        private void _changeCurrentTask(WorkingTask newWorkingTask)
        {
            var confirmChangesWindow = new ConfirmChangesWindow();
            confirmChangesWindow.PrevWorkingTask = WorkingTask.IsNullOrEmpty(this._workingManager.CurrentWorkingTask) ? new WorkingTask() { Name = "(None)" } : this._workingManager.CurrentWorkingTask;
            confirmChangesWindow.NextWorkingTask = newWorkingTask;

            var respConfirm = confirmChangesWindow.ShowDialog();
            if (respConfirm == null || respConfirm.Value == false)
                return;

            var descriptionText = confirmChangesWindow.NextWorkingTaskDescriptionText;
            this._workingManager.ChangeCurrentTask(newWorkingTask, descriptionText);

            foreach (var item in this._tasksMenu.DropDownItems)
            {
                if (item is ToolStripMenuItem)
                {
                    var castedItem = (ToolStripMenuItem)item;
                    if (castedItem.Tag == newWorkingTask)
                        castedItem.Checked = true;
                    else
                        castedItem.Checked = false;
                }
            }
        }


        private void _workingTaskMenuItem_Click(Object? sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem == null)
                throw new Exception();
            var newWorkingTask = menuItem.Tag as WorkingTask;
            if (newWorkingTask == null)
                throw new Exception();

            this._changeCurrentTask(newWorkingTask);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var isNewInstance = false;
            var mutexName = "Global\\Eobw-TaskRecorderApp_C021C768-5D6A-4FF6-956E-1AE40BA29E41";

            _mutex = new Mutex(true, mutexName, out isNewInstance);
            if (isNewInstance == false)
            {
                System.Windows.MessageBox.Show($"{this.ApplicationName} はすでに起動済みです", this.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Warning);
                Environment.Exit(0);
            }

            System.Windows.Forms.Application.EnableVisualStyles();

            //this._menu.Items.Add("設定(&S) ...", null, (obj, e) => { });
            this._menu.Items.Add(this._tasksMenu);
            this._menu.Items.Add("タスクを再読み込み(&R)", null, (sender, e) =>
            {
                this._loadTasks();
                this._updateTasksMenu();
            });

            this._menu.Items.Add("タスク定義フォルダを開く(&T) ...", null, (obj, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = this.TaskStorePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"エラーが発生しました: {ex.Message}");
                }
            });

            if (this._toolMenuInfo != null && this._toolMenuInfo.Tools != null)
            {
                var toolStripMenuItem = new ToolStripMenuItem(this._toolMenuInfo.MenuItemName);
                foreach (var tool in this._toolMenuInfo.Tools)
                {
                    var item = new ToolStripMenuItem(tool.Name);
                    item.Click += (sender, e) =>
                    {
                        var result = System.Windows.MessageBox.Show(
                            $"{tool.Name} を実行しますか？\n({tool.Execute})",
                            this.ApplicationName,
                            MessageBoxButton.OKCancel,
                            MessageBoxImage.Information,
                            MessageBoxResult.Cancel);
                        if (result == MessageBoxResult.Cancel)
                            return;

                        try
                        {
                            if (tool.Execute == null || String.IsNullOrEmpty(tool.Execute))
                                return;

                            Process.Start(new ProcessStartInfo
                            {
                                FileName = Path.Combine(this.ToolDirectoryPath, tool.Execute),
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"エラーが発生しました: {ex.Message}");
                        }
                    };

                    toolStripMenuItem.DropDownItems.Add(item);
                }

                this._menu.Items.Add(toolStripMenuItem);
            }

            this._menu.Items.Add("終了(&X)", null, (obj, e) =>
            {
                var result = System.Windows.MessageBox.Show(
                    $"{this.ApplicationName} を終了しますか？",
                    this.ApplicationName,
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Information,
                    MessageBoxResult.Cancel);
                if (result == MessageBoxResult.Cancel)
                    return;

                this.Shutdown();
            });

            this._notifyIcon.Visible = true;
            this._notifyIcon.Icon = Icon.ExtractAssociatedIcon(this.Location);
            this._notifyIcon.Text = $"{this.ApplicationName}: {this.TrayIconTip}";
            this._notifyIcon.ContextMenuStrip = _menu;

            this._notifyIcon.Click += (obj, e) =>
            {
                // クリックされたときの処理
                // NOP
            };

            this._checkCurrentTask();

            this._emptyCheckTimer.Interval = TimeSpan.FromMinutes(3);
            this._emptyCheckTimer.Tick += (sender, e) => this._checkCurrentTask();
            this._emptyCheckTimer.Start();

            this._statusCheckTimer.Interval = TimeSpan.FromMinutes(60);
            this._statusCheckTimer.Tick += (sender, e) =>
            {
                if (WorkingTask.IsNullOrEmpty(this._workingManager.CurrentWorkingTask))
                    return;

                this._notifyIcon.ShowBalloonTip(3000, this.ApplicationName, $"現在のタスク\n{this._workingManager.CurrentWorkingTask.Name}", ToolTipIcon.Info);
            };
            this._statusCheckTimer.Start();

            base.OnStartup(e);

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        private void _checkCurrentTask()
        {
            if (WorkingTask.IsNullOrEmpty(this._workingManager.CurrentWorkingTask) == false)
                return;

            this._notifyIcon.ShowBalloonTip(3000, this.ApplicationName, "タスクを選択してください", ToolTipIcon.Info);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            this._workingManager.Pulse();
            this._emptyCheckTimer.Stop();
            this._notifyIcon.Dispose();

            base.OnExit(e);
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            this.Shutdown();
            base.OnSessionEnding(e);
        }
    }

}
