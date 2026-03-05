using Amazon.S3;
using Amazon.S3.Model;
using Upload.Api.Application.Interfaces;

namespace Upload.Api.Infrastructure.FileSystem
{
    public class MinioFileStorage : IFileStorageFacade
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public MinioFileStorage(IConfiguration config)
        {
            _bucketName = config["Storage:BucketName"]!;

            var s3Config = new AmazonS3Config
            {
                ServiceURL = config["Storage:Endpoint"],
                ForcePathStyle = true
            };

            _s3Client = new AmazonS3Client(
                config["Storage:AccessKey"],
                config["Storage:SecretKey"],
                s3Config
            );
        }

        public async Task<string> SaveFileAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken ct = default)
        {
            var mediaId = Guid.NewGuid();
            var extension = Path.GetExtension(fileName);
            var objectKey = $"{mediaId}{extension}";

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = fileStream,
                ContentType = contentType
            };

            await _s3Client.PutObjectAsync(request, ct);

            return objectKey;
        }

        public async Task<bool> DeleteFileAsync(string objectKey, CancellationToken ct = default)
        {
            await _s3Client.DeleteObjectAsync(_bucketName, objectKey, ct);
            return true;
        }

        public async Task<bool> FileExistsAsync(string objectKey, CancellationToken ct = default)
        {
            try
            {
                await _s3Client.GetObjectMetadataAsync(_bucketName, objectKey, ct);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
