using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MemoEditor.Model
{
    public class DataItem
    {
        List<string> _firstGeneration; 
        public DataItem(string title)
        {
            Title = title;

            _initializeData();
        }

        private void _initializeData() {
           
            var dir = Directory.GetCurrentDirectory();
           // var dir = "c:\\";
           // var dir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

           _firstGeneration = new List<string>();
           _firstGeneration.Add(dir);
        }

        public string Title
        {
            get;
            private set;
        }

        public List<string> FirstGeneration
        {
            get { return _firstGeneration; }
            set { _firstGeneration = value; }
        }
    }
}
