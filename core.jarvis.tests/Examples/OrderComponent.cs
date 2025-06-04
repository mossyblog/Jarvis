using core.jarvis.Data;

namespace core.jarvis.tests.Examples;

/// <summary>
/// Example component representing an order in an e-commerce system.
/// Demonstrates proper component structure and business domain modeling.
/// Table: order_component (automatic snake_case mapping)
/// </summary>
public record OrderComponent : IComponent, IVersionedComponent
{
    /// <summary>
    /// Unique identifier for this component instance.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The entity this component belongs to.
    /// Required by IComponent interface.
    /// </summary>
    public Guid OwnerEntityId { get; set; }

    /// <summary>
    /// Unique identifier for this specific order.
    /// Maps to order_number in database.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Customer identifier who placed the order.
    /// Maps to customer_id in database.
    /// </summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the order.
    /// </summary>
    public string Status { get; set; } = "PENDING";

    /// <summary>
    /// Total amount of the order in cents to avoid floating point precision issues.
    /// Maps to total_amount_cents in database.
    /// </summary>
    public int TotalAmountCents { get; set; }

    /// <summary>
    /// When the order was placed.
    /// Maps to order_date in database.
    /// </summary>
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the order was last updated.
    /// Maps to updated_at in database.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional notes about the order.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Shipping address for the order.
    /// Maps to shipping_address in database.
    /// </summary>
    public string ShippingAddress { get; set; } = string.Empty;

    /// <summary>
    /// Whether the order has been paid for.
    /// Maps to is_paid in database.
    /// </summary>
    public bool IsPaid { get; set; }

    /// <summary>
    /// Calculated property for total amount in dollars.
    /// </summary>
    public decimal TotalAmount => TotalAmountCents / 100.0m;
    
    public int? Version { get; set; }
}