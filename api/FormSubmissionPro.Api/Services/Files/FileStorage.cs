namespace FormSubmissionPro.Api.Services.Files;

public record StoredFile(string FieldName, string FileName, long Size, string Url);

public interface IFileStorage
{
    /// <summary>Persists an uploaded file and returns its public URL + metadata.</summary>
    Task<StoredFile> SaveAsync(Guid submissionId, string fieldName, IFormFile file, CancellationToken ct = default);
}

/// <summary>
/// Local-disk storage under wwwroot/uploads/{submissionId}/. Files are served statically.
/// Swap for an S3-compatible implementation in production without touching callers.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    private readonly IHttpContextAccessor _http;

    public LocalFileStorage(IWebHostEnvironment env, IHttpContextAccessor http)
    {
        _root = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
        _http = http;
    }

    public async Task<StoredFile> SaveAsync(Guid submissionId, string fieldName, IFormFile file, CancellationToken ct = default)
    {
        var dir = Path.Combine(_root, submissionId.ToString());
        Directory.CreateDirectory(dir);

        // Sanitize the original file name to avoid path traversal.
        var safeName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "file";
        var path = Path.Combine(dir, safeName);

        await using (var stream = File.Create(path))
            await file.CopyToAsync(stream, ct);

        // Include PathBase so URLs are correct when hosted under a sub-application (e.g. /FormSubmissionPro).
        var req = _http.HttpContext!.Request;
        var url = $"{req.Scheme}://{req.Host}{req.PathBase}/uploads/{submissionId}/{Uri.EscapeDataString(safeName)}";
        return new StoredFile(fieldName, safeName, file.Length, url);
    }
}
