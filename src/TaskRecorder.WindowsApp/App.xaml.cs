﻿using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;

using TaskRecorder.Core;

namespace TaskRecorder.WindowsApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private WorkingManager _workingManager;

        private ContextMenuStrip _menu;
        private NotifyIcon _notifyIcon;

        private ToolStripMenuItem _tasksMenu;

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
                var exampleTask = new WorkingTask()
                {
                    Id = Guid.NewGuid(),
                    Name = "Example task",
                    Code = "example-task-001",
                    Description = "Task description",
                };

                using (var fs = File.OpenWrite(Path.Combine(this.TaskStorePath, "exampletask.json")))
                {
                    JsonSerializer.Serialize(fs, exampleTask, new JsonSerializerOptions()
                    {
                        WriteIndented = true,
                    });
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

            var resp = System.Windows.MessageBox.Show(
                $"タスクを切り替えますか？\n\n【現】{this._workingManager.CurrentWorkingTask.Name}\n【新】{newWorkingTask.Name}", this.ApplicationName, MessageBoxButton.YesNo);

            if (resp == MessageBoxResult.No)
                return;

            this._workingManager.ChangeCurrentTask(newWorkingTask, String.Empty);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            _menu.Items.Add("設定(&S) ...", null, (obj, e) => { });
            _menu.Items.Add(this._tasksMenu);
            _menu.Items.Add("終了(&X)", null, (obj, e) => { this.Shutdown(); });

            _notifyIcon.Visible = true;
            _notifyIcon.Icon = Icon.ExtractAssociatedIcon(this.Location);
            _notifyIcon.Text = $"{this.ApplicationName}: {this.TrayIconTip}";
            _notifyIcon.ContextMenuStrip = _menu;

            _notifyIcon.Click += (obj, e) =>
            {
                // クリックされたときの処理
                // NOP
            };

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            this._workingManager.Pulse();
            base.OnExit(e);
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            this.Shutdown();
            base.OnSessionEnding(e);
        }
    }

}
