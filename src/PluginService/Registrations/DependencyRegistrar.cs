using Common.Registrations;
using Microsoft.Extensions.DependencyInjection;
using PluginService.Managers;

namespace PluginService.Registrations
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public void Register(IServiceCollection services)
        {
            RegisterManagers(services);
        }

        private void RegisterManagers(IServiceCollection services)
        {
            services.AddSingleton<IPluginManager, PluginManager>();
        }
    }
}
