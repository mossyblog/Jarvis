# API Endpoint Routing System for Mixed-Component Forms

## Overview

The API routing system in Jarvis handles complex form submissions that contain multiple component types, orchestrating their save operations across different API endpoints while maintaining data consistency through transactions. This system is built on the ECS (Entity Component System) framework and follows the System + Handler pattern for maximum reliability and maintainability.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Routing Strategy](#routing-strategy)
3. [Transaction Management](#transaction-management)
4. [Error Handling & Rollback](#error-handling--rollback)
5. [API Endpoint Discovery](#api-endpoint-discovery)
6. [Azure Functions Integration](#azure-functions-integration)
7. [Performance Optimizations](#performance-optimizations)
8. [Real-World Examples](#real-world-examples)
9. [Frontend Implementation](#frontend-implementation)
10. [Testing Strategy](#testing-strategy)

## Architecture Overview

The API routing system consists of several layers:

```
┌─────────────────────────────────────────────────────────────┐
│                    Frontend Layer                           │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │   Form State    │  │  API Orchestrator│  │ Error Handler│ │
│  │   Management    │  │                 │  │              │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                  API Gateway Layer                          │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ Route Discovery │  │  Load Balancer  │  │ Rate Limiter │ │
│  │                 │  │                 │  │              │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                Azure Functions Layer                        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ Mixed Component │  │ Transaction     │  │ Component    │ │
│  │ Functions       │  │ Functions       │  │ Functions    │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                   System Layer                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ OrderSystem     │  │ AccountSystem   │  │ AuditSystem  │ │
│  │                 │  │                 │  │              │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                   Handler Layer                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ OrderHandler    │  │ AccountHandler  │  │ AuditHandler │ │
│  │                 │  │                 │  │              │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## Routing Strategy

### 1. Component Type Discovery

The system automatically detects component types in form submissions and routes them to appropriate endpoints:

```typescript
interface ComponentTypeMap {
  Account: '/api/accounts';
  Order: '/api/orders';
  Profile: '/api/profiles';
  Payment: '/api/payments';
  Audit: '/api/audit';
}

interface FormSubmission {
  entityId: string;
  components: {
    [key: string]: ComponentData;
  };
  transaction?: boolean;
  metadata?: {
    userId: string;
    timestamp: string;
    source: string;
  };
}
```

### 2. Routing Decision Matrix

The router uses a decision matrix to determine the optimal routing strategy:

| Component Count | Same System | Different Systems | Strategy |
|----------------|-------------|-------------------|-----------|
| 1 | ✓ | N/A | Direct Route |
| 2-5 | ✓ | N/A | System Transaction |
| 2-5 | N/A | ✓ | Distributed Transaction |
| 6+ | ✓ | N/A | Batch System Operation |
| 6+ | N/A | ✓ | Saga Pattern |

## Transaction Management

### Database Transaction Support

All mixed-component operations use the `ExecuteInTransaction` method from the DataContext:

```csharp
// MixedComponentSystem.cs
public class MixedComponentSystem
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<MixedComponentSystem> _logger;

    public MixedComponentSystem(IDataContext dataContext, ILogger<MixedComponentSystem> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    /// <summary>
    /// Saves multiple components atomically within a database transaction.
    /// Returns all saved components on success, or throws on any failure.
    /// </summary>
    public async Task<List<IComponent>> SaveMixedComponents(MixedComponentRequest request)
    {
        if (request.Components == null || !request.Components.Any())
        {
            throw new ValidationException("At least one component is required");
        }

        // Validate all components before starting transaction
        await ValidateComponents(request.Components);

        // Execute all saves within a single database transaction
        return await _dataContext.ExecuteInTransaction(async () =>
        {
            var savedComponents = new List<IComponent>();
            var entityId = request.EntityId ?? Guid.NewGuid();

            _logger.LogDebug("Starting mixed component transaction for entity: {EntityId}", entityId);

            // Process Account components
            if (request.Components.ContainsKey("Account"))
            {
                var accountHandler = _dataContext.For<AccountHandler>(entityId);
                var account = await accountHandler.CreateOrUpdate(request.Components["Account"]);
                savedComponents.Add(account);
            }

            // Process Order components
            if (request.Components.ContainsKey("Order"))
            {
                var orderHandler = _dataContext.For<OrderHandler>(entityId);
                var order = await orderHandler.CreateOrUpdate(request.Components["Order"]);
                savedComponents.Add(order);
            }

            // Process Profile components
            if (request.Components.ContainsKey("Profile"))
            {
                var profileHandler = _dataContext.For<AccountProfileHandler>(entityId);
                var profile = await profileHandler.CreateOrUpdate(request.Components["Profile"]);
                savedComponents.Add(profile);
            }

            // Log audit event for the transaction
            try
            {
                var auditHandler = _dataContext.For<AuditHandler>(Guid.NewGuid());
                await auditHandler.LogMixedComponentSave(entityId, savedComponents, request.Metadata?.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log audit event, but continuing with transaction");
            }

            _logger.LogInformation("Successfully saved {Count} components for entity: {EntityId}", 
                savedComponents.Count, entityId);

            return savedComponents;
        });
    }

    private async Task ValidateComponents(Dictionary<string, ComponentData> components)
    {
        var errors = new Dictionary<string, string[]>();

        foreach (var (type, data) in components)
        {
            switch (type)
            {
                case "Account":
                    if (string.IsNullOrEmpty(data.Email))
                        errors["Account.Email"] = new[] { "Email is required" };
                    break;
                case "Order":
                    if (data.Amount <= 0)
                        errors["Order.Amount"] = new[] { "Amount must be positive" };
                    break;
                case "Profile":
                    if (string.IsNullOrEmpty(data.Name))
                        errors["Profile.Name"] = new[] { "Name is required" };
                    break;
            }
        }

        if (errors.Any())
        {
            throw new ValidationException(errors);
        }
    }
}

// Request models
public class MixedComponentRequest
{
    public Guid? EntityId { get; set; }
    public Dictionary<string, ComponentData> Components { get; set; } = new();
    public ComponentMetadata? Metadata { get; set; }
}

public class ComponentData
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalFields { get; set; } = new();
}

public class ComponentMetadata
{
    public string? UserId { get; set; }
    public string? Source { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
```

## Error Handling & Rollback

### Comprehensive Error Handling

The system implements multiple layers of error handling:

```csharp
// MixedComponentFunction.cs
public class MixedComponentFunction
{
    private readonly MixedComponentSystem _system;
    private readonly ILogger<MixedComponentFunction> _logger;

    public MixedComponentFunction(MixedComponentSystem system, ILogger<MixedComponentFunction> logger)
    {
        _system = system;
        _logger = logger;
    }

    [Function("SaveMixedComponents")]
    public async Task<HttpResponseData> SaveMixedComponents(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "components/mixed")] 
        HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            // Extract user context
            var userIdStr = executionContext.GetUserId();
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, 
                    "UNAUTHORIZED", "User not authenticated");
            }

            // Parse request
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest,
                    "INVALID_REQUEST", "Request body is required");
            }

            var request = JsonSerializer.Deserialize<MixedComponentRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest,
                    "INVALID_REQUEST", "Invalid request format");
            }

            // Add metadata
            request.Metadata = new ComponentMetadata
            {
                UserId = userId.ToString(),
                Source = "web-form",
                Timestamp = DateTime.UtcNow,
                IpAddress = req.Headers.GetValues("X-Forwarded-For")?.FirstOrDefault() ?? "unknown",
                UserAgent = req.Headers.GetValues("User-Agent")?.FirstOrDefault() ?? "unknown"
            };

            // Execute the system operation
            var savedComponents = await _system.SaveMixedComponents(request);

            // Return success response
            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(savedComponents, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            }));
            return response;
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation failed for mixed component save: {Errors}", 
                string.Join(", ", ex.Errors.SelectMany(e => e.Value)));
            return await CreateValidationErrorResponse(req, ex.Errors);
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning("Business rule violation: {Code} - {Message}", ex.Code, ex.Message);
            return await CreateErrorResponse(req, HttpStatusCode.Conflict, ex.Code, ex.Message);
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning("Concurrency conflict during mixed component save: {Message}", ex.Message);
            return await CreateErrorResponse(req, HttpStatusCode.Conflict, 
                "CONCURRENCY_CONFLICT", "Data was modified by another user. Please refresh and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during mixed component save");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR", "An unexpected error occurred. Please try again.");
        }
    }

    private async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req, 
        HttpStatusCode statusCode, 
        string code, 
        string message)
    {
        var error = new
        {
            Code = code,
            Message = message,
            StatusCode = (int)statusCode,
            Timestamp = DateTime.UtcNow
        };

        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(error));
        return response;
    }

    private async Task<HttpResponseData> CreateValidationErrorResponse(
        HttpRequestData req,
        Dictionary<string, string[]> errors)
    {
        var validationError = new
        {
            Code = "VALIDATION_FAILED",
            Message = "One or more validation errors occurred",
            StatusCode = 400,
            Errors = errors,
            Timestamp = DateTime.UtcNow
        };

        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(validationError));
        return response;
    }
}
```

### Rollback Strategies

1. **Database Transaction Rollback**: Automatic rollback on any exception within `ExecuteInTransaction`
2. **Compensation Actions**: For distributed operations across multiple systems
3. **Event Sourcing**: Rollback through compensating events
4. **Audit Trail**: Complete transaction history for debugging and compliance

## API Endpoint Discovery

### Dynamic Endpoint Registration

The system uses a registry pattern to discover and register component endpoints:

```csharp
// IEndpointRegistry.cs
public interface IEndpointRegistry
{
    void RegisterEndpoint<TComponent>(string endpoint) where TComponent : IComponent;
    string GetEndpoint<TComponent>() where TComponent : IComponent;
    string GetEndpoint(Type componentType);
    Dictionary<Type, string> GetAllEndpoints();
}

