using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Shared.Application.Interfaces;

namespace Shared.Infrastructure.Storage
{
    public class MinioFileStorage : IFileStorageFacade
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public MinioFileStorage(IConfiguration config)
        {
            _bucketName = config["Storage:BucketName"]!;

            var accessKey = config["Storage:AccessKey"] ?? throw new InvalidOperationException("AccessKey not defined");
            var secretKey = config["Storage:SecretKey"] ?? throw new InvalidOperationException("SecretKey not defined");


            var s3Config = new AmazonS3Config
            {
                ServiceURL = config["Storage:Endpoint"],
                ForcePathStyle = true
            };

            _s3Client = new AmazonS3Client(
                accessKey,
                secretKey,
                s3Config
            );
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
        {
            var objectKey = $"{Guid.NewGuid()}-{fileName}";

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

        public async Task<bool> DeleteFileAsync(string filePath, CancellationToken ct = default)
        {
            try
            {
                await _s3Client.DeleteObjectAsync(_bucketName, filePath, ct);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string filePath, CancellationToken ct = default)
        {
            try
            {
                await _s3Client.GetObjectMetadataAsync(_bucketName, filePath, ct);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Stream> GetFileAsync(string objectKey, CancellationToken ct = default)
        {
            var response = await _s3Client.GetObjectAsync(_bucketName, objectKey, ct);
            return response.ResponseStream;
        }
    }
}
