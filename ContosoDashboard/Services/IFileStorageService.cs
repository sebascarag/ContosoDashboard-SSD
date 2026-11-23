namespace ContosoDashboard.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(
        Stream fileStream,
        int userId,
        int? projectId,
        string fileExtension,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string filePath, CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(string filePath, CancellationToken cancellationToken = default);

    Task<string> GetUrlAsync(string filePath, TimeSpan expiration);
}
