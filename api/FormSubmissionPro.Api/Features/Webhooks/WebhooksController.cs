using FormSubmissionPro.Api.Data;
using FormSubmissionPro.Api.Domain;
using FormSubmissionPro.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormSubmissionPro.Api.Features.Webhooks;

public record CreateWebhookRequest(Guid FormId, string Url);

/// <summary>Tenant-scoped CRUD for outbound webhooks. Requires authentication.</summary>
[Authorize]
[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly AppDbContext _db;
    public WebhooksController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid formId, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();

        var hooks = await _db.Webhooks
            .Where(w => w.OrganizationId == orgId && w.FormId == formId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new { w.Id, w.Url, w.IsActive, w.CreatedAt })
            .ToListAsync(ct);

        return Ok(hooks);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateWebhookRequest req, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();
        if (Services.Webhooks.WebhookUrlGuard.Validate(req.Url) is { } urlError)
            return BadRequest(new { message = urlError });

        // Ensure the target form belongs to this tenant.
        var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == req.FormId && f.OrganizationId == orgId, ct);
        if (form is null) return NotFound(new { message = "Form not found." });

        var hook = new Webhook
        {
            Id = Guid.NewGuid(),
            FormId = req.FormId,
            OrganizationId = orgId,
            Url = req.Url.Trim(),
        };
        _db.Webhooks.Add(hook);
        await _db.SaveChangesAsync(ct);

        return Ok(new { hook.Id, hook.Url, hook.IsActive });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();

        var hook = await _db.Webhooks.FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == orgId, ct);
        if (hook is null) return NotFound();

        _db.Webhooks.Remove(hook);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Recent delivery attempts for a form's webhooks (status, attempts, response).</summary>
    [HttpGet("deliveries")]
    public async Task<IActionResult> Deliveries([FromQuery] Guid formId, CancellationToken ct)
    {
        if (!this.TryGetOrgId(out var orgId)) return Unauthorized();

        var deliveries = await _db.WebhookDeliveries
            .Where(d => d.OrganizationId == orgId && d.FormId == formId)
            .OrderByDescending(d => d.CreatedAt)
            .Take(50)
            .Select(d => new
            {
                d.Id, d.Url, status = d.Status.ToString(), d.Attempts,
                d.ResponseStatus, d.Error, d.CreatedAt, d.LastAttemptAt,
            })
            .ToListAsync(ct);

        return Ok(deliveries);
    }
}
