namespace Glasswall.IcapServer.CloudProxyApp.Configuration
{
    public class CloudProxyCloudConfiguration : ICloudConfiguration
    {
        public string FileProcessingStorageConnectionString { get; set; }
        public string FileProcessingStorageOriginalStoreName { get; set; }
    }
}
