using System.Windows;
using MemoEditor.ViewModel;
using System.Diagnostics;
using System.Windows.Media;
using System;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using System.Windows.Input;
using Utility;
using GalaSoft.MvvmLight.Messaging;
using MSDN.Html.Editor;

namespace MemoEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, System.Windows.Forms.IWin32Window
    {
        static MainWindow _instance;

        public static readonly RoutedUICommand FindNextCommand =
            new RoutedUICommand("Find Next", "Find Next", typeof(MainWindow),
                new InputGestureCollection() { new KeyGesture(Key.F3) }
            );

        public static readonly RoutedUICommand AddTimeCommand =
            new RoutedUICommand("Add Time", "Add Time", typeof(MainWindow),
                new InputGestureCollection() { new KeyGesture(Key.F5) }
            );

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
            this.Width = userPrefs.WindowWidth;
            this.Height = userPrefs.WindowHeight;
            this.Top = userPrefs.WindowTop;
            this.Left = userPrefs.WindowLeft;
            this.WindowState = userPrefs.WindowState;
            this.EditText1.FontSize = userPrefs.FontSize;
            this.EditText1.FontFamily = new FontFamily(userPrefs.FontFamily);
            this.EditText1.SelectionBrush = Brushes.DodgerBlue;

            //Debug.WriteLine("font:" + userPrefs.FontFamily + " " + userPrefs.FontSize);

            _initializeMessenger();

            // 
            AddHandler(Keyboard.KeyUpEvent, (KeyEventHandler)HandleKeyUpEvent);
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

        private Findwindow _findwin;

        private MainViewModel ViewModel
        {
            get { return DataContext as MainViewModel; }
        }


        private void _initializeMessenger()
        {
            Messenger.Default.Register<CustomMessage>(this, (msg) =>
            {
                // Works with the Person object.
                CustomMessage m = (CustomMessage)msg;
                Debug.WriteLine("MainWindow.CustomMessage: " + m.msgtype);

                switch (m.msgtype)
                {
                    case CustomMessage.MessageType.SELECTED:
                        // editText1 goto first line 
                        if(EditText1.LineCount >= 0)
                            EditText1.ScrollToLine(0);

                        // goto text mode 
                        _changeHtmlMode(false);

                        var node = ViewModel.CurrentExplorerNode;
                        if (node != null && 
                            node.ExplorerType == ExplorerType.File &&
                            node.Name.EndsWith(".html"))
                        {
                            _changeHtmlMode(true);
                            _copyTextToHtml(true, true);
                        }
                        
                        break;

                    case CustomMessage.MessageType.FOLDER_CHANGED:
                        string path = m.str1;
                        SBText1.Text = path;
                        break;

                    case CustomMessage.MessageType.BEFORE_FILE_SAVE:
                        if (EditHtml1._HtmlEditor != null) 
                            ViewModel.EditHtml = EditHtml1._HtmlEditor.InnerHtml;

                        ViewModel.EditText = EditText1.Text;
                        break;

                    case CustomMessage.MessageType.AFTER_FILE_CHANGE_EXTENSION:
                        EditHtmlCommandBtn_Clicked(null, null);
                        break;

                    default:
                        break;
                }
            });
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

            //Debug.WriteLine("edit: " + ViewModel.EditText);            
            ViewModel.EditText1_TextChangedCommand.Execute(e.Source);
        }

        private void HandleKeyUpEvent(object sender, KeyEventArgs e)
        {
            // Ctrl-S 
            if (e.Key == Key.S && 
                   ((Keyboard.Modifiers & ModifierKeys.Control) > 0))
            {
                ViewModel.FileSaveCommand.Execute(e.Source);
            }
            else 
            {
                // EditHtml key pressed. 
                if (ViewModel.HtmlMode)
                {
                    ViewModel.EditHtml1_TextChangedCommand.Execute(e.Source);
                }
            }
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
        
        private void MenuItemStatusBarVisible_Click(object sender, RoutedEventArgs e)
        {
            StatusBar1.Visibility = (sender as MenuItem).IsChecked ? 
                Visibility.Visible : 
                Visibility.Collapsed; 
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

        private void Find_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_findwin == null)
            {
                _findwin = new Findwindow(this);
                _findwin.Owner = this;
            }
            
            _findwin.Show();
        }

        private void Find_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            if (EditText1.IsEnabled)
                e.CanExecute = true;
        }

        private void FindNext_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (_findwin != null)
            {
                _findwin.FindNext();
            }
        }

        private void FindNext_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (EditText1.IsEnabled)
                e.CanExecute = true;
        }

        private void SelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (ViewModel.HtmlMode)
            {
                EditHtml1.TextSelectAll();
            }
            else
            {
                EditText1.Focus();
                EditText1.SelectAll();
            }
        }

        private void SelectAll_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if(EditText1.IsEnabled)
                e.CanExecute = true; 
        }

        private void AddTime_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DateTime time = DateTime.Now;

            string datePatt = "yyyy-MM-dd hh:mm:ss tt";

            if (ViewModel.HtmlMode)
            {
                EditHtml1.SelectedHtml = time.ToString(datePatt);
            }
            else
            {
                EditText1.SelectedText = time.ToString(datePatt);
                EditText1.SelectionStart += EditText1.SelectedText.Length;
                EditText1.SelectionLength = 0;
            }
        }

        private void AddTime_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (EditText1.IsEnabled)
                e.CanExecute = true; 
        }

        private void EditHtmlCommandBtn_Clicked(object sender, RoutedEventArgs e)
        {
            bool goto_htmlmode = EditText1.IsVisible;
            bool treat_as_html = false;

            // if html -> text mode? 
            if (!goto_htmlmode)
            {
                string str = Properties.Resources.msg_htmltotext_modechange_ok;
                MessageBoxResult res = MainViewModel.MessageBoxShow_Question(str);
                if (res == MessageBoxResult.Cancel)
                    return;
            }
            else
            {
                // if text -> html mode? 
                string str = Properties.Resources.msg_text2html_open_as_htmlmode;
                MessageBoxResult res = MainViewModel.MessageBoxShow_Question(str, MessageBoxButton.YesNoCancel);
                if (res == MessageBoxResult.Cancel)
                    return;

                if (res == MessageBoxResult.Yes) 
                    treat_as_html = true;
            }

            _changeHtmlMode(goto_htmlmode);
            _copyTextToHtml(goto_htmlmode, treat_as_html);
        }

        private void _changeHtmlMode(bool goto_htmlmode) 
        {
            var btn = EditHtmlCommandBtn;

            if (goto_htmlmode)
            {
                // get into html mode 
                btn.Background = Brushes.LightGray; ;
                EditText1.Visibility = Visibility.Hidden;
                EditHtml1.Visibility = Visibility.Visible;
            }
            else
            {
                // get into text mode 
                btn.Background = Brushes.Transparent;
                EditText1.Visibility = Visibility.Visible;
                EditHtml1.Visibility = Visibility.Hidden;
            }

            ViewModel.HtmlMode = goto_htmlmode;
        }

        private void _copyTextToHtml(bool direction_text2html, bool treat_as_html = false)
        {
            HtmlEditorControl html = EditHtml1._HtmlEditor;
            if (direction_text2html)
            {
                if (treat_as_html)
                    html.InnerHtml = EditText1.Text;
                else
                    html.InnerText = EditText1.Text;
            }
            else
            {
                EditText1.Text = html.InnerText;
            }
        }

        private void MenuItemAddFavoriteFolder_Clicked(object sender, RoutedEventArgs e)
        {
            UserPreferences up = UserPreferences.Instance;
            var node_e = ViewModel.FirstGeneration.GetEnumerator();
            node_e.MoveNext();
            var node = node_e.Current;
            if (node.ExplorerType == ExplorerType.Folder) 
            {
                up.AddToFavoriteFolders(node.Path);
                up.Save();
                this.MenuItemFavoriteFolders.GetBindingExpression(MenuItem.ItemsSourceProperty).UpdateTarget();
            }
            else
            {
                MainViewModel.MessageBoxShow("Not folder");
            }
        }
        
        /* 
        private void Paste_Excuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ViewModel.HtmlMode)
                EditHtml1.SelectedText = Clipboard.GetText();
            else
                EditText1.SelectedText = Clipboard.GetText();
        }

        private void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Clipboard.ContainsText();
        }
        
        private void Cut_Excuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ViewModel.HtmlMode)
            {
                Clipboard.SetText(EditHtml1.SelectedText);
                EditHtml1.SelectedText = "";
            }
            else
            {
                Clipboard.SetText(EditText1.SelectedText);
                EditText1.SelectedText = "";
            }                
        }

        private void Cut_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = 
                (EditText1.SelectedText.Length > 0) || 
                (EditHtml1.SelectedText.Length > 0);
        }
        
        private void Copy_Excuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ViewModel.HtmlMode)
            {
                Clipboard.SetText(EditHtml1.SelectedText);
            }
            else
            {
                Clipboard.SetText(EditText1.SelectedText);
            }            
        }

        private void Copy_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute =
                (EditText1.SelectedText.Length > 0) ||
                (EditHtml1.SelectedText.Length > 0);
        }
        */

    }
}
