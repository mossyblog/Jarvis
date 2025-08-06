/**
 * Mock Recent Pages Data
 * 
 * Utility to populate recent pages with realistic test data.
 * This helps with development and testing of the Recent Pages List component.
 */

import { getRecentPagesManager, type RecentPageMetadata } from './recentPagesManager';

export const mockRecentPagesData: RecentPageMetadata[] = [
  {
    id: 'page-1',
    displayName: 'Analytics Dashboard',
    route: '/analytics',
    pageSlug: 'analytics',
    status: 'published',
    lastAccessed: new Date(Date.now() - 1000 * 60 * 15).toISOString(), // 15 minutes ago
    accessCount: 23,
    description: 'Comprehensive analytics dashboard with charts, metrics, and KPI tracking',
    tags: ['dashboard', 'analytics', 'charts', 'metrics'],
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 7).toISOString(), // 1 week ago
    updatedAt: new Date(Date.now() - 1000 * 60 * 60 * 6).toISOString(), // 6 hours ago
    createdBy: 'user-1',
    thumbnailUrl: 'data:image/svg+xml,' + encodeURIComponent(`
      <svg width="320" height="180" xmlns="http://www.w3.org/2000/svg">
        <rect width="100%" height="100%" fill="#f8fafc"/>
        <rect x="20" y="20" width="280" height="40" fill="#3b82f6" rx="4"/>
        <rect x="20" y="80" width="80" height="60" fill="#10b981" rx="4"/>
        <rect x="120" y="80" width="80" height="60" fill="#f59e0b" rx="4"/>
        <rect x="220" y="80" width="80" height="60" fill="#ef4444" rx="4"/>
        <text x="160" y="50" text-anchor="middle" fill="white" font-size="16">Analytics</text>
      </svg>
    `)
  },
  {
    id: 'page-2',
    displayName: 'User Management Portal',
    route: '/users',
    pageSlug: 'users',
    status: 'published',
    lastAccessed: new Date(Date.now() - 1000 * 60 * 45).toISOString(), // 45 minutes ago
    accessCount: 12,
    description: 'Complete user management system with roles, permissions, and profile editing',
    tags: ['users', 'admin', 'management', 'permissions'],
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 5).toISOString(), // 5 days ago
    updatedAt: new Date(Date.now() - 1000 * 60 * 60 * 12).toISOString(), // 12 hours ago
    createdBy: 'user-1',
    thumbnailUrl: 'data:image/svg+xml,' + encodeURIComponent(`
      <svg width="320" height="180" xmlns="http://www.w3.org/2000/svg">
        <rect width="100%" height="100%" fill="#f8fafc"/>
        <rect x="20" y="20" width="280" height="30" fill="#6366f1" rx="4"/>
        <circle cx="60" cy="80" r="15" fill="#8b5cf6"/>
        <circle cx="140" cy="80" r="15" fill="#06b6d4"/>
        <circle cx="220" cy="80" r="15" fill="#84cc16"/>
        <rect x="20" y="110" width="100" height="8" fill="#e2e8f0" rx="2"/>
        <rect x="140" y="110" width="80" height="8" fill="#e2e8f0" rx="2"/>
        <rect x="240" y="110" width="60" height="8" fill="#e2e8f0" rx="2"/>
        <text x="160" y="45" text-anchor="middle" fill="white" font-size="14">Users</text>
      </svg>
    `)
  },
  {
    id: 'page-3',
    displayName: 'Product Catalog',
    route: '/products',
    pageSlug: 'products',
    status: 'draft',
    lastAccessed: new Date(Date.now() - 1000 * 60 * 60 * 2).toISOString(), // 2 hours ago
    accessCount: 7,
    description: 'Product catalog with inventory management, pricing, and category organization',
    tags: ['products', 'catalog', 'inventory', 'e-commerce'],
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 3).toISOString(), // 3 days ago
    updatedAt: new Date(Date.now() - 1000 * 60 * 60 * 2).toISOString(), // 2 hours ago
    createdBy: 'user-2',
    thumbnailUrl: 'data:image/svg+xml,' + encodeURIComponent(`
      <svg width="320" height="180" xmlns="http://www.w3.org/2000/svg">
        <rect width="100%" height="100%" fill="#f8fafc"/>
        <rect x="20" y="20" width="280" height="30" fill="#f59e0b" rx="4"/>
        <rect x="20" y="65" width="80" height="80" fill="#fbbf24" rx="8"/>
        <rect x="120" y="65" width="80" height="80" fill="#fbbf24" rx="8"/>
        <rect x="220" y="65" width="80" height="80" fill="#fbbf24" rx="8"/>
        <text x="160" y="40" text-anchor="middle" fill="white" font-size="14">Products</text>
      </svg>
    `)
  },
  {
    id: 'page-4',
    displayName: 'Sales Reports',
    route: '/reports/sales',
    pageSlug: 'reports-sales',
    status: 'published',
    lastAccessed: new Date(Date.now() - 1000 * 60 * 60 * 6).toISOString(), // 6 hours ago
    accessCount: 15,
    description: 'Detailed sales reports with trends, forecasting, and performance metrics',
    tags: ['reports', 'sales', 'revenue', 'analytics'],
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 10).toISOString(), // 10 days ago
    updatedAt: new Date(Date.now() - 1000 * 60 * 60 * 24).toISOString(), // 1 day ago
    createdBy: 'user-1',
    thumbnailUrl: 'data:image/svg+xml,' + encodeURIComponent(`
      <svg width="320" height="180" xmlns="http://www.w3.org/2000/svg">
        <rect width="100%" height="100%" fill="#f8fafc"/>
        <rect x="20" y="20" width="280" height="30" fill="#10b981" rx="4"/>
        <path d="M 30 140 L 80 100 L 130 120 L 180 80 L 230 90 L 280 60" stroke="#10b981" stroke-width="3" fill="none"/>
        <rect x="20" y="150" width="40" height="10" fill="#34d399" rx="2"/>
        <rect x="80" y="150" width="60" height="10" fill="#34d399" rx="2"/>
        <rect x="160" y="150" width="50" height="10" fill="#34d399" rx="2"/>
        <text x="160" y="40" text-anchor="middle" fill="white" font-size="14">Sales</text>
      </svg>
    `)
  },
  {
    id: 'page-5',
    displayName: 'Settings & Configuration',
    route: '/settings',
    pageSlug: 'settings',
    status: 'published',
    lastAccessed: new Date(Date.now() - 1000 * 60 * 60 * 24).toISOString(), // 1 day ago
    accessCount: 4,
    description: 'System settings, user preferences, and application configuration options',
    tags: ['settings', 'config', 'preferences', 'admin'],
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 14).toISOString(), // 2 weeks ago
    updatedAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 3).toISOString(), // 3 days ago
    createdBy: 'user-1',
    thumbnailUrl: 'data:image/svg+xml,' + encodeURIComponent(`
      <svg width="320" height="180" xmlns="http://www.w3.org/2000/svg">
        <rect width="100%" height="100%" fill="#f8fafc"/>
        <rect x="20" y="20" width="280" height="30" fill="#6b7280" rx="4"/>
        <circle cx="80" cy="90" r="20" fill="none" stroke="#6b7280" stroke-width="2"/>
        <circle cx="80" cy="90" r="8" fill="#6b7280"/>
        <rect x="120" y="70" width="40" height="15" fill="#d1d5db" rx="7"/>
        <rect x="180" y="70" width="40" height="15" fill="#d1d5db" rx="7"/>
        <rect x="120" y="95" width="60" height="15" fill="#d1d5db" rx="7"/>
        <rect x="120" y="120" width="50" height="15" fill="#d1d5db" rx="7"/>
        <text x="160" y="40" text-anchor="middle" fill="white" font-size="14">Settings</text>
      </svg>
    `)
  },
  {
    id: 'page-6',
    displayName: 'Customer Support Portal',
    route: '/support',
    pageSlug: 'support',
    status: 'draft',
    lastAccessed: new Date(Date.now() - 1000 * 60 * 60 * 24 * 2).toISOString(), // 2 days ago
    accessCount: 3,
    description: 'Customer support portal with ticket management and knowledge base',
    tags: ['support', 'tickets', 'customer', 'help'],
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 4).toISOString(), // 4 days ago
    updatedAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 2).toISOString(), // 2 days ago
    createdBy: 'user-3',
    thumbnailUrl: 'data:image/svg+xml,' + encodeURIComponent(`
      <svg width="320" height="180" xmlns="http://www.w3.org/2000/svg">
        <rect width="100%" height="100%" fill="#f8fafc"/>
        <rect x="20" y="20" width="280" height="30" fill="#8b5cf6" rx="4"/>
        <circle cx="60" cy="90" r="25" fill="none" stroke="#8b5cf6" stroke-width="2"/>
        <path d="M 50 85 L 70 85 M 55 95 L 65 95" stroke="#8b5cf6" stroke-width="2"/>
        <rect x="100" y="70" width="180" height="10" fill="#c4b5fd" rx="2"/>
        <rect x="100" y="90" width="140" height="8" fill="#e0e7ff" rx="2"/>
        <rect x="100" y="105" width="160" height="8" fill="#e0e7ff" rx="2"/>
        <rect x="100" y="120" width="120" height="8" fill="#e0e7ff" rx="2"/>
        <text x="160" y="40" text-anchor="middle" fill="white" font-size="14">Support</text>
      </svg>
    `)
  },
  {
    id: 'page-7',
    displayName: 'Project Timeline',
    route: '/timeline',
    pageSlug: 'timeline',
    status: 'archived',
    lastAccessed: new Date(Date.now() - 1000 * 60 * 60 * 24 * 7).toISOString(), // 1 week ago
    accessCount: 1,
    description: 'Project timeline and milestone tracking (archived version)',
    tags: ['timeline', 'projects', 'milestones', 'archive'],
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 30).toISOString(), // 30 days ago
    updatedAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 20).toISOString(), // 20 days ago
    createdBy: 'user-2',
    thumbnailUrl: 'data:image/svg+xml,' + encodeURIComponent(`
      <svg width="320" height="180" xmlns="http://www.w3.org/2000/svg">
        <rect width="100%" height="100%" fill="#f8fafc"/>
        <rect x="20" y="20" width="280" height="30" fill="#9ca3af" rx="4"/>
        <line x1="30" y1="80" x2="290" y2="80" stroke="#9ca3af" stroke-width="2"/>
        <circle cx="80" cy="80" r="6" fill="#f87171"/>
        <circle cx="160" cy="80" r="6" fill="#fbbf24"/>
        <circle cx="240" cy="80" r="6" fill="#34d399"/>
        <rect x="70" y="90" width="20" height="6" fill="#e5e7eb" rx="1"/>
        <rect x="150" y="90" width="20" height="6" fill="#e5e7eb" rx="1"/>
        <rect x="230" y="90" width="20" height="6" fill="#e5e7eb" rx="1"/>
        <text x="160" y="40" text-anchor="middle" fill="white" font-size="14">Timeline</text>
      </svg>
    `)
  }
];

/**
 * Populate recent pages with mock data for development/testing
 */
export const populateMockRecentPages = (): void => {
  const manager = getRecentPagesManager();
  
  // Clear existing data
  manager.clearAll();
  
  // Add mock pages with staggered access times
  mockRecentPagesData.forEach((page, index) => {
    // Add some randomness to access counts and times
    const randomizedPage = {
      ...page,
      accessCount: page.accessCount + Math.floor(Math.random() * 5),
      lastAccessed: new Date(
        Date.now() - (index * 1000 * 60 * 30) - (Math.random() * 1000 * 60 * 60 * 24)
      ).toISOString()
    };
    
    manager.addPage(randomizedPage);
  });
  
  console.log(`✅ Populated ${mockRecentPagesData.length} mock recent pages`);
};

/**
 * Clear all recent pages data
 */
export const clearMockRecentPages = (): void => {
  const manager = getRecentPagesManager();
  manager.clearAll();
  console.log('🗑️ Cleared all recent pages data');
};

/**
 * Get statistics about mock data
 */
export const getMockDataStats = () => {
  const manager = getRecentPagesManager();
  return manager.getStatistics();
};

// Note: Auto-populate removed to prevent duplicate initialization
// Call populateMockRecentPages() explicitly when needed