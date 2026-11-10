namespace FormSubmissionPro.Api.Services.Email;

/// <summary>
/// Development transport: logs the email instead of sending it. Lets the whole
/// submit→email pipeline run with zero external configuration.
/// </summary>
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) => _logger = logger;

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "\n──────────── EMAIL (dev sink) ────────────\n" +
            "To:       {To}\n" +
            "From:     {FromName}\n" +
            "Reply-To: {ReplyTo}\n" +
            "Cc:       {Cc}\n" +
            "Subject:  {Subject}\n" +
            "──────────────────────────────────────────\n{Body}\n" +
            "──────────────────────────────────────────",
            message.To,
            message.FromName ?? "(default)",
            message.ReplyTo ?? "(none)",
            message.Cc is { Count: > 0 } ? string.Join(", ", message.Cc) : "(none)",
            message.Subject,
            message.HtmlBody);

        return Task.CompletedTask;
    }
}
