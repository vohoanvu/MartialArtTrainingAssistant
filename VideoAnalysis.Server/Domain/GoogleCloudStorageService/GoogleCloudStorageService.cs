using System.Security.Cryptography;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using SharedEntities;

namespace VideoAnalysis.Server.Domain.GoogleCloudStorageService
{
    public interface IGoogleCloudStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<string> GenerateSignedUrlAsync(string filePath, TimeSpan expiration);
        /// <summary>Generates a V4 signed URL the browser can PUT a file directly to (bypasses the app server / Cloudflare).</summary>
        Task<string> GenerateUploadUrlAsync(string objectName, TimeSpan expiration);
        Task DeleteFileAsync(string filePath);
        Task<string> CalculateFileHashAsync(Stream fileStream);
        /// <summary>The configured GCS bucket name.</summary>
        string BucketName { get; }
    }

    public class GoogleCloudStorageService : IGoogleCloudStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;
        private readonly GoogleCredential _credential;

        public GoogleCloudStorageService()
        {
            _credential = ResolveCredential();
            _storageClient = StorageClient.Create(_credential);
            _bucketName = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.GoogleCloudBucketName);
        }

        public string BucketName => _bucketName;

        // Use a mounted service-account key when one is present; otherwise fall back to
        // Application Default Credentials (the GCE VM's attached service account). ADC keeps us
        // compatible with the iam.disableServiceAccountKeyCreation org policy on the MyCoach project.
        private static GoogleCredential ResolveCredential()
        {
            var keyPath = Environment.GetEnvironmentVariable("GoogleCloud__ServiceAccountKeyPath");
            if (!string.IsNullOrEmpty(keyPath) && File.Exists(keyPath))
            {
                return GoogleCredential.FromFile(keyPath);
            }
            return GoogleCredential.GetApplicationDefault();
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
            // FromCredential signs locally when the credential carries a private key, and via the
            // IAM signBlob API when it does not (e.g. the VM's compute/ADC credential). The latter
            // requires the iam.serviceAccounts.signBlob permission (roles/iam.serviceAccountTokenCreator).
            var urlSigner = UrlSigner.FromCredential(_credential);
            return await urlSigner.SignAsync(_bucketName, objectName, expiration, HttpMethod.Get);
        }

        /// <inheritdoc/>
        public async Task<string> GenerateUploadUrlAsync(string objectName, TimeSpan expiration)
        {
            // V4 signed PUT URL — same keyless signBlob path as the GET signer. Content-Type is NOT
            // a signed header, so the browser may send any Content-Type; GCS stores the object with it.
            var urlSigner = UrlSigner.FromCredential(_credential);
            return await urlSigner.SignAsync(_bucketName, objectName, expiration, HttpMethod.Put);
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

