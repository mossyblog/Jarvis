/**
 * Pages Components Index
 * 
 * Export all page-related components and utilities
 */

export { RecentPagesList } from './RecentPagesList';
export type { RecentPagesListProps } from './RecentPagesList';

// Page creation and editing components
export { PageCreationModal } from './PageCreationModal';
export type { PageCreationModalProps } from './PageCreationModal';

// Re-export types and utilities from the recent pages manager
export type { 
  RecentPageMetadata, 
  RecentPagesStorage 
} from '@/utils/recentPagesManager';

export { 
  getRecentPagesManager,
  addToRecentPages,
  getRecentPages,
  removeFromRecentPages,
  clearRecentPages,
  useRecentPages
} from '@/utils/recentPagesManager';