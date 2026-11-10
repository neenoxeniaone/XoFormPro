namespace FormSubmissionPro.Api.Services.Spam;

/// <summary>
/// Host matching for the origin allow-list.
///
/// This replaces a substring test (`origin.Contains(domain)`), which allowed
/// "laili.co.uk.spammer.net" to satisfy an allow-list entry of "laili.co.uk".
/// Matching is now on the parsed host only: scheme and port are ignored, an entry
/// matches itself and any subdomain of itself, and nothing else.
/// </summary>
public static class OriginMatcher
{
    /// <summary>
    /// Extracts the host from an Origin header, or from a Referer URL. Returns null when
    /// neither yields a usable host.
    /// </summary>
    public static string? HostOf(string? origin, string? referer)
    {
        return Host(origin) ?? Host(referer);

        static string? Host(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();

            // "null" is what browsers send for opaque origins (sandboxed iframes, file://).
            if (value.Equals("null", StringComparison.OrdinalIgnoreCase)) return null;

            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host))
                return Normalize(uri.Host);

            // A bare host with no scheme ("example.com", "example.com:8080").
            if (Uri.TryCreate("https://" + value, UriKind.Absolute, out var bare) && !string.IsNullOrEmpty(bare.Host))
                return Normalize(bare.Host);

            return null;
        }
    }

    /// <summary>Parses the stored comma-separated allow-list into normalized hosts.</summary>
    public static string[] ParseList(string? allowedDomains)
    {
        if (string.IsNullOrWhiteSpace(allowedDomains)) return Array.Empty<string>();

        return allowedDomains
            .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry =>
            {
                // Tolerate pasted URLs and wildcard notation ("*.example.com").
                if (entry.StartsWith("*.", StringComparison.Ordinal)) entry = entry[2..];
                return HostOf(entry, null);
            })
            .Where(h => !string.IsNullOrEmpty(h))
            .Select(h => h!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// True when <paramref name="host"/> is one of <paramref name="allowed"/>, or a
    /// subdomain of one. An empty allow-list means "no restriction configured".
    /// </summary>
    public static bool IsAllowed(string? host, string[] allowed)
    {
        if (allowed.Length == 0) return true;
        if (string.IsNullOrWhiteSpace(host)) return false;

        host = Normalize(host);
        foreach (var entry in allowed)
        {
            if (host.Equals(entry, StringComparison.OrdinalIgnoreCase)) return true;
            // Subdomain: the dot is required so "notlaili.co.uk" cannot match "laili.co.uk".
            if (host.EndsWith("." + entry, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>Lower-cases, strips a trailing dot, and drops a leading "www." is NOT done —
    /// "www.example.com" must be listed explicitly, matching the document's example.</summary>
    private static string Normalize(string host)
    {
        host = host.Trim().TrimEnd('.').ToLowerInvariant();
        return host;
    }
}
