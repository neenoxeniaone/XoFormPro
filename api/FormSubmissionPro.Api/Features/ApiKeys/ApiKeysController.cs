using FormSubmissionPro.Api.Data;
using FormSubmissionPro.Api.Domain;
using FormSubmissionPro.Api.Infrastructure;
using FormSubmissionPro.Api.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormSubmissionPro.Api.Features.ApiKeys;

public record CreateApiKeyRequest(string Label);

/// <summary>Manage Submissions-API keys. JWT-authed (dashboard). Max 10 active keys.</summary>
[Authorize]
[ApiController]
[Route("api/apikeys")]
public class ApiKeysController : ControllerBase
{
    private const int MaxActiveKeys = 10;

    private readonly AppDbContext _db;
    private readonly IApiKeyService _keys;

    public ApiKeysController(AppDbContext db, IApiKeyService keys)
    {
        _db = db;
        _keys = keys;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();
        var keys = await _db.ApiKeys
            .Where(k => k.OrganizationId == orgId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new { k.Id, k.Label, k.Prefix, k.CreatedAt, k.LastUsedAt, k.RevokedAt })
            .ToListAsync(ct);
        return Ok(keys);
    }

    /// <summary>Creates a key. The full secret is returned ONCE and never stored in plaintext.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateApiKeyRequest req, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();

        var activeCount = await _db.ApiKeys.CountAsync(k => k.OrganizationId == orgId && k.RevokedAt == null, ct);
        if (activeCount >= MaxActiveKeys)
            return BadRequest(new { message = $"You can have at most {MaxActiveKeys} active keys." });

        var generated = _keys.Generate();
        var key = new ApiKey
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Label = string.IsNullOrWhiteSpace(req.Label) ? "API key" : req.Label.Trim(),
            Prefix = generated.Prefix,
            KeyHash = generated.Hash,
        };
        _db.ApiKeys.Add(key);
        await _db.SaveChangesAsync(ct);

        // fullKey is shown only here.
        return Ok(new { key.Id, key.Label, key.Prefix, fullKey = generated.FullKey, key.CreatedAt });
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();
        var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.OrganizationId == orgId, ct);
        if (key is null) return NotFound();
        if (key.RevokedAt is null)
        {
            key.RevokedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        return NoContent();
    }
}
