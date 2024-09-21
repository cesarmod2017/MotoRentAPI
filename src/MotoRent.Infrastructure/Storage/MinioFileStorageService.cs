using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace MotoRent.Infrastructure.Storage
{
    public class MinioFileStorageService : IFileStorageService
    {
        private readonly IMinioClient _minioClient;
        private readonly string _minioEndpoint;
        public MinioFileStorageService(IConfiguration configuration)
        {
            var endpoint = $"{configuration["Minio:Host"]}:{configuration["Minio:ApiPort"]}";
            var accessKey = configuration["Minio:RootUser"];
            var secretKey = configuration["Minio:RootPassword"];
            var secure = bool.Parse(configuration["Minio:UseSsl"] ?? "false");

            _minioClient = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(secure)
                .Build();

            _minioEndpoint = endpoint;
        }

        public async Task<string> UploadFileAsync(string bucketName, string objectName, Stream fileStream, string contentType)
        {
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putObjectArgs);

            return objectName;
        }

        public async Task<Stream> DownloadFileAsync(string bucketName, string objectName)
        {
            var memoryStream = new MemoryStream();
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(getObjectArgs);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public async Task<bool> DeleteFileAsync(string bucketName, string objectName)
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs);
            return true;
        }

        public async Task<string> GetPresignedUrlAsync(string bucketName, string objectName, int expiryInSeconds = 3600)
        {
            try
            {
                var presignedGetObjectArgs = new PresignedGetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithExpiry(expiryInSeconds);

                return await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);
            }
            catch (Exception ex)
            {
                // Log the exception
                throw new Exception("Error generating presigned URL", ex);
            }
        }

        public async Task SetBucketPublicReadPolicy(string bucketName)
        {
            var policy = @"{
                                ""Version"": ""2012-10-17"",
                                ""Statement"": [
                                    {
                                        ""Effect"": ""Allow"",
                                        ""Principal"": { ""AWS"": [""*""] },
                                        ""Action"": [""s3:GetBucketLocation"", ""s3:ListBucket""],
                                        ""Resource"": [""arn:aws:s3:::" + bucketName + @"""]
                                    },
                                    {
                                        ""Effect"": ""Allow"",
                                        ""Principal"": { ""AWS"": [""*""] },
                                        ""Action"": [""s3:GetObject""],
                                        ""Resource"": [""arn:aws:s3:::" + bucketName + @"/*""]
                                    }
                                ]
                            }";

            var args = new SetPolicyArgs()
                .WithBucket(bucketName)
                .WithPolicy(policy);

            await _minioClient.SetPolicyAsync(args);
            Console.WriteLine($"Public read policy set for bucket '{bucketName}'.");
        }
        public string GetPublicUrl(string bucketName, string objectName)
        {
            return $"http://{_minioEndpoint}/{bucketName}/{objectName}";
        }

        public async Task EnsureBucketExists(string bucketName)
        {
            var found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!found)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
            }
        }
    }
}
