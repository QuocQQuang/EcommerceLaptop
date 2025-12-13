using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.DTOs.Order;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using System.Security.Claims;

namespace EcommerceLaptop.API.Features.Orders;

// Get Order By ID
public record GetOrderByIdQuery(int OrderId) : IRequest<OrderDetailsDto>
{
    public int? AuthenticatedUserId { get; init; }
}

public class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderDetailsDto>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetOrderByIdHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<OrderDetailsDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Audits)
            .Include(o => o.User)
            .Include(o => o.Payments) // Added Payments to match GetOrderDetailsAsync if needed, or if UI needs it
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {request.OrderId} not found.");
        }

        return _mapper.Map<OrderDetailsDto>(order);
    }
}

// Get Customer Orders
public record GetCustomerOrdersQuery(int CustomerId, int Page = 1, int PageSize = 10) : IRequest<PagedResult<OrderDto>>
{
    public int? AuthenticatedUserId { get; init; }
}

public class GetCustomerOrdersHandler : IRequestHandler<GetCustomerOrdersQuery, PagedResult<OrderDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCustomerOrdersHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<OrderDto>> Handle(GetCustomerOrdersQuery request, CancellationToken cancellationToken)
    {
        if (request.AuthenticatedUserId.HasValue && request.AuthenticatedUserId != request.CustomerId)
        {
             throw new UnauthorizedAccessException("Access denied");
        }

        var query = _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Payments)
            .Where(o => o.UserId == request.CustomerId)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var orders = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var orderDtos = _mapper.Map<List<OrderDto>>(orders);

        return new PagedResult<OrderDto>
        {
            Items = orderDtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}

// Get Admin Orders
public record GetAdminOrdersQuery(int Page = 1, int PageSize = 20, string? Search = null, string? Status = null, int? CustomerId = null) : IRequest<PagedResult<AdminOrderDto>>;

public class GetAdminOrdersHandler : IRequestHandler<GetAdminOrdersQuery, PagedResult<AdminOrderDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly Microsoft.Extensions.Logging.ILogger<GetAdminOrdersHandler> _logger;

    public GetAdminOrdersHandler(ApplicationDbContext context, IMapper mapper, Microsoft.Extensions.Logging.ILogger<GetAdminOrdersHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<AdminOrderDto>> Handle(GetAdminOrdersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting enhanced admin orders with filters: page={Page}, pageSize={PageSize}, search={Search}, status={Status}, customerId={CustomerId}",
                request.Page, request.PageSize, request.Search, request.Status, request.CustomerId);

            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(o => o.OrderNumber.Contains(request.Search) ||
                                        o.User.Email.Contains(request.Search) ||
                                        o.User.FirstName.Contains(request.Search) ||
                                        o.User.LastName.Contains(request.Search));
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                if (Enum.TryParse<OrderStatus>(request.Status, true, out var orderStatus))
                {
                    query = query.Where(o => o.Status == orderStatus);
                }
            }

            if (request.CustomerId.HasValue)
            {
                query = query.Where(o => o.UserId == request.CustomerId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var adminOrderDtos = _mapper.Map<List<AdminOrderDto>>(orders);

            return new PagedResult<AdminOrderDto>
            {
                Items = adminOrderDtos,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enhanced admin orders with filters: search={Search}, status={Status}, customerId={CustomerId}", request.Search, request.Status, request.CustomerId);
            throw;
        }
    }
}
