using Glasswall.IcapServer.CloudProxyApp.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Glasswall.IcapServer.CloudProxyApp.Setup
{
    internal static class ServiceCollectionExtensionMethods
    {
        public static IServiceProvider ConfigureServices(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddSingleton<CloudProxyApplication>();

            var appConfig = new CloudProxyApplicationConfiguration();
            configuration.Bind(appConfig);

            serviceCollection.AddSingleton<IAppConfiguration>(appConfig);

            return serviceCollection.BuildServiceProvider(true);
        }
    }
}
