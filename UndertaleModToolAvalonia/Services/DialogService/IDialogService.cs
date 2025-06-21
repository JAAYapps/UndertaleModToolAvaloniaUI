using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Services.DialogService
{
    public interface IDialogService
    {
        public Task ShowDialogAsync<T>();
    }
}
