using Microsoft.Extensions.DependencyInjection;
using System;

namespace Glasswall.IcapServer.CloudProxyApp
{
    class Program
    {
        static ServiceProvider _serviceProvider;

        static int Main(string[] args)
        {
            try
            {
                RegisterServices();
                IServiceScope serviceScope = _serviceProvider.CreateScope();
                return serviceScope.ServiceProvider.GetRequiredService<CloudProxyApplication>().Run(args);
            }
            finally
            {
                DisposeServices();
            }
        }

        private static void DisposeServices()
        {
            if (_serviceProvider == null)
                return;

            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private static void RegisterServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<CloudProxyApplication>();
            _serviceProvider = services.BuildServiceProvider(true);
        }
    }
}
