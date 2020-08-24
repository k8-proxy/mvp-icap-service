namespace Glasswall.IcapServer.CloudProxyApp.Configuration
{
    interface IAppConfiguration
    {
        string InputFilepath { get; }
        string OutputFilepath { get; }
    }
}
