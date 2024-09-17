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

namespace TaskRecorder.WindowsApp
{
    /// <summary>
    /// TaskEditWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TaskEditWindow : Window
    {
        public TaskEditWindow()
        {
            this.DataContext = this;
            InitializeComponent();
        }


        public string TaskNameText
        {
            get { return (string)GetValue(TaskNameTextProperty); }
            set { SetValue(TaskNameTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TaskNameText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TaskNameTextProperty =
            DependencyProperty.Register("TaskNameText", typeof(string), typeof(TaskEditWindow), new PropertyMetadata(String.Empty));




        public string TaskShortNameText
        {
            get { return (string)GetValue(TaskShortNameTextProperty); }
            set { SetValue(TaskShortNameTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TaskShortNameText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TaskShortNameTextProperty =
            DependencyProperty.Register("TaskShortNameText", typeof(string), typeof(TaskEditWindow), new PropertyMetadata(String.Empty));




        public string TaskCodeText
        {
            get { return (string)GetValue(TaskCodeTextProperty); }
            set { SetValue(TaskCodeTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TaskCodeText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TaskCodeTextProperty =
            DependencyProperty.Register("TaskCodeText", typeof(string), typeof(TaskEditWindow), new PropertyMetadata(String.Empty));




        public string TaskIdText
        {
            get { return (string)GetValue(TaskIdTextProperty); }
            set { SetValue(TaskIdTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TaskIdText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TaskIdTextProperty =
            DependencyProperty.Register("TaskIdText", typeof(string), typeof(TaskEditWindow), new PropertyMetadata(String.Empty));




        public string TaskDescriptionText
        {
            get { return (string)GetValue(TaskDescriptionTextProperty); }
            set { SetValue(TaskDescriptionTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TaskDescriptionText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TaskDescriptionTextProperty =
            DependencyProperty.Register("TaskDescriptionText", typeof(string), typeof(TaskEditWindow), new PropertyMetadata(String.Empty));




        public Visibility ErrorMessageBoxVisibility
        {
            get { return (Visibility)GetValue(ErrorMessageBoxVisibilityProperty); }
            set { SetValue(ErrorMessageBoxVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ErrorMessageBoxVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ErrorMessageBoxVisibilityProperty =
            DependencyProperty.Register("ErrorMessageBoxVisibility", typeof(Visibility), typeof(TaskEditWindow), new PropertyMetadata(Visibility.Collapsed));




        public string ErrorMessageBoxText
        {
            get { return (string)GetValue(ErrorMessageBoxTextProperty); }
            set { SetValue(ErrorMessageBoxTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ErrorMessageBoxText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ErrorMessageBoxTextProperty =
            DependencyProperty.Register("ErrorMessageBoxText", typeof(string), typeof(TaskEditWindow), new PropertyMetadata(String.Empty));




        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var validationErrors = new List<string>();

            var taskId = Guid.Empty;
            if (Guid.TryParse(this.TaskIdText, out taskId) == false)
                validationErrors.Add($"有効な ID ではありません: {this.TaskIdText}");

            if (String.IsNullOrEmpty(this.TaskNameText))
                validationErrors.Add("タスク名は空にできません");
            if (String.IsNullOrEmpty(this.TaskCodeText))
                validationErrors.Add("Code は空にできません");

            if (validationErrors.Count == 0)
                this.DialogResult = true;

            this.ErrorMessageBoxVisibility = Visibility.Visible;
            this.ErrorMessageBoxText = String.Join("\n", validationErrors);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult= false;
        }
    }
}
