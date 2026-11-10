using System.Net;

namespace FormSubmissionPro.Api.Features.Submit;

/// <summary>The default success page shown after a (non-AJAX) form post when no redirect is set.</summary>
public static class SuccessPage
{
    public static string Html(string? title = null, string? message = null)
    {
        var h1 = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(title) ? "Form submitted successfully!" : title);
        var lines = string.IsNullOrWhiteSpace(message)
            ? "<p>Thank you for reaching out to us.</p><p>We'll get back to you soon.</p>"
            : $"<p>{WebUtility.HtmlEncode(message)}</p>";
        return Template.Replace("{{TITLE}}", h1).Replace("{{MESSAGE}}", lines);
    }

    private const string Template = """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Form submitted successfully</title>
  <style>
    * { box-sizing: border-box; }
    body {
      margin: 0; min-height: 100vh; display: flex; align-items: center; justify-content: center;
      font-family: ui-sans-serif, system-ui, -apple-system, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
      background: #f6f7fb; color: #0f172a; padding: 16px;
    }
    .card {
      width: 100%; max-width: 440px; background: #fff; border: 1px solid #e8eaf0;
      border-radius: 20px; padding: 40px 32px; text-align: center;
      box-shadow: 0 10px 30px -12px rgba(15, 23, 42, 0.18);
    }
    .tick {
      width: 72px; height: 72px; margin: 0 auto 20px; border-radius: 9999px;
      background: #dcfce7; display: flex; align-items: center; justify-content: center;
    }
    .tick svg { width: 38px; height: 38px; stroke: #16a34a; }
    h1 { font-size: 22px; margin: 0 0 10px; color: #0f172a; }
    p { margin: 4px 0; color: #475569; font-size: 15px; line-height: 1.6; }
    .footer { margin-top: 24px; font-size: 12px; color: #94a3b8; }
  </style>
</head>
<body>
  <div class="card">
    <div class="tick">
      <svg viewBox="0 0 24 24" fill="none" stroke-width="3" stroke-linecap="round" stroke-linejoin="round">
        <path d="M20 6 9 17l-5-5" />
      </svg>
    </div>
    <h1>{{TITLE}}</h1>
    {{MESSAGE}}
    <div class="footer">Powered by XOFormPro</div>
  </div>
</body>
</html>
""";
}
