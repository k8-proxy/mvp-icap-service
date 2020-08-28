﻿using Glasswall.IcapServer.CloudProxyApp.Configuration;
using Glasswall.IcapServer.CloudProxyApp.QueueAccess;
using Glasswall.IcapServer.CloudProxyApp.StorageAccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Glasswall.IcapServer.CloudProxyApp.Setup
{
    public static class ServiceCollectionExtensionMethods
    {
        public static IServiceProvider ConfigureServices(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddSingleton<CloudProxyApplication>();
            serviceCollection.AddTransient<IUploader, StorageUploader>();
            serviceCollection.AddTransient<IServiceQueueClient, ServiceBusQueueClient>();

            var appConfig = new CloudProxyApplicationConfiguration();
            configuration.Bind(appConfig);
            serviceCollection.AddSingleton<IAppConfiguration>(appConfig);

            var cloudConfig = new CloudProxyCloudConfiguration();
            configuration.Bind(cloudConfig);
            serviceCollection.AddSingleton<ICloudConfiguration>(cloudConfig);

            return serviceCollection.BuildServiceProvider(true);
        }
    }
}
