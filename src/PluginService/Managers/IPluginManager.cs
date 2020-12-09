using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace PluginService.Managers
{
    public interface IPluginManager
    {
        Assembly GetPluginAssemblyFromName(string name);
        void Initialize(IServiceCollection services, string pluginPath);
        T LoadPlugin<T>(Assembly assembly);
    }
}