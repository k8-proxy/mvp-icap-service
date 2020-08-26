using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Glasswall.IcapServer.CloudProxyApp.Setup
{
    internal static class ServiceCollectionCloudSetupExtensionMethods
    {
        public static IServiceCollection ConfigureCloudFactories(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<Func<string, BlobServiceClient>>((connectionString) => new BlobServiceClient(connectionString));
            return serviceCollection;
        }
    }
}
