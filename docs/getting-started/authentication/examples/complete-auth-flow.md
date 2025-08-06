# Complete Authentication Flow Example

This example demonstrates a full authentication implementation including registration, login, token refresh, and protected API calls.

## Backend Implementation

### 1. Program.cs Setup

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Register Jarvis
        services.RegisterJarvis(LogLevel.Information, context.Configuration);

        // Register authentication handlers
        services.AddScoped<IComponentHandler, AccountHandler>();
        services.AddScoped<AccountHandler>();
        services.AddScoped<IComponentHandler, AuthHandler>();
        services.AddScoped<AuthHandler>();

        // Register navigation
        services.AddScoped<IComponentHandler, NavigationHandler>();
        services.AddScoped<NavigationHandler>();

        // Register functions
        services.AddScoped<RegisterFunction>();
        services.AddScoped<AuthFunction>();
        services.AddScoped<NavigationFunction>();

        // Register services
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();
    })
    .Build();

host.Run();
```

### 2. Complete User Service

```csharp
public class UserService
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<UserService> _logger;

    public UserService(IDataContext dataContext, ILogger<UserService> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    public async Task<UserDto> RegisterUser(string email, string password)
    {
        // Create new user entity
        var userEntityId = Guid.NewGuid();
        
        try
        {
            // Register account
            var accountHandler = _dataContext.For<AccountHandler>(userEntityId);
            var account = await accountHandler.Register(new Account
            {
                Email = email,
                Password = password
            });

            // Create user profile
            var profileHandler = _dataContext.For<UserProfileHandler>(userEntityId);
            await profileHandler.CreateProfile(new UserProfile
            {
                DisplayName = email.Split('@')[0],
                CreatedAt = DateTime.UtcNow
            });

            // Assign default role
            var roleHandler = _dataContext.For<RoleHandler>(userEntityId);
            await roleHandler.AssignRole("User");

            _logger.LogInformation("User registered successfully: {Email}", email);

            return new UserDto
            {
                Id = userEntityId,
                Email = account.Email,
                IsActive = account.IsActive,
                CreatedAt = account.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register user: {Email}", email);
            throw;
        }
    }

    public async Task<AuthResult> AuthenticateUser(string email, string password, string ipAddress, string userAgent)
    {
        var authHandler = _dataContext.For<AuthHandler>(Guid.NewGuid());
        
        var authToken = await authHandler.Authenticate(new Account
        {
            Email = email,
            Password = password,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        if (string.IsNullOrEmpty(authToken.AccessToken))
        {
            throw new UnauthorizedException("Invalid credentials");
        }

        // Get user details
        var userQuery = _dataContext.Query()
            .With<Account>(a => a.Email == email);
        var users = await userQuery.ToEntityComponents();
        
        if (!users.Any())
        {
            throw new UnauthorizedException("User not found");
        }

        var userId = users.First().Key;

        return new AuthResult
        {
            UserId = userId,
            AccessToken = authToken.AccessToken,
            RefreshToken = authToken.RefreshToken,
            ExpiresIn = (int)(authToken.ExpiresAt - DateTime.UtcNow).TotalSeconds
        };
    }

    public async Task<AuthResult> RefreshTokens(string refreshToken)
    {
        var authHandler = _dataContext.For<AuthHandler>(Guid.NewGuid());
        var newTokens = await authHandler.RefreshToken(refreshToken);

        if (string.IsNullOrEmpty(newTokens.AccessToken))
        {
            throw new UnauthorizedException("Invalid refresh token");
        }

        return new AuthResult
        {
            UserId = newTokens.OwnerEntityId,
            AccessToken = newTokens.AccessToken,
            RefreshToken = newTokens.RefreshToken,
            ExpiresIn = (int)(newTokens.ExpiresAt - DateTime.UtcNow).TotalSeconds
        };
    }
}
```

## Frontend Implementation (React + TypeScript)

### 1. Authentication Service

```typescript
// authService.ts
interface AuthTokens {
    accessToken: string;
    refreshToken: string;
    expiresIn: number;
}

interface User {
    id: string;
    email: string;
    displayName?: string;
}

class AuthService {
    private baseUrl: string;
    private accessToken: string | null = null;
    private refreshToken: string | null = null;
    private refreshPromise: Promise<AuthTokens> | null = null;

    constructor(baseUrl: string) {
        this.baseUrl = baseUrl;
        this.loadTokensFromStorage();
    }

    private loadTokensFromStorage() {
        this.accessToken = localStorage.getItem('accessToken');
        this.refreshToken = localStorage.getItem('refreshToken');
    }

    private saveTokens(tokens: AuthTokens) {
        this.accessToken = tokens.accessToken;
        this.refreshToken = tokens.refreshToken;
        
        localStorage.setItem('accessToken', tokens.accessToken);
        localStorage.setItem('refreshToken', tokens.refreshToken);
        
        // Schedule token refresh
        const refreshTime = (tokens.expiresIn - 60) * 1000; // Refresh 1 minute before expiry
        setTimeout(() => this.refreshAccessToken(), refreshTime);
    }

    private clearTokens() {
        this.accessToken = null;
        this.refreshToken = null;
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
    }

    async register(email: string, password: string): Promise<User> {
        const response = await fetch(`${this.baseUrl}/api/auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Registration failed');
        }

        return response.json();
    }

    async login(email: string, password: string): Promise<User> {
        const response = await fetch(`${this.baseUrl}/api/security/auth`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Login failed');
        }

        const result = await response.json();
        this.saveTokens({
            accessToken: result.accessToken,
            refreshToken: result.refreshToken,
            expiresIn: result.expiresIn || 900 // Default 15 minutes
        });

        return {
            id: result.ownerEntityId,
            email: email
        };
    }

    async refreshAccessToken(): Promise<AuthTokens> {
        // Prevent multiple simultaneous refresh calls
        if (this.refreshPromise) {
            return this.refreshPromise;
        }

        this.refreshPromise = this.doRefresh();
        
        try {
            const tokens = await this.refreshPromise;
            return tokens;
        } finally {
            this.refreshPromise = null;
        }
    }

    private async doRefresh(): Promise<AuthTokens> {
        if (!this.refreshToken) {
            throw new Error('No refresh token available');
        }

        const response = await fetch(`${this.baseUrl}/api/security/refresh`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken: this.refreshToken })
        });

        if (!response.ok) {
            this.clearTokens();
            throw new Error('Token refresh failed');
        }

        const result = await response.json();
        const tokens = {
            accessToken: result.accessToken,
            refreshToken: result.refreshToken,
            expiresIn: result.expiresIn || 900
        };

        this.saveTokens(tokens);
        return tokens;
    }

    async makeAuthenticatedRequest(url: string, options: RequestInit = {}): Promise<Response> {
        if (!this.accessToken) {
            throw new Error('Not authenticated');
        }

        // First attempt with current token
        let response = await fetch(url, {
            ...options,
            headers: {
                ...options.headers,
                'Authorization': `Bearer ${this.accessToken}`
            }
        });

        // If unauthorized, try refreshing token
        if (response.status === 401 && this.refreshToken) {
            await this.refreshAccessToken();
            
            // Retry with new token
            response = await fetch(url, {
                ...options,
                headers: {
                    ...options.headers,
                    'Authorization': `Bearer ${this.accessToken}`
                }
            });
        }

        return response;
    }

    logout() {
        this.clearTokens();
        window.location.href = '/login';
    }

    isAuthenticated(): boolean {
        return !!this.accessToken;
    }

    getAccessToken(): string | null {
        return this.accessToken;
    }
}

export const authService = new AuthService(process.env.REACT_APP_API_URL || '');
```

### 2. React Authentication Context

```typescript
// AuthContext.tsx
import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { authService } from './authService';

interface AuthContextType {
    user: User | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    login: (email: string, password: string) => Promise<void>;
    register: (email: string, password: string) => Promise<void>;
    logout: () => void;
    makeAuthenticatedRequest: (url: string, options?: RequestInit) => Promise<Response>;
}

const AuthContext = createContext<AuthContextType | null>(null);

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within AuthProvider');
    }
    return context;
};

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [user, setUser] = useState<User | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        // Check if user is already authenticated
        const checkAuth = async () => {
            if (authService.isAuthenticated()) {
                try {
                    // Validate token by making a test request
                    const response = await authService.makeAuthenticatedRequest('/api/profile');
                    if (response.ok) {
                        const profile = await response.json();
                        setUser(profile);
                    }
                } catch (error) {
                    console.error('Auth check failed:', error);
                }
            }
            setIsLoading(false);
        };

        checkAuth();
    }, []);

    const login = useCallback(async (email: string, password: string) => {
        const user = await authService.login(email, password);
        setUser(user);
    }, []);

    const register = useCallback(async (email: string, password: string) => {
        const newUser = await authService.register(email, password);
        // Note: User is registered but not logged in (inactive by default)
        alert('Registration successful! Please wait for account activation.');
    }, []);

    const logout = useCallback(() => {
        authService.logout();
        setUser(null);
    }, []);

    const makeAuthenticatedRequest = useCallback(
        (url: string, options?: RequestInit) => {
            return authService.makeAuthenticatedRequest(url, options);
        },
        []
    );

    const value = {
        user,
        isAuthenticated: !!user,
        isLoading,
        login,
        register,
        logout,
        makeAuthenticatedRequest
    };

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
```

### 3. Complete App Example

```typescript
// App.tsx
import React from 'react';
import { BrowserRouter as Router, Route, Routes, Navigate } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardPage from './pages/DashboardPage';
import ProtectedRoute from './components/ProtectedRoute';
import Navigation from './components/Navigation';

function App() {
    return (
        <AuthProvider>
            <Router>
                <div className="app">
                    <Navigation />
                    <main className="app-content">
                        <Routes>
                            <Route path="/login" element={<LoginPage />} />
                            <Route path="/register" element={<RegisterPage />} />
                            <Route
                                path="/dashboard"
                                element={
                                    <ProtectedRoute>
                                        <DashboardPage />
                                    </ProtectedRoute>
                                }
                            />
                            <Route path="/" element={<Navigate to="/dashboard" />} />
                        </Routes>
                    </main>
                </div>
            </Router>
        </AuthProvider>
    );
}

export default App;
```

### 4. Login Page Component

```typescript
// LoginPage.tsx
import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const LoginPage: React.FC = () => {
    const navigate = useNavigate();
    const { login } = useAuth();
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');
        setLoading(true);

        try {
            await login(email, password);
            navigate('/dashboard');
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Login failed');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="login-page">
            <div className="login-container">
                <h1>Login to Jarvis</h1>
                <form onSubmit={handleSubmit}>
                    <div className="form-group">
                        <label htmlFor="email">Email</label>
                        <input
                            id="email"
                            type="email"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            required
                            disabled={loading}
                            placeholder="user@example.com"
                        />
                    </div>
                    <div className="form-group">
                        <label htmlFor="password">Password</label>
                        <input
                            id="password"
                            type="password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            required
                            disabled={loading}
                            placeholder="••••••••"
                        />
                    </div>
                    {error && <div className="error-message">{error}</div>}
                    <button type="submit" disabled={loading}>
                        {loading ? 'Logging in...' : 'Login'}
                    </button>
                </form>
                <p className="auth-link">
                    Don't have an account? <Link to="/register">Register</Link>
                </p>
            </div>
        </div>
    );
};

export default LoginPage;
```

## Testing the Implementation

### 1. Integration Test

```csharp
[Fact]
public async Task CompleteAuthenticationFlow_ShouldWork()
{
    // Arrange
    var email = "test@example.com";
    var password = "TestPassword123!";
    var userService = new UserService(TestDataContext(), NullLogger<UserService>.Instance);

    // Act 1: Register
    var user = await userService.RegisterUser(email, password);
    
    // Assert: User created but inactive
    user.ShouldNotBeNull();
    user.Email.ShouldBe(email);
    user.IsActive.ShouldBeFalse();

    // Act 2: Activate user (admin action)
    var accountHandler = TestDataContext().For<AccountHandler>(user.Id);
    await accountHandler.Activate();

    // Act 3: Login
    var authResult = await userService.AuthenticateUser(
        email, 
        password, 
        "127.0.0.1", 
        "TestAgent/1.0"
    );

    // Assert: Authentication successful
    authResult.ShouldNotBeNull();
    authResult.AccessToken.ShouldNotBeEmpty();
    authResult.RefreshToken.ShouldNotBeEmpty();

    // Act 4: Refresh token
    var newTokens = await userService.RefreshTokens(authResult.RefreshToken);

    // Assert: New tokens generated
    newTokens.AccessToken.ShouldNotBeEmpty();
    newTokens.AccessToken.ShouldNotBe(authResult.AccessToken);
}
```

### 2. E2E Test with Cypress

```javascript
// cypress/integration/auth.spec.js
describe('Authentication Flow', () => {
    it('should complete full authentication flow', () => {
        // Registration
        cy.visit('/register');
        cy.get('#email').type('cypress@example.com');
        cy.get('#password').type('CypressTest123!');
        cy.get('button[type="submit"]').click();
        
        // Should show success message
        cy.contains('Registration successful').should('be.visible');
        
        // Login (after admin activation)
        cy.visit('/login');
        cy.get('#email').type('cypress@example.com');
        cy.get('#password').type('CypressTest123!');
        cy.get('button[type="submit"]').click();
        
        // Should redirect to dashboard
        cy.url().should('include', '/dashboard');
        cy.contains('Welcome').should('be.visible');
        
        // Test authenticated request
        cy.window().then((win) => {
            // Access token should be stored
            expect(win.localStorage.getItem('accessToken')).to.exist;
        });
        
        // Logout
        cy.get('button#logout').click();
        cy.url().should('include', '/login');
    });
});
```

## Security Considerations

1. **Token Storage**: Store access tokens in memory, refresh tokens in httpOnly cookies
2. **HTTPS Only**: Always use HTTPS in production
3. **CORS Configuration**: Properly configure CORS for your domains
4. **Rate Limiting**: Implement rate limiting on authentication endpoints
5. **Audit Logging**: Log all authentication events

## Next Steps

- [Add Two-Factor Authentication](/docs/guides/authentication/two-factor-auth.md)
- [Implement Social Login](/docs/guides/authentication/social-login.md)
- [Setup Email Verification](/docs/guides/authentication/email-verification.md)
- [Configure Session Management](/docs/guides/authentication/session-management.md)

---

**Full Source Code**: [GitHub Repository](https://github.com/jarvis/examples/authentication)