// EndpointRegistry.cs
public class EndpointRegistry : IEndpointRegistry
{
    private readonly Dictionary<Type, string> _endpoints = new();

    public void RegisterEndpoint<TComponent>(string endpoint) where TComponent : IComponent
    {
        _endpoints[typeof(TComponent)] = endpoint;
    }

    public string GetEndpoint<TComponent>() where TComponent : IComponent
    {
        return _endpoints.TryGetValue(typeof(TComponent), out var endpoint) 
            ? endpoint 
            : throw new InvalidOperationException($"No endpoint registered for {typeof(TComponent).Name}");
    }

    public string GetEndpoint(Type componentType)
    {
        return _endpoints.TryGetValue(componentType, out var endpoint)
            ? endpoint
            : throw new InvalidOperationException($"No endpoint registered for {componentType.Name}");
    }

    public Dictionary<Type, string> GetAllEndpoints()
    {
        return new Dictionary<Type, string>(_endpoints);
    }
}

// ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterApiEndpoints(this IServiceCollection services)
    {
        services.AddSingleton<IEndpointRegistry, EndpointRegistry>();
        
        var registry = services.BuildServiceProvider().GetRequiredService<IEndpointRegistry>();
        
        // Register component endpoints
        registry.RegisterEndpoint<Account>("/api/accounts");
        registry.RegisterEndpoint<Order>("/api/orders");
        registry.RegisterEndpoint<Profile>("/api/profiles");
        registry.RegisterEndpoint<Payment>("/api/payments");
        registry.RegisterEndpoint<Audit>("/api/audit");
        
        return services;
    }
}
```

## Azure Functions Integration

### Function Configuration

Configure the Azure Functions host for optimal performance:

```json
// host.json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "excludedTypes": "Request"
      }
    }
  },
  "functionTimeout": "00:10:00",
  "healthMonitor": {
    "enabled": true,
    "healthCheckInterval": "00:00:30",
    "healthCheckWindow": "00:02:00",
    "healthCheckThreshold": 6,
    "counterThreshold": 0.80
  },
  "http": {
    "routePrefix": "api",
    "maxOutstandingRequests": 200,
    "maxConcurrentRequests": 100,
    "dynamicThrottlesEnabled": true
  },
  "extensions": {
    "http": {
      "routePrefix": "api"
    }
  }
}
```

### Dependency Injection Setup

```csharp
// Program.cs
public class Program
{
    public static void Main()
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureServices(services =>
            {
                // Register Jarvis framework
                services.RegisterJarvis(LogLevel.Information);
                
                // Register API endpoints
                services.RegisterApiEndpoints();
                
                // Register systems
                services.AddScoped<MixedComponentSystem>();
                services.AddScoped<OrderSystem>();
                services.AddScoped<AccountSystem>();
                
                // Register handlers
                services.AddScoped<IComponentHandler, AccountHandler>();
                services.AddScoped<AccountHandler>();
                services.AddScoped<IComponentHandler, OrderHandler>();
                services.AddScoped<OrderHandler>();
                services.AddScoped<IComponentHandler, AccountProfileHandler>();
                services.AddScoped<AccountProfileHandler>();
                
                // Add performance monitoring
                services.AddApplicationInsightsTelemetryWorkerService();
            })
            .Build();

