using MemoEditor.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using Utility;

namespace MemoEditor
{
    /// <summary>
    /// Findwindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Findwindow : Window
    {
        private MainWindow _main;
        private TextBox _editText;
        private int _startIndex;

        private bool? ModalDialogResult = null;
        IntPtr ownerHandle;
        IntPtr handle;

        public string FindString { get; private set; }
        public string ReplaceString { get; private set; }

        public Findwindow(MainWindow mainwin)
        {
            _main = mainwin;
            _editText = _main.EditText1;

            InitializeComponent();

            // for window odality operation
            ownerHandle = (new System.Windows.Interop.WindowInteropHelper(mainwin)).Handle;
            handle = (new System.Windows.Interop.WindowInteropHelper(this)).Handle;
            EnableWindow(handle, true);
            SetForegroundWindow(handle); 

            // initial setting 
            TextToFind.Focus();
            DirectionDown.IsChecked = true;
        }

        private void BtnFind_Click(object sender, RoutedEventArgs e)
        {
            FindString = TextToFind.Text;

            Debug.WriteLine("BtnFind_Click");

            FindNext();
        }

        public void FindNext() 
        {
            Debug.WriteLine("FindNext");

            if (_editText.SelectionLength > 0)
                _startIndex = _editText.SelectionStart + _editText.SelectionLength;
            else
                _startIndex = _editText.SelectionStart;

            if (_editText.IsEnabled && FindString != null)
            {
                string str = _editText.Text;
                int index = str.IndexOf(FindString, _startIndex, StringComparison.CurrentCultureIgnoreCase);
                if (index >= 0)
                {
                    _editText.SelectionStart = index;
                    _editText.SelectionLength = FindString.Length;
                    _editText.Focus();                    
                }
                else
                {
                    string findwin_cannot_find_message = Properties.Resources.ResourceManager.GetString("findwin_cannot_find_message");
                    MainViewModel.MessageBoxShow(findwin_cannot_find_message + ": " + FindString);
                }
            }
        }

        private void BtnReplace_Click(object sender, RoutedEventArgs e)
        {
            FindString = TextToFind.Text;
            ReplaceString = TextToReplace.Text;

            Debug.WriteLine("BtnReplace_Click");

            if (_editText.IsEnabled)
            {
                if (_editText.SelectionLength > 0)
                {
                    _editText.SelectedText = ReplaceString;
                }

                FindNext();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ModalDialogResult = false;
            this.Hide();
        }

        private void TextToFind_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnFind.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        public bool? ShowModelessDialog()
        {
            EnableWindow(ownerHandle, false);
            new ShowAndWaitHelper(this).ShowAndWait();
            return ModalDialogResult;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Closing -= new System.ComponentModel.CancelEventHandler(Window_Closing);
            EnableWindow(handle, false);
            EnableWindow(ownerHandle, true);
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        internal sealed class ShowAndWaitHelper
        {
            private readonly Window _window;
            private DispatcherFrame _dispatcherFrame;
            internal ShowAndWaitHelper(Window window)
            {
                if (window == null)
                {
                    throw new ArgumentNullException("panel");
                }
                this._window = window;
            }
            internal void ShowAndWait()
            {
                if (this._dispatcherFrame != null)
                {
                    throw new InvalidOperationException("Cannot call ShowAndWait while waiting for a previous call to ShowAndWait to return.");
                }
                this._window.Closed += new EventHandler(this.OnPanelClosed);
                _window.Show();
                this._dispatcherFrame = new DispatcherFrame();
                Dispatcher.PushFrame(this._dispatcherFrame);
            }
            private void OnPanelClosed(object source, EventArgs eventArgs)
            {
                this._window.Closed -= new EventHandler(this.OnPanelClosed);
                if (this._dispatcherFrame == null)
                {
                    return;
                }
                this._window.Closed -= new EventHandler(this.OnPanelClosed);
                this._dispatcherFrame.Continue = false;
                this._dispatcherFrame = null;
            }

        }

        private void Window_Activated(object sender, EventArgs e)
        {
            TextToFind.Focus();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                BtnCancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            // take down the window to lower side 
            Top = Top + 100;
        }

    }
}
