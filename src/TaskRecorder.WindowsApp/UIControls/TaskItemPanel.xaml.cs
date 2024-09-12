using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TaskRecorder.Core;

namespace TaskRecorder.WindowsApp.UIControls
{
    /// <summary>
    /// TaskItemPanel.xaml の相互作用ロジック
    /// </summary>
    public partial class TaskItemPanel : System.Windows.Controls.UserControl
    {
        public TaskItemPanel()
        {
            InitializeComponent();
        }

        public WorkingTask WorkingTask
        {
            get { return (WorkingTask)GetValue(WorkingTaskProperty); }
            set { SetValue(WorkingTaskProperty, value); }
        }

        public static readonly DependencyProperty WorkingTaskProperty =
            DependencyProperty.Register("WorkingTask", typeof(WorkingTask), typeof(TaskItemPanel), new PropertyMetadata(null));


        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == WorkingTaskProperty)
            {
                this.TaskNameLabel.Text = this.WorkingTask.Name;
                this.TaskDescriptionLabel.Text = this.WorkingTask.Description;
            }
        }
    }
}