        host.Run();
    }
}
```

## Performance Optimizations

### 1. Bulk Operations

For large component sets, use bulk operations:

```csharp
public async Task<List<IComponent>> SaveMixedComponentsBulk(BulkMixedComponentRequest request)
{
    if (request.BatchSize <= 0 || request.BatchSize > 100)
    {
        throw new ValidationException("Batch size must be between 1 and 100");
    }

    var allSavedComponents = new List<IComponent>();
    var batches = request.ComponentBatches.Chunk(request.BatchSize);

    foreach (var batch in batches)
    {
        var batchComponents = await _dataContext.ExecuteInTransaction(async () =>
        {
            var savedComponents = new List<IComponent>();
            
            // Process batch in parallel where possible
            var tasks = batch.Select(async componentSet =>
            {
                var entityId = componentSet.EntityId ?? Guid.NewGuid();
                var componentResults = new List<IComponent>();

                // Process components for this entity
                foreach (var (type, data) in componentSet.Components)
                {
                    var component = await ProcessComponent(type, data, entityId);
                    componentResults.Add(component);
                }

                return componentResults;
            });

            var batchResults = await Task.WhenAll(tasks);
            savedComponents.AddRange(batchResults.SelectMany(r => r));
            
            return savedComponents;
        });

        allSavedComponents.AddRange(batchComponents);
    }

    return allSavedComponents;
}
```

### 2. Connection Pooling

```csharp
// Configure connection pooling in startup
services.Configure<PgClientOptions>(options =>
{
    options.MaxPoolSize = 100;
    options.MinPoolSize = 5;
    options.ConnectionIdleLifetime = TimeSpan.FromMinutes(15);
    options.ConnectionMaxLifetime = TimeSpan.FromHours(1);
});
```

### 3. Caching Strategy

```csharp
public class CachedEndpointRegistry : IEndpointRegistry
{
    private readonly IEndpointRegistry _inner;
    private readonly IMemoryCache _cache;
    
