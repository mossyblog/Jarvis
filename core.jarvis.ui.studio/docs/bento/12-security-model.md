# Security Model

## Overview

The Bento Grid System implements a multi-layered security model that ensures proper access control at the page, component, and data levels. Security is enforced both at runtime and during the design phase.

## Security Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Security Layers                               │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                    Authentication Layer                      │ │
│  │                  (JWT Token Validation)                      │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                              ↓                                     │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                    Authorization Layer                       │ │
│  │              (Role & Permission Checking)                    │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                              ↓                                     │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                     Page Access Layer                        │ │
│  │               (Route-Level Protection)                       │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                              ↓                                     │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                  Component Security Layer                    │ │
│  │              (Component-Level Visibility)                     │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                              ↓                                     │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                     Data Security Layer                      │ │
│  │                 (Row-Level Security)                         │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Authentication

### JWT Token Structure

```typescript
interface BentoJWT {
  // Standard JWT claims
  sub: string; // User ID
  iat: number; // Issued at
  exp: number; // Expiration
  
  // Bento-specific claims
  roles: string[];
  permissions: string[];
  organizationId?: string;
  sessionId: string;
  
  // Security metadata
  ipAddress?: string;
  userAgent?: string;
  lastActivity?: number;
}

// Token validation
class TokenValidator {
  validateToken(token: string): BentoJWT | null {
    try {
      const decoded = jwt.verify(token, process.env.JWT_SECRET) as BentoJWT;
      
      // Additional validation
      if (decoded.exp < Date.now() / 1000) {
        throw new Error('Token expired');
      }
      
      if (decoded.lastActivity) {
        const inactivityLimit = 30 * 60 * 1000; // 30 minutes
        if (Date.now() - decoded.lastActivity > inactivityLimit) {
          throw new Error('Session timeout');
        }
      }
      
      return decoded;
    } catch (error) {
      return null;
    }
  }
}
```

### Session Management

```typescript
// Session handling for Bento
class BentoSessionManager {
  private sessions = new Map<string, SessionData>();
  
  createSession(user: User): Session {
    const sessionId = crypto.randomUUID();
    const session: SessionData = {
      id: sessionId,
      userId: user.id,
      roles: user.roles.map(r => r.name),
      permissions: this.flattenPermissions(user.roles),
      createdAt: Date.now(),
      lastActivity: Date.now(),
      metadata: {
        ipAddress: this.getClientIP(),
        userAgent: this.getUserAgent()
      }
    };
    
    this.sessions.set(sessionId, session);
    
    return {
      token: this.generateToken(session),
      expiresIn: 3600 // 1 hour
    };
  }
  
  refreshSession(sessionId: string): Session | null {
    const session = this.sessions.get(sessionId);
    if (!session) return null;
    
    // Check if session is still valid
    if (this.isSessionExpired(session)) {
      this.sessions.delete(sessionId);
      return null;
    }
    
    // Update activity
    session.lastActivity = Date.now();
    
    return {
      token: this.generateToken(session),
      expiresIn: 3600
    };
  }
  
  private flattenPermissions(roles: Role[]): string[] {
    const permissions = new Set<string>();
    
    roles.forEach(role => {
      role.permissions.forEach(perm => {
        permissions.add(`${perm.resource}:${perm.actions.join(',')}`);
      });
    });
    
    return Array.from(permissions);
  }
}
```

## Authorization

### Role-Based Access Control (RBAC)

```typescript
// Role definitions
interface Role {
  id: string;
  name: string;
  description: string;
  permissions: Permission[];
  inherits?: string[]; // Inherit from other roles
}

interface Permission {
  id: string;
  resource: string;
  actions: string[];
  conditions?: PermissionCondition[];
}

interface PermissionCondition {
  field: string;
  operator: 'equals' | 'notEquals' | 'in' | 'notIn';
  value: any;
}

// Default roles
const defaultRoles: Role[] = [
  {
    id: 'viewer',
    name: 'Viewer',
    description: 'Can view pages and components',
    permissions: [
      {
        id: 'view-pages',
        resource: 'page',
        actions: ['read']
      }
    ]
  },
  {
    id: 'editor',
    name: 'Editor',
    description: 'Can create and edit pages',
    inherits: ['viewer'],
    permissions: [
      {
        id: 'edit-pages',
        resource: 'page',
        actions: ['create', 'update']
      },
      {
        id: 'manage-layouts',
        resource: 'layout',
        actions: ['create', 'update', 'delete']
      }
    ]
  },
  {
    id: 'admin',
    name: 'Administrator',
    description: 'Full system access',
    inherits: ['editor'],
    permissions: [
      {
        id: 'manage-users',
        resource: 'user',
        actions: ['create', 'update', 'delete']
      },
      {
        id: 'manage-security',
        resource: 'security',
        actions: ['*']
      }
    ]
  }
];
```

