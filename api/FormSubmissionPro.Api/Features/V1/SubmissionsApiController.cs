using System.Text;
using FormSubmissionPro.Api.Data;
using FormSubmissionPro.Api.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FormSubmissionPro.Api.Features.V1;

/// <summary>
/// Public Submissions REST API (Pro). Authenticated with a w3f_live_ key via
/// Authorization: Bearer. Mirrors the Web3Forms /v1 contract.
/// </summary>
[ApiController]
[Route("v1")]
[EnableRateLimiting("api")]
public class SubmissionsApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IApiKeyService _keys;

    public SubmissionsApiController(AppDbContext db, IApiKeyService keys)
    {
        _db = db;
        _keys = keys;
    }

    private async Task<Guid?> AuthAsync(CancellationToken ct) =>
        await _keys.AuthenticateAsync(Request.Headers.Authorization.ToString(), ct);

    private IActionResult Unauthorized401() =>
        StatusCode(401, new { error = new { code = "unauthorized", message = "Invalid or missing API key." } });

    [HttpGet("forms")]
    public async Task<IActionResult> ListForms(CancellationToken ct)
    {
        var orgId = await AuthAsync(ct);
        if (orgId is null) return Unauthorized401();

        var forms = await _db.Forms
            .Where(f => f.OrganizationId == orgId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new
            {
                form_id = f.Id,
                form_name = f.Name,
                created_at = f.CreatedAt,
                total_count = f.Submissions.Count(s => !s.IsSpam),
            })
            .ToListAsync(ct);

        return Ok(new { data = forms });
    }

    [HttpGet("submissions")]
    public async Task<IActionResult> ListSubmissions(
        [FromQuery(Name = "form_id")] Guid? formId,
        [FromQuery] int limit = 50,
        [FromQuery] string? cursor = null,
        CancellationToken ct = default)
    {
        var orgId = await AuthAsync(ct);
        if (orgId is null) return Unauthorized401();
        if (formId is null)
            return StatusCode(400, new { error = new { code = "bad_request", message = "form_id is required." } });

        limit = Math.Clamp(limit, 1, 100);

        var query = _db.Submissions
            .Where(s => s.OrganizationId == orgId && s.FormId == formId && !s.IsSpam);

        if (TryDecodeCursor(cursor, out var cAt, out var cId))
        {
            query = query.Where(s => s.CreatedAt < cAt || (s.CreatedAt == cAt && s.Id.CompareTo(cId) < 0));
        }

        // Fetch one extra to know whether more pages exist.
        var rows = await query
            .OrderByDescending(s => s.CreatedAt).ThenByDescending(s => s.Id)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        var page = rows.Take(limit).ToList();
        string? nextCursor = hasMore && page.Count > 0
            ? EncodeCursor(page[^1].CreatedAt, page[^1].Id)
            : null;

        var data = page.Select(s => new
        {
            id = s.Id,
            form_id = s.FormId,
            submitted_at = s.CreatedAt,
            ip_address = s.IpAddress,
            user_agent = s.UserAgent,
            fields = s.Data.RootElement,
        });

        return Ok(new { data, has_more = hasMore, next_cursor = nextCursor });
    }

    [HttpGet("submissions/{id:guid}")]
    public async Task<IActionResult> GetSubmission(Guid id, CancellationToken ct)
    {
        var orgId = await AuthAsync(ct);
        if (orgId is null) return Unauthorized401();

        var s = await _db.Submissions.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == orgId, ct);
        if (s is null)
            return StatusCode(404, new { error = new { code = "not_found", message = "Submission not found." } });

        return Ok(new
        {
            id = s.Id,
            form_id = s.FormId,
            submitted_at = s.CreatedAt,
            ip_address = s.IpAddress,
            user_agent = s.UserAgent,
            fields = s.Data.RootElement,
        });
    }

    // Cursor = base64("{createdAtTicks}:{guid}")
    private static string EncodeCursor(DateTimeOffset at, Guid id) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{at.UtcTicks}:{id}"));

    private static bool TryDecodeCursor(string? cursor, out DateTimeOffset at, out Guid id)
    {
        at = default;
        id = default;
        if (string.IsNullOrWhiteSpace(cursor)) return false;
        try
        {
            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(cursor)).Split(':', 2);
            if (parts.Length != 2) return false;
            at = new DateTimeOffset(long.Parse(parts[0]), TimeSpan.Zero);
            return Guid.TryParse(parts[1], out id);
        }
        catch
        {
            return false;
        }
    }
}