    public CachedEndpointRegistry(IEndpointRegistry inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }
    
    public string GetEndpoint<TComponent>() where TComponent : IComponent
    {
        var cacheKey = $"endpoint_{typeof(TComponent).Name}";
        return _cache.GetOrCreate(cacheKey, factory =>
        {
            factory.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return _inner.GetEndpoint<TComponent>();
        });
    }
}
```

## Real-World Examples

### Example 1: Account and Order Creation

This example shows a real-world scenario where a customer creates an account and places an order simultaneously:

```typescript
// Frontend form submission
interface AccountOrderForm {
  account: {
    email: string;
    password: string;
    name: string;
  };
  order: {
    productId: string;
    quantity: number;
    amount: number;
    shippingAddress: Address;
  };
}

const submitAccountOrder = async (formData: AccountOrderForm): Promise<ComponentResponse> => {
  const request: MixedComponentRequest = {
    entityId: undefined, // Will be generated
    components: {
      Account: {
        email: formData.account.email,
        passwordHash: await hashPassword(formData.account.password),
        name: formData.account.name,
        isActive: true,
        authMethod: 'password'
      },
      Order: {
        productId: formData.order.productId,
        quantity: formData.order.quantity,
        amount: formData.order.amount,
        status: 'pending',
        shippingAddress: JSON.stringify(formData.order.shippingAddress)
      }
    }
  };

  const response = await apiService.post('/api/components/mixed', request);
  
  if (response.error) {
    throw new Error(response.error.message);
  }

  return response.data;
};
```

### Example 2: Profile Update with Audit Trail

```csharp
// Backend system handling profile update with automatic audit
public async Task<List<IComponent>> UpdateProfileWithAudit(Guid entityId, ProfileUpdateRequest request)
{
    return await _dataContext.ExecuteInTransaction(async () =>
    {
        var savedComponents = new List<IComponent>();

        // Update the profile
        var profileHandler = _dataContext.For<AccountProfileHandler>(entityId);
        var currentProfile = await profileHandler.Get();
        
        if (currentProfile == null)
        {
            throw new BusinessRuleException("PROFILE_NOT_FOUND", "Profile not found");
        }

        var updatedProfile = currentProfile with
        {
            Name = request.Name ?? currentProfile.Name,
            Email = request.Email ?? currentProfile.Email,
            Phone = request.Phone ?? currentProfile.Phone,
            LastUpdated = DateTime.UtcNow
        };

        var savedProfile = await profileHandler.UpdateProfile(updatedProfile);
        savedComponents.Add(savedProfile);

        // Create audit trail
        var auditHandler = _dataContext.For<AuditHandler>(Guid.NewGuid());
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            Action = "ProfileUpdate",
            ComponentType = "Profile",
            OldValues = JsonSerializer.Serialize(currentProfile),
            NewValues = JsonSerializer.Serialize(savedProfile),
            UserId = request.UserId,
            Timestamp = DateTime.UtcNow,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent
        };

        var savedAudit = await auditHandler.LogEvent(auditEvent);
        savedComponents.Add(savedAudit);

        return savedComponents;
    });
}
```

## Frontend Implementation

### API Service Layer

```typescript
// apiService.ts - Mixed component operations
export class MixedComponentApiService {
  private readonly baseUrl: string;
  private readonly authHeaders: () => HeadersInit;

