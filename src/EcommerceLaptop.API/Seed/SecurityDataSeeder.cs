using EcommerceLaptop.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Seed;

public static class SecurityDataSeeder
{
    public static void SeedIfEmpty(ApplicationDbContext context, ILogger logger)
    {
        var changed = false;

        if (!context.IPBlockRules.Any())
        {
            context.IPBlockRules.AddRange(new[]
            {
                new EcommerceLaptop.Core.Entities.IPBlockRule
                {
                    IPAddress = "203.0.113.45",
                    Type = "blacklist",
                    Reason = "Seed: Suspicious activity",
                    IsActive = true,
                    ThreatLevel = "high",
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new EcommerceLaptop.Core.Entities.IPBlockRule
                {
                    IPAddress = "192.168.1.0/24",
                    Type = "whitelist",
                    Reason = "Seed: Internal network",
                    IsActive = true,
                    ThreatLevel = "low",
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                }
            });
            changed = true;
        }

        if (!context.RateLimitRules.Any())
        {
            context.RateLimitRules.AddRange(new[]
            {
                new EcommerceLaptop.Core.Entities.RateLimitRule
                {
                    Name = "API Login Limit",
                    Endpoint = "/api/auth/login",
                    HttpMethod = "POST",
                    RequestsPerMinute = 5,
                    RequestsPerHour = 100,
                    RequestsPerDay = 1000,
                    IsActive = true,
                    Priority = 10,
                    Description = "Seed: Limit login attempts",
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IPWhitelist = "[]",
                    UserRoleExceptions = "[\"Admin\"]",
                    ApiKeyExceptions = "[]",
                    CooldownSeconds = 60
                },
                new EcommerceLaptop.Core.Entities.RateLimitRule
                {
                    Name = "General API Limit",
                    Endpoint = "/api/*",
                    HttpMethod = "ALL",
                    RequestsPerMinute = 60,
                    RequestsPerHour = 2000,
                    RequestsPerDay = 20000,
                    IsActive = true,
                    Priority = 1,
                    Description = "Seed: General API protection",
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IPWhitelist = "[]",
                    UserRoleExceptions = "[]",
                    ApiKeyExceptions = "[]",
                    CooldownSeconds = 60
                }
            });
            changed = true;
        }

        if (changed)
        {
            context.SaveChanges();
            logger.LogInformation("Seeded initial security data (IP rules, rate limit rules)");
        }
    }
}


