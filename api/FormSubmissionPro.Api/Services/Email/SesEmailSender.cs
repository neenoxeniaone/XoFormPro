using System.Net.Mail;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace FormSubmissionPro.Api.Services.Email;

/// <summary>
/// Sends mail through Amazon SES (SendEmail API). Unlike Microsoft Graph, SES honours the
/// From display name we provide, so the sender name comes from the message (the form's
/// From name) rather than a mailbox's directory display name.
///
/// Credentials are read from the shared <see cref="Domain.EmailConfiguration"/> columns:
///   GraphTenantId → AWS region, GraphClientId → access key id,
///   GraphClientSecret → secret access key (encrypted), GraphSender → verified From address.
/// </summary>
public class SesEmailSender : IEmailSender
{
    private readonly IEmailConfigProvider _config;
    private readonly Security.ISecretProtector _secrets;
    private readonly ILogger<SesEmailSender> _logger;

    public SesEmailSender(
        IEmailConfigProvider config,
        Security.ISecretProtector secrets,
        ILogger<SesEmailSender> logger)
    {
        _config = config;
        _secrets = secrets;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var cfg = await _config.GetAsync(ct);

        var region = RegionEndpoint.GetBySystemName(cfg.GraphTenantId);
        var creds = new BasicAWSCredentials(cfg.GraphClientId, _secrets.Unprotect(cfg.GraphClientSecret));
        using var client = new AmazonSimpleEmailServiceClient(creds, region);

        // "Display Name <from@addr>" — SES keeps the display name we set here.
        var fromName = string.IsNullOrWhiteSpace(message.FromName) ? cfg.DefaultFromName : message.FromName;
        var source = new MailAddress(cfg.GraphSender, fromName, System.Text.Encoding.UTF8).ToString();

        var request = new SendEmailRequest
        {
            Source = source,
            Destination = new Destination
            {
                ToAddresses = SplitAddresses(message.To).ToList(),
                CcAddresses = (message.Cc ?? Array.Empty<string>())
                    .SelectMany(SplitAddresses).ToList(),
            },
            Message = new Message
            {
                Subject = new Content { Charset = "UTF-8", Data = message.Subject },
                Body = new Body { Html = new Content { Charset = "UTF-8", Data = message.HtmlBody } },
            },
        };

        if (!string.IsNullOrWhiteSpace(message.ReplyTo))
            request.ReplyToAddresses = new List<string> { message.ReplyTo };

        var resp = await client.SendEmailAsync(request, ct);
        _logger.LogInformation("Email sent via SES to {To} (MessageId {Id}).", message.To, resp.MessageId);
    }

    /// <summary>Splits a recipient string on commas/semicolons so a form can notify several addresses.</summary>
    private static IEnumerable<string> SplitAddresses(string raw) =>
        raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
