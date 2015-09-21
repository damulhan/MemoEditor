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
                        CreateNew();
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
                            string str = Properties.Resources.ResourceManager.GetString("msg_cannot_change");
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
            string filter1 = "*" + FILE_EXTENSION1;
            string filter2 = "*" + FILE_EXTENSION2;

            if (ExplorerType != ExplorerType.Folder)
                return;

            ExplorerNode dummyExplorerNode = new ExplorerNode { ExplorerType = ExplorerType.Dummy };

            if (Children.Count == 1 && Children[0].ExplorerType == ExplorerType.Dummy)
            {
                Children.Clear();
                try
                {
                    foreach (string s in Directory.GetDirectories(Path))
                    {
                        Children.Add(new ExplorerNode
                                        {
                                            Name = s.Substring(s.LastIndexOf("\\") + 1),
                                            Path = s,
                                            ExplorerType = ExplorerType.Folder,
                                            Children = new ObservableCollection<ExplorerNode> { dummyExplorerNode },
                                            Parent = this,
                                        });
                    }

                    foreach (string s in Directory.GetFiles(Path, filter1))
                    {
                        Children.Add(new ExplorerNode
                                        {
                                            Name = s.Substring(s.LastIndexOf("\\") + 1),
                                            Path = s,
                                            ExplorerType = ExplorerType.File,
                                            Parent = this,
                                        });
                    }

                    foreach (string s in Directory.GetFiles(Path, filter2))
                    {
                        Children.Add(new ExplorerNode
                        {
                            Name = s.Substring(s.LastIndexOf("\\") + 1),
                            Path = s,
                            ExplorerType = ExplorerType.File,
                            Parent = this,
                        });
                    }

                }
                catch (Exception e) {
                    Debug.Write(e.ToString());
                }
            }
        }

        private ExplorerNode CreateNew()
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
            int i = 1;
            do
            {
                s = node.Path + "\\" + Properties.Resources.ResourceManager.GetString("memo_filename_prefix") + i.ToString() + "." + FILE_EXTENSION1;
                if (!File.Exists(s)) break;
                i++;
            } while (true);

            System.IO.File.WriteAllText(s, "");

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

            // Selecting node 
            node.IsExpanded = true;
            childnode.IsSelected = true;

            return childnode;
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
                s = node.Path + "\\" + Properties.Resources.ResourceManager.GetString("memo_foldername_prefix") 
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
                    if (node.ExplorerType == ExplorerType.Folder) 
                        Directory.Delete(node.Path);
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
