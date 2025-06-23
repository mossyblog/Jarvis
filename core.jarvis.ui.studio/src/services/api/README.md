# Mock API System for Dynamic Navigation

This mock API system provides role-based navigation and authentication for development purposes. It's designed to be easily replaced with a real API when ready.

## Features

- **Role-based navigation**: Dynamic menu items based on user permissions
- **Mock authentication**: Login with different user roles
- **Permission system**: Fine-grained access control
- **Easy migration**: Consistent interface for swapping to real API

## Mock Users

The system includes four mock users with different permission levels:

| Email | Name | Role | Access Level |
|-------|------|------|--------------|
| admin@jarvis.io | Admin User | Administrator | Full system access |
| dev@jarvis.io | Developer User | Developer | Development tools access |
| editor@jarvis.io | Editor User | Content Editor | Content management access |
| viewer@jarvis.io | Viewer User | Viewer | Read-only access |

## Usage

### Login
Navigate to `/login` and select a user role. Any password works in mock mode.

### Check Permissions
```typescript
const { hasPermission } = useAuth();

if (hasPermission('table-editor', 'write')) {
  // User can write to table editor
}
```

### Protected Routes
```typescript
<ProtectedRoute requiredPermission="schema-visualizer">
  <SchemaVisualizer />
</ProtectedRoute>
```

## Switching to Real API

To use a real API instead of the mock:

1. Set environment variable: `VITE_USE_MOCK_API=false`
2. Implement the `RealApiService` class methods in `apiService.ts`
3. Update the API URL: `VITE_API_URL=https://your-api-url.com`

The interface remains the same, ensuring a smooth transition.

## Architecture

```
src/services/api/
├── types.ts          # TypeScript interfaces
├── mockData.ts       # Mock users and navigation items
├── apiService.ts     # API service implementations
└── README.md         # This file

src/contexts/
└── AuthContext.tsx   # React context for auth state

src/components/auth/
└── ProtectedRoute.tsx # Route protection component
```

## Adding New Permissions

1. Add new navigation items in `mockData.ts`:
```typescript
{
  id: 'new-feature',
  label: 'New Feature',
  icon: 'Star',
  href: '/new-feature',
  requiredPermission: 'new-feature'
}
```

2. Add permissions to roles:
```typescript
permissions: [
  { id: 'perm-new', resource: 'new-feature', actions: ['read', 'write'] }
]
```

## Future Enhancements

- [ ] Add refresh token rotation
- [ ] Implement session timeout
- [ ] Add role hierarchy
- [ ] Support for nested permissions
- [ ] Add audit logging for permission checks