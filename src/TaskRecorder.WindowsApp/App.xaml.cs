using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using TaskRecorder.Core;

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

        private DispatcherTimer _timer;

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

        public string Location
        {
            get;
            private set;
        }

        public App()
        {
            this._menu = new ContextMenuStrip();
            this._notifyIcon = new NotifyIcon();
            this._tasksMenu = new ToolStripMenuItem("業務の切り替え");

            this.Location = Environment.ProcessPath ?? throw new Exception();

            this.ApplicationName = "Task Recorder";
            this.TrayIconTip = "業務履歴を記録します。";
            this.RepositoryPath = Path.Combine(Path.GetDirectoryName(this.Location) ?? "", "TaskLogs");
            this.TaskStorePath = Path.Combine(Path.GetDirectoryName(this.Location) ?? "", "TaskStore");

            this._loadAppConfig();

            if (Directory.Exists(this.RepositoryPath) == false)
                Directory.CreateDirectory(this.RepositoryPath);

            if (Directory.Exists(this.TaskStorePath) == false)
                Directory.CreateDirectory(this.TaskStorePath);

            this._workingManager = new WorkingManager(this.RepositoryPath);
            this._workingManager.LogFileTimeFormat = "yyyyMMdd-HHmmss-fff_";
            
            this._timer = new DispatcherTimer();
            
            this._loadTasks();
            this._updateTasksMenu();
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


        private void _workingTaskMenuItem_Click(Object? sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem == null)
                throw new Exception();
            var newWorkingTask = menuItem.Tag as WorkingTask;
            if (newWorkingTask == null)
                throw new Exception();

            //var gen = this._workingManager.CurrentWorkingTask.Name;
            //gen = String.IsNullOrEmpty(gen) ? "(None)" : gen;

            //var resp = System.Windows.MessageBox.Show(
            //    $"タスクを切り替えますか？\n\n【現】{gen}\n【新】{newWorkingTask.Name}", this.ApplicationName, MessageBoxButton.YesNo, MessageBoxImage.Information);

            //if (resp == MessageBoxResult.No)
            //    return;

            var confirmChangesWindow = new ConfirmChangesWindow();
            confirmChangesWindow.PrevWorkingTask = WorkingTask.IsNullOrEmpty(this._workingManager.CurrentWorkingTask) ? new WorkingTask() { Name = "(None)" } : this._workingManager.CurrentWorkingTask;
            confirmChangesWindow.NextWorkingTask = newWorkingTask;

            var respConfirm = confirmChangesWindow.ShowDialog();
            if (respConfirm == null || respConfirm.Value == false)
                return;

            this._workingManager.ChangeCurrentTask(newWorkingTask, String.Empty);

            foreach (var item in this._tasksMenu.DropDownItems)
            {
                if (item is ToolStripMenuItem)
                    ((ToolStripMenuItem)item).Checked = false;
            }
            menuItem.Checked = true;
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

            this._timer.Interval = TimeSpan.FromMinutes(5);
            this._timer.Tick += (sender, e) => this._checkCurrentTask();
            this._timer.Start();

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
            this._timer.Stop();
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
