using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModToolAvalonia.Models;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.FindReferencesTypesDialog
{
    public partial class FindReferencesResultsViewModel : ViewModelBase, IInitializable<FindReferencesResultsParameters>
    {
        public async Task<bool> InitializeAsync(FindReferencesResultsParameters parameters)
        {
            return true; // Temporary until others are working.
        }
    }
}
