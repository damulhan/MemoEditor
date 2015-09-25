using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using GalaSoft.MvvmLight;
using System.Collections.Specialized;

namespace MemoEditor
{
    public class UserPreferences : ViewModelBase
    {
        private static UserPreferences _instance;

        #region Member Variables

        private double _windowTop;
        private double _windowLeft;
        private double _windowHeight;
        private double _windowWidth;
        private System.Windows.WindowState _windowState;
        private string _workingFolder;
        private double _fontSize;
        private string _fontFamily;
        private bool _showStatusbar;
        private System.Collections.Specialized.StringCollection _favoriteFolders;

        #endregion //Member Variables

        #region Public Properties

        public double WindowTop
        {
            get { return _windowTop; }
            set { _windowTop = value; }
        }

        public double WindowLeft
        {
            get { return _windowLeft; }
            set { _windowLeft = value; }
        }

        public double WindowHeight
        {
            get { return _windowHeight; }
            set { _windowHeight = value; }
        }

        public double WindowWidth
        {
            get { return _windowWidth; }
            set { _windowWidth = value; }
        }

        public System.Windows.WindowState WindowState
        {
            get { return _windowState; }
            set { _windowState = value; }
        }

        public string WorkingFolder
        {
            get { return _workingFolder; }
            set { _workingFolder = value; }
        }

        public double FontSize 
        {
            get { return _fontSize; }
            set { _fontSize = value; }
        }
        public string FontFamily
        {
            get { return _fontFamily; }
            set { _fontFamily = value; }
        }

        public IEnumerable<string> FavoriteFolders
        {
            get {
                var col = (_favoriteFolders != null) ? 
                    _favoriteFolders : 
                    new StringCollection();
                return col.OfType<string>();
            }
        }
        public bool ShowStatusbar
        {
            get { return _showStatusbar; }
            set { _showStatusbar = value; }
        }

        public void AddToFavoriteFolders(string str)
        {
            _favoriteFolders.Insert(0, str);
            while (_favoriteFolders.Count > 10) {
                _favoriteFolders.RemoveAt(10);
            }

            RaisePropertyChanged("FavoriteFolders");
        }

        #endregion //Public Properties

        #region Constructor

        public UserPreferences()
        {
            //Load the settings
            Load();

            //Size it to fit the current screen
            SizeToFit();

            //Move the window at least partially into view
            MoveIntoView();
        }

        public static UserPreferences Instance
        {
            get {
                if (_instance == null)
                {
                    _instance = new UserPreferences();
                }

                return _instance;  
            }
        }

        #endregion //Constructor

        #region Functions

        /// <summary>
        /// If the saved window dimensions are larger than the current screen shrink the
        /// window to fit.
        /// </summary>
        public void SizeToFit()
        {
            if (_windowHeight > System.Windows.SystemParameters.VirtualScreenHeight)
            {
                _windowHeight = System.Windows.SystemParameters.VirtualScreenHeight;
            }

            if (_windowWidth > System.Windows.SystemParameters.VirtualScreenWidth)
            {
                _windowWidth = System.Windows.SystemParameters.VirtualScreenWidth;
            }
        }

        /// <summary>
        /// If the window is more than half off of the screen move it up and to the left 
        /// so half the height and half the width are visible.
        /// </summary>
        public void MoveIntoView()
        {
            if (_windowTop + _windowHeight / 2 > System.Windows.SystemParameters.VirtualScreenHeight)
            {
                _windowTop = System.Windows.SystemParameters.VirtualScreenHeight - _windowHeight;
            }

            if (_windowLeft + _windowWidth / 2 > System.Windows.SystemParameters.VirtualScreenWidth)
            {
                _windowLeft = System.Windows.SystemParameters.VirtualScreenWidth - _windowWidth;
            }

            if (_windowTop < 0)
            {
                _windowTop = 0;
            }

            if (_windowLeft < 0)
            {
                _windowLeft = 0;
            }
        }


        private void Load()
        {
            _windowTop = Properties.Settings.Default.WindowTop;
            _windowLeft = Properties.Settings.Default.WindowLeft;
            _windowHeight = Properties.Settings.Default.WindowHeight;
            _windowWidth = Properties.Settings.Default.WindowWidth;
            _windowState = Properties.Settings.Default.WindowState;
            _workingFolder = Properties.Settings.Default.WorkingFolder;
            _fontSize = Properties.Settings.Default.FontSize;
            _fontFamily = Properties.Settings.Default.FontFamily;
            _favoriteFolders = Properties.Settings.Default.FavoriteFolders;
            if(_favoriteFolders == null)
                _favoriteFolders = new StringCollection();
            _showStatusbar = Properties.Settings.Default.ShowStatusbar;
        }
        

        public void Save()
        {
            if (_windowState != System.Windows.WindowState.Minimized)
            {
                Debug.WriteLine("UserPref: _windowTop: " + _windowTop);
                Properties.Settings.Default.WindowTop = _windowTop;

                Debug.WriteLine("UserPref: _windowLeft: " + _windowLeft);
                Properties.Settings.Default.WindowLeft = _windowLeft;

                Debug.WriteLine("UserPref: _windowHeight: " + _windowHeight);
                Properties.Settings.Default.WindowWidth = _windowWidth;

                Debug.WriteLine("UserPref: _windowWidth: " + _windowWidth);
                Properties.Settings.Default.WindowHeight = _windowHeight;

                Debug.WriteLine("UserPref: _windowState: " + _windowState);
                Properties.Settings.Default.WindowState = _windowState;

                Debug.WriteLine("UserPref: _workingFolder: " + _workingFolder);
                Properties.Settings.Default.WorkingFolder = _workingFolder;

                Debug.WriteLine("UserPref: _fontSize: " + _fontSize);
                Properties.Settings.Default.FontSize = _fontSize;

                Debug.WriteLine("UserPref: _fontFamily: " + _fontFamily);
                Properties.Settings.Default.FontFamily = _fontFamily;

                Debug.WriteLine("UserPref: _favoriteFolders: " + _favoriteFolders);
                Properties.Settings.Default.FavoriteFolders = _favoriteFolders;

                Debug.WriteLine("UserPref: _showStatusBar: " + ShowStatusbar);
                Properties.Settings.Default.ShowStatusbar = ShowStatusbar;
                
                Properties.Settings.Default.Save();
            }
        }

        #endregion //Functions

    }
}
