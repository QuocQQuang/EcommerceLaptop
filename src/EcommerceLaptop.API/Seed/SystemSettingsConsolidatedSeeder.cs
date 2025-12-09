using EcommerceLaptop.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Seed;

public static class SystemSettingsConsolidatedSeeder
{
    public static void Seed(ApplicationDbContext context, ILogger logger)
    {
        var changed = false;

        void Upsert(string key, string value, string category, string dataType, bool isEncrypted, string description)
        {
            var existing = context.SystemSettings.FirstOrDefault(s => s.SettingKey == key);
            if (existing == null)
            {
                context.SystemSettings.Add(new EcommerceLaptop.Core.Entities.SystemSetting
                {
                    Category = category,
                    SettingKey = key,
                    SettingValue = value,
                    DataType = dataType,
                    IsEncrypted = isEncrypted,
                    Description = description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                changed = true;
            }
        }

        // SMTP / Email
        Upsert("smtp_host", "smtp.gmail.com", "Email", "string", false, "SMTP server host");
        Upsert("smtp_port", "587", "Email", "int", false, "SMTP server port");
        Upsert("smtp_username", "", "Email", "string", false, "SMTP username");
        Upsert("smtp_password", "", "Email", "string", true, "SMTP password (encrypted)");
        Upsert("smtp_enable_ssl", "false", "Email", "boolean", false, "Enable SSL for SMTP");
        Upsert("smtp_enable_tls", "true", "Email", "boolean", false, "Enable TLS for SMTP");

        // Email rate limit
        Upsert("email_rate_limit_max_requests", "5", "Notifications", "int", false, "Max emails per window");
        Upsert("email_rate_limit_window_seconds", "900", "Notifications", "int", false, "Rate limit window seconds");
        Upsert("email_rate_limit_enabled", "true", "Notifications", "boolean", false, "Enable email rate limiting");
        Upsert("email_rate_limit_cooldown_minutes", "60", "Notifications", "int", false, "Cooldown when exceeded");

        // Ch seed Email v Notifications theo yu cu; b cc nhm khc

        if (changed)
        {
            context.SaveChanges();
            logger.LogInformation("SystemSettings consolidated seed applied.");
        }
        else
        {
            logger.LogInformation("SystemSettings consolidated seed: no changes needed.");
        }
    }
}


