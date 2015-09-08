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
        public static readonly string FILE_EXTENSION = "txt";

        public static ExplorerNode SelectedNode { get; private set; }

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
                        string filetype = "." + FILE_EXTENSION;

                        newname = value;

                        // if new file extension is not ends with .txt 
                        if (ExplorerType == ExplorerType.File && !newname.ToLower().EndsWith(filetype))
                        {
                            newname += filetype;
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
                            }
                            else if(ExplorerType == ExplorerType.Folder) 
                            {
                                System.IO.Directory.Move(oldpath, newpath);
                            }

                            _name = newname;

                            Path = newpath;

                            RaisePropertyChanged("Name");

                            Messenger.Default.Send(new CustomMessage(CustomMessage.MessageType.RENAME_FILE, oldpath, newpath));
                        }
                        catch (System.IO.FileNotFoundException)
                        {
                            MainViewModel.MessageBoxShow("변경할 수 없습니다.");
                        }
                    }
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
            string filter = "*" + FILE_EXTENSION;

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

                    foreach (string s in Directory.GetFiles(Path, filter))
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
                s = node.Path + "\\" + Properties.Resources.ResourceManager.GetString("memo_filename_prefix") + i.ToString() + "." + FILE_EXTENSION;
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

            // 선택하기 
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
        public bool IsSelected
        {
            get { return _isSelected; }

            set
            {
                Debug.WriteLine("IsSelected: " + this.ToString());

                Set(() => IsSelected, ref _isSelected, value);

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
