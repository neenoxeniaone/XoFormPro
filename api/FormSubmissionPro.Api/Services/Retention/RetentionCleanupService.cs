using FormSubmissionPro.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FormSubmissionPro.Api.Services.Retention;

/// <summary>
/// Periodically deletes submissions older than each plan's retention window, along with
/// their uploaded files.
///
/// DISABLED BY DEFAULT. Both windows are zero unless configured, and zero means "keep
/// forever" — deleting a customer's enquiries is not something that should happen because
/// nobody set a value. Turn it on deliberately in configuration:
///
///   "Retention": { "FreeDays": 30, "ProDays": 365 }
///
/// or per-host: Retention__FreeDays / Retention__ProDays.
///
/// The token sweep below always runs: expired form-token nonces are worthless by
/// definition and are not customer data.
/// </summary>
public class RetentionCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(12);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<RetentionCleanupService> _logger;
    private readonly int _freeRetentionDays;
    private readonly int _proRetentionDays;

    public RetentionCleanupService(
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment env,
        IConfiguration config,
        ILogger<RetentionCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _env = env;
        _logger = logger;
        _freeRetentionDays = Math.Max(0, config.GetValue("Retention:FreeDays", 0));
        _proRetentionDays = Math.Max(0, config.GetValue("Retention:ProDays", 0));

        if (_freeRetentionDays == 0 && _proRetentionDays == 0)
            _logger.LogInformation("Retention cleanup is disabled — submissions are kept indefinitely.");
        else
            _logger.LogInformation(
                "Retention cleanup enabled: free {Free} day(s), pro {Pro} day(s). 0 = keep forever.",
                _freeRetentionDays, _proRetentionDays);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await SweepAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Retention sweep failed."); }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTimeOffset.UtcNow;

        // Form-token nonces: one row per page load of a form using the tick box or the
        // token check. They are useless once expired, so clear them out on the same sweep.
        var staleTokens = await db.FormTokens.Where(t => t.ExpiresAt < now).ExecuteDeleteAsync(ct);
        if (staleTokens > 0)
            _logger.LogInformation("Retention sweep deleted {Count} expired form tokens.", staleTokens);

        // Nothing configured: keep every submission. Return before building any query, so
        // there is no path from an unset value to a delete.
        if (_freeRetentionDays == 0 && _proRetentionDays == 0) return;

        // A window of 0 for one tier means that tier keeps its data forever, so push its
        // cutoff back beyond any real CreatedAt rather than treating 0 as "delete today".
        var freeCutoff = _freeRetentionDays > 0 ? now.AddDays(-_freeRetentionDays) : DateTimeOffset.MinValue;
        var proCutoff = _proRetentionDays > 0 ? now.AddDays(-_proRetentionDays) : DateTimeOffset.MinValue;

        // Find expired submissions (join plan to pick the right cutoff).
        var expired = await db.Submissions
            .Join(db.Organizations, s => s.OrganizationId, o => o.Id, (s, o) => new { s, o.PlanTier })
            .Where(x => (x.PlanTier == "pro" && x.s.CreatedAt < proCutoff)
                     || (x.PlanTier != "pro" && x.s.CreatedAt < freeCutoff))
            .Select(x => x.s.Id)
            .Take(5000)
            .ToListAsync(ct);

        if (expired.Count == 0) return; // tokens were already swept above

        // Remove uploaded files for each expired submission.
        var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads");
        foreach (var id in expired)
        {
            var dir = Path.Combine(uploadsRoot, id.ToString());
            if (Directory.Exists(dir))
                try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
        }

        var deleted = await db.Submissions.Where(s => expired.Contains(s.Id)).ExecuteDeleteAsync(ct);
        _logger.LogInformation("Retention sweep deleted {Count} expired submissions.", deleted);
    }
}
