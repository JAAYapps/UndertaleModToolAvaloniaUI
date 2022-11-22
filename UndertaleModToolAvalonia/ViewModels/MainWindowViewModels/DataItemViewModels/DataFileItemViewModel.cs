using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.Models;
using UndertaleModToolAvalonia.ViewModels;

namespace UndertaleModToolAvalonia.ViewModels.MainWindowViewModels.DataItemViewModels
{
    public class DataFileItemViewModel : ViewModelBase
    {
        public DataFileItemViewModel(IEnumerable<DataFileItem> items)
        {
            Items = new ObservableCollection<DataFileItem>(items);
        }

        public ObservableCollection<DataFileItem> Items { get; }
    }
}
