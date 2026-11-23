using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed record DocumentUploadMetadata(
    string Title,
    string? Description,
    string Category,
    string? Tags,
    int? ProjectId);

public sealed class DocumentUploadException : Exception
{
    public DocumentUploadException(string message)
        : base(message)
    {
    }

    public DocumentUploadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public interface IDocumentService
{
    Task<Document> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        DocumentUploadMetadata metadata,
        int requestingUserId,
        CancellationToken cancellationToken = default);

    Task<List<Document>> GetMyDocumentsAsync(int requestingUserId, CancellationToken cancellationToken = default);
}

public static class DocumentUploadPolicy
{
    public const long MaxFileSizeBytes = 25 * 1024 * 1024;

    public static IReadOnlyList<string> Categories { get; } = Array.AsReadOnly(
    [
        "Project Documents",
        "Team Resources",
        "Personal Files",
        "Reports",
        "Presentations",
        "Other"
    ]);
}

public sealed class DocumentService : IDocumentService
{
    private static readonly IReadOnlyDictionary<string, string> SupportedFileTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".txt"] = "text/plain",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IFileScanService _fileScanner;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService fileStorage,
        IFileScanService fileScanner,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _fileScanner = fileScanner;
        _logger = logger;
    }

    public async Task<Document> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        DocumentUploadMetadata metadata,
        int requestingUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        ArgumentNullException.ThrowIfNull(metadata);

        if (!fileStream.CanRead || !fileStream.CanSeek)
        {
            throw new DocumentUploadException("The selected file could not be read. Please select it again.");
        }

        ValidateMetadata(metadata);

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName.Length > 255 ||
            Path.GetFileName(fileName) != fileName ||
            !SupportedFileTypes.TryGetValue(extension, out var canonicalContentType))
        {
            throw new DocumentUploadException("The selected file type is not supported.");
        }

        if (fileStream.Length > DocumentUploadPolicy.MaxFileSizeBytes)
        {
            throw new DocumentUploadException("Each file must be 25 MB or smaller.");
        }

        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > 255)
        {
            throw new DocumentUploadException("The selected file has invalid type information.");
        }

        var uploader = await _context.Users
            .FirstOrDefaultAsync(user => user.UserId == requestingUserId, cancellationToken);
        if (uploader is null)
        {
            throw new UnauthorizedAccessException("The signed-in user could not be verified.");
        }

        if (metadata.ProjectId is int projectId)
        {
            var project = await _context.Projects
                .Include(candidate => candidate.ProjectMembers)
                .FirstOrDefaultAsync(candidate => candidate.ProjectId == projectId, cancellationToken);

            if (project is null)
            {
                throw new DocumentUploadException("The selected project does not exist.");
            }

            var isProjectManager = project.ProjectManagerId == requestingUserId;
            var isProjectMember = project.ProjectMembers.Any(member => member.UserId == requestingUserId);
            if (uploader.Role != UserRole.Administrator && !isProjectManager && !isProjectMember)
            {
                throw new UnauthorizedAccessException("You do not have access to upload documents to this project.");
            }
        }

        fileStream.Position = 0;
        var scanResult = await _fileScanner.ScanAsync(
            fileStream,
            fileName,
            canonicalContentType,
            cancellationToken);
        if (scanResult.Status != FileScanStatus.Accepted)
        {
            throw new DocumentUploadException(scanResult.Message);
        }

        fileStream.Position = 0;
        var storedPath = await _fileStorage.UploadAsync(
            fileStream,
            requestingUserId,
            metadata.ProjectId,
            extension,
            cancellationToken);

        var now = DateTime.UtcNow;
        var document = new Document
        {
            Title = metadata.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(metadata.Description) ? null : metadata.Description.Trim(),
            Category = metadata.Category,
            Tags = string.IsNullOrWhiteSpace(metadata.Tags) ? null : metadata.Tags.Trim(),
            ProjectId = metadata.ProjectId,
            UploadedByUserId = requestingUserId,
            UploadedAt = now,
            FileName = fileName,
            FilePath = storedPath,
            FileSizeBytes = fileStream.Length,
            FileType = canonicalContentType,
            CreatedAt = now,
            UpdatedAt = now
        };
        var activity = new DocumentActivityLog
        {
            Document = document,
            ActorUserId = requestingUserId,
            Action = "Upload",
            OccurredAt = now,
            Success = true
        };

        _context.Documents.Add(document);
        _context.DocumentActivityLogs.Add(activity);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception persistenceException)
        {
            _context.Entry(document).State = EntityState.Detached;
            _context.Entry(activity).State = EntityState.Detached;

            try
            {
                await _fileStorage.DeleteAsync(storedPath, cancellationToken);
            }
            catch (Exception cleanupException)
            {
                _logger.LogError(
                    cleanupException,
                    "Failed to clean up stored file {FilePath} after metadata persistence failed.",
                    storedPath);
                throw new DocumentUploadException(
                    "The document record could not be saved and the stored file could not be cleaned up. Contact an administrator.",
                    persistenceException);
            }

            _logger.LogError(persistenceException, "Failed to persist uploaded document metadata.");
            throw new DocumentUploadException("The document could not be saved. The upload was cancelled.", persistenceException);
        }

        return document;
    }

    public async Task<List<Document>> GetMyDocumentsAsync(
        int requestingUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .AsNoTracking()
            .Where(document => document.UploadedByUserId == requestingUserId)
            .Include(document => document.Project)
            .OrderByDescending(document => document.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    private static void ValidateMetadata(DocumentUploadMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata.Title) || metadata.Title.Trim().Length > 255)
        {
            throw new DocumentUploadException("A document title of 1 to 255 characters is required.");
        }

        if (metadata.Description?.Length > 2000)
        {
            throw new DocumentUploadException("The description must be 2,000 characters or fewer.");
        }

        if (!DocumentUploadPolicy.Categories.Contains(metadata.Category, StringComparer.Ordinal))
        {
            throw new DocumentUploadException("Select a valid document category.");
        }
    }
}
