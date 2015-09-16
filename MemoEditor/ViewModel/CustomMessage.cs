using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GalaSoft.MvvmLight.Messaging;

namespace MemoEditor.ViewModel
{
    class CustomMessage 
    {
        public enum MessageType
        {
            RENAME_FILE = 1,
            SELECTED,
            OPENED,
            CLOSED,
            CREATE_NEW,
            CREATED_NEW,
            CREATE_NEW_FOLDER,
            CREATED_NEW_FOLDER,
            DELETE_FILE,
            TREEVIEW_DESTROYED,
            BEFORE_FILE_SAVE,
            AFTER_FILE_SAVE,
        };

        public MessageType msgtype;
        public string str1;
        public string str2;
        public Object obj;

        public CustomMessage(MessageType msgtype, string str1="", string str2="", Object obj=null) 
        {
            this.msgtype = msgtype;
            this.str1 = str1;
            this.str2 = str2;
            this.obj = obj;
        }
    }
}
