using FormSubmissionPro.Api.Data;
using FormSubmissionPro.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FormSubmissionPro.Api.Services.Email;

public interface IEmailConfigProvider
{
    /// <summary>Returns the platform email settings (the single Id=1 row), cached briefly.</summary>
    Task<EmailConfiguration> GetAsync(CancellationToken ct = default);
}

/// <summary>
/// Loads <see cref="EmailConfiguration"/> from the database with a short in-memory cache,
/// so editing the row takes effect within ~30s without a restart.
/// </summary>
public class DbEmailConfigProvider : IEmailConfigProvider
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private EmailConfiguration? _cached;
    private DateTimeOffset _loadedAt = DateTimeOffset.MinValue;

    public DbEmailConfigProvider(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task<EmailConfiguration> GetAsync(CancellationToken ct = default)
    {
        if (_cached is not null && DateTimeOffset.UtcNow - _loadedAt < CacheTtl)
            return _cached;

        await _lock.WaitAsync(ct);
        try
        {
            if (_cached is not null && DateTimeOffset.UtcNow - _loadedAt < CacheTtl)
                return _cached;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await db.EmailConfigurations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct)
                      ?? new EmailConfiguration();

            _cached = row;
            _loadedAt = DateTimeOffset.UtcNow;
            return row;
        }
        finally
        {
            _lock.Release();
        }
    }
}
