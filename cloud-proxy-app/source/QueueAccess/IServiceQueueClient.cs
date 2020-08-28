namespace Glasswall.IcapServer.CloudProxyApp.QueueAccess
{
    public interface IServiceQueueClient
    {
        IQueueListener Register(string messageType, string identifier);
    }
}