  constructor(baseUrl: string, getAuthHeaders: () => HeadersInit) {
    this.baseUrl = baseUrl;
    this.authHeaders = getAuthHeaders;
  }

  async saveMixedComponents(request: MixedComponentRequest): Promise<ApiResponse<IComponent[]>> {
    try {
      const response = await fetch(`${this.baseUrl}/api/components/mixed`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...this.authHeaders()
        },
        body: JSON.stringify(request)
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: 'Request failed' }));
        return {
          error: {
            message: errorData.message || `Error: ${response.status}`,
            code: errorData.code || `HTTP_${response.status}`,
            details: errorData.errors || {}
          }
        };
      }

      const data = await response.json();
      return { data };
    } catch (error) {
      return {
        error: {
          message: error instanceof Error ? error.message : 'Network error',
          code: 'NETWORK_ERROR'
        }
      };
    }
  }

  async saveWithRetry(
    request: MixedComponentRequest, 
    maxRetries: number = 3
  ): Promise<ApiResponse<IComponent[]>> {
    let lastError: ApiError | undefined;

    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      const result = await this.saveMixedComponents(request);
      
      if (!result.error) {
        return result;
      }

      lastError = result.error;

      // Don't retry on client errors (4xx)
      if (result.error.code.startsWith('HTTP_4')) {
        break;
      }

      // Exponential backoff
      if (attempt < maxRetries) {
        await new Promise(resolve => setTimeout(resolve, Math.pow(2, attempt) * 1000));
      }
    }

    return { error: lastError! };
  }
}
```

### Form Orchestration

```typescript
// formOrchestrator.ts - Coordinates complex form submissions
export class FormOrchestrator {
  private readonly apiService: MixedComponentApiService;
  private readonly validationService: ValidationService;

