using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MSDN.Html.Editor
{
    /// <summary>
    /// HtmlEditor.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class HtmlEditor : System.Windows.Controls.UserControl
    {

        public MSDN.Html.Editor.HtmlEditorControl _HtmlEditor { get; set; }

        public HtmlEditor()
        {
            InitializeComponent();

            this._HtmlEditor = HtmlEditor1;

            //-- Initialize 
            HtmlEditor1.ToolbarDock = DockStyle.Top;

            // loading default css 
            string cssFile = System.IO.Path.GetFullPath(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\Resources\default.css");
            if (File.Exists(cssFile))
            {
                MSDN.Html.Editor.HtmlEditorControl editor = HtmlEditor1;
                editor.StylesheetUrl = cssFile;
            }
        }

        public string InnerHtml 
        {
            get
            {
                if (_HtmlEditor != null)
                    return _HtmlEditor.InnerHtml;
                else
                    return "";
            }
            set
            {
                _HtmlEditor.InnerHtml = value;
            }
        }

        public string InnerText
        {
            get
            {
                if (_HtmlEditor != null)
                    return _HtmlEditor.InnerText;
                else
                    return "";
            }
            set
            {
                _HtmlEditor.InnerText = value;
            }
        }
    }
}
