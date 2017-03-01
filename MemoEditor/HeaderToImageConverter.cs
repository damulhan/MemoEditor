using MemoEditor.ViewModel;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MemoEditor
{
    [ValueConversion(typeof(string), typeof(bool))]
    public class HeaderToImageConverter : IValueConverter
    {
        public static HeaderToImageConverter Instance = new HeaderToImageConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var node = value as ExplorerNode;
            if (node == null)
                return null;

            // This code bring about design view failure; after design time, 
            // fetching icon return error; how to fix it?? 
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return null;

            if (node.ExplorerType == ExplorerType.File)
            {
                return GetIconForPath(node.Path);
            }
            else if (node.ExplorerType == ExplorerType.Drive)
            {
                var uri = new Uri("pack://application:,,,./Resources/diskdrive.png");
                var source = new BitmapImage(uri);
                return source;
            }
            else
            {
                var uri = new Uri("pack://application:,,,./Resources/folder.png");
                var source = new BitmapImage(uri);
                return source;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert back");
        }

        private static ImageSource GetIconForPath(string path)
        {
            if (File.Exists(path))
            {
                using (System.Drawing.Icon sysicon = System.Drawing.Icon.ExtractAssociatedIcon(path))
                {
                    if (sysicon != null)
                    {
                        var icon = Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        return icon;
                    }
                }
            }
            return null;
        }
    }
}

