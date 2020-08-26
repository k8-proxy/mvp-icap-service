namespace Glasswall.IcapServer.CloudProxyApp.Configuration
{
    internal class CloudProxyApplicationConfiguration : IAppConfiguration
    {
        public string InputFilepath { get; set; }

        public string OutputFilepath { get; set; }
    }
}