### Permission Checking

```typescript
// Authorization service
class BentoAuthorizationService {
  constructor(
    private user: User,
    private roles: Role[]
  ) {}
  
  // Check if user has permission
  hasPermission(
    resource: string,
    action: string,
    context?: Record<string, any>
  ): boolean {
    const userPermissions = this.getUserPermissions();
    
    return userPermissions.some(permission => {
      // Check resource match
      if (permission.resource !== resource && permission.resource !== '*') {
        return false;
      }
      
      // Check action match
      if (!permission.actions.includes(action) && !permission.actions.includes('*')) {
        return false;
      }
      
      // Check conditions
      if (permission.conditions && context) {
        return this.evaluateConditions(permission.conditions, context);
      }
      
      return true;
    });
  }
  
  // Check if user has role
  hasRole(roleName: string): boolean {
    return this.user.roles.some(r => r.name === roleName);
  }
  
  // Get all user permissions including inherited
  private getUserPermissions(): Permission[] {
    const permissions: Permission[] = [];
    const processedRoles = new Set<string>();
    
    const processRole = (roleName: string) => {
      if (processedRoles.has(roleName)) return;
      processedRoles.add(roleName);
      
      const role = this.roles.find(r => r.name === roleName);
      if (!role) return;
      
      // Add role permissions
      permissions.push(...role.permissions);
      
      // Process inherited roles
      role.inherits?.forEach(inheritedRole => {
        processRole(inheritedRole);
      });
    };
    
    // Process user roles
    this.user.roles.forEach(role => {
      processRole(role.name);
    });
    
    return permissions;
  }
  
  private evaluateConditions(
    conditions: PermissionCondition[],
    context: Record<string, any>
  ): boolean {
    return conditions.every(condition => {
      const value = context[condition.field];
      
      switch (condition.operator) {
        case 'equals':
          return value === condition.value;
        case 'notEquals':
          return value !== condition.value;
        case 'in':
          return Array.isArray(condition.value) && 
            condition.value.includes(value);
        case 'notIn':
          return Array.isArray(condition.value) && 
            !condition.value.includes(value);
        default:
          return false;
      }
    });
  }
}
```

## Page Security

### Page Access Control

```typescript
// Page security implementation
class PageSecurityManager {
  constructor(
    private authService: BentoAuthorizationService,
    private storageService: BentoStorageService
  ) {}
  
  async canAccessPage(pageId: string): Promise<boolean> {
    const page = await this.storageService.getPage(pageId);
    if (!page) return false;
    
    return this.evaluatePageSecurity(page);
  }
  
  async canAccessRoute(route: string): Promise<boolean> {
    const page = await this.storageService.getPageByRoute(route);
    if (!page) return false;
    
    return this.evaluatePageSecurity(page);
  }
  
  private evaluatePageSecurity(page: BentoPage): boolean {
    const security = page.bindings.security;
    
    // Public pages
    if (security.isPublic) return true;
    
    // Check roles
    if (security.requiredRoles?.length > 0) {
      const hasRequiredRole = security.requiredRoles.some(role => 
        this.authService.hasRole(role)
      );
      if (!hasRequiredRole) return false;
    }
    
    // Check permissions
    if (security.requiredPermissions?.length > 0) {
      const hasRequiredPermission = security.requiredPermissions.every(perm => 
        this.authService.hasPermission('page', perm, { pageId: page.id })
      );
      if (!hasRequiredPermission) return false;
    }
    
    // Check custom rules
    if (security.customRules?.length > 0) {
      return this.evaluateCustomRules(security.customRules, page);
    }
    
    return true;
  }
  
  private evaluateCustomRules(
    rules: SecurityRule[],
    page: BentoPage
  ): boolean {
    return rules.every(rule => {
      switch (rule.type) {
        case 'expression':
          return this.evaluateExpression(rule.rule, { page });
        case 'function':
          return this.callSecurityFunction(rule.rule, rule.parameters);
        default:
          return false;
      }
    });
  }
}
```

### Route Protection

```typescript
// React route guard
export const BentoProtectedRoute: React.FC<{
  pageId?: string;
  route?: string;
  children: React.ReactNode;
}> = ({ pageId, route, children }) => {
  const { user } = useAuth();
  const [hasAccess, setHasAccess] = useState<boolean | null>(null);
  const [loading, setLoading] = useState(true);
  
  useEffect(() => {
    const checkAccess = async () => {
      if (!user) {
        setHasAccess(false);
        setLoading(false);
        return;
      }
      
      const securityManager = new PageSecurityManager(
        new BentoAuthorizationService(user, roles),
        storageService
      );
      
      const canAccess = pageId
        ? await securityManager.canAccessPage(pageId)
        : await securityManager.canAccessRoute(route!);
        
      setHasAccess(canAccess);
      setLoading(false);
    };
    
    checkAccess();
  }, [user, pageId, route]);
  
  if (loading) return <LoadingSpinner />;
  
  if (!hasAccess) {
    return <AccessDenied />;
  }
  
  return <>{children}</>;
};
```

