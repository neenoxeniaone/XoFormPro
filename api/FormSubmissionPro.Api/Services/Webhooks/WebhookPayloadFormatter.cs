using System.Text;
using System.Text.Json;

namespace FormSubmissionPro.Api.Services.Webhooks;

/// <summary>
/// Transforms the stored delivery payload into a provider-specific body. Slack and
/// Discord incoming-webhook URLs get nicely formatted messages; everything else gets
/// the raw JSON as-is.
/// </summary>
public static class WebhookPayloadFormatter
{
    public static (string body, string contentType) Format(string url, string storedPayload)
    {
        var host = Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.Host.ToLowerInvariant() : "";

        if (host.Contains("hooks.slack.com"))
            return (Slack(storedPayload), "application/json");
        if (host.Contains("discord.com") || host.Contains("discordapp.com"))
            return (Discord(storedPayload), "application/json");

        // Default: forward the raw payload unchanged.
        return (storedPayload, "application/json");
    }

    private static (string formName, JsonElement data) Parse(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var formName = root.TryGetProperty("formName", out var fn) ? fn.GetString() ?? "Form" : "Form";
        var data = root.TryGetProperty("data", out var d) ? d.Clone() : default;
        return (formName, data);
    }

    private static string FieldsText(JsonElement data)
    {
        var sb = new StringBuilder();
        if (data.ValueKind == JsonValueKind.Object)
            foreach (var p in data.EnumerateObject())
                sb.Append($"*{p.Name}:* {(p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.GetRawText())}\n");
        return sb.ToString().TrimEnd();
    }

    private static string Slack(string payload)
    {
        var (formName, data) = Parse(payload);
        var text = $"*New submission — {formName}*\n{FieldsText(data)}";
        return JsonSerializer.Serialize(new { text });
    }

    private static string Discord(string payload)
    {
        var (formName, data) = Parse(payload);
        // Discord uses **bold**; reuse the Slack-style text but swap markers.
        var content = $"**New submission — {formName}**\n{FieldsText(data).Replace("*", "**")}";
        return JsonSerializer.Serialize(new { content });
    }
}
