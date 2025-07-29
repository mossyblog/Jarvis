# Authentication Implementation Guides

This guide covers common authentication scenarios and implementation patterns for integrating authentication into your Jarvis applications.

## Table of Contents

1. [Frontend Integration](#frontend-integration)
2. [API Integration](#api-integration)
3. [Token Management](#token-management)
4. [User Management](#user-management)
5. [Session Management](#session-management)
6. [Custom Authentication Flows](#custom-authentication-flows)

## Frontend Integration

### React/JavaScript Integration

#### Setting Up Authentication Context

Create a React context to manage authentication state:

```javascript
// AuthContext.js
import React, { createContext, useContext, useState, useEffect } from 'react';

const AuthContext = createContext();

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);
  const [accessToken, setAccessToken] = useState(null);
  const [refreshToken, setRefreshToken] = useState(null);

  useEffect(() => {
    // Load tokens from localStorage on app start
    const storedAccessToken = localStorage.getItem('accessToken');
    const storedRefreshToken = localStorage.getItem('refreshToken');
    
    if (storedAccessToken && storedRefreshToken) {
      setAccessToken(storedAccessToken);
      setRefreshToken(storedRefreshToken);
      // Validate token and set user
      validateAndSetUser(storedAccessToken);
    } else {
      setLoading(false);
    }
  }, []);

  const validateAndSetUser = async (token) => {
    try {
      const response = await fetch('/api/security/validate', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ token }),
      });

      if (response.ok) {
        const result = await response.json();
        setUser({
          id: result.userId,
          email: result.email
        });
      } else {
        // Token invalid, clear storage
        clearAuth();
      }
    } catch (error) {
      console.error('Token validation failed:', error);
      clearAuth();
    } finally {
      setLoading(false);
    }
  };

  const login = async (email, password) => {
    try {
      const response = await fetch('/api/security/auth', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, password }),
      });

      if (response.ok) {
        const authToken = await response.json();
        
        // Store tokens
        localStorage.setItem('accessToken', authToken.accessToken);
        localStorage.setItem('refreshToken', authToken.refreshToken);
        
        // Update state
        setAccessToken(authToken.accessToken);
        setRefreshToken(authToken.refreshToken);
        setUser({
          id: authToken.ownerEntityId,
          email: email
        });

        return { success: true };
      } else {
        const error = await response.json();
        return { success: false, error: error.message };
      }
    } catch (error) {
      console.error('Login failed:', error);
      return { success: false, error: 'Login failed' };
    }
  };

  const register = async (email, password) => {
    try {
      const response = await fetch('/api/auth/register', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, password }),
      });

      if (response.ok) {
        const account = await response.json();
        return { 
          success: true, 
          message: 'Account created! Please wait for activation.',
          account 
        };
      } else {
        const error = await response.json();
        return { success: false, error: error.message };
      }
    } catch (error) {
      console.error('Registration failed:', error);
      return { success: false, error: 'Registration failed' };
    }
  };

  const logout = () => {
    clearAuth();
  };

  const clearAuth = () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    setAccessToken(null);
    setRefreshToken(null);
    setUser(null);
  };

  const refreshTokens = async () => {
    if (!refreshToken) return false;

    try {
      const response = await fetch('/api/security/refresh', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ refreshToken }),
      });

      if (response.ok) {
        const newTokens = await response.json();
        
        // Update stored tokens
        localStorage.setItem('accessToken', newTokens.accessToken);
        localStorage.setItem('refreshToken', newTokens.refreshToken);
        
        // Update state
        setAccessToken(newTokens.accessToken);
        setRefreshToken(newTokens.refreshToken);
        
        return true;
      } else {
        // Refresh failed, clear auth
        clearAuth();
        return false;
      }
    } catch (error) {
      console.error('Token refresh failed:', error);
      clearAuth();
      return false;
    }
  };

  const value = {
    user,
    loading,
    accessToken,
    login,
    register,
    logout,
    refreshTokens,
    isAuthenticated: !!user && !!accessToken
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};
```

#### HTTP Interceptor for Token Management

Set up automatic token refresh with Axios:

```javascript
// apiClient.js
import axios from 'axios';

let authContext = null;

export const setAuthContext = (context) => {
  authContext = context;
};

const apiClient = axios.create({
  baseURL: '/api',
  timeout: 10000,
});

// Request interceptor to add auth token
apiClient.interceptors.request.use(
  (config) => {
    if (authContext?.accessToken) {
      config.headers.Authorization = `Bearer ${authContext.accessToken}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor to handle token refresh
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      if (authContext?.refreshTokens) {
        const refreshSuccess = await authContext.refreshTokens();
        
        if (refreshSuccess && authContext.accessToken) {
          originalRequest.headers.Authorization = `Bearer ${authContext.accessToken}`;
          return apiClient(originalRequest);
        }
      }
      
      // Refresh failed, redirect to login
      window.location.href = '/login';
    }

    return Promise.reject(error);
  }
);

export default apiClient;
```

#### Login Component

```javascript
// LoginForm.js
import React, { useState } from 'react';
import { useAuth } from './AuthContext';

const LoginForm = () => {
  const { login, isAuthenticated } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  if (isAuthenticated) {
    return <div>Already logged in!</div>;
  }

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    const result = await login(email, password);
    
    if (!result.success) {
      setError(result.error);
    }
    
    setLoading(false);
  };

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label htmlFor="email">Email:</label>
        <input
          id="email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          disabled={loading}
        />
      </div>
      
      <div>
        <label htmlFor="password">Password:</label>
        <input
          id="password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          disabled={loading}
        />
      </div>
      
      {error && <div style={{ color: 'red' }}>{error}</div>}
      
      <button type="submit" disabled={loading}>
        {loading ? 'Logging in...' : 'Login'}
      </button>
    </form>
  );
};

export default LoginForm;
```

#### Protected Route Component

```javascript
// ProtectedRoute.js
import React from 'react';
import { useAuth } from './AuthContext';

const ProtectedRoute = ({ children }) => {
  const { isAuthenticated, loading } = useAuth();

  if (loading) {
    return <div>Loading...</div>;
  }

  if (!isAuthenticated) {
    return <div>Please log in to access this page.</div>;
  }

  return children;
};

export default ProtectedRoute;
```

### Mobile App Integration

#### React Native Example

```javascript
// AuthService.js for React Native
import AsyncStorage from '@react-native-async-storage/async-storage';

class AuthService {
  constructor(baseUrl) {
    this.baseUrl = baseUrl;
  }

  async login(email, password) {
    try {
      const response = await fetch(`${this.baseUrl}/api/security/auth`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, password }),
      });

      if (response.ok) {
        const authToken = await response.json();
        
        // Store tokens securely
        await AsyncStorage.setItem('accessToken', authToken.accessToken);
        await AsyncStorage.setItem('refreshToken', authToken.refreshToken);
        
        return { success: true, token: authToken };
      } else {
        const error = await response.json();
        return { success: false, error: error.message };
      }
    } catch (error) {
      return { success: false, error: 'Network error' };
    }
  }

  async getStoredToken() {
    try {
      return await AsyncStorage.getItem('accessToken');
    } catch (error) {
      return null;
    }
  }

  async logout() {
    try {
      await AsyncStorage.multiRemove(['accessToken', 'refreshToken']);
    } catch (error) {
      console.error('Logout error:', error);
    }
  }
}

