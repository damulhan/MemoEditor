using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using MemoEditor.Model;
using MemoEditor;
using Utility;
using System.Collections;
using System.Windows.Media;
using System.Windows.Controls;
using System;
using System.Linq;

namespace MemoEditor.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        #region Event mapper 
        // event mapper 
        public RelayCommand OnLoadedCommand { get; private set; }
        public RelayCommand OnClosingCommand { get; private set; }
        public RelayCommand FileNewCommand { get; private set; }
        public RelayCommand FileSaveCommand { get; private set; }
        public RelayCommand EditText1_TextChangedCommand { get; private set; }
        public RelayCommand EditHtml1_TextChangedCommand { get; private set; }        
        public RelayCommand FolderNewCommand { get; private set; }
        public RelayCommand FileRenameCommand { get; private set; }
        public RelayCommand FileChangeExtensionTXTCommand { get; private set; }
        public RelayCommand FileChangeExtensionHTMLCommand { get; private set; }
        public RelayCommand SetAsRootCommand { get; private set; }                
        public RelayCommand RunExplorerCommand { get; private set; }                        
        public RelayCommand FileDeleteCommand { get; private set; }
        public RelayCommand HelpInfoCommand { get; private set; }
        public RelayCommand FolderChangeCommand { get; private set; }
        public RelayCommand ExitCommand { get; private set; }
        public RelayCommand SettingCommand { get; private set; }
        public RelayCommand EditHtmlCommand { get; private set; }
        #endregion

        #region Variable Definition 

        // data service 
        private readonly IDataService _dataService;

        private UserPreferences _userPrefs;

        public IDataService DataService
        {
            get { return _dataService; }
        }

        private string _title;
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                RaisePropertyChanged("Title");
            }
        }

        private string _welcomeTitle = string.Empty;

        /// <summary>
        /// Gets the WelcomeTitle property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string WelcomeTitle
        {
            get
            {
                return _welcomeTitle;
            }

            set
            {
                if (_welcomeTitle == value)
                {
                    return;
                }

                _welcomeTitle = value;
                RaisePropertyChanged("WelcomeTitle");
            }
        }

        private ObservableCollection<ExplorerNode> _firstGeneration;
        public ObservableCollection<ExplorerNode> FirstGeneration
        {
            get
            {
                return _firstGeneration;
            }
            set
            {
                if (_firstGeneration == value)
                {
                    return;
                }

                _firstGeneration = value;
                RaisePropertyChanged("FirstGeneration");
            }
        }

        private bool _htmlMode;
        public bool HtmlMode
        {
            get
            {
                return _htmlMode;
            }
            set
            {
                _htmlMode = value;
                RaisePropertyChanged("HtmlMode");
            }
        }

        private bool _textChanged = false;
        private string _editText = "";
        private string _editTextOld = "";

        public string EditText
        {
            get
            {
                return _editText;
            }

            set
            {
                if (_editText != value)
                {
                    _editText = value;
                    RaisePropertyChanged("EditText");
                }
            }
        }

        private string _editHtml = "";
        public string EditHtml
        {
            get
            {
                return _editHtml;
            }

            set
            {
                if (_editHtml != value)
                {
                    _editHtml = value;
                    RaisePropertyChanged("EditHtml");
                }
            }
        }

        public bool EditTextSavable
        {
            get
            {
                return _textChanged;
            }
        }

        private bool _isEnabledEditText;
        public bool IsEnabledEditText
        {
            get { return _isEnabledEditText; }
            set
            {
                _isEnabledEditText = value;
                RaisePropertyChanged("IsEnabledEditText");
            }
        }

        private ExplorerNode _currentExplorerNode = null;

        public ExplorerNode CurrentExplorerNode
        {
            get
            {
                return _currentExplorerNode;
            }
            set
            {
                _currentExplorerNode = value;
                RaisePropertyChanged("CurrentExplorerNode");
            }
        }

        public IEnumerable RecentFiles
        {
            get
            {
                UserPreferences up = UserPreferences.Instance;
                return up.FavoriteFolders.Select((path, index) =>
                        new FavoriteFolderViewModel(this, index, path));
            }
        }

        public bool ShowStatusbar 
        {
            get
            {
                UserPreferences up = UserPreferences.Instance;
                return up.ShowStatusbar;
            }
        }

        #endregion 

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IDataService dataService)
        {
            // Init data service 
            _dataService = dataService;
            _dataService.GetData(
                (item, error) =>
                {
                    if (error != null)
                    {
                        // Report error here
                        return;
                    }

                    _initializeData(item);
                });

            // Init in-between view model messenger 
            _initializeMessenger();

            // Connecting window event with command 
            OnLoadedCommand = new RelayCommand(OnLoaded, () => true);
            OnClosingCommand = new RelayCommand(OnClosing, () => true);
            FileNewCommand = new RelayCommand(OnFileNew, () => true);
            FileSaveCommand = new RelayCommand(OnFileSave, () => EditTextSavable);
            EditText1_TextChangedCommand = new RelayCommand(OnEditText1_TextChanged, () => true);
            EditHtml1_TextChangedCommand = new RelayCommand(OnEditHtml1_TextChanged, () => true);
            FolderNewCommand = new RelayCommand(OnFolderNew, () => true);
            FileRenameCommand = new RelayCommand(OnFileRename, () => true);
            FileChangeExtensionTXTCommand = new RelayCommand(OnFileChangeExtensionTXT, () => true);
            FileChangeExtensionHTMLCommand = new RelayCommand(OnFileChangeExtensionHTML, () => true);
            SetAsRootCommand = new RelayCommand(OnSetAsRoot, () =>
                CurrentExplorerNode != null && 
                CurrentExplorerNode.ExplorerType == ExplorerType.Folder);
            RunExplorerCommand = new RelayCommand(OnRunExplorer, () =>
                CurrentExplorerNode != null &&
                CurrentExplorerNode.ExplorerType == ExplorerType.Folder);
            FileDeleteCommand = new RelayCommand(OnFileDelete, () => true);
            HelpInfoCommand = new RelayCommand(OnHelpInfo, () => true);
            FolderChangeCommand = new RelayCommand(OnFolderChange, () => true);
            ExitCommand = new RelayCommand(OnExit, () => true);
            SettingCommand = new RelayCommand(OnSetting, () => true);
            EditHtmlCommand = new RelayCommand(OnEditHtml, () => true);

            // Loading user preference
            this._userPrefs = UserPreferences.Instance;
        }

        private void _initializeData(DataItem item)
        {
            // Title
            Title = Version.APP_NAME;

            // WelcomeTitle 
            WelcomeTitle = item.Title;

            // FirstGeneration 
            var first_path = item.FirstGeneration.First<string>();
            _folderChange(first_path);

            // EditText 
            EditText = "";
            EditHtml = "";

            // IsEnabled_EditText 
            IsEnabledEditText = false;
        }

        private void _initializeMessenger()
        {
            Messenger.Default.Register<CustomMessage>(this, (msg) =>
            {
                // Works with the Person object.
                CustomMessage m = (CustomMessage)msg;
                Debug.WriteLine("MainViewModel.CustomMessage: " + m.msgtype);

                switch (m.msgtype)
                {
                    case CustomMessage.MessageType.RENAME_FILE:
                        break;

                    case CustomMessage.MessageType.SELECTED:
                        var node = (ExplorerNode)m.obj;

                        //Debug.WriteLine("EditText: " + EditText);
                        // saving old files 
                        OnFileSave();

                        // load files 
                        if (node != null)
                        {
                            _currentExplorerNode = node;
                            if (node.ExplorerType == ExplorerType.File)
                            {
                                FileOpen(node.Path);
                            }
                            else if(node.ExplorerType == ExplorerType.Folder)
                            {
                                string file = node.Path + "\\" + Properties.Resources.str_folder_desc + ".";
                                string file1 = file + ExplorerNode.FILE_EXTENSION1;
                                string file2 = file + ExplorerNode.FILE_EXTENSION2;
                                string folder_desc_file = Properties.Resources.str_folder_desc + "." + ExplorerNode.FILE_EXTENSION1;

                                EditTextInit();

                                if (System.IO.File.Exists(file1))
                                {
                                    FileOpen(file1);
                                }
                                else if (System.IO.File.Exists(file2))
                                {
                                    FileOpen(file2);
                                }
                                else
                                {
                                    OnFileNew(folder_desc_file);
                                }
                                
                            }
                            else 
                            {
                                EditTextInit();
                            }
                        }
                        else
                        {
                            EditTextInit();
                        }

                        // Title change 
                        Title = Version.APP_NAME + " - " + node.Name;
                        break;

                    case CustomMessage.MessageType.CREATED_NEW:
                        break;

                    case CustomMessage.MessageType.CREATED_NEW_FOLDER:
                        break;

                    default:
                        break;
                }
            });
        }


        #region Event functions 

        private void EditTextInit()
        {
            _editTextOld = "";
            EditText = "";
            EditHtml = "";
            IsEnabledEditText = false;
            _textChanged = false;
        }

        private void OnFileNew()
        {
            OnFileNew(null);
        }

        private void OnFileNew(string filename=null)
        {
            Debug.WriteLine("File New..");

            OnFileSave();

            Messenger.Default.Send(new CustomMessage(
                CustomMessage.MessageType.CREATE_NEW, filename));
        }

        private void OnFolderNew()
        {
            Debug.WriteLine("New folder ..");

            OnFileSave();

            Messenger.Default.Send(new CustomMessage(
                CustomMessage.MessageType.CREATE_NEW_FOLDER));
        }

        private void OnFileSave()
        {
            if (_textChanged && _currentExplorerNode != null)
            {

                Debug.WriteLine("File Save..");

                try
                {
                    Messenger.Default.Send(new CustomMessage(
                                    CustomMessage.MessageType.BEFORE_FILE_SAVE));

                    string text = "";
                    if (HtmlMode)
                    {
                        string html_header = "<!DOCTYPE html>\n" +
                                            "<head><meta http-equiv=\"content-type\" content=\"text/html;charset=utf-8\" /></head>\n" +
                                            "<body>\n";
                        if (EditHtml != null && EditHtml.IndexOf("content-type") < 0)
                            text = html_header + EditHtml;
                        else
                            text = EditHtml;
                    }
                    else
                        text = EditText;

                    Debug.WriteLine("Saving.." + text);

                    string descfile = ExplorerNode.GetDescFileName(_currentExplorerNode);
                    if(descfile != null) {
                        string path;
                        if(_currentExplorerNode.ExplorerType == ExplorerType.Folder)
                            path = _currentExplorerNode.Path + "\\" + descfile;
                        else 
                            path = System.IO.Path.GetDirectoryName(_currentExplorerNode.Path) + "\\" + descfile;

                        System.IO.File.WriteAllText(path, text);
                    } else {
                        System.IO.File.WriteAllText(_currentExplorerNode.Path, text);
                    }

                    _editTextOld = text;

                    Messenger.Default.Send(new CustomMessage(
                                    CustomMessage.MessageType.AFTER_FILE_SAVE));
                }
                catch (System.IO.IOException e)
                {
                    MessageBoxShow(e.Message);
                    return;
                }
            }

            _textChanged = false;

        }

        private void FileOpen(string path)
        {
            Debug.WriteLine("FileOpen: " + path);

            // Open new file 
            try
            {
                string text = System.IO.File.ReadAllText(path);

                if (text != EditText)
                {
                    EditText = text;
                    _editTextOld = text;
                }

                // set as editable 
                IsEnabledEditText = true;

                // set text changed to false 
                _textChanged = false;

            }
            catch (System.IO.FileNotFoundException e)
            {
                MessageBoxShow(e.Message);
                return;
            }
        }

        private void OnFileRename()
        {
            Debug.WriteLine("FileRename..");

            OnFileSave();

            //treeView.BeginEdit();

            Key key = Key.F2;

            if (Keyboard.PrimaryDevice != null)
            {
                if (Keyboard.PrimaryDevice.ActiveSource != null)
                {
                    var e = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, key)
                    {
                        RoutedEvent = Keyboard.PreviewKeyDownEvent
                    };
                    InputManager.Current.ProcessInput(e);

                    // Note: Based on your requirements you may also need to fire events for:
                    // RoutedEvent = Keyboard.PreviewKeyDownEvent
                    // RoutedEvent = Keyboard.KeyUpEvent
                    // RoutedEvent = Keyboard.PreviewKeyUpEvent
                }
            }
        }

        private void OnFileChangeExtensionTXT()
        {
            Messenger.Default.Send(new CustomMessage(
                CustomMessage.MessageType.FILE_CHANGE_EXTENSION, "txt", "", null));
        }

        private void OnFileChangeExtensionHTML()
        {
            Messenger.Default.Send(new CustomMessage(
                CustomMessage.MessageType.FILE_CHANGE_EXTENSION, "html", "", null));
        }


        private void OnFileDelete()
        {
            string messageBoxText = Properties.Resources.msg_delete;
            string caption = Version.APP_NAME;
            MessageBoxButton button = MessageBoxButton.OKCancel;
            MessageBoxImage icon = MessageBoxImage.Warning;
            MessageBoxResult result = MessageBoxEx.Show(MainWindow.Instance, messageBoxText, caption, button, icon);

            // Process message box results
            switch (result)
            {
                case MessageBoxResult.OK:
                    string name1 = "";
                    string name2 = "";
                    object obj = null;
                    Messenger.Default.Send(new CustomMessage(
                        CustomMessage.MessageType.DELETE_FILE, name1, name2, obj));
                    break;
                case MessageBoxResult.Cancel:
                    // User pressed Cancel button
                    // ...
                    break;
            }
        }

        private void OnHelpInfo()
        {
            string messageBoxText = Version.APP_NAME + "\n" +
                "©2015 greatcorea9000@hanmail.net\n" +
                "ver " + Version.VERSION;
            MessageBoxShow(messageBoxText);
        }

        private void OnEditText1_TextChanged()
        {
            _textChanged = true;
        }

        private void OnEditHtml1_TextChanged()
        {
            _textChanged = true;
        }

        private void OnFolderChange()
        {
            System.Windows.Forms.FolderBrowserDialog dialog
                = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OnFileSave();

                _folderChange(dialog.SelectedPath);
            }
        }

        private void OnSetAsRoot()
        {
            if (CurrentExplorerNode != null && CurrentExplorerNode.ExplorerType == ExplorerType.Folder)
            {
                OnFileSave();
                
                _folderChange(CurrentExplorerNode.Path);
            }
        }

        private void OnRunExplorer()
        {
            if (CurrentExplorerNode != null && CurrentExplorerNode.ExplorerType == ExplorerType.Folder)
            {
                Process process = new Process();
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.FileName = CurrentExplorerNode.Path;
                process.Start();
            }
        }

        private void _folderChange(string path)
        {
            if (System.IO.Directory.Exists(path))
            {
                if (FirstGeneration != null)
                    FirstGeneration.Clear();

                Messenger.Default.Send(new CustomMessage(
                    CustomMessage.MessageType.TREEVIEW_DESTROYED));

                // FirstGeneration 
                var firsts = new ObservableCollection<ExplorerNode>();
                var node = new ExplorerNode(path);
                firsts.Add(node);
                FirstGeneration = firsts;
                node.IsExpanded = true;
                node.IsSelected = true;

                Messenger.Default.Send(new CustomMessage(
                    CustomMessage.MessageType.FOLDER_CHANGED, path, null, null));

                // Save in Setting 
                if (_userPrefs != null)
                    _userPrefs.WorkingFolder = path;
            }
            else
            {
                MessageBoxShow(Properties.Resources.msg_no_folder_exsits);
                return;
            }
        }

        private void OnLoaded()
        {
            // Make initial tree node 
            try
            {
                var workingdir = _userPrefs.WorkingFolder;

                if (workingdir == "")
                {
                    //workingdir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    workingdir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                // Folder change 
                _folderChange(workingdir);
            }
            catch (System.IO.FileNotFoundException e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        private void OnClosing()
        {
            OnFileSave();
        }

        private void OnExit()
        {
            OnClosing();
            Application.Current.Shutdown();
        }

        private void OnSetting()
        {

        }

        private void OnEditHtml()
        {
            /*
            var html_editor = new HtmlEditor2();
            html_editor.Owner = MainWindow.Instance;
            html_editor.Show();
            */
            //EditText1.Hide();
            //EditHtml1.Show();
        }

        #endregion


        #region Utility 

        public static MessageBoxResult MessageBoxShow(string msg)
        {
            string messageBoxText = msg;
            string caption = Version.APP_NAME;
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Information;
            MessageBoxResult result = MessageBoxEx.Show(MainWindow.Instance, messageBoxText, caption, button, icon);
            return result;
        }

        public static MessageBoxResult MessageBoxShow_Question(string msg, MessageBoxButton button = MessageBoxButton.OKCancel)
        {
            string messageBoxText = msg;
            string caption = Version.APP_NAME;
            MessageBoxImage icon = MessageBoxImage.Question;
            MessageBoxResult result = MessageBoxEx.Show(MainWindow.Instance, messageBoxText, caption, button, icon);
            return result;
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}

        #endregion 

        public class FavoriteFolderViewModel
        {
            private int _index;
            private string _path;
            private MainViewModel _vm;

            public FavoriteFolderViewModel(MainViewModel vm, int index, string path)
            {
                _vm = vm;
                _index = index;
                _path = path;
            }

            public string Name 
            {
                get {
                    var _name = _path.Substring(_path.LastIndexOf("\\") + 1);
                    return string.Format("_{0} - {1}", _index + 1, _name);
                }
            }

            public RelayCommand Open 
            {
                get
                {
                    return new RelayCommand(() => { 
                        // open path; 
                        _vm._folderChange(_path);
                    }, () => true);
                }
            }
        }

    }
}