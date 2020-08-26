namespace Glasswall.IcapServer.CloudProxyApp.Configuration
{
    public interface ICloudConfiguration
    {
        public string FileProcessingStorageConnectionString { get; set; }
        public string FileProcessingStorageOriginalStoreName { get; set; }
    }
}