  constructor(apiService: MixedComponentApiService, validationService: ValidationService) {
    this.apiService = apiService;
    this.validationService = validationService;
  }

  async submitMixedForm<T extends Record<string, unknown>>(
    formData: T,
    componentMapping: ComponentMapping<T>
  ): Promise<SubmissionResult> {
    try {
      // 1. Validate form data
      const validationResult = await this.validationService.validateForm(formData, componentMapping);
      if (!validationResult.isValid) {
        return {
          success: false,
          errors: validationResult.errors
        };
      }

      // 2. Transform form data to components
      const components = this.transformToComponents(formData, componentMapping);

      // 3. Create request
      const request: MixedComponentRequest = {
        entityId: formData.entityId as string || undefined,
        components,
        metadata: {
          source: 'web-form',
          timestamp: new Date().toISOString(),
          userAgent: navigator.userAgent
        }
      };

      // 4. Submit with retry logic
      const response = await this.apiService.saveWithRetry(request);

      if (response.error) {
        return {
          success: false,
          errors: { _form: [response.error.message] },
          details: response.error.details
        };
      }

      // 5. Process successful response
      const savedComponents = response.data!;
      return {
        success: true,
        data: savedComponents,
        entityId: this.extractEntityId(savedComponents)
      };

    } catch (error) {
      return {
        success: false,
        errors: { _form: [error instanceof Error ? error.message : 'Unexpected error'] }
      };
    }
  }

  private transformToComponents<T>(
    formData: T, 
    mapping: ComponentMapping<T>
  ): Record<string, ComponentData> {
    const components: Record<string, ComponentData> = {};

    for (const [componentType, fieldMapping] of Object.entries(mapping)) {
      const componentData: ComponentData = {};
      
      for (const [fieldName, formPath] of Object.entries(fieldMapping)) {
        const value = this.getNestedValue(formData, formPath);
        if (value !== undefined && value !== null) {
          componentData[fieldName] = value;
        }
      }

      if (Object.keys(componentData).length > 0) {
        components[componentType] = componentData;
      }
    }

    return components;
  }

  private extractEntityId(components: IComponent[]): string {
    // Extract entity ID from the first component
    return components.length > 0 ? components[0].ownerEntityId : '';
  }

  private getNestedValue(obj: any, path: string): any {
    return path.split('.').reduce((current, key) => current?.[key], obj);
  }
}

// Types for form orchestration
interface ComponentMapping<T> {
  [componentType: string]: {
    [componentField: string]: keyof T | string; // string for nested paths like "address.street"
  };
}

interface SubmissionResult {
  success: boolean;
  data?: IComponent[];
  entityId?: string;
  errors?: Record<string, string[]>;
  details?: Record<string, unknown>;
}
```

### React Hook for Mixed Forms

```typescript
// useMixedForm.ts - React hook for handling mixed component forms
export function useMixedForm<T extends Record<string, unknown>>(
  initialData: T,
  componentMapping: ComponentMapping<T>,
  options: MixedFormOptions = {}
) {
  const [formData, setFormData] = useState<T>(initialData);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isDirty, setIsDirty] = useState(false);

  const orchestrator = useMemo(() => 
    new FormOrchestrator(apiService, validationService), []
  );

  const updateField = useCallback((path: string, value: unknown) => {
    setFormData(prev => {
      const updated = { ...prev };
      setNestedValue(updated, path, value);
      return updated;
    });
    setIsDirty(true);
    
    // Clear field-specific errors
    if (errors[path]) {
      setErrors(prev => {
        const updated = { ...prev };
        delete updated[path];
        return updated;
      });
    }
  }, [errors]);

  const submit = useCallback(async (): Promise<SubmissionResult> => {
    setIsSubmitting(true);
    setErrors({});

    try {
      const result = await orchestrator.submitMixedForm(formData, componentMapping);
      
      if (result.success) {
        setIsDirty(false);
        if (options.onSuccess) {
          options.onSuccess(result.data!, result.entityId!);
        }
      } else {
        setErrors(result.errors || {});
        if (options.onError) {
          options.onError(result.errors || {});
        }
      }

      return result;
    } finally {
      setIsSubmitting(false);
    }
  }, [formData, componentMapping, options, orchestrator]);

  const reset = useCallback(() => {
    setFormData(initialData);
    setErrors({});
    setIsDirty(false);
  }, [initialData]);

  return {
    formData,
    errors,
    isSubmitting,
    isDirty,
    updateField,
    submit,
    reset
  };
}

