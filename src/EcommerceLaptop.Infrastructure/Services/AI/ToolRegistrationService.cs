using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Infrastructure.Services.AI.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services.AI;

/// <summary>
/// Background service that registers all AI chatbot tools on startup
/// </summary>
public class ToolRegistrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ToolRegistrationService> _logger;

    public ToolRegistrationService(
        IServiceProvider serviceProvider,
        ILogger<ToolRegistrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var toolRegistry = scope.ServiceProvider.GetRequiredService<IToolRegistry>();

            // Register all tools
            _logger.LogInformation("Registering AI chatbot tools...");

            var inventoryTool = scope.ServiceProvider.GetRequiredService<GetProductInventoryTool>();
            toolRegistry.RegisterTool(inventoryTool);

            // Future tools will be registered here:
            // var orderStatusTool = scope.ServiceProvider.GetRequiredService<CheckOrderStatusTool>();
            // toolRegistry.RegisterTool(orderStatusTool);

            var registeredTools = toolRegistry.GetAllDefinitions();
            _logger.LogInformation("Successfully registered {ToolCount} AI tools: {ToolNames}",
                registeredTools.Count,
                string.Join(", ", registeredTools.Select(t => t.Name)));

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering AI chatbot tools");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Tool registration service stopping");
        return Task.CompletedTask;
    }
}
