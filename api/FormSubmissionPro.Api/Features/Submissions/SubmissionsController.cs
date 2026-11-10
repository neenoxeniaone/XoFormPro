using System.Text;
using System.Text.Json;
using FormSubmissionPro.Api.Data;
using FormSubmissionPro.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormSubmissionPro.Api.Features.Submissions;

public record PatchSubmissionRequest(bool? IsRead, bool? IsStarred);
public record BulkSubmissionRequest(Guid[] Ids, string Action); // action: delete | read | unread | star | unstar

/// <summary>Tenant-scoped management of stored submissions. Requires authentication.</summary>
[Authorize]
[ApiController]
[Route("api/submissions")]
public class SubmissionsController : ControllerBase
{
    private readonly AppDbContext _db;
    public SubmissionsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? formId,
        [FromQuery] bool includeSpam = false,
        [FromQuery] bool starredOnly = false,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] string? q = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] string sort = "newest",
        [FromQuery] string? formKind = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var query = Filter(orgId, formId, includeSpam, starredOnly, unreadOnly, q, from, to, formKind);

        query = sort switch
        {
            "oldest" => query.OrderBy(s => s.CreatedAt),
            _ => query.OrderByDescending(s => s.CreatedAt),
        };

        var total = await query.CountAsync(ct);
        // Counted within the same filter set as the list, so the badge cannot contradict it.
        var unread = await Filter(orgId, formId, includeSpam, starredOnly, true, q, from, to, formKind)
            .CountAsync(ct);

        // Materialize first; jsonb (JsonDocument) projections can't be translated to SQL.
        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var shaped = rows.Select(s => new
        {
            s.Id, s.FormId, s.Subject, s.SenderEmail, s.IsSpam, s.IsRead, s.IsStarred,
            s.IpAddress, s.UserAgent, s.CreatedAt,
            Data = s.Data.RootElement,
        });

        return Ok(new { total, unread, page, pageSize, items = shaped });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(Guid id, PatchSubmissionRequest req, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();
        var s = await _db.Submissions.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId, ct);
        if (s is null) return NotFound();

        if (req.IsRead is { } r) s.IsRead = r;
        if (req.IsStarred is { } st) s.IsStarred = st;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();
        var s = await _db.Submissions.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId, ct);
        if (s is null) return NotFound();
        _db.Submissions.Remove(s);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> Bulk(BulkSubmissionRequest req, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();
        if (req.Ids is null || req.Ids.Length == 0) return BadRequest(new { message = "No ids provided." });

        var rows = await _db.Submissions
            .Where(s => s.OrganizationId == orgId && req.Ids.Contains(s.Id))
            .ToListAsync(ct);

        switch (req.Action?.ToLowerInvariant())
        {
            case "delete": _db.Submissions.RemoveRange(rows); break;
            case "read": rows.ForEach(s => s.IsRead = true); break;
            case "unread": rows.ForEach(s => s.IsRead = false); break;
            case "star": rows.ForEach(s => s.IsStarred = true); break;
            case "unstar": rows.ForEach(s => s.IsStarred = false); break;
            default: return BadRequest(new { message = "Unknown action." });
        }
        await _db.SaveChangesAsync(ct);
        return Ok(new { affected = rows.Count });
    }

    /// <summary>Daily non-spam submission counts for the last N days (for the overview chart).</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> Stats([FromQuery] int days = 30, CancellationToken ct = default)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();
        days = Math.Clamp(days, 7, 90);

        var startDate = DateTime.UtcNow.Date.AddDays(-(days - 1));
        var since = new DateTimeOffset(startDate, TimeSpan.Zero);
        var times = await _db.Submissions
            .Where(s => s.OrganizationId == orgId && !s.IsSpam && s.CreatedAt >= since)
            .Select(s => s.CreatedAt)
            .ToListAsync(ct);

        var counts = times.GroupBy(t => t.UtcDateTime.Date).ToDictionary(g => g.Key, g => g.Count());

        // Fill every day in the window (including zeros) so the chart is continuous.
        var series = Enumerable.Range(0, days).Select(i =>
        {
            var date = startDate.AddDays(i);
            return new { date = date.ToString("yyyy-MM-dd"), count = counts.GetValueOrDefault(date, 0) };
        });

        return Ok(new { days, series });
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] Guid? formId,
        [FromQuery] bool includeSpam = false,
        [FromQuery] string? q = null,
        [FromQuery] string format = "csv",
        [FromQuery] string? formKind = null,
        CancellationToken ct = default)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();

        var rows = await Filter(orgId, formId, includeSpam, false, false, q, null, null, formKind)
            .OrderByDescending(s => s.CreatedAt)
            .Take(10000)
            .ToListAsync(ct);

        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var json = rows.Select(s => new
            {
                s.Id, s.FormId, s.Subject, s.SenderEmail, s.IsSpam, s.CreatedAt,
                data = JsonSerializer.Deserialize<JsonElement>(s.Data.RootElement.GetRawText()),
            });
            var bytes = JsonSerializer.SerializeToUtf8Bytes(json, new JsonSerializerOptions { WriteIndented = true });
            return File(bytes, "application/json", $"submissions-{stamp}.json");
        }

        // CSV — dynamic columns from the union of all field keys.
        // Skip "email": it has its own fixed Email column (from SenderEmail), so including
        // the data key too would duplicate it.
        var keys = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "email" };
        foreach (var s in rows)
            foreach (var p in s.Data.RootElement.EnumerateObject())
                if (seen.Add(p.Name)) keys.Add(p.Name);

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", new[] { "Date", "Subject", "Email" }.Concat(keys).Select(Csv)));
        foreach (var s in rows)
        {
            var cells = new List<string> { s.CreatedAt.ToString("u"), s.Subject, s.SenderEmail ?? "" };
            foreach (var k in keys)
                cells.Add(s.Data.RootElement.TryGetProperty(k, out var v)
                    ? (v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.GetRawText())
                    : "");
            sb.AppendLine(string.Join(",", cells.Select(Csv)));
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"submissions-{stamp}.csv");
    }

    private static string Csv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    /// <summary>Shared filter used by list, count, and export.</summary>
    internal IQueryable<Domain.Submission> Filter(
        Guid orgId, Guid? formId, bool includeSpam, bool starredOnly, bool unreadOnly,
        string? q, DateTimeOffset? from, DateTimeOffset? to, string? formKind = null)
    {
        var query = _db.Submissions.Where(s => s.OrganizationId == orgId);
        if (formId is { } fid) query = query.Where(s => s.FormId == fid);
        // Restrict to submissions whose parent form is of this kind (form vs list).
        if (!string.IsNullOrWhiteSpace(formKind))
            query = query.Where(s => _db.Forms.Any(f => f.Id == s.FormId && f.Kind == formKind));

        if (!includeSpam) query = query.Where(s => !s.IsSpam);
        if (starredOnly) query = query.Where(s => s.IsStarred);
        if (unreadOnly) query = query.Where(s => !s.IsRead);
        if (from is { } f) query = query.Where(s => s.CreatedAt >= f);
        if (to is { } t) query = query.Where(s => s.CreatedAt <= t);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = $"%{q.Trim()}%";
            // SearchText holds the concatenated field values (see SubmitController).
            query = query.Where(s =>
                EF.Functions.ILike(s.Subject, term) ||
                (s.SenderEmail != null && EF.Functions.ILike(s.SenderEmail, term)) ||
                EF.Functions.ILike(s.SearchText, term));
        }
        return query;
    }
}
