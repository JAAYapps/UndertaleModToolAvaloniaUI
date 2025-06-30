using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.ViewModels
{
    /// <summary>
    /// Defines a contract for a ViewModel that can be initialized with parameters.
    /// </summary>
    /// <typeparam name="TParams">The type of the parameters object.</typeparam>
    public interface IInitializable<in TParams>
    {
        /// <summary>
        /// Initializes the ViewModel with the given parameters.
        /// </summary>
        Task<bool> InitializeAsync(TParams parameters);
    }
}
