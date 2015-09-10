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

            // Loading user preferences 
            var userPrefs = UserPreferences.Instance;
            this.Height = userPrefs.WindowHeight;
            this.Width = userPrefs.WindowWidth;
            this.Top = userPrefs.WindowTop;
            this.Left = userPrefs.WindowLeft;
            this.WindowState = userPrefs.WindowState;
            this.EditText1.FontSize = userPrefs.FontSize;
            this.EditText1.FontFamily = new FontFamily(userPrefs.FontFamily);

            Debug.WriteLine("font:" + userPrefs.FontFamily + " " + userPrefs.FontSize);
        }

        public IntPtr Handle
        {
            get { return new WindowInteropHelper(this).Handle; }
        }

        public static MainWindow Instance 
        {
            get { 
                return _instance;
            }
        }

        private MainViewModel ViewModel
        {
            get { return DataContext as MainViewModel; }
        }

        #region toolbar events 
        private void FileNew_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ViewModel.FileNewCommand.CanExecute(e.Source);
        }

        private void FileNew_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            ViewModel.FileNewCommand.Execute(null);
        }

        private void FileSave_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ViewModel.FileSaveCommand.CanExecute(e.Source);
        }

        private void FileSave_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
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
            var userPrefs = UserPreferences.Instance; 

            userPrefs.WindowHeight = this.Height;
            userPrefs.WindowWidth = this.Width;
            userPrefs.WindowTop = this.Top;
            userPrefs.WindowLeft = this.Left;
            userPrefs.WindowState = this.WindowState;
            userPrefs.FontSize = this.EditText1.FontSize;
            userPrefs.FontFamily = this.EditText1.FontFamily.ToString();

            userPrefs.Save();
            
            // Call View model's on closing 
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
        
        private void MenuItemTopmost_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = (sender as MenuItem).IsChecked;
        }

        private void MenuItemFontChange_Clicked(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FontDialog fontDialog = new System.Windows.Forms.FontDialog();
            fontDialog.Font = new System.Drawing.Font(EditText1.FontFamily.ToString(), (float)EditText1.FontSize);

            if (fontDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FontFamilyConverter ffc = new FontFamilyConverter();

                EditText1.FontSize = (double)fontDialog.Font.Size;
                EditText1.FontFamily = (FontFamily)ffc.ConvertFromString(fontDialog.Font.Name);
                
                /*
                if (fontDialog.Font.Bold)
                    textAnnotation.FontWeight = FontWeights.Bold;
                else
                    textAnnotation.FontWeight = FontWeights.Normal;

                if (fontDialog.Font.Italic)
                    textAnnotation.FontStyle = FontStyles.Italic;
                else
                    textAnnotation.FontStyle = FontStyles.Normal;
                */
            }
        }        

    }
}