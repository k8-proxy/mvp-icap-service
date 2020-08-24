namespace Glasswall.IcapServer.CloudProxyApp.Configuration
{
    interface IAppConfigurationChecker
    {
        void CheckConfiguration(IAppConfiguration configuration);
    }
}
