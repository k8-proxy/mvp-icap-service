using Azure.Storage.Blobs;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Glasswall.IcapServer.CloudProxyApp.Setup
{
    internal static class ServiceCollectionCloudSetupExtensionMethods
    {
        public static IServiceCollection ConfigureCloudFactories(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<Func<string, BlobServiceClient>>((connectionString) => new BlobServiceClient(connectionString));

            serviceCollection.AddSingleton<Func<string, string, IQueueClient>>((connectionString, queueName) => new QueueClient(connectionString, queueName));
            return serviceCollection;
        }
    }
}
