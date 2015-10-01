using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.IO;
using GalaSoft.MvvmLight;
using System.Diagnostics;
using GalaSoft.MvvmLight.Messaging;
using System.Windows;

namespace MemoEditor.ViewModel
{
    public class ExplorerNode : ViewModelBase
    {
        public static readonly ExplorerNode dummyExplorerNode = new ExplorerNode();
        public static readonly string FILE_EXTENSION1 = "txt";
        public static readonly string FILE_EXTENSION2 = "html";

        public static ExplorerNode SelectedNode { get; private set; }
        public static ExplorerNode PreviousNode { get; private set; }

        public ExplorerNode Parent { get; private set; }

        private string _name;

        public ExplorerNode()
        {
            ExplorerType = ExplorerType.Dummy;
        }

        public ExplorerNode(string path)
        {
            _name = path.Substring(path.LastIndexOf("\\") + 1);
            if (_name == "") { _name = "(NULL)";  }
            Path = path;
            ExplorerType = ExplorerType.Folder;
            Children = new ObservableCollection<ExplorerNode> { dummyExplorerNode };

            ReinitializeMessenger();
        }
        
        public void ReinitializeMessenger()
        {
            _unregisterMessenger();
            _initializeMessenger();
        }

        public void _unregisterMessenger()
        {
            Messenger.Default.Unregister<CustomMessage>(this);
        }

