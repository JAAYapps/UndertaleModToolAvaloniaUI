using System.Threading.Tasks;
using UndertaleModLib.Scripting;

namespace UndertaleModToolAvalonia.Services.ScriptService
{
    public interface IScriptService : IScriptInterface
    {
        public Task RunScript(string path);
    }
}
