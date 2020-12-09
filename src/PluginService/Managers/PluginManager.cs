namespace PluginService.Managers
{
    using Common.Extensions;
    using Common.Registrations;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using PluginService.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    public class PluginManager : IPluginManager
    {
        private const string PLUGIN_CONFIG_FILE_NAME = "plugin.json";

        private readonly IServiceProvider _serviceProvider;

        public static Dictionary<string, Assembly> PluginDict { get; set; }

        public PluginManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Initialize(IServiceCollection services, string pluginPath)
        {
            var pluginFolder = new DirectoryInfo(DirectoryModel.BaseDirectory.GetFullPath(pluginPath));

            var pluginConfigFiles = GetPluginConfigFiles(pluginFolder);
            var pluginFiles = GetPluginFiles(pluginFolder);

            PluginDict = new Dictionary<string, Assembly>();

            Parallel.ForEach(pluginConfigFiles, pluginConfigFile =>
            {
                var pluginConfig = GetPluginConfigFromFilePath(pluginConfigFile.FullName);
                var pluginFile = pluginFiles.AsParallel().Where(file => file.Name.Equals(pluginConfig.DllName)).FirstOrDefault();
                var assembly = LoadAssemblyFromFilePath(pluginFile.FullName);

                // register plugin dependencies
                var dependencyRegistrar = LoadPlugin<IDependencyRegistrar>(assembly);
                dependencyRegistrar.Register(services);

                PluginDict.Add(pluginConfig.Name, assembly);
            });
        }

        private Type GetMatchedPluginTypeFromAssembly(Assembly assembly, Type pluginType)
        {
            if (assembly == null)
            {
                // TODO: log error
            }

            return assembly
                .GetTypes()
                .ToList()
                .Where(
                    type => !type.IsInterface && !type.IsAbstract && type.GetInterface(pluginType.FullName) != null
                    )
                .FirstOrDefault();
        }

        public T LoadPlugin<T>(Assembly assembly)
        {
            var pluginType = typeof(T);
            var type = GetMatchedPluginTypeFromAssembly(assembly, pluginType);

            if (type == null)
            {
                // TODO: log error
            }

            var parameters = type
                .GetConstructors()
                .FirstOrDefault()
                .GetParameters()
                .Select(
                    param =>
                    {
                        var instant = _serviceProvider.GetRequiredService(param.ParameterType);
                        return instant;
                    });

            if (parameters != null && parameters.Any())
            {
                return (T)Activator.CreateInstance(type, parameters.ToArray());
            }
            else
            {
                return (T)Activator.CreateInstance(type);
            }
        }

        public Assembly GetPluginAssemblyFromName(string name)
        {
            return PluginDict[name];
        }

        private PluginConfigModel GetPluginConfigFromFilePath(string filePath)
        {
            var text = File.ReadAllText(filePath);
            return GetPluginConfigFromFileText(text);
        }

        private PluginConfigModel GetPluginConfigFromFileText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new PluginConfigModel();

            var config = JsonConvert.DeserializeObject<PluginConfigModel>(text);
            return config;
        }

        private List<FileInfo> GetPluginConfigFiles(DirectoryInfo pluginFolder)
        {
            var pluginConfigFiles = pluginFolder.GetFiles(PLUGIN_CONFIG_FILE_NAME, SearchOption.AllDirectories).ToList();
            return pluginConfigFiles;
        }

        private List<FileInfo> GetPluginFiles(DirectoryInfo pluginFolder)
        {
            var pluginFiles = pluginFolder.GetFiles("*.dll", SearchOption.AllDirectories).ToList();
            return pluginFiles;
        }

        private Assembly LoadAssemblyFromFilePath(string filePath)
        {
            return Assembly.LoadFrom(filePath);
        }
    }
}
