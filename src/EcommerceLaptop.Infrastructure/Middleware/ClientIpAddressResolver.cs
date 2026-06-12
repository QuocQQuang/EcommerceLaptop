using System.Net;
using Microsoft.AspNetCore.Http;

namespace EcommerceLaptop.Infrastructure.Middleware;

public static class ClientIpAddressResolver
{
    private static readonly string[] ForwardedHeaderNames =
    [
        "CF-Connecting-IP",
        "X-Forwarded-For",
        "X-Real-IP",
        "X-Client-IP",
        "X-Original-Forwarded-For"
    ];

    public static string GetClientIpAddress(this HttpContext context, string fallback = "Unknown")
    {
        foreach (var headerName in ForwardedHeaderNames)
        {
            var ip = ExtractFirstValidIp(context.Request.Headers[headerName].FirstOrDefault());
            if (!string.IsNullOrWhiteSpace(ip))
            {
                return ip;
            }
        }

        return context.Connection.RemoteIpAddress == null
            ? fallback
            : NormalizeIpAddress(context.Connection.RemoteIpAddress);
    }

    private static string? ExtractFirstValidIp(string? headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

        foreach (var part in headerValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = part.Trim();
            if (candidate.StartsWith("[", StringComparison.Ordinal) && candidate.Contains(']'))
            {
                candidate = candidate[1..candidate.IndexOf(']')];
            }
            else if (candidate.Count(c => c == ':') == 1)
            {
                candidate = candidate[..candidate.LastIndexOf(':')];
            }

            if (IPAddress.TryParse(candidate, out var parsed))
            {
                return NormalizeIpAddress(parsed);
            }
        }

        return null;
    }

    private static string NormalizeIpAddress(IPAddress ipAddress)
    {
        if (IPAddress.IsLoopback(ipAddress))
        {
            return "127.0.0.1";
        }

        return ipAddress.IsIPv4MappedToIPv6
            ? ipAddress.MapToIPv4().ToString()
            : ipAddress.ToString();
    }
}
