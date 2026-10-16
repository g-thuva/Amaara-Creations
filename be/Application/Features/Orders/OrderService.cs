using be.Domain.Common;
using be.Domain.Orders;
using be.Domain.Payments;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;

namespace be.Application.Features.Orders;
public sealed class OrderService(IApplicationDbContext db, OrderLifecycleService lifecycle, IRequestActor _actor)
{
    public async Task<List<OrderResponse>> GetUserOrders(CancellationToken ct) => (await Query().Where(o => o.UserId == _actor.User.FindFirstValue(ClaimTypes.NameIdentifier)).OrderByDescending(o => o.Id).ToListAsync(ct)).Select(Map)
        .ToList();
    public async Task<OrderResponse> GetOrder(int id, CancellationToken ct)
    {
        var order = await Query().SingleOrDefaultAsync(o => o.Id == id && o.UserId == _actor.User.FindFirstValue(ClaimTypes.NameIdentifier), ct);
        return order == null ? throw new CommerceException("ORDERS_NOT_FOUND", "Resource not found.", 404) : Map(order);
    }

    public async Task<object?> GetAllOrders(string? status = null, string? search = null, int pageNumber = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = Query();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, out var parsed))
            query = query.Where(o => o.Status == parsed);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o => o.OrderNumber.Contains(search) || o.User!.Name.Contains(search) || o.User.Email!.Contains(search));
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var count = await query.CountAsync(ct);
        var orders = (await query.OrderByDescending(o => o.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct)).Select(Map).ToList();
        return new
        {
            Orders = orders,
            TotalCount = count,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((decimal)count / pageSize)
        };
    }

    public async Task<OrderResponse> GetAdminOrder(int id, CancellationToken ct)
    {
        var order = await Query().SingleOrDefaultAsync(o => o.Id == id, ct);
        return order == null ? throw new CommerceException("ORDERS_NOT_FOUND", "Resource not found.", 404) : Map(order);
    }

    public async Task<OrderResponse> UpdateOrderStatus(int id, UpdateOrderStatusRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, false, out var target) || !Enum.IsDefined(target))
            throw new CommerceException("INVALID_ORDER_TRANSITION", "Invalid order status.");
        await lifecycle.TransitionAsync(id, target, _actor.User.FindFirstValue(ClaimTypes.NameIdentifier)!, request.Reason, ct);
        return await GetAdminOrder(id, ct);
    }

    private IQueryable<Order> Query() => db.Orders.AsNoTracking()
        .Include(o => o.User)
        .Include(o => o.OrderItems)
        .Include(o => o.Payments)
        .Include(o => o.Shipments)
        .Include(o => o.StatusHistory)
        .AsSplitQuery();
    public static OrderResponse Map(Order o)
    {
        var payments = o.Payments.OrderByDescending(p => p.Id).ToList();
        var payment = payments.FirstOrDefault(p => p.Status is PaymentStatus.Paid or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded) ?? payments.FirstOrDefault();
        var shipment = o.Shipments.OrderByDescending(s => s.Id).FirstOrDefault();
        return new OrderResponse
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            UserId = o.UserId,
            UserName = o.User?.Name ?? string.Empty,
            UserEmail = o.User?.Email ?? string.Empty,
            OrderDate = o.OrderDate,
            Total = o.Total,
            Subtotal = o.Subtotal,
            DiscountTotal = o.DiscountTotal,
            ShippingTotal = o.ShippingTotal,
            TaxTotal = o.TaxTotal,
            PaymentFee = o.PaymentFee,
            Currency = o.Currency,
            PricesIncludeTax = o.PricesIncludeTaxSnapshot,
            Status = o.Status.ToString(),
            PaymentStatus = payment?.Status.ToString() ?? "Unpaid",
            FulfilmentStatus = shipment?.Status.ToString() ?? "NotReady",
            ShippingAddress = o.ShippingAddress,
            ShippingCity = o.ShippingCity,
            ShippingCountry = o.ShippingCountry,
            ShippingPostalCode = o.ShippingPostalCode,
            ShippingRecipientName = o.ShippingRecipientName,
            ShippingPhone = o.ShippingPhone,
            ShippingRegion = o.ShippingRegion,
            CouponCode = o.CouponCode,
            ShippingMethodName = o.ShippingMethodName,
            IsPickup = o.IsPickup,
            PickupLocation = o.PickupLocationSnapshot,
            Notes = o.Notes,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            OrderItems = o.OrderItems.Select(i => new OrderItemResponse { Id = i.Id, ItemType = i.ItemType.ToString(), ProductId = i.ProductId ?? 0, ProductVariantId = i.ProductVariantId, CustomDesignId = i.CustomDesignId, ProductName = i.ProductNameSnapshot, Sku = i.SkuSnapshot, ProductImageUrl = i.ImageSnapshot ?? string.Empty, Price = i.Price, Quantity = i.Quantity, Subtotal = i.Subtotal, ConfigurationSnapshotJson = i.ConfigurationSnapshotJson, CustomDesignSnapshotVersion = i.CustomDesignSnapshotVersion })
                .ToList(),
            Payments = payments.Select(p => new PaymentSummaryDto(p.Id, p.Status.ToString(), p.Amount, p.Currency, p.Provider, p.ProviderReference, p.ProviderTransactionId, p.PaidAt))
                .ToList(),
            Shipments = o.Shipments.Select(s => new ShipmentSummaryDto(s.Id, s.Status.ToString(), s.Courier, s.TrackingNumber, s.TrackingUrl, s.ShippedAt, s.DeliveredAt))
                .ToList(),
            History = o.StatusHistory.OrderBy(h => h.Id).Select(h => new OrderHistoryDto(h.PreviousStatus, h.NewStatus, h.Reason, h.CreatedAt)).ToList(),
            AvailableTransitions = Enum.GetValues<OrderStatus>()
                .Where(s => be.Domain.Orders.OrderTransitionPolicy.CanTransition(o.Status, s) && (s != OrderStatus.Cancelled || !payments.Any(p => p.Status is PaymentStatus.Pending or PaymentStatus.Authorized or PaymentStatus.Paid or PaymentStatus.PartiallyRefunded)))
                .Select(s => s.ToString())
                .ToList()
        };
    }
}
