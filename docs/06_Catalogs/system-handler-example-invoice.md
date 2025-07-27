# System + Handler Pattern: Complete Invoice Example

This guide walks through building a complete invoice management system using the System + Handler pattern. This example demonstrates all the key concepts in a straightforward, finance-based scenario.

## Table of Contents
1. [Overview](#overview)
2. [Components (Data Models)](#components-data-models)
3. [Handlers (CRUD Operations)](#handlers-crud-operations)
4. [Systems (Business Logic)](#systems-business-logic)
5. [Azure Functions (HTTP Layer)](#azure-functions-http-layer)
6. [Complete Working Example](#complete-working-example)
7. [Testing](#testing)
8. [Common Scenarios](#common-scenarios)

## Overview

We'll build an invoice system that can:
- Create invoices with line items
- Calculate totals automatically
- Update invoice status (Draft → Sent → Paid)
- Handle partial payments
- Query invoices by customer

This demonstrates:
- **Components**: Invoice, LineItem, Payment
- **Handlers**: One for each component type
- **Systems**: InvoiceSystem orchestrates everything
- **Functions**: Thin HTTP adapters

## Components (Data Models)

Components are simple data structures with no logic:

```csharp
// Invoice.cs
public record Invoice : BaseComponent
{
    public string InvoiceNumber { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public decimal Subtotal { get; init; }
    public decimal TaxRate { get; init; } = 0.10m; // 10% default
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal BalanceDue { get; init; }
    public InvoiceStatus Status { get; init; } = InvoiceStatus.Draft;
    public DateTime DueDate { get; init; }
    public DateTime? SentDate { get; init; }
    public DateTime? PaidDate { get; init; }
}

// LineItem.cs
public record LineItem : BaseComponent
{
    public Guid InvoiceId { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}

// Payment.cs
public record Payment : BaseComponent
{
    public Guid InvoiceId { get; init; }
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty; // "card", "bank", "cash"
    public string ReferenceNumber { get; init; } = string.Empty;
    public DateTime PaymentDate { get; init; }
}

// Enums
public enum InvoiceStatus
{
    Draft,
    Sent,
    PartiallyPaid,
    Paid,
    Overdue,
    Cancelled
}
```

## Handlers (CRUD Operations)

Each handler manages one component type. They only do CRUD, no business logic:

```csharp
// InvoiceHandler.cs
public class InvoiceHandler : ComponentHandler<Invoice>
{
    public InvoiceHandler(IDataContext dataContext, ILogger<InvoiceHandler> logger)
        : base(dataContext, logger)
    {
    }

    public async Task<Invoice> CreateInvoice(Invoice newInvoice)
    {
        // Handler just saves - no calculations or validation
        var invoice = newInvoice with { OwnerEntityId = OwnerEntityId };
        await DataContext.Commit(invoice);
        Logger.LogInformation("Created invoice {Number} for customer {CustomerId}", 
            invoice.InvoiceNumber, invoice.CustomerId);
        return invoice;
    }

    public async Task<Invoice> UpdateInvoice(Invoice updatedInvoice)
    {
        // Simple update - no business logic
        var invoice = updatedInvoice with 
        { 
            OwnerEntityId = OwnerEntityId,
            LastUpdated = DateTime.UtcNow 
        };
        await DataContext.Commit(invoice);
        return invoice;
    }

    public async Task<Invoice?> GetByInvoiceNumber(string invoiceNumber)
    {
        var results = await DataContext.Query()
            .WithAll<Invoice>(inv => inv.InvoiceNumber == invoiceNumber)
            .ToList<Invoice>();
        
        return results.FirstOrDefault();
    }
}

// LineItemHandler.cs
public class LineItemHandler : ComponentHandler<LineItem>
{
    public LineItemHandler(IDataContext dataContext, ILogger<LineItemHandler> logger)
        : base(dataContext, logger)
    {
    }

    public async Task<LineItem> CreateLineItem(LineItem newItem)
    {
        var item = newItem with { OwnerEntityId = OwnerEntityId };
        await DataContext.Commit(item);
        return item;
    }

    public async Task<List<LineItem>> GetInvoiceLineItems(Guid invoiceId)
    {
        return await DataContext.Query()
            .WithAll<LineItem>(item => item.InvoiceId == invoiceId)
            .ToList<LineItem>();
    }

    public async Task DeleteLineItem(Guid lineItemId)
    {
        var item = await Get();
        if (item != null)
        {
            await DataContext.Remove<LineItem>(lineItemId);
        }
    }
}

// PaymentHandler.cs
public class PaymentHandler : ComponentHandler<Payment>
{
    public PaymentHandler(IDataContext dataContext, ILogger<PaymentHandler> logger)
        : base(dataContext, logger)
    {
    }

    public async Task<Payment> RecordPayment(Payment newPayment)
    {
        var payment = newPayment with { OwnerEntityId = OwnerEntityId };
        await DataContext.Commit(payment);
        Logger.LogInformation("Recorded payment of {Amount} for invoice {InvoiceId}", 
            payment.Amount, payment.InvoiceId);
        return payment;
    }

    public async Task<List<Payment>> GetInvoicePayments(Guid invoiceId)
    {
        return await DataContext.Query()
            .WithAll<Payment>(p => p.InvoiceId == invoiceId)
            .ToList<Payment>();
    }
}
```

## Systems (Business Logic)

The System orchestrates handlers and contains all business logic:

```csharp
// InvoiceSystem.cs
public class InvoiceSystem
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<InvoiceSystem> _logger;
    private static int _invoiceCounter = 1000; // Simple counter for demo

    public InvoiceSystem(IDataContext dataContext, ILogger<InvoiceSystem> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new invoice with line items and calculates totals
    /// </summary>
    public async Task<List<IComponent>> CreateInvoiceWithItems(CreateInvoiceRequest request)
    {
        var components = new List<IComponent>();
        
        // Validate request
        if (!request.Items.Any())
        {
            throw new ValidationException("Invoice must have at least one line item");
        }

        if (request.DueDateDays < 0)
        {
            throw new ValidationException("Due date days cannot be negative");
        }

        // Generate invoice number (in real app, this would be more sophisticated)
        var invoiceNumber = $"INV-{DateTime.UtcNow.Year}-{Interlocked.Increment(ref _invoiceCounter):D5}";
        var invoiceId = Guid.NewGuid();

        // Calculate totals from line items
        decimal subtotal = 0;
        var lineItems = new List<LineItem>();

        foreach (var itemRequest in request.Items)
        {
            if (itemRequest.Quantity <= 0)
            {
                throw new ValidationException($"Invalid quantity for {itemRequest.Description}");
            }

            if (itemRequest.UnitPrice < 0)
            {
                throw new ValidationException($"Invalid price for {itemRequest.Description}");
            }

            var lineTotal = itemRequest.Quantity * itemRequest.UnitPrice;
            subtotal += lineTotal;

            var lineItem = new LineItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                Description = itemRequest.Description,
                Quantity = itemRequest.Quantity,
                UnitPrice = itemRequest.UnitPrice,
                LineTotal = lineTotal,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            lineItems.Add(lineItem);
        }

        // Calculate tax and total
        var taxAmount = Math.Round(subtotal * request.TaxRate, 2);
        var totalAmount = subtotal + taxAmount;

        // Create invoice
        var invoiceHandler = _dataContext.For<InvoiceHandler>(invoiceId);
        var invoice = await invoiceHandler.CreateInvoice(new Invoice
        {
            Id = invoiceId,
            InvoiceNumber = invoiceNumber,
            CustomerId = request.CustomerId,
            Subtotal = subtotal,
            TaxRate = request.TaxRate,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            PaidAmount = 0,
            BalanceDue = totalAmount,
            Status = InvoiceStatus.Draft,
            DueDate = DateTime.UtcNow.AddDays(request.DueDateDays),
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        });
        components.Add(invoice);

        // Create line items
        foreach (var lineItem in lineItems)
        {
            var itemHandler = _dataContext.For<LineItemHandler>(lineItem.Id);
            var savedItem = await itemHandler.CreateLineItem(lineItem);
            components.Add(savedItem);
        }

        _logger.LogInformation("Created invoice {Number} with {ItemCount} items, total {Total}", 
            invoiceNumber, lineItems.Count, totalAmount);

        return components;
    }

    /// <summary>
    /// Sends an invoice to the customer (changes status from Draft to Sent)
    /// </summary>
    public async Task<IComponent> SendInvoice(Guid invoiceId)
    {
        // Get current invoice
        var invoiceHandler = _dataContext.For<InvoiceHandler>(invoiceId);
        var invoice = await invoiceHandler.Get();

        if (invoice == null)
        {
            throw new BusinessRuleException("INVOICE_NOT_FOUND", "Invoice not found");
        }

        // Validate status
        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new BusinessRuleException("INVALID_STATUS", 
                $"Cannot send invoice in {invoice.Status} status");
        }

        // Update status
        var sentInvoice = invoice with
        {
            Status = InvoiceStatus.Sent,
            SentDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        var updated = await invoiceHandler.UpdateInvoice(sentInvoice);
        
        _logger.LogInformation("Sent invoice {Number} to customer {CustomerId}", 
            invoice.InvoiceNumber, invoice.CustomerId);

        return updated;
    }

    /// <summary>
    /// Records a payment and updates invoice status
    /// </summary>
    public async Task<List<IComponent>> RecordPayment(RecordPaymentRequest request)
    {
        var components = new List<IComponent>();

        // Validate payment amount
        if (request.Amount <= 0)
        {
            throw new ValidationException("Payment amount must be positive");
        }

        // Get invoice
        var invoiceHandler = _dataContext.For<InvoiceHandler>(request.InvoiceId);
        var invoice = await invoiceHandler.Get();

        if (invoice == null)
        {
            throw new BusinessRuleException("INVOICE_NOT_FOUND", "Invoice not found");
        }

        // Check if already paid
        if (invoice.Status == InvoiceStatus.Paid)
        {
            throw new BusinessRuleException("ALREADY_PAID", "Invoice is already paid in full");
        }

        // Check for overpayment
        if (request.Amount > invoice.BalanceDue)
        {
            throw new BusinessRuleException("OVERPAYMENT", 
                $"Payment amount ({request.Amount}) exceeds balance due ({invoice.BalanceDue})");
        }

        // Record payment
        var paymentHandler = _dataContext.For<PaymentHandler>(Guid.NewGuid());
        var payment = await paymentHandler.RecordPayment(new Payment
        {
            Id = Guid.NewGuid(),
            InvoiceId = request.InvoiceId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            ReferenceNumber = request.ReferenceNumber ?? string.Empty,
            PaymentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        });
        components.Add(payment);

        // Update invoice
        var newPaidAmount = invoice.PaidAmount + request.Amount;
        var newBalance = invoice.TotalAmount - newPaidAmount;
        var newStatus = newBalance == 0 ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;

        var updatedInvoice = invoice with
        {
            PaidAmount = newPaidAmount,
            BalanceDue = newBalance,
            Status = newStatus,
            PaidDate = newStatus == InvoiceStatus.Paid ? DateTime.UtcNow : null,
            LastUpdated = DateTime.UtcNow
        };

        var savedInvoice = await invoiceHandler.UpdateInvoice(updatedInvoice);
        components.Add(savedInvoice);

        _logger.LogInformation("Recorded payment of {Amount} for invoice {Number}. New balance: {Balance}", 
            request.Amount, invoice.InvoiceNumber, newBalance);

        return components;
    }

    /// <summary>
    /// Gets invoice with all related data
    /// </summary>
    public async Task<List<IComponent>> GetInvoiceDetails(Guid invoiceId)
    {
        var components = new List<IComponent>();

        // Get invoice
        var invoiceHandler = _dataContext.For<InvoiceHandler>(invoiceId);
        var invoice = await invoiceHandler.Get();

        if (invoice == null)
        {
            throw new BusinessRuleException("INVOICE_NOT_FOUND", "Invoice not found");
        }
        components.Add(invoice);

        // Get line items
        var itemHandler = _dataContext.For<LineItemHandler>(invoiceId);
        var lineItems = await itemHandler.GetInvoiceLineItems(invoiceId);
        components.AddRange(lineItems);

        // Get payments
        var paymentHandler = _dataContext.For<PaymentHandler>(invoiceId);
        var payments = await paymentHandler.GetInvoicePayments(invoiceId);
        components.AddRange(payments);

        return components;
    }

    /// <summary>
    /// Updates overdue invoices
    /// </summary>
    public async Task<List<IComponent>> MarkOverdueInvoices()
    {
        var updatedInvoices = new List<IComponent>();

        // Find sent invoices past due date
        var overdueInvoices = await _dataContext.Query()
            .WithAll<Invoice>(inv => 
                inv.Status == InvoiceStatus.Sent && 
                inv.DueDate < DateTime.UtcNow)
            .ToList<Invoice>();

        foreach (var invoice in overdueInvoices)
        {
            var handler = _dataContext.For<InvoiceHandler>(invoice.Id);
            var updated = invoice with
            {
                Status = InvoiceStatus.Overdue,
                LastUpdated = DateTime.UtcNow
            };
            
            var savedInvoice = await handler.UpdateInvoice(updated);
            updatedInvoices.Add(savedInvoice);
        }

        _logger.LogInformation("Marked {Count} invoices as overdue", updatedInvoices.Count);
        return updatedInvoices;
    }
}

// Request DTOs
public class CreateInvoiceRequest
{
    public Guid CustomerId { get; set; }
    public int DueDateDays { get; set; } = 30;
    public decimal TaxRate { get; set; } = 0.10m;
    public List<LineItemRequest> Items { get; set; } = new();
}

public class LineItemRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class RecordPaymentRequest
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
}
```

## Azure Functions (HTTP Layer)

Functions are thin - they just adapt HTTP to System calls:

```csharp
// InvoiceFunction.cs
public class InvoiceFunction
{
    private readonly InvoiceSystem _invoiceSystem;
    private readonly ILogger<InvoiceFunction> _logger;

    public InvoiceFunction(InvoiceSystem invoiceSystem, ILogger<InvoiceFunction> logger)
    {
        _invoiceSystem = invoiceSystem;
        _logger = logger;
    }

    [Function("CreateInvoice")]
    public async Task<HttpResponseData> CreateInvoice(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "invoices")] 
        HttpRequestData req)
    {
        var requestBody = await req.ReadAsStringAsync();
        var request = JsonSerializer.Deserialize<CreateInvoiceRequest>(requestBody);
        
        if (request == null)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var components = await _invoiceSystem.CreateInvoiceWithItems(request);
        
        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(components);
        return response;
    }

    [Function("SendInvoice")]
    public async Task<HttpResponseData> SendInvoice(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "invoices/{invoiceId}/send")] 
        HttpRequestData req,
        Guid invoiceId)
    {
        var invoice = await _invoiceSystem.SendInvoice(invoiceId);
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(invoice);
        return response;
    }

    [Function("RecordPayment")]
    public async Task<HttpResponseData> RecordPayment(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "invoices/payments")] 
        HttpRequestData req)
    {
        var requestBody = await req.ReadAsStringAsync();
        var request = JsonSerializer.Deserialize<RecordPaymentRequest>(requestBody);
        
        if (request == null)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var components = await _invoiceSystem.RecordPayment(request);
        
        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(components);
        return response;
    }

    [Function("GetInvoice")]
    public async Task<HttpResponseData> GetInvoice(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "invoices/{invoiceId}")] 
        HttpRequestData req,
        Guid invoiceId)
    {
        var components = await _invoiceSystem.GetInvoiceDetails(invoiceId);
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(components);
        return response;
    }
}
```

## Complete Working Example

Here's how it all works together:

```csharp
// 1. Create an invoice
POST /api/invoices
{
    "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "dueDateDays": 30,
    "taxRate": 0.08,
    "items": [
        {
            "description": "Consulting Services - January",
            "quantity": 40,
            "unitPrice": 150.00
        },
        {
            "description": "Software License",
            "quantity": 1,
            "unitPrice": 500.00
        }
    ]
}

// Response: List of components
[
    {
        "$type": "Invoice",
        "id": "invoice-id",
        "invoiceNumber": "INV-2024-01001",
        "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "subtotal": 6500.00,
        "taxRate": 0.08,
        "taxAmount": 520.00,
        "totalAmount": 7020.00,
        "paidAmount": 0,
        "balanceDue": 7020.00,
        "status": "Draft",
        "dueDate": "2024-02-15T00:00:00Z"
    },
    {
        "$type": "LineItem",
        "id": "line-item-1",
        "invoiceId": "invoice-id",
        "description": "Consulting Services - January",
        "quantity": 40,
        "unitPrice": 150.00,
        "lineTotal": 6000.00
    },
    {
        "$type": "LineItem",
        "id": "line-item-2",
        "invoiceId": "invoice-id",
        "description": "Software License",
        "quantity": 1,
        "unitPrice": 500.00,
        "lineTotal": 500.00
    }
]

// 2. Send the invoice
POST /api/invoices/{invoice-id}/send

// Response: Updated invoice
{
    "$type": "Invoice",
    "status": "Sent",
    "sentDate": "2024-01-16T12:00:00Z",
    // ... other fields
}

// 3. Record a payment
POST /api/invoices/payments
{
    "invoiceId": "invoice-id",
    "amount": 3000.00,
    "paymentMethod": "bank",
    "referenceNumber": "CHK-12345"
}

// Response: Payment and updated invoice
[
    {
        "$type": "Payment",
        "id": "payment-id",
        "invoiceId": "invoice-id",
        "amount": 3000.00,
        "paymentMethod": "bank",
        "referenceNumber": "CHK-12345",
        "paymentDate": "2024-01-20T10:00:00Z"
    },
    {
        "$type": "Invoice",
        "paidAmount": 3000.00,
        "balanceDue": 4020.00,
        "status": "PartiallyPaid",
        // ... other fields
    }
]
```

## Testing

### Testing the System
```csharp
[Fact]
public async Task InvoiceSystem_CreateInvoice_CalculatesTotalsCorrectly()
{
    // Arrange
    var system = _serviceProvider.GetRequiredService<InvoiceSystem>();
    var request = new CreateInvoiceRequest
    {
        CustomerId = Guid.NewGuid(),
        TaxRate = 0.10m,
        Items = new List<LineItemRequest>
        {
            new() { Description = "Service A", Quantity = 2, UnitPrice = 100.00m },
            new() { Description = "Service B", Quantity = 1, UnitPrice = 50.00m }
        }
    };
    
    // Act
    var components = await system.CreateInvoiceWithItems(request);
    
    // Assert
    var invoice = components.OfType<Invoice>().First();
    invoice.Subtotal.ShouldBe(250.00m);      // (2*100) + (1*50)
    invoice.TaxAmount.ShouldBe(25.00m);      // 10% of 250
    invoice.TotalAmount.ShouldBe(275.00m);   // 250 + 25
    invoice.BalanceDue.ShouldBe(275.00m);    // No payments yet
    
    var lineItems = components.OfType<LineItem>().ToList();
    lineItems.Count.ShouldBe(2);
    lineItems[0].LineTotal.ShouldBe(200.00m);
    lineItems[1].LineTotal.ShouldBe(50.00m);
    
    // Cleanup
    TrackEntity(invoice.Id);
    lineItems.ForEach(item => TrackEntity(item.Id));
}

[Fact]
public async Task InvoiceSystem_RecordPayment_UpdatesBalanceCorrectly()
{
    // Arrange - Create invoice first
    var system = _serviceProvider.GetRequiredService<InvoiceSystem>();
    var createRequest = new CreateInvoiceRequest
    {
        CustomerId = Guid.NewGuid(),
        Items = new List<LineItemRequest>
        {
            new() { Description = "Service", Quantity = 1, UnitPrice = 1000.00m }
        }
    };
    
    var createResult = await system.CreateInvoiceWithItems(createRequest);
    var invoice = createResult.OfType<Invoice>().First();
    TrackEntity(invoice.Id);
    
    // Send invoice (required before payment)
    await system.SendInvoice(invoice.Id);
    
    // Act - Record payment
    var paymentRequest = new RecordPaymentRequest
    {
        InvoiceId = invoice.Id,
        Amount = 600.00m,
        PaymentMethod = "card",
        ReferenceNumber = "CARD-123"
    };
    
    var paymentResult = await system.RecordPayment(paymentRequest);
    
    // Assert
    var payment = paymentResult.OfType<Payment>().First();
    var updatedInvoice = paymentResult.OfType<Invoice>().First();
    
    payment.Amount.ShouldBe(600.00m);
    updatedInvoice.PaidAmount.ShouldBe(600.00m);
    updatedInvoice.BalanceDue.ShouldBe(500.00m);  // 1100 - 600
    updatedInvoice.Status.ShouldBe(InvoiceStatus.PartiallyPaid);
    
    TrackEntity(payment.Id);
}
```

### Testing Handlers
```csharp
[Fact]
public async Task InvoiceHandler_CreateInvoice_StoresCorrectly()
{
    // Arrange
    var entityId = Guid.NewGuid();
    var handler = TestDataContext().For<InvoiceHandler>(entityId);
    TrackEntity(entityId);
    
    var invoice = new Invoice
    {
        Id = Guid.NewGuid(),
        InvoiceNumber = "TEST-001",
        CustomerId = Guid.NewGuid(),
        TotalAmount = 100.00m,
        Status = InvoiceStatus.Draft,
        DueDate = DateTime.UtcNow.AddDays(30),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    
    // Act
    var saved = await handler.CreateInvoice(invoice);
    
    // Assert
    saved.OwnerEntityId.ShouldBe(entityId);
    saved.InvoiceNumber.ShouldBe("TEST-001");
    
    // Verify retrieval
    var retrieved = await handler.Get();
    retrieved.ShouldNotBeNull();
    retrieved.Id.ShouldBe(invoice.Id);
}
```

## Common Scenarios

### Scenario 1: Cancel an Invoice
```csharp
public async Task<IComponent> CancelInvoice(Guid invoiceId, string reason)
{
    var handler = _dataContext.For<InvoiceHandler>(invoiceId);
    var invoice = await handler.Get();
    
    if (invoice == null)
        throw new BusinessRuleException("NOT_FOUND", "Invoice not found");
        
    if (invoice.Status == InvoiceStatus.Paid)
        throw new BusinessRuleException("CANNOT_CANCEL", "Cannot cancel paid invoice");
        
    var cancelled = invoice with
    {
        Status = InvoiceStatus.Cancelled,
        UpdatedAt = DateTime.UtcNow
    };
    
    return await handler.UpdateInvoice(cancelled);
}
```

### Scenario 2: Apply Discount
```csharp
public async Task<List<IComponent>> ApplyDiscount(Guid invoiceId, decimal discountPercent)
{
    if (discountPercent <= 0 || discountPercent > 100)
        throw new ValidationException("Invalid discount percentage");
        
    var handler = _dataContext.For<InvoiceHandler>(invoiceId);
    var invoice = await handler.Get();
    
    if (invoice == null)
        throw new BusinessRuleException("NOT_FOUND", "Invoice not found");
        
    if (invoice.Status != InvoiceStatus.Draft)
        throw new BusinessRuleException("INVALID_STATUS", "Can only apply discount to draft invoices");
        
    var discountAmount = Math.Round(invoice.Subtotal * (discountPercent / 100), 2);
    var newSubtotal = invoice.Subtotal - discountAmount;
    var newTax = Math.Round(newSubtotal * invoice.TaxRate, 2);
    var newTotal = newSubtotal + newTax;
    
    var updated = invoice with
    {
        Subtotal = newSubtotal,
        TaxAmount = newTax,
        TotalAmount = newTotal,
        BalanceDue = newTotal,
        UpdatedAt = DateTime.UtcNow
    };
    
    var saved = await handler.UpdateInvoice(updated);
    return new List<IComponent> { saved };
}
```

### Scenario 3: Monthly Invoice Report
```csharp
public async Task<List<IComponent>> GetMonthlyInvoiceReport(int year, int month)
{
    var startDate = new DateTime(year, month, 1);
    var endDate = startDate.AddMonths(1);
    
    var invoices = await _dataContext.Query()
        .WithAll<Invoice>(inv => 
            inv.CreatedAt >= startDate && 
            inv.CreatedAt < endDate)
        .ToList<Invoice>();
        
    return invoices.Cast<IComponent>().ToList();
}
```

## Key Takeaways

1. **Components are just data** - No business logic, just properties
2. **Handlers do CRUD only** - Create, Read, Update, Delete operations
3. **Systems orchestrate** - All business logic lives here
4. **Functions are thin** - Just HTTP adaptation, no logic
5. **Return IComponent** - Always return components, not custom objects
6. **Direct handler calls** - Systems call handlers directly via `_dataContext.For<T>()`

This pattern makes the code:
- **Testable** - Each layer can be tested independently
- **Maintainable** - Clear separation of concerns
- **Scalable** - Easy to add new features without touching existing code
- **Understandable** - Each piece has one clear responsibility