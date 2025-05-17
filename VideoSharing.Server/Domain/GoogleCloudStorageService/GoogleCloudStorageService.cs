using System.Security.Cryptography;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using SharedEntities;

namespace VideoSharing.Server.Domain.GoogleCloudStorageService
{
    public interface IGoogleCloudStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<string> GenerateSignedUrlAsync(string filePath, TimeSpan expiration);
        Task DeleteFileAsync(string filePath);
        Task<string> CalculateFileHashAsync(Stream fileStream);
    }

    public class GoogleCloudStorageService : IGoogleCloudStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;

        public GoogleCloudStorageService()
        {
            var credential = GoogleCredential.FromFile(Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.GoogleCloudServiceAccountKeyPath));
            _storageClient = StorageClient.Create(credential);
            _bucketName = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.GoogleCloudBucketName);
        }

        /// <inheritdoc/>
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
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

        /// <inheritdoc/>
        public async Task<string> GenerateSignedUrlAsync(string filePath, TimeSpan expiration)
        {
            var objectName = filePath.Replace($"gs://{_bucketName}/", "");
            var urlSigner = UrlSigner.FromCredential(
                await GoogleCredential.FromFileAsync(Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.GoogleCloudServiceAccountKeyPath), CancellationToken.None));
            return await urlSigner.SignAsync(_bucketName, objectName, expiration, HttpMethod.Get);
        }
        
        /// <inheritdoc/>
        public async Task DeleteFileAsync(string filePath)
        {
            var objectName = filePath.Replace($"gs://{_bucketName}/", "");
            await _storageClient.DeleteObjectAsync(_bucketName, objectName);
        }

        public async Task<string> CalculateFileHashAsync(Stream fileStream)
        {
            using var md5 = MD5.Create();
            var hashBytes = await md5.ComputeHashAsync(fileStream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
