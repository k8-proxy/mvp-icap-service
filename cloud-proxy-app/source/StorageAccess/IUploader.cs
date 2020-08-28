using System;
using System.Threading.Tasks;

namespace Glasswall.IcapServer.CloudProxyApp.StorageAccess
{
    public interface IUploader
    {
        Task UploadInputFile(Guid id, string sourceFilePath);
    }
}
