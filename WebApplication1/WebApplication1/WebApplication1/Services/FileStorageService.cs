using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using SkiaSharp;
using WebApplication1.Models;

namespace WebApplication1.Services;

// Проверява, смалява и пази качените файлове извън wwwroot.
public class FileStorageService
{
    public const long MaxFileSize = 10 * 1024 * 1024;
    private readonly string _storagePath;

    public FileStorageService(IWebHostEnvironment environment)
    {
        _storagePath = Path.Combine(environment.ContentRootPath, "Data", "Uploads");
        Directory.CreateDirectory(_storagePath);
    }

    public async Task<ClinicFile> SaveAsync(
        IFormFile upload,
        int appointmentId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (upload.Length is <= 0 or > MaxFileSize)
        {
            throw new InvalidDataException("Файлът трябва да е до 10 MB.");
        }

        var originalName = SafeDisplayName(upload.FileName);
        var extension = Path.GetExtension(originalName).ToLowerInvariant();
        var storedId = Guid.NewGuid().ToString("N");
        var record = new ClinicFile
        {
            OriginalFileName = originalName,
            StoredFileName = storedId,
            ContentType = "application/pdf",
            AppointmentId = appointmentId,
            UploadedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        if (extension == ".pdf")
        {
            await SavePdfAsync(upload, record, cancellationToken);
            return record;
        }

        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
        {
            throw new InvalidDataException("Разрешени са JPG, PNG, WebP и PDF файлове.");
        }

        await SaveImageAsync(upload, record, extension, cancellationToken);
        return record;
    }

    public Task DeleteStoredFilesAsync(IEnumerable<ClinicFile> files)
    {
        foreach (var file in files)
        {
            DeleteIfExists(file.StoredFileName);
            if (file.ThumbnailFileName is not null)
            {
                DeleteIfExists(file.ThumbnailFileName);
            }
        }

        return Task.CompletedTask;
    }

    public string GetPath(string storedName) => Path.Combine(_storagePath, storedName);

    private async Task SavePdfAsync(IFormFile upload, ClinicFile record, CancellationToken cancellationToken)
    {
        var storedName = record.StoredFileName + ".pdf";
        await using var input = upload.OpenReadStream();
        var header = new byte[5];
        var read = await input.ReadAsync(header, cancellationToken);
        if (read != header.Length || !header.AsSpan().SequenceEqual("%PDF-"u8))
        {
            throw new InvalidDataException("PDF файлът е невалиден.");
        }

        input.Position = 0;
        try
        {
            await using var output = new FileStream(GetPath(storedName), FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await input.CopyToAsync(output, cancellationToken);
        }
        catch
        {
            DeleteIfExists(storedName);
            throw;
        }

        record.StoredFileName = storedName;
        record.SizeBytes = new FileInfo(GetPath(storedName)).Length;
    }

    private async Task SaveImageAsync(
        IFormFile upload,
        ClinicFile record,
        string extension,
        CancellationToken cancellationToken)
    {
        var fullName = record.StoredFileName + ".jpg";
        var thumbName = record.StoredFileName + "-small.jpg";
        try
        {
            // Форматът и размерът се проверяват преди да се прочетат всички пиксели.
            await using var uploadStream = upload.OpenReadStream();
            using var buffer = new MemoryStream();
            await uploadStream.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            using var headerStream = new MemoryStream(bytes, writable: false);
            using var codec = SKCodec.Create(headerStream);
            var expectedFormat = extension switch
            {
                ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
                ".png" => SKEncodedImageFormat.Png,
                ".webp" => SKEncodedImageFormat.Webp,
                _ => throw new InvalidDataException("Този вид снимка не е разрешен.")
            };

            if (codec is null || codec.EncodedFormat != expectedFormat || codec.FrameCount != 1
                || codec.Info.Width > 6000 || codec.Info.Height > 6000
                || (long)codec.Info.Width * codec.Info.Height > 25_000_000)
            {
                throw new InvalidDataException("Снимката е невалидна или е прекалено голяма.");
            }

            using var imageStream = new MemoryStream(bytes, writable: false);
            using var original = SKBitmap.Decode(imageStream);
            if (original is null)
            {
                throw new InvalidDataException("Снимката не може да бъде отворена.");
            }

            // Записваме по-лека снимка и отделна миниатюра за списъците.
            using var optimized = ResizeToFit(original, 1600);
            using var thumbnail = ResizeToFit(optimized, 320);
            var mainSize = SaveJpeg(optimized, GetPath(fullName), 82);
            var thumbnailSize = SaveJpeg(thumbnail, GetPath(thumbName), 76);
            if (mainSize > MaxFileSize || thumbnailSize > MaxFileSize)
            {
                throw new InvalidDataException("Снимката е прекалено голяма след обработка.");
            }

            record.StoredFileName = fullName;
            record.ThumbnailFileName = thumbName;
            record.ContentType = "image/jpeg";
            record.SizeBytes = mainSize;
        }
        catch (InvalidDataException)
        {
            DeleteIfExists(fullName);
            DeleteIfExists(thumbName);
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            DeleteIfExists(fullName);
            DeleteIfExists(thumbName);
            throw new InvalidDataException("Снимката не може да бъде отворена. Избери JPG, PNG или WebP файл.", exception);
        }
    }

    private static SKBitmap ResizeToFit(SKBitmap source, int maxSide)
    {
        var ratio = Math.Min(1d, Math.Min(maxSide / (double)source.Width, maxSide / (double)source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * ratio));
        var height = Math.Max(1, (int)Math.Round(source.Height * ratio));
        return source.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
            ?? throw new InvalidDataException("Снимката не може да бъде смалена.");
    }

    private static long SaveJpeg(SKBitmap bitmap, string path, int quality)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality)
            ?? throw new InvalidDataException("Снимката не може да бъде записана.");
        using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        data.SaveTo(output);
        return output.Length;
    }

    private static string SafeDisplayName(string? fileName)
    {
        var leafName = Path.GetFileName(fileName ?? string.Empty);
        var safeName = Regex.Replace(leafName, @"[^\p{L}\p{N}._ -]", "_").Trim(' ', '.');
        if (string.IsNullOrWhiteSpace(safeName))
        {
            throw new InvalidDataException("Името на файла е невалидно.");
        }

        return safeName.Length <= 180 ? safeName : safeName[..180];
    }

    private void DeleteIfExists(string fileName)
    {
        var path = GetPath(fileName);
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }
}
