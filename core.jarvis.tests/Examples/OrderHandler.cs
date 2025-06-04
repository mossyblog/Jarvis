using core.jarvis.Data;
using core.jarvis.Exceptions;
using core.jarvis.Validation;
using Microsoft.Extensions.Logging;

namespace core.jarvis.tests.Examples;

/// <summary>
/// Example handler demonstrating best practices for business logic implementation.
/// Shows validation patterns, audit integration, and error handling.
/// </summary>
public class OrderHandler : ComponentHandler<OrderComponent>
{
    private readonly IAuditService _auditService;

    public OrderHandler(
        IDataContext dataContext, 
        ILogger<OrderHandler> logger,
        IAuditService auditService)
        : base(dataContext, logger)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    /// <summary>
    /// Creates a new order with validation and audit trail.
    /// Demonstrates component creation patterns.
    /// </summary>
    public async Task<OrderComponent> CreateOrder(
        string orderNumber, 
        string customerId, 
        decimal totalAmount, 
        string shippingAddress)
    {
        // Input validation using Guard pattern
        Guard.AgainstEmpty(orderNumber, nameof(orderNumber));
        Guard.AgainstEmpty(customerId, nameof(customerId));
        Guard.AgainstEmpty(shippingAddress, nameof(shippingAddress));
        if (totalAmount <= 0) 
            throw new ValidationException(nameof(totalAmount), "Total amount must be positive");

        // Business rule validation
        var existingOrder = await GetOrDefault();
        Ensure(existingOrder == null, "Order already exists for this entity");

        // Create new order component
        var order = GenerateOrderComponent(OwnerEntityId);
        order.OrderNumber = orderNumber;
        order.CustomerId = customerId;
        order.TotalAmountCents = (int)(totalAmount * 100);
        order.ShippingAddress = shippingAddress;
        order.Status = "PENDING";
        order.OrderDate = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        // Persist to database
        var success = await DataContext.TryCommit(order);
        if (!success)
        {
            throw new ConcurrencyException("Order creation failed due to concurrent modification");
        }

        // Audit trail
        await _auditService.LogEvent("OrderCreated", OwnerEntityId, new
        {
            OrderNumber = orderNumber,
            CustomerId = customerId,
            TotalAmount = totalAmount,
            Status = "PENDING"
        });


        return order;
    }

    /// <summary>
    /// Confirms an order, changing status from PENDING to CONFIRMED.
    /// Demonstrates business state transitions with validation.
    /// </summary>
    public async Task<bool> ConfirmOrder()
    {
        var order = await GetRequired();

        // Business rule validation
        Ensure(order.Status == "PENDING", "Only pending orders can be confirmed");

        // Update status
        var previousStatus = order.Status;
        order.Status = "CONFIRMED";
        order.UpdatedAt = DateTime.UtcNow;

        // Persist changes
        var success = await DataContext.TryCommit(order);
        if (!success)
        {
            throw new ConcurrencyException("Order confirmation failed due to concurrent modification");
        }

        // Audit trail for state change
        await _auditService.LogChange("OrderStatusChanged", OwnerEntityId, previousStatus, "CONFIRMED", new
        {
            From = previousStatus,
            To = "CONFIRMED",
            OrderNumber = order.OrderNumber
        });

        Logger.LogInformation("Confirmed order {OrderNumber} for entity {OwnerEntityId}", 
            order.OrderNumber, OwnerEntityId);

        return true;
    }

    /// <summary>
    /// Cancels an order with reason validation.
    /// Demonstrates complex business logic with audit requirements.
    /// </summary>
    public async Task<bool> CancelOrder(string reason)
    {
        Guard.AgainstEmpty(reason, nameof(reason));

        var order = await GetRequired();

        // Business rule validation
        Ensure(order.Status != "SHIPPED", "Cannot cancel orders that have been shipped");
        Ensure(order.Status != "CANCELLED", "Order is already cancelled");

        // Special validation for paid orders
        if (order.IsPaid)
        {
            Ensure(reason.Length >= 10, "Cancellation reason must be detailed for paid orders");
        }

        var previousStatus = order.Status;
        order.Status = "CANCELLED";
        order.Notes = $"Cancelled: {reason}";
        order.UpdatedAt = DateTime.UtcNow;

        // Persist changes
        var success = await DataContext.TryCommit(order);
        if (!success)
        {
            throw new ConcurrencyException("Order cancellation failed due to concurrent modification");
        }

        // Detailed audit trail for cancellation
        await _auditService.LogEvent("OrderCancelled", OwnerEntityId, new
        {
            OrderNumber = order.OrderNumber,
            PreviousStatus = previousStatus,
            Reason = reason,
            WasPaid = order.IsPaid,
            CancelledAt = DateTime.UtcNow
        });

        Logger.LogWarning("Cancelled order {OrderNumber} (was {PreviousStatus}): {Reason}", 
            order.OrderNumber, previousStatus, reason);

        return true;
    }

    /// <summary>
    /// Gets order summary for reporting.
    /// Demonstrates read-only operations and calculated properties.
    /// </summary>
    public async Task<OrderSummary> GetOrderSummary()
    {
        var order = await GetRequired();

        return new OrderSummary
        {
            EntityId = OwnerEntityId,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            IsPaid = order.IsPaid,
            OrderDate = order.OrderDate,
            UpdatedAt = order.UpdatedAt,
            IsShippable = order.Status == "CONFIRMED" && order.IsPaid,
            DaysOld = (DateTime.UtcNow - order.OrderDate).Days
        };
    }

    private OrderComponent GenerateOrderComponent(Guid ownerEntityId)
    {
        var component = new OrderComponent();
        var ownerEntityIdProp = typeof(OrderComponent).GetProperty("OwnerEntityId");
        if (ownerEntityIdProp != null && ownerEntityIdProp.CanWrite)
        {
            ownerEntityIdProp.SetValue(component, ownerEntityId);
        }
        return component;
    }
}

/// <summary>
/// Data transfer object for order summary information.
/// </summary>
public class OrderSummary
{
    public Guid EntityId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsShippable { get; set; }
    public int DaysOld { get; set; }
}