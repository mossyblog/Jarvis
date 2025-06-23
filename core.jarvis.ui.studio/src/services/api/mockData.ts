import type { User, Role, NavigationItem } from './types';

export const mockRoles: Record<string, Role> = {
  admin: {
    id: 'role-admin',
    name: 'Administrator',
    description: 'Full system access',
    permissions: [
      { id: 'perm-1', resource: '*', actions: ['*'] }
    ]
  },
  developer: {
    id: 'role-dev',
    name: 'Developer',
    description: 'Access to development tools',
    permissions: [
      { id: 'perm-2', resource: 'table-editor', actions: ['read', 'write'] },
      { id: 'perm-3', resource: 'schema-visualizer', actions: ['read'] },
      { id: 'perm-4', resource: 'sql-editor', actions: ['read', 'write'] },
      { id: 'perm-5', resource: 'database', actions: ['read'] },
      { id: 'perm-6', resource: 'functions', actions: ['read', 'write'] },
      { id: 'perm-7', resource: 'api', actions: ['read'] },
      { id: 'perm-8', resource: 'logs', actions: ['read'] }
    ]
  },
  editor: {
    id: 'role-editor',
    name: 'Content Editor',
    description: 'Content management access',
    permissions: [
      { id: 'perm-9', resource: 'table-editor', actions: ['read', 'write'] },
      { id: 'perm-10', resource: 'storage', actions: ['read', 'write'] },
      { id: 'perm-11', resource: 'realtime', actions: ['read'] }
    ]
  },
  viewer: {
    id: 'role-viewer',
    name: 'Viewer',
    description: 'Read-only access',
    permissions: [
      { id: 'perm-12', resource: 'home', actions: ['read'] },
      { id: 'perm-13', resource: 'reports', actions: ['read'] },
      { id: 'perm-14', resource: 'api', actions: ['read'] }
    ]
  }
};

export const mockUsers: User[] = [
  {
    id: 'user-1',
    email: 'admin@jarvis.io',
    name: 'Admin User',
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=admin',
    roles: [mockRoles.admin],
    preferences: {
      theme: 'dark',
      sidebarBehavior: 'open'
    }
  },
  {
    id: 'user-2',
    email: 'dev@jarvis.io',
    name: 'Developer User',
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=dev',
    roles: [mockRoles.developer],
    preferences: {
      theme: 'dark',
      sidebarBehavior: 'expandable'
    }
  },
  {
    id: 'user-3',
    email: 'editor@jarvis.io',
    name: 'Editor User',
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=editor',
    roles: [mockRoles.editor],
    preferences: {
      theme: 'dark',
      sidebarBehavior: 'open'
    }
  },
  {
    id: 'user-4',
    email: 'viewer@jarvis.io',
    name: 'Viewer User',
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=viewer',
    roles: [mockRoles.viewer],
    preferences: {
      theme: 'dark',
      sidebarBehavior: 'closed'
    }
  }
];

export const fullNavigationItems: NavigationItem[] = [
  { id: 'home', label: 'Project overview', icon: 'Home', href: '/', requiredPermission: 'home' },
  { id: 'table-editor', label: 'Table Editor', icon: 'Table', href: '/editor', requiredPermission: 'table-editor' },
  { id: 'schema-visualizer', label: 'Schema Visualizer', icon: 'Database', href: '/SchemaVisualizer', requiredPermission: 'schema-visualizer' },
  { id: 'sql-editor', label: 'SQL Editor', icon: 'FileCode2', href: '/sql', requiredPermission: 'sql-editor' },
  { id: 'database', label: 'Database', icon: 'Database', href: '/database', requiredPermission: 'database' },
  { id: 'auth', label: 'Authentication', icon: 'Shield', href: '/auth', requiredPermission: 'auth' },
  { id: 'storage', label: 'Storage', icon: 'HardDrive', href: '/storage', requiredPermission: 'storage' },
  { id: 'functions', label: 'Edge Functions', icon: 'FileText', href: '/functions', requiredPermission: 'functions' },
  { id: 'realtime', label: 'Realtime', icon: 'Radio', href: '/realtime', requiredPermission: 'realtime' },
  { id: 'advisors', label: 'Advisors', icon: 'AlertTriangle', href: '/advisors', requiredPermission: 'advisors' },
  { id: 'reports', label: 'Reports', icon: 'ScrollText', href: '/reports', requiredPermission: 'reports' },
  { id: 'logs', label: 'Logs', icon: 'FileText', href: '/logs', requiredPermission: 'logs' },
  { id: 'api', label: 'API Docs', icon: 'FileCode2', href: '/api', requiredPermission: 'api' },
  { id: 'integrations', label: 'Integrations', icon: 'Plug', href: '/integrations', requiredPermission: 'integrations' },
  { id: 'settings', label: 'Project Settings', icon: 'Settings', href: '/settings', requiredPermission: 'settings' }
];