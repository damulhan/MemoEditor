using System.Windows;
using MemoEditor.ViewModel;
using System.Diagnostics;
using System.Windows.Media;
using System;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Data;

namespace MemoEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, System.Windows.Forms.IWin32Window
    {
        static MainWindow _instance; 

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Closing += (s, e) => ViewModelLocator.Cleanup();

            _instance = this;
        }

        public IntPtr Handle
        {
            get { return new WindowInteropHelper(this).Handle; }
        }

        public static MainWindow Instance 
        {
            get { return _instance; }
        }

        private MainViewModel ViewModel
        {
            get { return DataContext as MainViewModel; }
        }

        #region toolbar events 
        private void FileNew_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            //Debug.WriteLine("FileNew_CanExecute: " + sender.ToString() + " " + e.Source.ToString());
            e.CanExecute = ViewModel.FileNewCommand.CanExecute(e.Source);
        }

        private void FileNew_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            //Debug.WriteLine("FileNew_Executed: " + sender.ToString() + " " + e.Source.ToString());
            ViewModel.FileNewCommand.Execute(null);
        }

        private void FileSave_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            //Debug.WriteLine("FileSave_CanExecute: " + sender.ToString() + " " + e.Source.ToString());
            e.CanExecute = ViewModel.FileSaveCommand.CanExecute(e.Source);
        }

        private void FileSave_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            //Debug.WriteLine("FileSave_Executed: " + sender.ToString() + " " + e.Source.ToString());
            ViewModel.FileSaveCommand.Execute(null);
        }

        private void EditText1_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            TextBox _editText1 = e.Source as TextBox;
            
            BindingExpression be = _editText1.GetBindingExpression(TextBox.TextProperty);
            be.UpdateSource();

            ViewModel.EditText1_TextChangedCommand.Execute(e.Source);
        }

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.OnLoadedCommand.Execute(e.Source);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ViewModel.OnClosingCommand.Execute(e);
        }

        private void FileExplorer1_Loaded(object sender, RoutedEventArgs e)
        {
            var treeview = e.Source as MemoEditor.FileExplorerControl;
            var items = treeview.TreeView1.Items;
            if(items.Count > 0) {
                ExplorerNode node = items.GetItemAt(0) as ExplorerNode;
                node.IsExpanded = true;
                node.IsSelected = true; 
            }
        }

    }
}