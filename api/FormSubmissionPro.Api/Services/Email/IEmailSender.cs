namespace FormSubmissionPro.Api.Services.Email;

public record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? ReplyTo = null,
    string? FromName = null,
    IReadOnlyList<string>? Cc = null);

/// <summary>
/// Transport abstraction. The dev/console implementation needs no credentials so the
/// app runs out-of-the-box; a Resend/SMTP implementation is swapped in for production.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
