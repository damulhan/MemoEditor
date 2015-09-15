using System;
using System.Collections.Generic;
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
        public HtmlEditor()
        {
            InitializeComponent();

            HtmlEditor1.ToolbarDock = DockStyle.Top;
        }
    }
}
