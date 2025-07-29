using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    /// <summary>Stores the information about the tab with a general info.</summary>
    public partial class UndertaleGeneralInfoEditorViewModel : EditorContentViewModel
    {
        /// <summary>The selected room.</summary>
        [ObservableProperty]
        public object selectedRoom;

        /// <summary>The scroll position of the room list grid.</summary>
        [ObservableProperty]
        public double roomListScrollPosition;

        /// <summary>Whether the room list is expanded.</summary>
        [ObservableProperty]
        public bool isRoomListExpanded;

        [ObservableProperty]
        public UndertaleGeneralInfo info;

        [ObservableProperty]
        public UndertaleOptions options;

        [ObservableProperty]
        public UndertaleLanguage language;

        public UndertaleGeneralInfoEditorViewModel(string title, UndertaleGeneralInfo generalInfo, UndertaleOptions options, UndertaleLanguage language) : base(title)
        {
            Info = generalInfo;
            Options = options;
            Language = language;
        }
    }
}
