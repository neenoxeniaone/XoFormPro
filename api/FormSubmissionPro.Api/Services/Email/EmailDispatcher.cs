namespace FormSubmissionPro.Api.Services.Email;

/// <summary>
/// The IEmailSender the app depends on. Reads the DB-backed settings per send and routes to
/// the configured provider: Amazon SES or Microsoft Graph, falling back to the console dev sink.
/// </summary>
public class EmailDispatcher : IEmailSender
{
    private readonly IEmailConfigProvider _config;
    private readonly ConsoleEmailSender _console;
    private readonly GraphEmailSender _graph;
    private readonly SesEmailSender _ses;

    public EmailDispatcher(IEmailConfigProvider config, ConsoleEmailSender console, GraphEmailSender graph, SesEmailSender ses)
    {
        _config = config;
        _console = console;
        _graph = graph;
        _ses = ses;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var cfg = await _config.GetAsync(ct);

        // Provider selection is explicit (the SES and Graph credentials share columns).
        if (cfg.UsesSes && cfg.SesIsConfigured)
        {
            await _ses.SendAsync(message, ct);
            return;
        }

        if (cfg.GraphIsConfigured)
        {
            await _graph.SendAsync(message, ct);
            return;
        }

        await _console.SendAsync(message, ct);
    }
}