interface MixedFormOptions {
  onSuccess?: (components: IComponent[], entityId: string) => void;
  onError?: (errors: Record<string, string[]>) => void;
}
```

## Testing Strategy

### Integration Tests

```csharp
// MixedComponentIntegrationTests.cs
public class MixedComponentIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task SaveMixedComponents_AccountAndOrder_SavesBothAtomically()
    {
        // Arrange
        var system = _serviceProvider.GetRequiredService<MixedComponentSystem>();
        var request = new MixedComponentRequest
        {
            Components = new Dictionary<string, ComponentData>
            {
                ["Account"] = new ComponentData
                {
                    Email = "test@example.com",
                    Name = "Test User"
                },
                ["Order"] = new ComponentData
                {
                    Amount = 99.99m,
                    Status = "pending"
                }
            }
        };

        // Act
        var result = await system.SaveMixedComponents(request);

        // Assert
        result.Count.ShouldBe(2);
        
        var account = result.OfType<Account>().FirstOrDefault();
        var order = result.OfType<Order>().FirstOrDefault();
        
        account.ShouldNotBeNull();
        order.ShouldNotBeNull();
        
        // Both should have the same entity ID
        account.OwnerEntityId.ShouldBe(order.OwnerEntityId);
        
        // Cleanup
        TrackEntity(account.OwnerEntityId);
    }

    [Fact]
    public async Task SaveMixedComponents_ValidationFailure_RollsBackTransaction()
    {
        // Arrange
        var system = _serviceProvider.GetRequiredService<MixedComponentSystem>();
        var request = new MixedComponentRequest
        {
            Components = new Dictionary<string, ComponentData>
            {
                ["Account"] = new ComponentData
                {
                    Email = "", // Invalid - empty email
                    Name = "Test User"
                },
                ["Order"] = new ComponentData
                {
                    Amount = 99.99m,
                    Status = "pending"
                }
            }
        };

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => system.SaveMixedComponents(request));
        
        // Verify no components were saved
        var accountCount = await _dataContext.Query().WithAll<Account>().ToList<Account>();
        var orderCount = await _dataContext.Query().WithAll<Order>().ToList<Order>();
        
        accountCount.Count.ShouldBe(0);
        orderCount.Count.ShouldBe(0);
    }
}
```

### Frontend Tests

```typescript
// formOrchestrator.test.ts
describe('FormOrchestrator', () => {
  let orchestrator: FormOrchestrator;
  let mockApiService: jest.Mocked<MixedComponentApiService>;
  let mockValidationService: jest.Mocked<ValidationService>;

  beforeEach(() => {
    mockApiService = {
      saveMixedComponents: jest.fn(),
      saveWithRetry: jest.fn()
    } as any;

    mockValidationService = {
      validateForm: jest.fn()
    } as any;

    orchestrator = new FormOrchestrator(mockApiService, mockValidationService);
  });

  it('should successfully submit mixed form with account and order', async () => {
    // Arrange
    const formData = {
      email: 'test@example.com',
      name: 'Test User',
      amount: 99.99,
      productId: 'prod-123'
    };

    const mapping: ComponentMapping<typeof formData> = {
      Account: {
        email: 'email',
        name: 'name'
      },
      Order: {
        amount: 'amount',
        productId: 'productId',
        status: () => 'pending' // static value
      }
    };

    mockValidationService.validateForm.mockResolvedValue({ isValid: true, errors: {} });
    mockApiService.saveWithRetry.mockResolvedValue({
      data: [
        { id: '1', ownerEntityId: 'entity-1', $type: 'Account' },
        { id: '2', ownerEntityId: 'entity-1', $type: 'Order' }
      ]
    });

    // Act
    const result = await orchestrator.submitMixedForm(formData, mapping);

    // Assert
    expect(result.success).toBe(true);
    expect(result.entityId).toBe('entity-1');
    expect(mockApiService.saveWithRetry).toHaveBeenCalledWith({
      entityId: undefined,
      components: {
        Account: { email: 'test@example.com', name: 'Test User' },
        Order: { amount: 99.99, productId: 'prod-123', status: 'pending' }
      },
      metadata: expect.objectContaining({
        source: 'web-form'
      })
    });
  });

  it('should handle validation errors', async () => {
    // Arrange
    const formData = { email: '', name: 'Test' };
    const mapping = { Account: { email: 'email', name: 'name' } };

    mockValidationService.validateForm.mockResolvedValue({
      isValid: false,
      errors: { email: ['Email is required'] }
    });

    // Act
    const result = await orchestrator.submitMixedForm(formData, mapping);

    // Assert
    expect(result.success).toBe(false);
    expect(result.errors).toEqual({ email: ['Email is required'] });
    expect(mockApiService.saveWithRetry).not.toHaveBeenCalled();
  });
});
```

## Security Considerations

### Input Validation

```csharp
public class ComponentValidationMiddleware
{
    public async Task InvokeAsync(FunctionContext context, FunctionExecutionDelegate next)
    {
        // Validate component types are allowed
        var allowedTypes = new[] { "Account", "Order", "Profile", "Payment" };
        
        if (context.BindingContext.BindingData.TryGetValue("req", out var reqObj) &&
            reqObj is HttpRequestData request)
        {
            var body = await request.ReadAsStringAsync();
            var componentRequest = JsonSerializer.Deserialize<MixedComponentRequest>(body);
            
            if (componentRequest?.Components?.Keys.Any(type => !allowedTypes.Contains(type)) == true)
            {
                throw new SecurityException("Unauthorized component type");
            }
        }

        await next(context);
    }
}
```

### Authorization

```csharp
[RequirePermission("components", "write")]
public async Task<HttpResponseData> SaveMixedComponents(/* parameters */)
{
    // Function implementation
}
```

## Monitoring and Observability

### Application Insights Integration

```csharp
public class TelemetryMiddleware
{
    private readonly TelemetryClient _telemetryClient;

