using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;

namespace MemoEditor
{
    class Version
    {
        public static string APP_NAME = Properties.Resources.app_name;

        public static string VERSION = Assembly.GetEntryAssembly().GetName().Version.ToString();

    }
}