        private void _initializeMessenger()
        {
            Messenger.Default.Register<CustomMessage>(this, (msg) =>
            {
                // Works with the Person object.
                CustomMessage m = (CustomMessage)msg;
                Debug.WriteLine("ExplorerNode.CustomMessage: " + m.msgtype);

                switch (m.msgtype)
                {
                    case CustomMessage.MessageType.CREATE_NEW:
                        CreateNew(m.str1);
                        break;
                    case CustomMessage.MessageType.CREATE_NEW_FOLDER:
                        CreateNewFolder();
                        break;
                    case CustomMessage.MessageType.DELETE_FILE:
                        DeleteFileOrFolder();
                        break;
                    case CustomMessage.MessageType.TREEVIEW_DESTROYED:
                        _unregisterMessenger();
                        SelectedNode = null;
                        break;
                    case CustomMessage.MessageType.FILE_CHANGE_EXTENSION:
                        if(m.str1 == "txt") 
                            _renameExtensionToTxt();
                        else
                            _renameExtensionToHtml();
                        break;
                    default:
                        break;
                }
            });
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name == null)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                } 
                else if (_name != value)
                {
                    if (Path != null)
                    {
                        string path, oldpath, newpath, newname;
                        string filetype1 = "." + FILE_EXTENSION1;
                        string filetype2 = "." + FILE_EXTENSION2;

                        newname = value;

                        // if new file extension is not ends with .txt 
                        if (ExplorerType == ExplorerType.File && 
                            !newname.ToLower().EndsWith(filetype1) &&
                            !newname.ToLower().EndsWith(filetype2))
                        {
                            newname += filetype1;
                        }

                        path = Path.Substring(0, Path.LastIndexOf("\\"));
                        oldpath = path + "\\" + _name;
                        newpath = path + "\\" + newname;

                        try
                        {
                            Debug.WriteLine("file renamed: {0} {1}", oldpath, newpath);

                            if (ExplorerType == ExplorerType.File)
                            {
                                System.IO.File.Move(oldpath, newpath);

                                _name = newname;
                                Path = newpath;
                            }
                            else if(ExplorerType == ExplorerType.Folder) 
                            {
                                System.IO.Directory.Move(oldpath, newpath);

                                _name = newname;
                                Path = newpath;

                                //IsExpanded = false;

                                // Represh children 
                                Children.Clear();
                                ExplorerNode childnode = new ExplorerNode
                                {
                                    Name = ".",
                                    Path = "\\.",
                                    ExplorerType = ExplorerType.Dummy,
                                    Children = new ObservableCollection<ExplorerNode> { dummyExplorerNode },
                                    Parent = this,
                                };
                                Children.Add(childnode);

                                //IsExpanded = true;
                                Expand();
                            }

                            RaisePropertyChanged("Name");

                            Messenger.Default.Send(new CustomMessage(CustomMessage.MessageType.RENAME_FILE, oldpath, newpath));
                        }
                        catch (System.IO.FileNotFoundException)
                        {
                            string str = Properties.Resources.msg_cannot_change;
                            MainViewModel.MessageBoxShow(str);
                        }
                        catch (System.IO.IOException e)
                        {
                            MainViewModel.MessageBoxShow(e.ToString());
                        }
                    }
                }
            }
        }

        private void _renameExtensionToTxt() 
        {
            //MessageBox.Show("_renameExtensionToTxt");
            var node = SelectedNode;
            if (node != null && node.ExplorerType == ExplorerType.File) 
            {
                if (node.Name.ToLower().EndsWith(".html"))
                {
                    var newname = node.Name.Substring(0, node.Name.LastIndexOf(".html")) + ".txt";
                    node.Name = newname;

                    Messenger.Default.Send(new CustomMessage(CustomMessage.MessageType.AFTER_FILE_CHANGE_EXTENSION, "txt", ""));
                }
            }
        }

        private void _renameExtensionToHtml()
        {
            //MessageBox.Show("_renameExtensionToHtml");
            var node = SelectedNode;
            if (node != null && node.ExplorerType == ExplorerType.File)
            {
                if (node.Name.ToLower().EndsWith(".txt"))
                {
                    var newname = node.Name.Substring(0, node.Name.LastIndexOf(".txt")) + ".html";
                    node.Name = newname;

                    Messenger.Default.Send(new CustomMessage(CustomMessage.MessageType.AFTER_FILE_CHANGE_EXTENSION, "html", ""));
                }
            }
        }

        public string Path { get; set; }

        public ExplorerType ExplorerType { get; set; }

        public ObservableCollection<ExplorerNode> Children { get; set; }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                Set(() => this.IsExpanded, ref _isExpanded, value);

                //Debug.WriteLine("IsExpanded: " + this.ToString());

                if (value) Expand();
            }
        }

        private void Expand()
        {
            string filter1 = "*." + FILE_EXTENSION1;
            string filter2 = "*." + FILE_EXTENSION2;

            if (ExplorerType != ExplorerType.Folder)
                return;

            ExplorerNode dummyExplorerNode = new ExplorerNode { ExplorerType = ExplorerType.Dummy };

            //if (Children.Count == 1 && Children[0].ExplorerType == ExplorerType.Dummy)
            if(true)
            {
                Children.Clear();
                try
                {
                    foreach (string s in Directory.GetDirectories(Path))
                    {
                        string filename = s.Substring(s.LastIndexOf("\\") + 1);
                        string path = s;

                        Children.Add(new ExplorerNode
                                        {
                                            Name = filename,
                                            Path = path,
                                            ExplorerType = ExplorerType.Folder,
                                            Children = new ObservableCollection<ExplorerNode> { dummyExplorerNode },
                                            Parent = this,
                                        });
                    }

                    List<ExplorerNode> _children = new List<ExplorerNode>();

                    foreach (string s in Directory.GetFiles(Path, filter1))
                    {
                        string filename = s.Substring(s.LastIndexOf("\\") + 1);
                        string path = s;
                        string descfile = Properties.Resources.str_folder_desc + "." + ExplorerNode.FILE_EXTENSION1;

                        if (filename == descfile)
                            continue;

                        _children.Add(new ExplorerNode
                                        {
                                            Name = filename,
                                            Path = path,
                                            ExplorerType = ExplorerType.File,
                                            Parent = this,
                                        });
                    }

                    foreach (string s in Directory.GetFiles(Path, filter2))
                    {
                        string filename = s.Substring(s.LastIndexOf("\\") + 1);
                        string path = s;
                        string descfile = Properties.Resources.str_folder_desc + "." + ExplorerNode.FILE_EXTENSION2;

                        if (filename == descfile)
                            continue;

                        _children.Add(new ExplorerNode
                        {
                            Name = filename,
                            Path = path,
                            ExplorerType = ExplorerType.File,
                            Parent = this,
                        });
                    }

                    _children.Sort(delegate(ExplorerNode t1, ExplorerNode t2)
                    {
                        return t1.Name.CompareTo(t2.Name); 
                    });

                    foreach (var item in _children)
                        Children.Add(item);
                }
                catch (Exception e) {
                    Debug.Write(e.ToString());
                }
            }
        }

        public static string GetDescFileName(ExplorerNode node)
        {
            string filename = null;
            if (node != null)
            {
                if (node.ExplorerType == ExplorerType.Folder)
                {
                    string dirname = node.Path + "\\";
                    string file1 = Properties.Resources.str_folder_desc + "." + ExplorerNode.FILE_EXTENSION1;
                    string file2 = Properties.Resources.str_folder_desc + "." + ExplorerNode.FILE_EXTENSION2;
                    
                    if (System.IO.File.Exists(dirname + file1))
                    {
                        filename = file1;
                    }
                    else if (System.IO.File.Exists(dirname + file2))
                    {
                        filename = file2;
                    }
                }

            }

            return filename;
        }

        private ExplorerNode CreateNew(string filename = null)
        {
            Debug.WriteLine("CreateNew");

            if (SelectedNode == null)
                return null;

            ExplorerNode node = SelectedNode;

            if (node != null && node.ExplorerType != ExplorerType.Folder)
                node = node.Parent;

            // return to default
            if (node == null)
                node = this;

            if (node.Children.Count == 1 && node.Children[0].ExplorerType == ExplorerType.Dummy)
                node.Expand();

            string s = node.Path;
            if (filename != null)
            {
                s = node.Path + "\\" + filename;
            }
            else
            {
                int i = 1;
                do
                {
                    s = node.Path + "\\" + Properties.Resources.memo_filename_prefix + i.ToString() + "." + FILE_EXTENSION1;
                    if (!File.Exists(s)) break;
                    i++;
                } while (true);
            }

            System.IO.File.WriteAllText(s, "");

            string descfile = Properties.Resources.str_folder_desc + "." + ExplorerNode.FILE_EXTENSION1;
            if (descfile != filename)
            {
                ExplorerNode childnode = new ExplorerNode
                {
                    Name = s.Substring(s.LastIndexOf("\\") + 1),
                    Path = s,
                    ExplorerType = ExplorerType.File,
                    Parent = node,
                };

                node.Children.Add(childnode);

                Messenger.Default.Send(new CustomMessage(
                    CustomMessage.MessageType.CREATED_NEW, childnode.Name, childnode.Path, childnode));

                childnode.IsSelected = true;

                // Selecting node 
                node.IsExpanded = true;

                return childnode;
            }
            else
            {
                // Selecting node 
                node.IsExpanded = true;

                return null;
            }
        }

        private ExplorerNode CreateNewFolder()
        {
            if (SelectedNode == null)
                return null;

            ExplorerNode node = SelectedNode;

            if (node != null && node.ExplorerType != ExplorerType.Folder)
                node = node.Parent;

            // return to default
            if (node == null)
                node = this;

            if (node.Children.Count == 1 && node.Children[0].ExplorerType == ExplorerType.Dummy)
                node.Expand();

            //Debug.WriteLine(Path);

            string s = node.Path;
            int i = 1;
            do
            {
                s = node.Path + "\\" + Properties.Resources.memo_foldername_prefix 
                    + i.ToString();
                if( !Directory.Exists(s) ) break;
                i++;
            } while (true);

            Directory.CreateDirectory(s);

            ExplorerNode childnode = new ExplorerNode
            {
                Name = s.Substring(s.LastIndexOf("\\") + 1),
                Path = s,
                ExplorerType = ExplorerType.Folder,
                Children = new ObservableCollection<ExplorerNode> { dummyExplorerNode },
                Parent = node,
            };

            node.Children.Add(childnode);

            Messenger.Default.Send(new CustomMessage(
                CustomMessage.MessageType.CREATED_NEW_FOLDER, childnode.Name, childnode.Path, childnode));

            // Selecting  
            node.IsExpanded = true;
            childnode.IsSelected = true; 

            return childnode;
        }

        private void DeleteFileOrFolder()
        {
            if (SelectedNode == null)
                return;

            var node = SelectedNode;

            if (node != null)
            {
                try 
                { 
                    if (node.ExplorerType == ExplorerType.Folder) {

                        // delete desc file 
                        string descfilepath = node.Path + "\\" + GetDescFileName(node);
                        Collection<string> files = new Collection<string>();
                        foreach (string s in Directory.GetFiles(node.Path)) files.Add(s);
                        if (files.Count == 1 && files[0] == descfilepath)
                        {
                            try
                            {
                                File.Delete(descfilepath);
                            }
                            catch (IOException e) {
                                MainViewModel.MessageBoxShow(e.Message);
                            }
                        }

                        // delete folder 
                        Directory.Delete(node.Path);
                    }                        
                    else if (node.ExplorerType == ExplorerType.File)
                        File.Delete(node.Path);
                }
                catch (IOException e) {
                    MainViewModel.MessageBoxShow(e.Message);
                    return;
                }

                if (node.Parent != null)
                {
                    ExplorerNode parent = node.Parent;
                    parent.Children.Remove(node);
                    parent.IsSelected = true;
                }

                node = null;
            }
        }

        private bool _isSelected;
        private object p;
        public bool IsSelected
        {
            get { return _isSelected; }

            set
            {
                Debug.WriteLine("IsSelected: " + this.ToString());

                Set(() => IsSelected, ref _isSelected, value);

                PreviousNode = SelectedNode;                
                SelectedNode = this;

                // send message 
                Messenger.Default.Send(new CustomMessage(
                    CustomMessage.MessageType.SELECTED, "", "", this));
            }
        }

        public override string ToString() 
        {
            //return "ExplorerNode[name:" + Name + ",path:" + Path + ",type:" + ExplorerType.ToString() + "]";
            return "ExplorerNode[name:" + Name + "]";
        }
        
    }
}
