# Auth Token Persistence for Development

This document explains the authentication token persistence feature that makes development easier by maintaining login sessions across server restarts.

## Overview

When developing with the Jarvis Studio UI, you no longer need to log in every time you restart the development server. The system now:

1. **Stores both access and refresh tokens** in localStorage
2. **Automatically refreshes expired tokens** on app startup
3. **Schedules token refresh** before expiration
4. **Maintains authentication state** across server restarts

## How It Works

### Initial Login
1. User logs in with credentials
2. API returns access token (short-lived) and refresh token (long-lived)
3. Both tokens are stored in localStorage
4. User info is also cached

### On App Startup
1. AuthContext checks for stored tokens
2. If access token is expired but refresh token is valid:
   - Automatically refreshes the access token
   - Updates the stored tokens
   - Loads user data and navigation
3. If access token is still valid:
   - Uses existing token
   - Schedules refresh for later

### Automatic Token Refresh
- System calculates when token will expire
- Schedules refresh 5 minutes before expiration
- Refreshes silently in the background
- User stays logged in seamlessly

## Development Mode

In development (`npm run dev`):
- Token persistence is **automatically enabled**
- A yellow indicator appears on the dashboard
- Tokens persist for the lifetime of the refresh token (typically 7-30 days)

## Token Storage

Tokens are stored in localStorage with these keys:
- `jarvis_auth_token` - Access token
- `jarvis_refresh_token` - Refresh token
- `jarvis_current_user` - Cached user data

## Security Considerations

This feature is designed for development convenience:
- Only enabled in development mode by default
- Production deployments should use appropriate session management
- Tokens are stored in plain text in localStorage
- Suitable for development, not for sensitive production data

## Testing the Feature

1. Start the dev server: `npm run dev`
2. Log in once with your credentials
3. Stop the server (Ctrl+C)
4. Start the server again
5. You should be automatically logged in

## Troubleshooting

If auto-login isn't working:

1. **Check localStorage**:
   - Open browser DevTools → Application → Local Storage
   - Verify tokens are present

2. **Check token expiration**:
   - Access tokens expire quickly (15 mins - 1 hour)
   - Refresh tokens last longer (7-30 days)
   - If refresh token expired, you'll need to log in again

3. **Clear and retry**:
   ```javascript
   // In browser console
   localStorage.clear();
   location.reload();
   ```

## API Requirements

For this to work with your Jarvis API:
- `/api/security/auth` - Returns both access and refresh tokens
- `/api/security/refresh` - Accepts refresh token, returns new tokens
- Tokens should follow JWT format with `exp` claim

## Benefits

- **Faster development** - No repeated logins
- **Better DX** - Focus on coding, not authentication
- **Preserves state** - Navigation and user data persist
- **Automatic refresh** - Tokens refresh seamlessly