export default AuthService;
```

## API Integration

### Creating Authenticated HTTP Clients

#### C# HttpClient Example

```csharp
public class AuthenticatedHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private string? _accessToken;
    private string? _refreshToken;

    public AuthenticatedHttpClient(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var loginRequest = new { email, password };
        var response = await _httpClient.PostAsJsonAsync("/api/security/auth", loginRequest);
        
        if (response.IsSuccessStatusCode)
        {
            var authToken = await response.Content.ReadFromJsonAsync<AuthToken>();
            _accessToken = authToken.AccessToken;
            _refreshToken = authToken.RefreshToken;
            
            // Set default authorization header
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            
            return true;
        }
        
        return false;
    }

    public async Task<HttpResponseMessage> GetAsync(string requestUri)
    {
        var response = await _httpClient.GetAsync(requestUri);
        
        // Handle token refresh on 401
        if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(_refreshToken))
        {
            if (await RefreshTokenAsync())
            {
                // Retry with new token
                response = await _httpClient.GetAsync(requestUri);
            }
        }
        
        return response;
    }

    private async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken)) return false;

        var refreshRequest = new { refreshToken = _refreshToken };
        var response = await _httpClient.PostAsJsonAsync("/api/security/refresh", refreshRequest);
        
        if (response.IsSuccessStatusCode)
        {
            var newTokens = await response.Content.ReadFromJsonAsync<AuthToken>();
            _accessToken = newTokens.AccessToken;
            _refreshToken = newTokens.RefreshToken;
            
            // Update authorization header
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            
            return true;
        }
        
        // Refresh failed, clear tokens
        _accessToken = null;
        _refreshToken = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
        
        return false;
    }
}
```

## Token Management

### Automatic Token Refresh

#### Server-Side Token Refresh Service

```csharp
public class TokenRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenRefreshService> _logger;

    public TokenRefreshService(IServiceProvider serviceProvider, ILogger<TokenRefreshService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dataContext = scope.ServiceProvider.GetRequiredService<IDataContext>();
                
                // Find tokens expiring soon
                var expiringTokens = await dataContext.Query()
                    .With<AuthToken>(t => !t.IsRevoked)
                    .With<AuthToken>(t => t.RefreshExpiresAt > DateTime.UtcNow)
                    .With<AuthToken>(t => t.RefreshExpiresAt < DateTime.UtcNow.AddDays(7)) // Refresh if expires in 7 days
                    .ToEntityComponents();

                foreach (var (entityId, components) in expiringTokens)
                {
                    var token = components.Get<AuthToken>();
                    if (token != null)
                    {
                        await RefreshTokenForUser(dataContext, token);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in token refresh service");
            }

            // Run every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task RefreshTokenForUser(IDataContext dataContext, AuthToken token)
    {
        try
        {
            var authHandler = dataContext.For<AuthHandler>(Guid.NewGuid());
            var newToken = await authHandler.RefreshToken(token.RefreshToken);
            
            if (!string.IsNullOrEmpty(newToken.AccessToken))
            {
                _logger.LogInformation("Refreshed token for user {UserId}", token.OwnerEntityId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token for user {UserId}", token.OwnerEntityId);
        }
    }
}
```

### Token Cleanup Service

```csharp
public class TokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dataContext = scope.ServiceProvider.GetRequiredService<IDataContext>();
                
                // Remove expired tokens
                var expiredTokens = await dataContext.Query()
                    .With<AuthToken>(t => t.RefreshExpiresAt < DateTime.UtcNow)
                    .ToEntityIds();

                foreach (var tokenEntityId in expiredTokens)
                {
                    var tokenHandler = dataContext.For<AuthTokenHandler>(tokenEntityId);
                    await tokenHandler.Remove();
                }

                _logger.LogInformation("Cleaned up {Count} expired tokens", expiredTokens.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in token cleanup service");
            }

            // Run daily
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
```

## User Management

### Admin User Management System

```csharp
public class UserManagementSystem : SystemBase
{
    public UserManagementSystem(IDataContext dataContext, ILogger<UserManagementSystem> logger)
        : base(dataContext, logger)
    {
    }

    /// <summary>
    /// Get all user accounts with pagination
    /// </summary>
    public async Task<UserListResult> GetUsers(int page = 1, int pageSize = 50)
    {
        var query = DataContext.Query()
            .With<Account>();

        var allAccounts = await query.ToEntityComponents();
        var accounts = allAccounts.Values
            .Select(c => c.Get<Account>())
            .Where(a => a != null)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new UserListResult
        {
            Users = accounts.Select(a => new UserInfo
            {
                Id = a.OwnerEntityId,
                Email = a.Email,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                LastUpdated = a.LastUpdated
            }).ToList(),
            TotalCount = allAccounts.Count,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Activate a user account
    /// </summary>
    public async Task<bool> ActivateUser(Guid userEntityId)
    {
        try
        {
            var accountHandler = DataContext.For<AccountHandler>(userEntityId);
            await accountHandler.Activate();
            
            Logger.LogInformation("User {UserId} activated", userEntityId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to activate user {UserId}", userEntityId);
            return false;
        }
    }

    /// <summary>
    /// Deactivate a user account and revoke all sessions
    /// </summary>
    public async Task<bool> DeactivateUser(Guid userEntityId)
    {
        try
        {
            // Deactivate account
            var accountHandler = DataContext.For<AccountHandler>(userEntityId);
            await accountHandler.Deactivate();

            // Revoke all active sessions
            var userTokens = await DataContext.Query()
                .With<AuthToken>(t => t.OwnerEntityId == userEntityId)
                .With<AuthToken>(t => !t.IsRevoked)
                .ToEntityIds();

            foreach (var tokenEntityId in userTokens)
            {
                var tokenHandler = DataContext.For<AuthTokenHandler>(tokenEntityId);
                await tokenHandler.RevokeToken();
            }
            
            Logger.LogInformation("User {UserId} deactivated and sessions revoked", userEntityId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to deactivate user {UserId}", userEntityId);
            return false;
        }
    }

    /// <summary>
    /// Get user's active sessions
    /// </summary>
    public async Task<List<SessionInfo>> GetUserSessions(Guid userEntityId)
    {
        var sessions = await DataContext.Query()
            .With<AuthToken>(t => t.OwnerEntityId == userEntityId)
            .With<AuthToken>(t => !t.IsRevoked)
            .With<AuthToken>(t => t.RefreshExpiresAt > DateTime.UtcNow)
            .ToEntityComponents();

        return sessions.Values
            .Select(c => c.Get<AuthToken>())
            .Where(t => t != null)
            .Select(t => new SessionInfo
            {
                SessionId = t.SessionId,
                ClientId = t.ClientId,
                IpAddress = t.IpAddress,
                UserAgent = t.UserAgent,
                IssuedAt = t.IssuedAt,
                ExpiresAt = t.RefreshExpiresAt
            })
            .OrderByDescending(s => s.IssuedAt)
            .ToList();
    }
}

// Supporting models
public class UserListResult
{
    public List<UserInfo> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class SessionInfo
{
    public Guid SessionId { get; set; }
    public string? ClientId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

## Session Management

### Session Monitoring Service

```csharp
public class SessionMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionMonitoringService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dataContext = scope.ServiceProvider.GetRequiredService<IDataContext>();
                
                // Monitor for suspicious session activity
                await DetectSuspiciousSessions(dataContext);
                
                // Enforce session limits
                await EnforceSessionLimits(dataContext);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in session monitoring service");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task DetectSuspiciousSessions(IDataContext dataContext)
    {
        // Find sessions with multiple IPs
        var sessions = await dataContext.Query()
            .With<AuthToken>(t => !t.IsRevoked)
            .With<AuthToken>(t => t.RefreshExpiresAt > DateTime.UtcNow)
            .ToEntityComponents();

        var sessionsByUser = sessions.Values
            .Select(c => c.Get<AuthToken>())
            .Where(t => t != null)
            .GroupBy(t => t.OwnerEntityId);

        foreach (var userSessions in sessionsByUser)
        {
            var distinctIps = userSessions
                .Where(s => !string.IsNullOrEmpty(s.IpAddress))
                .Select(s => s.IpAddress)
                .Distinct()
                .Count();

            // Alert if user has sessions from more than 3 different IPs
            if (distinctIps > 3)
            {
                _logger.LogWarning("User {UserId} has sessions from {IpCount} different IP addresses", 
                    userSessions.Key, distinctIps);
                
                // Could trigger additional security measures here
            }
        }
    }

    private async Task EnforceSessionLimits(IDataContext dataContext)
    {
        var sessions = await dataContext.Query()
            .With<AuthToken>(t => !t.IsRevoked)
            .With<AuthToken>(t => t.RefreshExpiresAt > DateTime.UtcNow)
            .ToEntityComponents();

        var sessionsByUser = sessions
            .GroupBy(kvp => kvp.Value.Get<AuthToken>()?.OwnerEntityId)
            .Where(g => g.Key.HasValue);

        foreach (var userSessions in sessionsByUser)
        {
            var userTokens = userSessions
                .Select(kvp => new { EntityId = kvp.Key, Token = kvp.Value.Get<AuthToken>() })
                .Where(x => x.Token != null)
                .OrderByDescending(x => x.Token.IssuedAt)
                .ToList();

            // Keep only the 5 most recent sessions
            if (userTokens.Count > 5)
            {
                var tokensToRevoke = userTokens.Skip(5);
                
                foreach (var tokenToRevoke in tokensToRevoke)
                {
                    var tokenHandler = dataContext.For<AuthTokenHandler>(tokenToRevoke.EntityId);
                    await tokenHandler.RevokeToken();
                }

                _logger.LogInformation("Revoked {Count} old sessions for user {UserId}", 
                    tokensToRevoke.Count(), userSessions.Key);
            }
        }
    }
}
```

## Custom Authentication Flows

### Two-Factor Authentication Extension

```csharp
public class TwoFactorAuthHandler : ComponentHandler<TwoFactorAuth>
{
    private readonly ITwoFactorService _twoFactorService;

    public TwoFactorAuthHandler(
        IDataContext dataContext, 
        ILogger<TwoFactorAuthHandler> logger,
        ITwoFactorService twoFactorService)
        : base(dataContext, logger)
    {
        _twoFactorService = twoFactorService;
    }

    public async Task<TwoFactorSetupResult> SetupTwoFactor(Guid userEntityId)
    {
        // Generate secret key
        var secret = _twoFactorService.GenerateSecret();
        var qrCodeUrl = _twoFactorService.GenerateQrCodeUrl(secret, "user@example.com");

        // Store secret (not yet active)
        var twoFactorAuth = new TwoFactorAuth
        {
            OwnerEntityId = userEntityId,
            Secret = secret,
            IsEnabled = false,
            BackupCodes = _twoFactorService.GenerateBackupCodes()
        };

        await DataContext.Commit(twoFactorAuth);

        return new TwoFactorSetupResult
        {
            Secret = secret,
            QrCodeUrl = qrCodeUrl,
            BackupCodes = twoFactorAuth.BackupCodes
        };
    }

    public async Task<bool> VerifyAndEnable(string code)
    {
        var twoFactorAuth = await GetOrDefault();
        if (twoFactorAuth == null) return false;

        var isValid = _twoFactorService.VerifyCode(twoFactorAuth.Secret, code);
        if (isValid)
        {
            var enabled = twoFactorAuth with { IsEnabled = true };
            await DataContext.Commit(enabled);
        }

        return isValid;
    }

    public async Task<bool> VerifyCode(string code)
    {
        var twoFactorAuth = await GetOrDefault();
        if (twoFactorAuth?.IsEnabled != true) return false;

        // Check regular code
        if (_twoFactorService.VerifyCode(twoFactorAuth.Secret, code))
        {
            return true;
        }

        // Check backup codes
        if (twoFactorAuth.BackupCodes.Contains(code))
        {
            // Remove used backup code
            var updatedCodes = twoFactorAuth.BackupCodes.Where(c => c != code).ToList();
            var updated = twoFactorAuth with { BackupCodes = updatedCodes };
            await DataContext.Commit(updated);
            return true;
        }

        return false;
    }
}

public record TwoFactorAuth : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public string Secret { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public List<string> BackupCodes { get; init; } = new();
}
```

### Social Login Integration

```csharp
public class SocialAuthHandler : ComponentHandler<SocialAccount>
{
    private readonly ISocialAuthService _socialAuthService;

    public async Task<AuthToken> AuthenticateWithGoogle(string googleToken)
    {
        // Verify Google token
        var googleUser = await _socialAuthService.VerifyGoogleToken(googleToken);
        if (googleUser == null) return new AuthToken();

        // Find or create account
        var accountQuery = DataContext.Query()
            .With<Account>(a => a.Email == googleUser.Email);
        var existingAccounts = await accountQuery.ToEntityComponents();

        Account account;
        if (existingAccounts.Any())
        {
            // Existing account
            var entityId = existingAccounts.First().Key;
            var accountHandler = DataContext.For<AccountHandler>(entityId);
            account = await accountHandler.GetOrDefault();
        }
        else
        {
            // Create new account
            var entityId = Guid.NewGuid();
            var accountHandler = DataContext.For<AccountHandler>(entityId);
            account = await accountHandler.Register(new Account
            {
                Email = googleUser.Email,
                Password = Guid.NewGuid().ToString(), // Random password for social accounts
                AuthMethod = "google"
            });
            
            // Auto-activate social accounts
            await accountHandler.Activate();
        }

        // Create/update social account link
        var socialAccount = new SocialAccount
        {
            OwnerEntityId = account.OwnerEntityId,
            Provider = "google",
            ProviderId = googleUser.Id,
            Email = googleUser.Email,
            Name = googleUser.Name,
            ProfilePicture = googleUser.Picture
        };
        
        await DataContext.Commit(socialAccount);

        // Generate auth token
        var tokenService = DataContext.GetService<ITokenService>();
        var authToken = new AuthToken
        {
            OwnerEntityId = account.OwnerEntityId,
            AccessToken = tokenService.AccessToken(account.OwnerEntityId, account.Email),
            RefreshToken = tokenService.RefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            RefreshExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        return authToken;
    }
}

public record SocialAccount : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public string Provider { get; init; } = string.Empty; // google, facebook, etc.
    public string ProviderId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ProfilePicture { get; init; }
}
```

## Configuration Examples

### Production Configuration

```json
{
  "Jwt": {
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 30,
    "SecretKey": "${JWT_SECRET_KEY}",
    "Issuer": "MyApp",
    "Audience": "MyApp-Users"
  },
  "Authentication": {
    "EnableTwoFactor": true,
    "MaxFailedAttempts": 5,
    "LockoutDurationMinutes": 15,
    "MaxActiveSessions": 5,
    "RequireEmailVerification": true
  },
  "PasswordPolicy": {
    "MinLength": 12,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireNumbers": true,
    "RequireSpecialChars": true,
    "PreventCommonPasswords": true
  }
}
```

### Development Configuration

```json
{
  "Jwt": {
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7,
    "SecretKey": "your-development-secret-key-must-be-256-bits-long",
    "Issuer": "MyApp-Dev",
    "Audience": "MyApp-Dev-Users"
  },
  "Authentication": {
    "EnableTwoFactor": false,
    "MaxFailedAttempts": 10,
    "LockoutDurationMinutes": 5,
    "MaxActiveSessions": 10,
    "RequireEmailVerification": false
  }
}
```

---

**Next**: [Security Considerations](authentication-security.md) - Security best practices and advanced security features