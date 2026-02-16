using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.Services.Payment;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Infrastructure.Services.Payment;

/// <summary>
/// Factory for creating payment service instances based on gateway type
/// Implements Factory pattern for payment service creation
/// </summary>
public class PaymentServiceFactory : IPaymentServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentServiceFactory> _logger;

    public PaymentServiceFactory(IServiceProvider serviceProvider, ILogger<PaymentServiceFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Creates appropriate payment service for the specified gateway
    /// </summary>
    public IPaymentGatewayService CreatePaymentService(PaymentGateway gateway)
    {
        try
        {
            _logger.LogDebug("Creating payment service for gateway {Gateway}", gateway);

            return gateway switch
            {
                PaymentGateway.PayPal => _serviceProvider.GetRequiredService<IPayPalService>(),
                PaymentGateway.Stripe => _serviceProvider.GetRequiredService<IStripeService>(),
                PaymentGateway.SePay => _serviceProvider.GetRequiredService<SePayGatewayAdapter>(),
                _ => throw new NotSupportedException($"Payment gateway {gateway} is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment service for gateway {Gateway}", gateway);
            throw;
        }
    }

    /// <summary>
    /// Gets all available payment gateways
    /// </summary>
    public IReadOnlyList<PaymentGateway> GetAvailableGateways()
    {
        var availableGateways = new List<PaymentGateway>();

        try
        {
            if (_serviceProvider.GetService<IPayPalService>() != null)
                availableGateways.Add(PaymentGateway.PayPal);

            if (_serviceProvider.GetService<IStripeService>() != null)
                availableGateways.Add(PaymentGateway.Stripe);

            // SePay Gateway Adapter
            if (_serviceProvider.GetService<SePayGatewayAdapter>() != null)
                availableGateways.Add(PaymentGateway.SePay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining available gateways");
        }

        return availableGateways.AsReadOnly();
    }

    /// <summary>
    /// Checks if a gateway is currently enabled and available
    /// </summary>
    public bool IsGatewayAvailable(PaymentGateway gateway)
    {
        try
        {
            var service = CreatePaymentService(gateway);
            return service != null;
        }
        catch
        {
            return false;
        }
    }

    #region Additional Methods for Backward Compatibility

    /// <summary>
    /// Gets the appropriate payment service for the specified gateway (nullable version)
    /// </summary>
    public IPaymentGatewayService? GetPaymentService(PaymentGateway gateway)
    {
        try
        {
            return CreatePaymentService(gateway);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets all available payment services
    /// </summary>
    public IEnumerable<IPaymentGatewayService> GetAllPaymentServices()
    {
        var services = new List<IPaymentGatewayService>();

        try
        {
            var paypalService = _serviceProvider.GetService<IPayPalService>();
            if (paypalService != null) services.Add(paypalService);

            var stripeService = _serviceProvider.GetService<IStripeService>();
            if (stripeService != null) services.Add(stripeService);

            var sePayAdapter = _serviceProvider.GetService<SePayGatewayAdapter>();
            if (sePayAdapter != null) services.Add(sePayAdapter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment services");
        }

        return services;
    }

    /// <summary>
    /// Gets all supported gateways
    /// </summary>
    public IEnumerable<PaymentGateway> GetSupportedGateways()
    {
        return GetAvailableGateways();
    }

    /// <summary>
    /// Checks if a specific gateway is supported
    /// </summary>
    public bool IsGatewaySupported(PaymentGateway gateway)
    {
        return IsGatewayAvailable(gateway);
    }

    /// <summary>
    /// Gets gateway-specific service with strong typing
    /// </summary>
    public T? GetGatewayService<T>() where T : class, IPaymentGatewayService
    {
        try
        {
            return _serviceProvider.GetService<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gateway service of type {ServiceType}", typeof(T).Name);
            return null;
        }
    }

    /// <summary>
    /// Gets SePay transaction monitoring service
    /// Note: SePay is a transaction monitoring service, not a traditional payment gateway
    /// </summary>
    public ISePayService? GetSePayService()
    {
        try
        {
            return _serviceProvider.GetService<ISePayService>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SePay service");
            return null;
        }
    }

    /// <summary>
    /// Checks if SePay transaction monitoring is available
    /// </summary>
    public bool IsSePayAvailable()
    {
        try
        {
            var service = _serviceProvider.GetService<ISePayService>();
            return service != null;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}