using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

namespace VideoSharing.Server.Domain.GoogleCloudStorageService
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface IGoogleCloudStorageService
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Task<string> GenerateSignedUrlAsync(string filePath, TimeSpan expiration);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Task DeleteFileAsync(string filePath);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class GoogleCloudStorageService : IGoogleCloudStorageService
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;
        private readonly IConfiguration _configuration;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public GoogleCloudStorageService(IConfiguration config)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            _configuration = config;
            var credential = GoogleCredential.FromFile(config["GoogleCloud:ServiceAccountKeyPath"]);
            _storageClient = StorageClient.Create(credential);
            _bucketName = config["GoogleCloud:BucketName"]!;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var objectName = $"{Guid.NewGuid()}_{fileName}";
            var uploadedObject = await _storageClient.UploadObjectAsync(
                _bucketName,
                objectName,
                contentType,
                fileStream
            );
            return $"gs://{_bucketName}/{objectName}";
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public async Task<string> GenerateSignedUrlAsync(string filePath, TimeSpan expiration)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var objectName = filePath.Replace($"gs://{_bucketName}/", "");
            var urlSigner = UrlSigner.FromCredential(
                await GoogleCredential.FromFileAsync(_configuration["GoogleCloud:ServiceAccountKeyPath"], CancellationToken.None));
            return await urlSigner.SignAsync(_bucketName, objectName, expiration, HttpMethod.Get);
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public async Task DeleteFileAsync(string filePath)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var objectName = filePath.Replace($"gs://{_bucketName}/", "");
            await _storageClient.DeleteObjectAsync(_bucketName, objectName);
        }
    }
}
