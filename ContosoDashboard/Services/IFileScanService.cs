namespace ContosoDashboard.Services;

public enum FileScanStatus
{
    Accepted,
    Rejected,
    Unavailable
}

public sealed record FileScanResult(FileScanStatus Status, string Message);

public interface IFileScanService
{
    Task<FileScanResult> ScanAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
