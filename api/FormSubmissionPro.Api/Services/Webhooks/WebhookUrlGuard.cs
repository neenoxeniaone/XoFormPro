using System.Net;
using System.Net.Sockets;

namespace FormSubmissionPro.Api.Services.Webhooks;

/// <summary>
/// Keeps customer-supplied webhook URLs from reaching the server's own network
/// (localhost, the LAN, cloud metadata at 169.254.169.254, ...).
///
/// Two layers: <see cref="Validate"/> gives a clear error when the URL is saved, and
/// <see cref="ConnectAsync"/> re-checks the resolved address at connect time on every
/// delivery — a hostname that resolved publicly when saved can be re-pointed at an
/// internal address later, so the save-time check alone is not enough.
/// </summary>
public static class WebhookUrlGuard
{
    public const string HttpClientName = "webhooks";

    /// <summary>Returns an error message, or null when the URL is acceptable.</summary>
    public static string? Validate(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return "A valid absolute URL is required.";
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            return "Webhook URLs must start with http:// or https://.";
        if (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
            return "Webhook URLs cannot point at localhost.";
        if (IPAddress.TryParse(uri.Host.Trim('[', ']'), out var ip) && !IsPublic(ip))
            return "Webhook URLs cannot point at a private or internal address.";
        return null;
    }

    /// <summary>SocketsHttpHandler.ConnectCallback: refuses to open a socket to a non-public address.</summary>
    public static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken ct)
    {
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
        var target = addresses.FirstOrDefault(IsPublic)
            ?? throw new HttpRequestException(
                $"Blocked webhook to {context.DnsEndPoint.Host}: it resolves to a private or internal address.");

        var socket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(new IPEndPoint(target, context.DnsEndPoint.Port), ct);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    public static bool IsPublic(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any)
            || ip.Equals(IPAddress.Broadcast) || ip.Equals(IPAddress.IPv6None))
            return false;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return !(b[0] == 0                                   // 0.0.0.0/8
                  || b[0] == 10                                  // 10/8
                  || b[0] == 127                                 // loopback
                  || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)  // CGNAT 100.64/10
                  || (b[0] == 169 && b[1] == 254)                // link-local + cloud metadata
                  || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)   // 172.16/12
                  || (b[0] == 192 && b[1] == 168)                // 192.168/16
                  || (b[0] == 192 && b[1] == 0 && b[2] == 0)     // 192.0.0/24
                  || (b[0] == 198 && (b[1] == 18 || b[1] == 19)) // benchmarking
                  || b[0] >= 224);                               // multicast + reserved
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast) return false;
            var b = ip.GetAddressBytes();
            if ((b[0] & 0xFE) == 0xFC) return false; // fc00::/7 unique-local
            return true;
        }

        return false;
    }
}
