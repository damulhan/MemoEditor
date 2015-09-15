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
        // event mapper 
        public RelayCommand OnLoadedCommand { get; private set; }
        public RelayCommand OnClosingCommand { get; private set; }
        public RelayCommand FileNewCommand { get; private set; }
        public RelayCommand FileSaveCommand { get; private set; }
        public RelayCommand EditText1_TextChangedCommand { get; private set; }
        public RelayCommand FolderNewCommand { get; private set; }
        public RelayCommand FileRenameCommand { get; private set; }
        public RelayCommand FileDeleteCommand { get; private set; }
        public RelayCommand HelpInfoCommand { get; private set; }
        public RelayCommand FolderChangeCommand { get; private set; }
        public RelayCommand ExitCommand { get; private set; }
        public RelayCommand SettingCommand { get; private set; }
        public RelayCommand EditHtmlCommand { get; private set; } 

        // data service 
        private readonly IDataService _dataService;

        private UserPreferences _userPrefs;
        
        public IDataService DataService {
            get { return _dataService; }
        }

        private string _title;
        public string Title
        {
            get { 
                return _title; 
            }
            set { 
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

        private bool _textChanged = false;
        private string _editText = "";
        private string _editTextOld = ""; 

        public string EditText
        {
            get { 
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

        public bool EditTextSavable
        {
            get {
                return _textChanged;
            }
        }

        private bool _isEnabledEditText;
        public bool IsEnabledEditText {
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
            get { return _currentExplorerNode;  }
            set { 
                _currentExplorerNode = value;
                RaisePropertyChanged("CurrentExplorerNode");
            }
        }

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
            FolderNewCommand = new RelayCommand(OnFolderNew, () => true);
            FileRenameCommand = new RelayCommand(OnFileRename, () => true);
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
            var firsts = new ObservableCollection<ExplorerNode>();
            foreach (var i in item.FirstGeneration)
            {
                string s = (string)i;
                firsts.Add(new ExplorerNode(s));
            }
            FirstGeneration = firsts;

            // EditText 
            EditText = "";

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

                        // saving old files 
                        OnFileSave();

                        // load files 
                        if (node != null && node.ExplorerType == ExplorerType.File) {
                            _currentExplorerNode = node;
                            FileOpen(node.Path);
                        } else {
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
        
        private void EditTextInit()
        {
            _editTextOld = "";
            EditText = "";
            IsEnabledEditText = false;
            _textChanged = false; 
        }

        private void OnFileNew()
        {
            Debug.WriteLine("File New..");

            OnFileSave();

            Messenger.Default.Send(new CustomMessage(
                CustomMessage.MessageType.CREATE_NEW));
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
            //MessageBox.Show("The Save command was invoked");
            Debug.WriteLine("File Save..");

            if (_textChanged && _currentExplorerNode != null)
            {
                try
                {
                    Debug.WriteLine("Saving.." + EditText);

                    System.IO.File.WriteAllText(_currentExplorerNode.Path, EditText);
                    _editTextOld = EditText;
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

        private void OnFileDelete()
        {
            string messageBoxText = Properties.Resources.ResourceManager.GetString("msg_delete");
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
            string messageBoxText = Version.APP_NAME + "\n"+
                "©2015 greatcorea9000@hanmail.net\n"+
                "ver " + Version.VERSION;
            MessageBoxShow(messageBoxText);
        }

        private void OnEditText1_TextChanged()
        {
            _textChanged = true; 
        }

        private void OnFolderChange()
        {
            System.Windows.Forms.FolderBrowserDialog dialog 
                = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FirstGeneration.Clear();

                Messenger.Default.Send(new CustomMessage(
                    CustomMessage.MessageType.TREEVIEW_DESTROYED));

                // FirstGeneration 
                var firsts = new ObservableCollection<ExplorerNode>();
                var node = new ExplorerNode(dialog.SelectedPath);
                firsts.Add(node);
                FirstGeneration = firsts;
                node.IsExpanded = true;
                node.IsSelected = true;

                // Save in Setting 
                _userPrefs.WorkingFolder = dialog.SelectedPath;
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

                // FirstGeneration 
                var firsts = new ObservableCollection<ExplorerNode>();
                firsts.Add(new ExplorerNode(workingdir));
                FirstGeneration = firsts;
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
            var html_editor = new HtmlEditor2();
            html_editor.Owner = MainWindow.Instance;
            html_editor.Show();
        }

        public static void MessageBoxShow(string msg) 
        {
            string messageBoxText = msg;
            string caption = Version.APP_NAME;
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Information;
            MessageBoxResult result = MessageBoxEx.Show(MainWindow.Instance, messageBoxText, caption, button, icon);
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}
    }
}