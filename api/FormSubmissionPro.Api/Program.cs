using System.Text;
using System.Threading.RateLimiting;
using FormSubmissionPro.Api.Data;
using FormSubmissionPro.Api.Infrastructure;
using FormSubmissionPro.Api.Services.Auth;
using FormSubmissionPro.Api.Services.Captcha;
using FormSubmissionPro.Api.Services.Email;
using FormSubmissionPro.Api.Services.Files;
using FormSubmissionPro.Api.Services.Retention;
using FormSubmissionPro.Api.Services.Spam;
using FormSubmissionPro.Api.Services.Webhooks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Structured logging (Serilog) ──
builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
// Outbound webhooks hit customer-supplied URLs: block internal targets at connect time,
// and don't follow redirects (a public URL could otherwise bounce to an internal one).
builder.Services.AddHttpClient(WebhookUrlGuard.HttpClientName)
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        ConnectCallback = WebhookUrlGuard.ConnectAsync,
        AllowAutoRedirect = false,
    });
builder.Services.AddHttpContextAccessor();

// ── Error handling + health checks ──
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!, name: "postgres");

// ── Authentication (JWT) ──
var configuredKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(configuredKey) && !builder.Environment.IsDevelopment())
{
    // This key is committed, so booting on it outside Development would let anyone who has
    // read the repo forge a token for any tenant. Refuse to start instead.
    throw new InvalidOperationException(
        "Jwt:SigningKey is not configured. Set the Jwt__SigningKey environment variable on the host.");
}
var jwtOptions = new JwtOptions
{
    SigningKey = configuredKey ?? "dev-only-insecure-signing-key-change-me-in-production-1234567890",
};
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IApiKeyService, ApiKeyService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();

// ── Rate limiting ──
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        return ValueTask.CompletedTask;
    };

    // Public submit endpoint: per client IP.
    options.AddPolicy("submit", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));

    // Auth endpoints: a password-guessing and reset-mail-bombing brake, per IP.
    options.AddPolicy("auth", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(5) }));

    // Submissions REST API: per API key (falls back to IP).
    options.AddPolicy("api", http =>
    {
        var key = http.Request.Headers.Authorization.ToString();
        var partition = string.IsNullOrEmpty(key) ? (http.Connection.RemoteIpAddress?.ToString() ?? "anon") : key;
        return RateLimitPartition.GetFixedWindowLimiter(partition,
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1) });
    });
});

// PostgreSQL via EF Core. snake_case naming + fsp_ table prefix (see AppDbContext) so
// raw SQL needs no quoting: SELECT * FROM fsp_email_configurations;
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
       .UseSnakeCaseNamingConvention());

// Email: settings live in the database (EmailConfigurations row, Id=1). The dispatcher
// reads them per send and routes to the console sink or Microsoft Graph.
builder.Services.AddSingleton<IEmailConfigProvider, DbEmailConfigProvider>();
builder.Services.AddSingleton<ConsoleEmailSender>();
builder.Services.AddSingleton<GraphEmailSender>();
builder.Services.AddSingleton<SesEmailSender>();
builder.Services.AddSingleton<IEmailSender, EmailDispatcher>();

// Pro-feature services.
// Data Protection keys encrypt secrets at rest (SecretProtector). In a container they must
// live on a persistent volume, or every restart generates new keys and stored secrets stop
// decrypting. Under IIS the default (user profile / registry) location is used.
var dataProtection = builder.Services.AddDataProtection();
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    // Fixed name so the keys stay valid regardless of the container's content root.
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath))
                  .SetApplicationName("XOFormPro");
}
builder.Services.AddSingleton<FormSubmissionPro.Api.Services.Security.ISecretProtector, FormSubmissionPro.Api.Services.Security.SecretProtector>();
builder.Services.AddSingleton<ICaptchaVerifier, CaptchaVerifier>();
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
// Signed form tokens: issued at page load by /form-token, redeemed once by /submit.
builder.Services.AddScoped<IFormTokenService, FormTokenService>();

builder.Services.AddHostedService<WebhookProcessor>();
builder.Services.AddHostedService<RetentionCleanupService>();

// CORS: the public /submit endpoint must accept posts from any site; the dashboard
// runs on the Vite dev server. Wide-open for dev — tighten per-origin in production.
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Behind a reverse proxy (the Kubernetes ingress), take the client IP and scheme from
// X-Forwarded-For / X-Forwarded-Proto. Without this every visitor appears to come from
// the proxy's IP, so the per-IP rate limits become one global limit, and upload URLs are
// built as http://. Opt-in: on a host reachable directly, trusting these headers would
// let clients spoof their IP.
if (builder.Configuration.GetValue("App:TrustForwardedHeaders", false))
{
    var fwd = new ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                         | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto,
        ForwardLimit = 1,
    };
    // The pod is only reachable through the ingress, whose address isn't fixed.
    fwd.KnownNetworks.Clear();
    fwd.KnownProxies.Clear();
    app.UseForwardedHeaders(fwd);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Apply pending migrations on startup (creates/updates the schema in dev and prod).
using (var migrationScope = app.Services.CreateScope())
    migrationScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

// Optional path base for sub-application hosting (e.g. "/FormSubmissionPro").
// Under IIS as a virtual application this is set automatically, but this allows
// overriding it for Kestrel/other hosts via config.
var pathBase = builder.Configuration["App:PathBase"];
if (!string.IsNullOrWhiteSpace(pathBase))
    app.UsePathBase(pathBase);

app.UseSerilogRequestLogging();
app.UseCors();

// Serve the built React app (wwwroot/index.html + assets) and uploaded files.
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Uploads are visitor-supplied and served from the dashboard's own origin. An
        // uploaded .html/.svg opened in the browser would otherwise run script as the
        // dashboard and could read the login token. Force download, forbid MIME sniffing,
        // and sandbox anything that does get rendered.
        if (ctx.Context.Request.Path.StartsWithSegments("/uploads"))
        {
            var headers = ctx.Context.Response.Headers;
            headers.ContentDisposition = "attachment";
            headers.XContentTypeOptions = "nosniff";
            headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
        }
    },
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// SPA fallback: any non-API, non-file route serves index.html for client-side routing.
app.MapFallbackToFile("index.html");

app.Run();
