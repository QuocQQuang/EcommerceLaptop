namespace EcommerceLaptop.Core.DTOs.Admin;

public class SecurityMetricsDto
{
    public int TotalBlocked { get; set; }
    public int RateLimitViolations { get; set; }
    public int ActiveRules { get; set; }
    public int SuspiciousIPs { get; set; }
    public int TodayBlocked { get; set; }
    public List<TopBlockedIpDto> TopBlockedIPs { get; set; } = new();
    public List<EndpointRateLimitStatDto> RateLimitStats { get; set; } = new();
}

public class TopBlockedIpDto
{
    public string Ip { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime LastSeen { get; set; }
}

public class EndpointRateLimitStatDto
{
    public string Endpoint { get; set; } = string.Empty;
    public int Violations { get; set; }
    public DateTime LastViolation { get; set; }
}



