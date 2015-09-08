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
        public static string REGFILE = "MemoEditor.ini";
        public static string REGSECTION = "MemoEditor";
        public static string REGKEY_WorkingFolder = "WorkingFolder";

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

        // data service 
        private readonly IDataService _dataService;
        
        public IDataService DataService {
            get { return _dataService; }
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
                Debug.WriteLine("EditText new value: " + value);
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
                //Debug.WriteLine("EditTextSavable called: _textChanged " + _textChanged.ToString() + " text:" + (_editText != _editTextOld).ToString()); 
                //return _currentExplorerNode != null && 
                //    _currentExplorerNode.ExplorerType == ExplorerType.File && 
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

            // 
            _initializeMessenger();

            //
            OnLoadedCommand = new RelayCommand(OnLoaded, () => true);
            OnClosingCommand = new RelayCommand(OnClosing, () => true);
            FileNewCommand = new RelayCommand(FileNew, () => true);
            FileSaveCommand = new RelayCommand(FileSave, () => EditTextSavable);
            EditText1_TextChangedCommand = new RelayCommand(EditText1_TextChanged, () => true);
            FolderNewCommand = new RelayCommand(FolderNew, () => true);
            FileRenameCommand = new RelayCommand(FileRename, () => true);
            FileDeleteCommand = new RelayCommand(FileDelete, () => true);
            HelpInfoCommand = new RelayCommand(HelpInfo, () => true);
            FolderChangeCommand = new RelayCommand(FolderChange, () => true);
            ExitCommand = new RelayCommand(Exit, () => true);
            SettingCommand = new RelayCommand(Setting, () => true);
        }
        
        private void _initializeData(DataItem item)
        {
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
                        FileSave();

                        // load files 
                        if (node != null && node.ExplorerType == ExplorerType.File) {
                            _currentExplorerNode = node;
                            FileOpen(node.Path);
                        } else {
                            EditTextInit();
                        }
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

        private void FileNew()
        {
            //MessageBox.Show("The New command was invoked");
            Debug.WriteLine("File New..");

            FileSave();

            Messenger.Default.Send(new CustomMessage(
                CustomMessage.MessageType.CREATE_NEW));
        }

        private void FolderNew()
        {
            //MessageBox.Show("The New command was invoked");
            Debug.WriteLine("New folder ..");

            FileSave();

            Messenger.Default.Send(new CustomMessage(
                CustomMessage.MessageType.CREATE_NEW_FOLDER));
        }
        
        private void FileSave()
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

        private void FileRename()
        {
            Debug.WriteLine("FileRename..");

            FileSave();

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

        private void FileDelete()
        {
            string messageBoxText = "삭제하겠습니까?";
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

        private void HelpInfo()
        {
            string messageBoxText = "MemoEditor\n"+
                "©2015 greatcorea9000@hanmail.net\n"+
                "ver " + Version.VERSION;
            MessageBoxShow(messageBoxText);
        }

        private void EditText1_TextChanged()
        {
            _textChanged = true; 
        }

        private void FolderChange()
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

                // registry에 추가 
                //var reg = new Utility.ModifyRegistry.ModifyRegistry();
                //reg.Write(REGKEY_WorkingFolder, dialog.SelectedPath);

                IniFile ini = new IniFile();
                try
                {
                    ini.AddSection(REGSECTION).AddKey(REGKEY_WorkingFolder).Value = dialog.SelectedPath;
                    ini.Save(REGFILE);
                }
                catch (System.IO.IOException e)
                {
                    MessageBoxShow(e.Message);
                }
            }
        }

        private void OnLoaded()
        {
            /*
            var reg = new Utility.ModifyRegistry.ModifyRegistry();
            string workfolder = reg.Read(REGKEY_WorkingFolder);
            Debug.WriteLine(workfolder);

            if (workfolder != null)
            {
                // FirstGeneration 
                var firsts = new ObservableCollection<ExplorerNode>();
                firsts.Add(new ExplorerNode(workfolder));
                FirstGeneration = firsts;
            }
            */

            IniFile ini = new IniFile();
            try
            {
                ini.Load(REGFILE);
                string workfolder = ini.GetSection(REGSECTION).GetKey(REGKEY_WorkingFolder).GetValue();

                if (workfolder != null)
                {
                    // FirstGeneration 
                    var firsts = new ObservableCollection<ExplorerNode>();
                    firsts.Add(new ExplorerNode(workfolder));
                    FirstGeneration = firsts;
                }
            }
            catch (System.IO.FileNotFoundException e)
            {
                Debug.WriteLine(e.ToString());
                //MessageBoxShow(e.Message);
            }
        }

        private void OnClosing()
        {
            FileSave();
        }
        
        private void Exit()
        {
            OnClosing();
            Application.Current.Shutdown();
        }

        private void Setting()
        {

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