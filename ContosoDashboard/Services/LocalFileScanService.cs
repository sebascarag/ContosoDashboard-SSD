using System.IO.Compression;
using System.Text;

namespace ContosoDashboard.Services;

/// <summary>
/// Performs offline file-type consistency checks for training. This is not antivirus software.
/// </summary>
public sealed class LocalFileScanService : IFileScanService
{
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] OleSignature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    public async Task<FileScanResult> ScanAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        if (!fileStream.CanRead || !fileStream.CanSeek)
        {
            return new FileScanResult(
                FileScanStatus.Unavailable,
                "The local training scanner requires a readable, seekable stream.");
        }

        var originalPosition = fileStream.Position;
        try
        {
            fileStream.Position = 0;
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var header = new byte[8];
            var bytesRead = await fileStream.ReadAsync(header, cancellationToken);
            if (bytesRead == 0)
            {
                return Rejected("Empty files are not accepted.");
            }

            var matches = extension switch
            {
                ".pdf" => StartsWith(header, bytesRead, PdfSignature),
                ".jpg" or ".jpeg" => bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => StartsWith(header, bytesRead, PngSignature),
                ".doc" or ".xls" or ".ppt" => StartsWith(header, bytesRead, OleSignature),
                ".docx" => await IsOfficeOpenXmlAsync(fileStream, "word/document.xml", cancellationToken),
                ".xlsx" => await IsOfficeOpenXmlAsync(fileStream, "xl/workbook.xml", cancellationToken),
                ".pptx" => await IsOfficeOpenXmlAsync(fileStream, "ppt/presentation.xml", cancellationToken),
                ".txt" => await IsTextFileAsync(fileStream, cancellationToken),
                _ => false
            };

            if (!matches)
            {
                return Rejected("The file contents do not match a supported document type.");
            }

            return new FileScanResult(
                FileScanStatus.Accepted,
                "The file passed local training type checks. This is not antivirus scanning.");
        }
        catch (InvalidDataException)
        {
            return Rejected("The document container is invalid or unreadable.");
        }
        catch (DecoderFallbackException)
        {
            return Rejected("The text file is not valid UTF-8 text.");
        }
        finally
        {
            fileStream.Position = originalPosition;
        }
    }

    private static bool StartsWith(byte[] source, int sourceLength, byte[] signature)
    {
        if (sourceLength < signature.Length)
        {
            return false;
        }

        return source.AsSpan(0, signature.Length).SequenceEqual(signature);
    }

    private static Task<bool> IsOfficeOpenXmlAsync(
        Stream fileStream,
        string requiredEntry,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        fileStream.Position = 0;
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);
        var hasContentTypes = archive.Entries.Any(entry => entry.FullName == "[Content_Types].xml");
        var hasRequiredEntry = archive.Entries.Any(entry => entry.FullName == requiredEntry);
        return Task.FromResult(hasContentTypes && hasRequiredEntry);
    }

    private static async Task<bool> IsTextFileAsync(Stream fileStream, CancellationToken cancellationToken)
    {
        fileStream.Position = 0;
        using var reader = new StreamReader(
            fileStream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096,
            leaveOpen: true);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.Any(character => char.IsControl(character) && character is not '\t' and not '\r'))
            {
                return false;
            }
        }

        return true;
    }

    private static FileScanResult Rejected(string message) => new(FileScanStatus.Rejected, message);
}