    public async Task InvokeAsync(FunctionContext context, FunctionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var operation = _telemetryClient.StartOperation<RequestTelemetry>("MixedComponentSave");

        try
        {
            await next(context);
            operation.Telemetry.Success = true;
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;
            _telemetryClient.TrackException(ex);
            throw;
        }
        finally
        {
            operation.Telemetry.Duration = stopwatch.Elapsed;
            _telemetryClient.StopOperation(operation);
        }
    }
}
```

### Custom Metrics

```csharp
public async Task<List<IComponent>> SaveMixedComponents(MixedComponentRequest request)
{
    var componentCount = request.Components?.Count ?? 0;
    _telemetryClient.TrackMetric("MixedComponent.ComponentCount", componentCount);
    
    using var activity = _telemetryClient.StartOperation<DependencyTelemetry>("Database.Transaction");
    
    try
    {
        var result = await _dataContext.ExecuteInTransaction(async () =>
        {
            // Implementation
        });
        
        _telemetryClient.TrackMetric("MixedComponent.SaveSuccess", 1);
        return result;
    }
    catch (Exception ex)
    {
        _telemetryClient.TrackMetric("MixedComponent.SaveFailure", 1);
        _telemetryClient.TrackException(ex, new Dictionary<string, string>
        {
            ["ComponentTypes"] = string.Join(",", request.Components?.Keys ?? Array.Empty<string>()),
            ["ComponentCount"] = componentCount.ToString()
        });
        throw;
    }
}
```

This comprehensive API routing system ensures reliable, scalable, and maintainable handling of mixed-component form submissions while providing excellent developer experience and robust error handling.