using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Services.UpdateService
{
    public interface IUpdateService : INotifyPropertyChanged
    {
        /// <summary>
        /// Checks for updates and guides the user through the update process.
        /// </summary>
        public Task CheckForUpdatesAsync();

        /// <summary>
        /// A property to check if an update is currently in progress.
        /// </summary>
        bool IsUpdateInProgress { get; }
    }
}
