using Glasswall.IcapServer.CloudProxyApp.AdaptationService;
using Glasswall.IcapServer.CloudProxyApp.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Glasswall.IcapServer.CloudProxyApp.Setup
{
    public static class ServiceCollectionExtensionMethods
    {
        public static IServiceCollection ConfigureServices(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddLogging(configure => configure.AddConsole());

            serviceCollection.AddSingleton<NativeProxyApplication>();

            var appConfig = new CloudProxyApplicationConfiguration();
            configuration.Bind(appConfig);
            serviceCollection.AddSingleton<IAppConfiguration>(appConfig);

            var cloudConfig = new CloudProxyCloudConfiguration();
            configuration.Bind(cloudConfig);
            serviceCollection.AddSingleton<ICloudConfiguration>(cloudConfig);

            serviceCollection.AddTransient(typeof(IAdaptationServiceClient<>), typeof(RabbitMqClient<>));
            serviceCollection.AddTransient<IResponseProcessor, AdaptationOutcomeProcessor>();

            return serviceCollection;
        }
    }
}
