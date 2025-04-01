using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

namespace VideoSharing.Server.Domain.GoogleCloudStorageService
{
    public interface IGoogleCloudStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<string> GenerateSignedUrlAsync(string filePath, TimeSpan expiration);
    }

    public class GoogleCloudStorageService : IGoogleCloudStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;
        private readonly IConfiguration _configuration;

        public GoogleCloudStorageService(IConfiguration config)
        {
            _configuration = config;
            var credential = GoogleCredential.FromFile(config["GoogleCloud:ServiceAccountKeyPath"]);
            _storageClient = StorageClient.Create(credential);
            _bucketName = config["GoogleCloud:BucketName"]!;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var objectName = $"{Guid.NewGuid()}_{fileName}";
            var uploadedObject = await _storageClient.UploadObjectAsync(
                _bucketName,
                objectName,
                contentType,
                fileStream,
                new UploadObjectOptions { PredefinedAcl = PredefinedObjectAcl.Private }
            );
            return $"gs://{_bucketName}/{objectName}";
        }

        public async Task<string> GenerateSignedUrlAsync(string filePath, TimeSpan expiration)
        {
            var objectName = filePath.Replace($"gs://{_bucketName}/", "");
            var urlSigner = UrlSigner.FromCredential(
                await GoogleCredential.FromFileAsync(_configuration["GoogleCloud:ServiceAccountKeyPath"], CancellationToken.None));
            return await urlSigner.SignAsync(_bucketName, objectName, expiration, HttpMethod.Get);
        }
    }
}
