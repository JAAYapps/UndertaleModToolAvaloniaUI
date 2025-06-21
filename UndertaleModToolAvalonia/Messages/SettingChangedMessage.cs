using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Messages
{
    public class SettingChangedMessage
    {
        public string SettingName { get; }
        public object NewValue { get; }

        public SettingChangedMessage(string settingName, object newValue)
        {
            SettingName = settingName;
            NewValue = newValue;
        }
    }
}
