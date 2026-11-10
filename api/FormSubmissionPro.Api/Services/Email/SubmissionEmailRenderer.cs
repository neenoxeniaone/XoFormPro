using System.Net;
using System.Text;

namespace FormSubmissionPro.Api.Services.Email;

/// <summary>
/// Builds the submission notification email: a greeting, an intro line naming the form,
/// then each field stacked as label / value.
/// </summary>
public static class SubmissionEmailRenderer
{
    public static string Render(IReadOnlyDictionary<string, string> fields, string formName, string? intro = null, string kind = "form")
    {
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family:Arial,Helvetica,sans-serif;max-width:600px;margin:auto;color:#0f172a;line-height:1.6\">");

        sb.Append("<p style=\"margin:0 0 12px\">Hello,</p>");
        var safeName = WebUtility.HtmlEncode(formName);
        var lead = !string.IsNullOrWhiteSpace(intro)
            ? WebUtility.HtmlEncode(intro)
            : kind == "list"
                ? $"🎉 You have a new subscriber to <strong>{safeName}</strong>."
                : $"A new submission was received on <strong>{safeName}</strong>. Details below.";
        sb.Append($"<p style=\"margin:0 0 24px\">{lead}</p>");

        foreach (var (key, value) in fields)
        {
            sb.Append("<div style=\"margin-bottom:16px\">");
            sb.Append($"<div style=\"font-weight:bold;color:#111;margin-bottom:2px\">{WebUtility.HtmlEncode(Label(key))}</div>");
            sb.Append($"<div style=\"color:#333;white-space:pre-wrap\">{FormatValue(value)}</div>");
            sb.Append("</div>");
        }

        sb.Append("<hr style=\"border:none;border-top:1px solid #eee;margin:24px 0 12px\" />");
        sb.Append("<p style=\"color:#999;font-size:12px;margin:0\">Sent via XOFormPro</p>");
        sb.Append("</div>");
        return sb.ToString();
    }

    /// <summary>Turns a field key into a readable label, e.g. "first_name" -> "First name".</summary>
    private static string Label(string key)
    {
        var clean = key.Replace('_', ' ').Replace('-', ' ').Trim();
        if (clean.Length == 0) return key;
        return char.ToUpperInvariant(clean[0]) + clean[1..];
    }

    /// <summary>Renders email-looking values as clickable mailto links; everything else encoded.</summary>
    private static string FormatValue(string value)
    {
        var encoded = WebUtility.HtmlEncode(value);
        if (value.Count(c => c == '@') == 1 && value.Contains('.') && !value.Contains(' '))
            return $"<a href=\"mailto:{encoded}\" style=\"color:#7c3aed\">{encoded}</a>";
        return encoded;
    }
}
