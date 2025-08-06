# Navigation System Implementation Guide

Learn how to implement dynamic navigation menus based on user permissions using the Jarvis Navigation system.

## Overview

The Navigation system provides dynamic menu generation based on user roles and permissions. It automatically shows/hides menu items based on what the authenticated user is allowed to access.

## Architecture

```
┌─────────────────┐
│NavigationFunction│ ─── GET /api/navigation ──→ 
└────────┬────────┘
         │ Authenticated Context
         ▼
┌─────────────────┐
│NavigationHandler│ ─── Builds menu based on ──→ User Permissions
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│NavigationItem   │ ─── Component storing ──→ Menu Structure
└─────────────────┘
```

## Components

### NavigationItem Component

```csharp
public record NavigationItem : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // Menu structure
    public string Title { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public string? Route { get; init; }
    public int Order { get; init; }
    public bool IsActive { get; init; } = true;
    
    // Permissions
    public string? RequiredPermission { get; init; }
    public List<string> RequiredRoles { get; init; } = new();
    
    // Hierarchy
    public Guid? ParentId { get; init; }
    public List<NavigationItem> Children { get; init; } = new();
    
    // UI hints
    public string? BadgeText { get; init; }
    public string? BadgeColor { get; init; }
    public bool IsDivider { get; init; }
    public bool IsExternal { get; init; }
}
```

## Implementation

### 1. Navigation Handler

```csharp
public class NavigationHandler : ComponentHandler<NavigationItem>
{
    private readonly IAuthContext _authContext;
    
    public NavigationHandler(
        IDataContext dataContext,
        ILogger<NavigationHandler> logger,
        IServiceProvider serviceProvider)
        : base(dataContext, logger)
    {
        _authContext = serviceProvider.GetRequiredService<IAuthContext>();
    }

    public async Task<List<NavigationItem>> GetUserNavigation()
    {
        // Get current user's permissions
        var userPermissions = _authContext.Permissions;
        var userRoles = _authContext.Roles;

        // Get all navigation items
        var allItems = await DataContext.Query()
            .With<NavigationItem>(n => n.IsActive)
            .ToEntityComponents();

        var navigationItems = allItems.Values
            .Select(c => c.Get<NavigationItem>())
            .Where(n => n != null)
            .ToList();

        // Filter based on permissions
        var accessibleItems = navigationItems
            .Where(item => CanAccessMenuItem(item, userPermissions, userRoles))
            .OrderBy(item => item.Order)
            .ToList();

        // Build hierarchy
        return BuildNavigationHierarchy(accessibleItems);
    }

    private bool CanAccessMenuItem(
        NavigationItem item, 
        List<string> permissions, 
        List<string> roles)
    {
        // Check permission requirement
        if (!string.IsNullOrEmpty(item.RequiredPermission))
        {
            if (!permissions.Contains(item.RequiredPermission))
                return false;
        }

        // Check role requirements
        if (item.RequiredRoles.Any())
        {
            if (!item.RequiredRoles.Any(r => roles.Contains(r)))
                return false;
        }

        return true;
    }

    private List<NavigationItem> BuildNavigationHierarchy(List<NavigationItem> items)
    {
        var lookup = items.ToLookup(i => i.ParentId);
        var rootItems = lookup[null].ToList();

        foreach (var item in rootItems)
        {
            AddChildren(item, lookup);
        }

        return rootItems;
    }

    private void AddChildren(NavigationItem parent, ILookup<Guid?, NavigationItem> lookup)
    {
        var children = lookup[parent.Id].OrderBy(c => c.Order).ToList();
        parent.Children.AddRange(children);

        foreach (var child in children)
        {
            AddChildren(child, lookup);
        }
    }
}
```

### 2. Navigation Function

```csharp
[Function("Navigation")]
public class NavigationFunction
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<NavigationFunction> _logger;

    public NavigationFunction(
        IDataContext dataContext,
        ILogger<NavigationFunction> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    [Function("GetNavigation")]
    public async Task<HttpResponseData> GetNavigation(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "navigation")] 
        HttpRequestData req,
        FunctionContext context)
    {
        try
        {
            // Get authenticated user context
            var userId = context.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            }

            // Get navigation for user
            var navigationHandler = _dataContext.For<NavigationHandler>(Guid.Parse(userId));
            var navigation = await navigationHandler.GetUserNavigation();

            // Return navigation menu
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(navigation);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get navigation");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }
}
```

### 3. Setup Initial Navigation

