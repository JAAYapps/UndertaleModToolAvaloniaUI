using System;
using CommunityToolkit.Mvvm.ComponentModel;
using UndertaleModToolAvalonia.Models;

namespace UndertaleModToolAvalonia.ViewModels.StartPageViewModels.DataItemViewModels
{
    public partial class DataFileItemViewModel : ViewModelBase
    {
        /// <summary>
        /// Creates a new DataFileItemViewModel for the given <see cref="Models.DataFileItem"/>
        /// </summary>
        /// <param name="item">The item to load</param>
        public DataFileItemViewModel(DataFileItem item)
        {
            Name = item.Name;
            Preview = item.Preview;
        }

        [ObservableProperty] public string name = String.Empty;

        [ObservableProperty] public string preview = String.Empty;
        
        /// <summary>
        /// Gets a DataFileItem of this ViewModel
        /// </summary>
        /// <returns>The DataFileItem</returns>
        public DataFileItem GetDataFileItem()
        {
            return new DataFileItem()
            {
                Name = Name,
                Preview = Preview
            };
        }
    }
}
