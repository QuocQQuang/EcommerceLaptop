using MediatR;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Features.Payments;

public record GetPaymentByTransactionIdQuery(string TransactionId) : IRequest<Payment?>;

public class GetPaymentByTransactionIdHandler : IRequestHandler<GetPaymentByTransactionIdQuery, Payment?>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetPaymentByTransactionIdHandler> _logger;

    public GetPaymentByTransactionIdHandler(ApplicationDbContext context, ILogger<GetPaymentByTransactionIdHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Payment?> Handle(GetPaymentByTransactionIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.TransactionId == request.TransactionId, cancellationToken);

            _logger.LogInformation("Database lookup for transaction {TransactionId}: {Found}",
                request.TransactionId, payment != null ? "Found" : "Not Found");

            return payment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up payment by transaction ID {TransactionId}", request.TransactionId);
            return null;
        }
    }
}
