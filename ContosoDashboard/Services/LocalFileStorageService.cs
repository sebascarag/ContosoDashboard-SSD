using System.Text.RegularExpressions;

namespace ContosoDashboard.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private static readonly Regex SafeExtension = new(@"^\.[a-z0-9]{1,10}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly string _rootDirectory;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<LocalFileStorageService> logger)
    {
        var configuredRoot = configuration["FileStorage:RootPath"];
        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            throw new InvalidOperationException("FileStorage:RootPath must be configured.");
        }

        _rootDirectory = Path.GetFullPath(
            Path.IsPathRooted(configuredRoot)
                ? configuredRoot
                : Path.Combine(environment.ContentRootPath, configuredRoot));
        _logger = logger;

        var webRoot = Path.GetFullPath(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"));
        var relativeToWebRoot = Path.GetRelativePath(webRoot, _rootDirectory);
        if (relativeToWebRoot == "." ||
            (!Path.IsPathRooted(relativeToWebRoot) &&
             relativeToWebRoot != ".." &&
             !relativeToWebRoot.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("The configured document storage root must be outside wwwroot.");
        }
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        int userId,
        int? projectId,
        string fileExtension,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        if (!fileStream.CanRead)
        {
            throw new ArgumentException("The upload stream must be readable.", nameof(fileStream));
        }

        if (userId <= 0 || projectId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "User and project identifiers must be positive.");
        }

        var extension = fileExtension.StartsWith('.') ? fileExtension : $".{fileExtension}";
        if (!SafeExtension.IsMatch(extension))
        {
            throw new ArgumentException("The file extension is invalid.", nameof(fileExtension));
        }

        var projectSegment = projectId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "personal";
        var relativePath = Path.Combine(
            userId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            projectSegment,
            $"{Guid.NewGuid():N}{extension}").Replace(Path.DirectorySeparatorChar, '/');
        var destination = ResolvePath(relativePath);
        var directory = Path.GetDirectoryName(destination)
            ?? throw new InvalidOperationException("The upload destination has no parent directory.");

        Directory.CreateDirectory(directory);

        try
        {
            await using var destinationStream = new FileStream(
                destination,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await fileStream.CopyToAsync(destinationStream, cancellationToken);
            return relativePath;
        }
        catch
        {
            try
            {
                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }
            }
            catch (Exception cleanupException)
            {
                _logger.LogError(cleanupException, "Failed to clean up incomplete document upload at {Destination}", destination);
            }

            throw;
        }
    }

    public Task DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        File.Delete(ResolvePath(filePath));
        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = new FileStream(
            ResolvePath(filePath),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);
        return Task.FromResult(stream);
    }

    public Task<string> GetUrlAsync(string filePath, TimeSpan expiration)
    {
        throw new NotSupportedException("Local files are served only through authorized document endpoints.");
    }

    private string ResolvePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || Path.IsPathRooted(filePath))
        {
            throw new ArgumentException("The document path must be a non-empty relative path.", nameof(filePath));
        }

        var platformPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_rootDirectory, platformPath));
        var relativePath = Path.GetRelativePath(_rootDirectory, fullPath);
        if (relativePath == "." ||
            relativePath == ".." ||
            relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(relativePath))
        {
            throw new UnauthorizedAccessException("The document path resolves outside the configured storage root.");
        }

        return fullPath;
    }
}