## Component Security

### Component-Level Visibility

```typescript
// Component security wrapper
export const SecureComponent: React.FC<{
  component: GridComponent;
  children: React.ReactNode;
}> = ({ component, children }) => {
  const { user } = useAuth();
  const authService = new BentoAuthorizationService(user, roles);
  
  // Check component visibility
  const isVisible = useMemo(() => {
    if (!component.bindings?.visibility) return true;
    
    const visibility = component.bindings.visibility;
    
    // Check permissions
    if (visibility.requiredPermissions?.length > 0) {
      const hasPermission = visibility.requiredPermissions.every(perm =>
        authService.hasPermission('component', perm, { 
          componentId: component.id,
          componentType: component.componentType
        })
      );
      if (!hasPermission) return false;
    }
    
    // Check roles
    if (visibility.requiredRoles?.length > 0) {
      const hasRole = visibility.requiredRoles.some(role =>
        authService.hasRole(role)
      );
      if (!hasRole) return false;
    }
    
    // Check conditions
    if (visibility.condition) {
      return evaluateCondition(visibility.condition, { user, component });
    }
    
    return true;
  }, [component, user]);
  
  if (!isVisible) return null;
  
  return <>{children}</>;
};
```

### Data Access Control

```typescript
// Component data security
class ComponentDataSecurity {
  constructor(
    private authService: BentoAuthorizationService,
    private dataService: DataService
  ) {}
  
  async getComponentData(
    component: GridComponent,
    dataSource: string
  ): Promise<any> {
    // Check data access permission
    if (!this.authService.hasPermission('data', 'read', {
      dataSource,
      componentId: component.id
    })) {
      throw new SecurityError('Access denied to data source');
    }
    
    // Apply row-level security
    const query = this.applyRowLevelSecurity(
      dataSource,
      this.authService.user
    );
    
    return await this.dataService.executeQuery(query);
  }
  
  private applyRowLevelSecurity(
    dataSource: string,
    user: User
  ): DataQuery {
    const baseQuery = this.dataService.getQuery(dataSource);
    
    // Add security filters
    const securityFilters: Filter[] = [];
    
    // Organization filter
    if (user.organizationId) {
      securityFilters.push({
        field: 'organizationId',
        operator: 'equals',
        value: user.organizationId
      });
    }
    
    // Role-based filters
    const roleFilters = this.getRoleBasedFilters(user.roles, dataSource);
    securityFilters.push(...roleFilters);
    
    return {
      ...baseQuery,
      filters: [...(baseQuery.filters || []), ...securityFilters]
    };
  }
}
```

## Security in Edit Mode

### Design-Time Security

```typescript
// Security for page builder
class PageBuilderSecurity {
  constructor(
    private authService: BentoAuthorizationService
  ) {}
  
  // Check if user can create pages
  canCreatePage(): boolean {
    return this.authService.hasPermission('page', 'create');
  }
  
  // Check if user can edit specific page
  canEditPage(page: BentoPage): boolean {
    // Check ownership
    if (page.createdBy === this.authService.user.id) {
      return this.authService.hasPermission('page', 'update:own');
    }
    
    // Check general permission
    return this.authService.hasPermission('page', 'update', {
      pageId: page.id
    });
  }
  
  // Get available components for user
  getAvailableComponents(): ComponentConfig[] {
    const registry = componentRegistry.getAll();
    
    return Object.values(registry).filter(config => {
      // Check component creation permission
      return this.authService.hasPermission('component', 'use', {
        componentType: config.component.name
      });
    });
  }
  
  // Validate page security settings
  validatePageSecurity(
    securityBindings: SecurityBindings
  ): ValidationResult {
    const errors: string[] = [];
    
    // Validate roles exist
    if (securityBindings.requiredRoles) {
      const invalidRoles = securityBindings.requiredRoles.filter(role =>
        !this.roleExists(role)
      );
      if (invalidRoles.length > 0) {
        errors.push(`Invalid roles: ${invalidRoles.join(', ')}`);
      }
    }
    
    // Validate permissions
    if (securityBindings.requiredPermissions) {
      const invalidPerms = securityBindings.requiredPermissions.filter(perm =>
        !this.permissionExists(perm)
      );
      if (invalidPerms.length > 0) {
        errors.push(`Invalid permissions: ${invalidPerms.join(', ')}`);
      }
    }
    
    return {
      valid: errors.length === 0,
      errors
    };
  }
}
```

