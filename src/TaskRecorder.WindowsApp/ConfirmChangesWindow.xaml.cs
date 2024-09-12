using System;
using System.Collections.Generic;
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
    /// ConfirmChangesWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfirmChangesWindow : Window
    {
        public ConfirmChangesWindow()
        {
            this.DataContext = this;
            InitializeComponent();
        }



        public WorkingTask PrevWorkingTask
        {
            get { return (WorkingTask)GetValue(PrevWorkingTaskProperty); }
            set { SetValue(PrevWorkingTaskProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PrevWorkingTask.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PrevWorkingTaskProperty =
            DependencyProperty.Register("PrevWorkingTask", typeof(WorkingTask), typeof(ConfirmChangesWindow), new PropertyMetadata(null));



        public WorkingTask NextWorkingTask
        {
            get { return (WorkingTask)GetValue(NextWorkingTaskProperty); }
            set { SetValue(NextWorkingTaskProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NextWorkingTask.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NextWorkingTaskProperty =
            DependencyProperty.Register("NextWorkingTask", typeof(WorkingTask), typeof(ConfirmChangesWindow), new PropertyMetadata(null));


        //public WorkingTask PrevWorkingTask
        //{
        //    get;
        //    set;
        //}

        //public WorkingTask NextWorkingTask
        //{
        //    get;
        //    set;
        //}

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