```csharp
public class NavigationSetupHandler : ComponentHandler<NavigationItem>
{
    public async Task SetupDefaultNavigation()
    {
        var navigationItems = new List<NavigationItem>
        {
            // Dashboard
            new NavigationItem
            {
                Title = "Dashboard",
                Icon = "home",
                Route = "/dashboard",
                Order = 1,
                RequiredPermission = "dashboard.view"
            },
            
            // Orders menu
            new NavigationItem
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Title = "Orders",
                Icon = "shopping-cart",
                Order = 2,
                RequiredPermission = "orders.view"
            },
            
            // Orders submenu items
            new NavigationItem
            {
                Title = "All Orders",
                Route = "/orders",
                ParentId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Order = 1,
                RequiredPermission = "orders.view"
            },
            new NavigationItem
            {
                Title = "Create Order",
                Route = "/orders/new",
                ParentId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Order = 2,
                RequiredPermission = "orders.create"
            },
            
            // Admin section
            new NavigationItem
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Title = "Administration",
                Icon = "settings",
                Order = 100,
                RequiredRoles = new List<string> { "Admin" }
            },
            
            // Admin submenu
            new NavigationItem
            {
                Title = "Users",
                Route = "/admin/users",
                ParentId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Order = 1,
                RequiredPermission = "admin.users.view"
            },
            new NavigationItem
            {
                Title = "Settings",
                Route = "/admin/settings",
                ParentId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Order = 2,
                RequiredPermission = "admin.settings.view"
            }
        };

        foreach (var item in navigationItems)
        {
            await DataContext.Commit(item with { OwnerEntityId = OwnerEntityId });
        }
    }
}
```

## Frontend Integration

### React Navigation Component

```typescript
import { useState, useEffect } from 'react';
import { NavLink } from 'react-router-dom';

interface NavigationItem {
    id: string;
    title: string;
    icon?: string;
    route?: string;
    children: NavigationItem[];
    badgeText?: string;
    badgeColor?: string;
    isDivider?: boolean;
    isExternal?: boolean;
}

export function Navigation() {
    const [navigation, setNavigation] = useState<NavigationItem[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetchNavigation();
    }, []);

    const fetchNavigation = async () => {
        try {
            const response = await fetch('/api/navigation', {
                headers: {
                    'Authorization': `Bearer ${getAccessToken()}`
                }
            });

            if (response.ok) {
                const data = await response.json();
                setNavigation(data);
            }
        } catch (error) {
            console.error('Failed to load navigation:', error);
        } finally {
            setLoading(false);
        }
    };

    const renderMenuItem = (item: NavigationItem) => {
        if (item.isDivider) {
            return <hr key={item.id} className="nav-divider" />;
        }

        if (item.children.length > 0) {
            return (
                <li key={item.id} className="nav-item has-children">
                    <a className="nav-link">
                        {item.icon && <i className={`icon-${item.icon}`} />}
                        <span>{item.title}</span>
                        <i className="icon-chevron-down" />
                    </a>
                    <ul className="nav-submenu">
                        {item.children.map(renderMenuItem)}
                    </ul>
                </li>
            );
        }

        if (item.route) {
            return (
                <li key={item.id} className="nav-item">
                    {item.isExternal ? (
                        <a href={item.route} className="nav-link" target="_blank">
                            {item.icon && <i className={`icon-${item.icon}`} />}
                            <span>{item.title}</span>
                            {item.badgeText && (
                                <span className={`badge badge-${item.badgeColor || 'primary'}`}>
                                    {item.badgeText}
                                </span>
                            )}
                        </a>
                    ) : (
                        <NavLink to={item.route} className="nav-link">
                            {item.icon && <i className={`icon-${item.icon}`} />}
                            <span>{item.title}</span>
                            {item.badgeText && (
                                <span className={`badge badge-${item.badgeColor || 'primary'}`}>
                                    {item.badgeText}
                                </span>
                            )}
                        </NavLink>
                    )}
                </li>
            );
        }

        return null;
    };

    if (loading) {
        return <div className="nav-loading">Loading navigation...</div>;
    }

    return (
        <nav className="app-navigation">
            <ul className="nav-menu">
                {navigation.map(renderMenuItem)}
            </ul>
        </nav>
    );
}
```

### Dynamic Badge Updates

```typescript
// Hook to update navigation badges
export function useNavigationBadge() {
    const updateBadge = async (navigationItemId: string, badgeText: string, badgeColor?: string) => {
        try {
            await fetch(`/api/navigation/${navigationItemId}/badge`, {
                method: 'PATCH',
                headers: {
                    'Authorization': `Bearer ${getAccessToken()}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ badgeText, badgeColor })
            });
        } catch (error) {
            console.error('Failed to update badge:', error);
        }
    };

    return { updateBadge };
}

// Usage example
const { updateBadge } = useNavigationBadge();

// Update orders count
const orderCount = await fetchPendingOrderCount();
await updateBadge('orders-menu-id', orderCount.toString(), 'danger');
```

## Permission-Based Visibility

### Define Permissions

```csharp
public static class NavigationPermissions
{
    // Dashboard
    public const string DashboardView = "dashboard.view";
    
    // Orders
    public const string OrdersView = "orders.view";
    public const string OrdersCreate = "orders.create";
    public const string OrdersEdit = "orders.edit";
    public const string OrdersDelete = "orders.delete";
    
    // Customers
    public const string CustomersView = "customers.view";
    public const string CustomersCreate = "customers.create";
    public const string CustomersEdit = "customers.edit";
    
