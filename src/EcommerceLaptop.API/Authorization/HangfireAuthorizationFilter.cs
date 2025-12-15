using Hangfire.Dashboard;
using System.Security.Claims;

namespace EcommerceLaptop.API.Authorization
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Fallback for local development if needed, but safer to enforce auth
            // if (httpContext.Request.Host.Host == "localhost") return true;

            return httpContext.User.Identity?.IsAuthenticated == true &&
                   (httpContext.User.IsInRole("Admin") || httpContext.User.HasClaim("is_admin", "true"));
        }
    }
}
