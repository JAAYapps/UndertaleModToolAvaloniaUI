using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.Models.EditorModels;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class DescriptionViewModel : EditorContentViewModel
    {
        [ObservableProperty]
        private string heading;

        [ObservableProperty]
        private string description;

        public DescriptionViewModel(string title, Description description) : base(title)
        {
            Heading = description.Heading;
            Description = description.Message;
        }
    }
}
