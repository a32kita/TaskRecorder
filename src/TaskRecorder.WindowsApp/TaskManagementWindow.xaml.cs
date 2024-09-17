using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TaskRecorder.Core;

namespace TaskRecorder.WindowsApp
{
    /// <summary>
    /// TaskManagementWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TaskManagementWindow : Window
    {
        private WorkingManager _workingManager;


        /// <summary>
        /// WorkingTask はコンストラクタで指定された WorkingManager クラスに対して直接管理が行われます。
        /// このリストは UI 用のものです。
        /// </summary>
        public ObservableCollection<WorkingTask> WorkingTasks
        {
            get;
            private set;
        }


        public event EventHandler? RequestedUpdateTasks;

        public event EventHandler<RequestedAddTaskEventArgs>? RequestedAddTask;


        public TaskManagementWindow(WorkingManager workingManager)
        {
            InitializeComponent();
            this.DataContext = this;

            this._workingManager = workingManager;
            this.WorkingTasks = new ObservableCollection<WorkingTask>();

            this._reloadTasks();
        }

        private void _reloadTasksButton_Click(object sender, RoutedEventArgs e)
        {
            this._reloadTasks();
        }

        private void _reloadTasks()
        {
            this.RequestedUpdateTasks?.Invoke(this, EventArgs.Empty);
            var workingTasks = this._workingManager.WorkingTasks;
            if (workingTasks == null)
                return;

            this.WorkingTasks.Clear();
            foreach (var task in workingTasks)
                this.WorkingTasks.Add(task);
        }

        private void _addNewTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var taskEditWindow = new TaskEditWindow();
            taskEditWindow.Owner = this;
            taskEditWindow.Title = "新規タスクの追加";
            taskEditWindow.TaskNameText = "Untitled Task";
            taskEditWindow.TaskIdText = Guid.NewGuid().ToString();
            taskEditWindow.ShowDialog();

            if (taskEditWindow.DialogResult == false)
                return;

            var taskId = Guid.Empty;
            if (Guid.TryParse(taskEditWindow.TaskIdText, out taskId) == false)
            {
                var result = System.Windows.MessageBox.Show(
                    $"GUID へのパースに失敗しました；\n{taskEditWindow.TaskIdText}",
                    "ERROR",
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
                return;
            };

            this.RequestedAddTask?.Invoke(this, new RequestedAddTaskEventArgs(new WorkingTask()
            {
                Name = taskEditWindow.TaskNameText,
                ShortName = taskEditWindow.TaskShortNameText,
                Code = taskEditWindow.TaskCodeText,
                Id = taskId,
                Description = taskEditWindow.TaskDescriptionText,
            }));

            this._reloadTasks();
        }


        public class RequestedAddTaskEventArgs
        {
            public WorkingTask WorkingTask
            {
                get;
            }

            public RequestedAddTaskEventArgs(WorkingTask workingTask)
            {
                this.WorkingTask = workingTask;
            }
        }
    }
}
