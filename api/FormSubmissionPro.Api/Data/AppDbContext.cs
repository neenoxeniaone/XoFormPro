using FormSubmissionPro.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FormSubmissionPro.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Form> Forms => Set<Form>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Webhook> Webhooks => Set<Webhook>();
    public DbSet<EmailConfiguration> EmailConfigurations => Set<EmailConfiguration>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<FormToken> FormTokens => Set<FormToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Organization>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.NotificationEmail).HasMaxLength(320).IsRequired();
            e.Property(x => x.PlanTier).HasMaxLength(20);
        });

        b.Entity<Form>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.AccessKey).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Kind).HasMaxLength(20);
            e.Property(x => x.DefaultSubject).HasMaxLength(300);
            e.Property(x => x.FromName).HasMaxLength(200);
            e.Property(x => x.ToEmailOverride).HasMaxLength(320);
            e.Property(x => x.CaptchaProvider).HasMaxLength(20);
            e.HasOne(x => x.Organization)
                .WithMany(o => o.Forms)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Webhook>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Url).HasMaxLength(2000).IsRequired();
            e.HasIndex(x => x.FormId);
            e.HasOne(x => x.Form)
                .WithMany(f => f.Webhooks)
                .HasForeignKey(x => x.FormId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(320).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ApiKey>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Label).HasMaxLength(200);
            e.Property(x => x.Prefix).HasMaxLength(40);
            e.Property(x => x.KeyHash).HasMaxLength(128);
            e.Ignore(x => x.IsActive);
            e.HasIndex(x => x.KeyHash);
            e.HasIndex(x => x.OrganizationId);
            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<UserToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(128);
            e.Property(x => x.Purpose).HasConversion<int>();
            e.HasIndex(x => x.TokenHash);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Invitation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.Role).HasMaxLength(20);
            e.Property(x => x.TokenHash).HasMaxLength(128);
            e.HasIndex(x => x.TokenHash);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<WebhookDelivery>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Url).HasMaxLength(2000);
            e.Property(x => x.Payload).HasColumnType("jsonb");
            e.Property(x => x.Status).HasConversion<int>();
            // The processor polls for due, not-yet-final deliveries.
            e.HasIndex(x => new { x.Status, x.NextAttemptAt });
            e.HasIndex(x => x.FormId);
        });

        b.Entity<EmailConfiguration>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.DefaultFromName).HasMaxLength(200);
            e.Property(x => x.Provider).HasMaxLength(20);
            e.Ignore(x => x.GraphIsConfigured);
            e.Ignore(x => x.UsesSes);
            e.Ignore(x => x.SesIsConfigured);
            // Seed the single settings row; empty credential fields → console sink until filled in.
            e.HasData(new EmailConfiguration
            {
                Id = 1,
                DefaultFromName = "FormSubmissionPro",
                Provider = "graph",
                GraphTenantId = "",
                GraphClientId = "",
                GraphClientSecret = "",
                GraphSender = "",
                UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            });
        });

        b.Entity<FormToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Origin).HasMaxLength(255);
            e.HasIndex(x => x.ExpiresAt);
            e.HasIndex(x => x.FormId);
        });

        b.Entity<Submission>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Data).HasColumnType("jsonb");
            e.Property(x => x.Subject).HasMaxLength(300);
            e.Property(x => x.SenderEmail).HasMaxLength(320);
            e.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
            e.HasIndex(x => new { x.FormId, x.CreatedAt });
            e.HasOne(x => x.Form)
                .WithMany(f => f.Submissions)
                .HasForeignKey(x => x.FormId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Prefix every table with "fsp_". Combined with UseSnakeCaseNamingConvention()
        // this yields names like fsp_email_configurations (no SQL quoting needed).
        foreach (var entity in b.Model.GetEntityTypes())
        {
            entity.SetTableName("fsp_" + entity.GetTableName());
        }
    }
}
