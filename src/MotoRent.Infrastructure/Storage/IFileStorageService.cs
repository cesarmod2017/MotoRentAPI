namespace MotoRent.Infrastructure.Storage
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(string bucketName, string objectName, Stream fileStream, string contentType);
        Task<Stream> DownloadFileAsync(string bucketName, string objectName);
        Task<bool> DeleteFileAsync(string bucketName, string objectName);

        Task<string> GetPresignedUrlAsync(string bucketName, string objectName, int expiryInSeconds = 3600);
        string GetPublicUrl(string bucketName, string objectName);
        Task SetBucketPublicReadPolicy(string bucketName);
        Task EnsureBucketExists(string bucketName);
    }
}