## Audit Logging

### Security Event Logging

```typescript
// Audit log for security events
interface SecurityAuditLog {
  id: string;
  timestamp: Date;
  userId: string;
  action: SecurityAction;
  resource: string;
  resourceId: string;
  outcome: 'success' | 'failure';
  details?: Record<string, any>;
  ipAddress?: string;
  userAgent?: string;
}

enum SecurityAction {
  Login = 'login',
  Logout = 'logout',
  PageAccess = 'page_access',
  PageCreate = 'page_create',
  PageUpdate = 'page_update',
  PageDelete = 'page_delete',
  PermissionDenied = 'permission_denied',
  SecurityViolation = 'security_violation'
}

class SecurityAuditLogger {
  async logEvent(
    action: SecurityAction,
    resource: string,
    resourceId: string,
    outcome: 'success' | 'failure',
    details?: Record<string, any>
  ): Promise<void> {
    const log: SecurityAuditLog = {
      id: crypto.randomUUID(),
      timestamp: new Date(),
      userId: this.getCurrentUserId(),
      action,
      resource,
      resourceId,
      outcome,
      details,
      ipAddress: this.getClientIP(),
      userAgent: this.getUserAgent()
    };
    
    await this.saveLog(log);
    
    // Alert on security violations
    if (action === SecurityAction.SecurityViolation) {
      await this.alertSecurityTeam(log);
    }
  }
  
  async getAuditTrail(
    filters: AuditFilters
  ): Promise<SecurityAuditLog[]> {
    return await this.queryLogs(filters);
  }
}
```

## Security Best Practices

### 1. Input Validation

```typescript
// Validate all user inputs
class SecurityValidator {
  validatePageRoute(route: string): ValidationResult {
    const errors: string[] = [];
    
    // Check for path traversal
    if (route.includes('..') || route.includes('//')) {
      errors.push('Invalid route: potential path traversal');
    }
    
    // Check for XSS attempts
    if (/<script|javascript:|onerror=/i.test(route)) {
      errors.push('Invalid route: potential XSS');
    }
    
    // Validate format
    if (!/^\/[a-z0-9-\/]*$/.test(route)) {
      errors.push('Route must start with / and contain only lowercase letters, numbers, hyphens');
    }
    
    return { valid: errors.length === 0, errors };
  }
  
  sanitizeComponentProps(props: Record<string, any>): Record<string, any> {
    const sanitized: Record<string, any> = {};
    
    for (const [key, value] of Object.entries(props)) {
      if (typeof value === 'string') {
        // Sanitize HTML
        sanitized[key] = DOMPurify.sanitize(value);
      } else if (typeof value === 'object' && value !== null) {
        // Recursively sanitize objects
        sanitized[key] = this.sanitizeComponentProps(value);
      } else {
        sanitized[key] = value;
      }
    }
    
    return sanitized;
  }
}
```

### 2. Content Security Policy

```typescript
// CSP headers for Bento pages
const bentoCSP = {
  'default-src': ["'self'"],
  'script-src': ["'self'", "'unsafe-inline'", "'unsafe-eval'"], // Required for component rendering
  'style-src': ["'self'", "'unsafe-inline'"], // Required for dynamic styles
  'img-src': ["'self'", 'data:', 'https:'],
  'font-src': ["'self'"],
  'connect-src': ["'self'", process.env.API_URL],
  'frame-ancestors': ["'none'"],
  'base-uri': ["'self'"],
  'form-action': ["'self'"]
};
```

### 3. Rate Limiting

```typescript
// Rate limiting for API endpoints
class BentoRateLimiter {
  private limits = {
    pageCreate: { requests: 10, window: 3600 }, // 10 per hour
    pageUpdate: { requests: 100, window: 3600 }, // 100 per hour
    componentAdd: { requests: 1000, window: 3600 }, // 1000 per hour
    dataFetch: { requests: 1000, window: 60 } // 1000 per minute
  };
  
  async checkLimit(
    userId: string,
    action: keyof typeof this.limits
  ): Promise<boolean> {
    const key = `rate:${userId}:${action}`;
    const limit = this.limits[action];
    
    const count = await redis.incr(key);
    if (count === 1) {
      await redis.expire(key, limit.window);
    }
    
    return count <= limit.requests;
  }
}
```

## Next Steps

1. Review [Architecture](./02-architecture.md) for system design
2. Check [Implementation Plan](./07-implementation-plan.md) for security tasks
3. See [Testing Strategy](./08-testing-strategy.md) for security testing
4. Explore [Migration Guide](./14-migration-guide.md) for security migration