    // Admin
    public const string AdminUsersView = "admin.users.view";
    public const string AdminUsersManage = "admin.users.manage";
    public const string AdminSettingsView = "admin.settings.view";
    public const string AdminSettingsManage = "admin.settings.manage";
}
```

### Assign Permissions to Roles

```csharp
public class RoleSetup
{
    public static Dictionary<string, List<string>> DefaultRolePermissions = new()
    {
        ["User"] = new List<string>
        {
            NavigationPermissions.DashboardView,
            NavigationPermissions.OrdersView,
            NavigationPermissions.CustomersView
        },
        
        ["Manager"] = new List<string>
        {
            NavigationPermissions.DashboardView,
            NavigationPermissions.OrdersView,
            NavigationPermissions.OrdersCreate,
            NavigationPermissions.OrdersEdit,
            NavigationPermissions.CustomersView,
            NavigationPermissions.CustomersCreate,
            NavigationPermissions.CustomersEdit
        },
        
        ["Admin"] = new List<string>
        {
            // All permissions
            NavigationPermissions.DashboardView,
            NavigationPermissions.OrdersView,
            NavigationPermissions.OrdersCreate,
            NavigationPermissions.OrdersEdit,
            NavigationPermissions.OrdersDelete,
            NavigationPermissions.CustomersView,
            NavigationPermissions.CustomersCreate,
            NavigationPermissions.CustomersEdit,
            NavigationPermissions.AdminUsersView,
            NavigationPermissions.AdminUsersManage,
            NavigationPermissions.AdminSettingsView,
            NavigationPermissions.AdminSettingsManage
        }
    };
}
```

## Advanced Features

### Dynamic Menu Items

Add menu items based on runtime conditions:

```csharp
public async Task<List<NavigationItem>> GetDynamicNavigation()
{
    var baseNavigation = await GetUserNavigation();
    
    // Add notifications menu if user has unread notifications
    var notificationCount = await GetUnreadNotificationCount();
    if (notificationCount > 0)
    {
        baseNavigation.Insert(0, new NavigationItem
        {
            Title = "Notifications",
            Icon = "bell",
            Route = "/notifications",
            BadgeText = notificationCount.ToString(),
            BadgeColor = "danger",
            Order = 0
        });
    }
    
    // Add recent items submenu
    var recentItems = await GetRecentItems();
    if (recentItems.Any())
    {
        var recentMenu = new NavigationItem
        {
            Title = "Recent",
            Icon = "clock",
            Order = 1,
            Children = recentItems.Select(item => new NavigationItem
            {
                Title = item.Name,
                Route = $"/items/{item.Id}",
                Order = 0
            }).ToList()
        };
        
        baseNavigation.Insert(1, recentMenu);
    }
    
    return baseNavigation;
}
```

### Context-Sensitive Navigation

Show different navigation based on context:

```csharp
public async Task<List<NavigationItem>> GetContextNavigation(string context)
{
    return context switch
    {
        "order-details" => await GetOrderDetailsNavigation(),
        "customer-profile" => await GetCustomerProfileNavigation(),
        "admin-panel" => await GetAdminNavigation(),
        _ => await GetUserNavigation()
    };
}

private async Task<List<NavigationItem>> GetOrderDetailsNavigation()
{
    return new List<NavigationItem>
    {
        new() { Title = "Order Summary", Route = "#summary", Icon = "info" },
        new() { Title = "Items", Route = "#items", Icon = "list" },
        new() { Title = "Payment", Route = "#payment", Icon = "credit-card" },
        new() { Title = "Shipping", Route = "#shipping", Icon = "truck" },
        new() { Title = "History", Route = "#history", Icon = "clock" }
    };
}
```

## Testing

### Unit Tests

```csharp
public class NavigationHandlerTests : IntegrationTestBase
{
    [Fact]
    public async Task GetUserNavigation_ShouldFilterByPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var handler = TestDataContext().For<NavigationHandler>(userId);
        
        // Set up test navigation items
        await SetupTestNavigation();
        
        // Mock user with limited permissions
        MockAuthContext(new[] { "dashboard.view", "orders.view" });
        
        // Act
        var navigation = await handler.GetUserNavigation();
        
        // Assert
        navigation.ShouldNotBeEmpty();
        navigation.ShouldNotContain(n => n.RequiredPermission == "admin.users.view");
        navigation.ShouldContain(n => n.Title == "Dashboard");
        navigation.ShouldContain(n => n.Title == "Orders");
    }
}
```

## Best Practices

1. **Cache Navigation**: Navigation doesn't change often, cache it
2. **Lazy Load Children**: For large menus, load children on demand
3. **Icon Library**: Use consistent icon naming (Font Awesome, Material Icons)
4. **Accessibility**: Include ARIA labels and keyboard navigation
5. **Mobile Responsive**: Design for collapsible mobile menus

## Related Documentation

- [Permission System Guide](permissions-guide.md)
- [Role Management](role-management.md)
- [Frontend Authentication](frontend-integration.md)
- [API Security](/docs/architecture/authentication/api-security.md)

---

**Next Steps**: [Implement Role-Based Features](role-based-features.md) | [Dynamic UI Components](dynamic-ui-components.md)