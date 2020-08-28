using Azure.Storage.Blobs;
using Glasswall.IcapServer.CloudProxyApp.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Glasswall.IcapServer.CloudProxyApp.StorageAccess
{
    public class StorageUploader : IUploader
    {
        private readonly Func<string, BlobServiceClient> _blobServiceClientFactory;
        private readonly ICloudConfiguration _cloudConfiguration;

        public StorageUploader(Func<string, BlobServiceClient> blobServiceClientFactory, ICloudConfiguration cloudConfiguration)
        {
            _blobServiceClientFactory = blobServiceClientFactory;
            _cloudConfiguration = cloudConfiguration;
        }

        public async Task UploadInputFile(Guid id, string sourceFilePath)
        {
            BlobServiceClient blobServiceClient = _blobServiceClientFactory(_cloudConfiguration.FileProcessingStorageConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_cloudConfiguration.FileProcessingStorageOriginalStoreName);
            BlobClient blobClient = containerClient.GetBlobClient(id.ToString());
            
            using FileStream uploadFileStream = File.OpenRead(sourceFilePath);
            await blobClient.UploadAsync(uploadFileStream, true);
        }
    }
}
