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
    }
}
