/**
 * UIStudioInterface Component
 * 
 * Main discovery dashboard interface for UIStudio that provides users with
 * a comprehensive view of their pages, templates, and quick actions.
 * 
 * This component implements the discovery dashboard requirements from Task 1.1.1:
 * - Component structure setup with proper imports
 * - Required hooks from useUIStudio
 * - UI components from shadcn/ui
 * - Bento types and utilities
 * - Component props interface
 * - Component state management interface
 * - TypeScript generics for data handling
 * 
 * @module UIStudioInterface
 */

import React, { useState, useCallback, useMemo, useRef, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';

// UIStudio hooks and services
import {
  useUIStudioPublishedPages,
  useUIStudioPagesByOwner,
  useCreateUIStudioPage,
  useDeleteUIStudioPage,
  useUIStudioTemplatesByOwner,
  useUIStudioErrorHandler
} from '../../hooks/useUIStudio';

// Search functionality
import { useSearchState } from '../../hooks/useSearchState';
import { SearchInputWithSuggestions } from '../ui/search-suggestions';
import { SearchHighlight, SearchTitle, SearchDescription } from '../ui/search-highlight';

// Shadcn/ui components
import { Button } from '../ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '../ui/card';
import { Input } from '../ui/input';
import { Badge } from '../ui/badge';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '../ui/dialog-temp'; // TEMPORARY: Using non-Radix dialog due to React 19 issue
import { Label } from '../ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select-temp'; // TEMPORARY: Using non-Radix select due to React 19 issue
import { Textarea } from '../ui/textarea';
import { Tabs, TabsList, TabsTrigger } from '../ui/tabs-temp'; // TEMPORARY: Using non-Radix tabs due to React 19 issue
import { ScrollArea } from '../ui/scroll-area';
import { LoadingSpinner } from '../ui/loading-spinner';
// import { Sheet, SheetContent } from '../ui/sheet'; // TEMPORARILY DISABLED DUE TO REACT 19 ISSUE
import { Separator } from '../ui/separator';
import { Pagination } from '../ui/pagination';

// Icons for navigation (using Lucide React)
import { 
  Home, 
  FileText, 
  Layers, 
  Settings, 
  Users, 
  BarChart3, 
  FolderOpen,
  Plus,
  ChevronRight,
  Clock,
  Star,
  ChevronLeft,
  ChevronsLeft,
  ChevronsRight
} from 'lucide-react';

// Bento types and utilities
import type {
  DeepPartial
} from '../../types/bento';

// UIStudio types
import type {
  UIStudioPage,
  UIStudioTemplate,
  UIStudioEntityId,
  UIStudioPageType,
  CreatePageRequest,
  GetPublishedPagesQuery
} from '../../types/uistudio';

// UIStudio Header and Footer components
import { UIStudioHeader } from './UIStudioHeader';
import { UIStudioFooter } from './UIStudioFooter';

// Template Gallery Grid component
import { TemplateGalleryGrid } from '../templates/TemplateGalleryGrid';

// Quick Actions Panel
import { QuickActionsPanel } from '../panels/QuickActionsPanel';

// Keyboard Navigation
import { useKeyboardNavigation } from '../../hooks/useKeyboardNavigation';
import { useKeyboardNavigationContext } from '../keyboard/KeyboardNavigationProvider';
import { QuickHelpButton } from '../keyboard/KeyboardShortcutDisplay';

// Auth context
import { useAuth } from '../../contexts/AuthContext';
import type { KeyboardShortcut, NavigationItem } from '../../hooks/useKeyboardNavigation';

// ============================================================================
// Component Props Interface
// ============================================================================

/**
 * Props for the UIStudioInterface component
 */
export interface UIStudioInterfaceProps {
  /** Current user entity ID for data filtering */
  userEntityId?: UIStudioEntityId;
  
  /** Optional initial view mode */
  initialView?: 'grid' | 'list' | 'card';
  
  /** Optional initial filter settings */
  initialFilters?: DeepPartial<UIStudioFilters>;
  
  /** Optional callback when a page is selected */
  onPageSelect?: (page: UIStudioPage) => void;
  
  /** Optional callback when navigation occurs */
  onNavigate?: (path: string) => void;
  
  /** Optional custom CSS classes */
  className?: string;
  
  /** Optional loading override */
  isLoading?: boolean;
  
  /** Optional error override */
  error?: string | null;
}

// ============================================================================
// State Management Interfaces
// ============================================================================

/**
 * View state configuration for the interface
 */
export interface UIStudioViewState {
  /** Current view mode */
  mode: 'grid' | 'list' | 'card';
  
  /** Sort configuration */
  sort: {
    field: keyof UIStudioPage;
    direction: 'asc' | 'desc';
  };
}

/**
 * Pagination state management interface
 */
export interface UIStudioPaginationState {
  /** Current page number (1-based) */
  currentPage: number;
  
  /** Items per page */
  pageSize: number;
  
  /** Total number of items */
  totalItems: number;
  
  /** Total number of pages */
  totalPages: number;
  
  /** Whether there is a next page */
  hasNextPage: boolean;
  
  /** Whether there is a previous page */
  hasPreviousPage: boolean;
  
  /** Offset for database queries */
  offset: number;
  
  /** Available page size options */
  pageSizeOptions: number[];
}

/**
 * Filter state for page discovery
 */
export interface UIStudioFilters {
  /** Search query string */
  search: string;
  
  /** Filter by page type */
  pageType: UIStudioPageType | 'all';
  
  /** Filter by page status */
  status: 'draft' | 'published' | 'archived' | 'all';
  
  /** Filter by owner */
  owner: UIStudioEntityId | 'all' | 'me';
  
  /** Date range filter */
  dateRange: {
    from?: string;
    to?: string;
  };
  
  /** Tag filter */
  tags: string[];
}

/**
 * Selection state for bulk operations
 */
export interface UIStudioSelectionState {
  /** Selected page IDs */
  selectedPages: Set<UIStudioEntityId>;
  
  /** Selection mode enabled */
  isSelectionMode: boolean;
  
  /** Bulk operation in progress */
  bulkOperation: string | null;
}

/**
 * Modal state management
 */
export interface UIStudioModalState {
  /** Create page modal open */
  createPageOpen: boolean;
  
  /** Template gallery modal open */
  templateGalleryOpen: boolean;
  
  /** Import/export modal open */
  importExportOpen: boolean;
  
  /** Mobile sidebar open */
  mobileSidebarOpen: boolean;
  
  /** Confirmation dialog state */
  confirmDialog: {
    open: boolean;
    title: string;
    description: string;
    action: (() => void) | null;
  };
}

/**
 * Cache management interface
 */
export interface UIStudioCache<T> {
  /** Cache data */
  data: T | null;
  
  /** Cache timestamp */
  timestamp: number;
  
  /** Cache TTL in milliseconds */
  ttl: number;
  
  /** Cache status */
  status: 'fresh' | 'stale' | 'expired';
}

// ============================================================================
// TypeScript Generics for Data Handling
// ============================================================================

/**
 * Generic API response wrapper with type safety
 */
export type UIStudioApiResult<T> = {
  data: T | null;
  loading: boolean;
  error: string | null;
  refetch: () => void;
};

/**
 * Generic mutation result with optimistic updates
 */
export type UIStudioMutationResult<TData, TVariables> = {
  mutate: (variables: TVariables) => Promise<TData>;
  loading: boolean;
  error: string | null;
  data: TData | null;
  reset: () => void;
};

/**
 * Generic pagination result
 */
export type UIStudioPaginatedResult<T> = {
  items: T[];
  totalCount: number;
  pageInfo: {
    hasNextPage: boolean;
    hasPreviousPage: boolean;
    currentPage: number;
    totalPages: number;
  };
};

/**
 * Generic search result with highlighting
 */
export type UIStudioSearchResult<T> = {
  item: T;
  score: number;
  highlights: Record<string, string[]>;
  matchedFields: string[];
};

// ============================================================================
// URL State Management Utilities
// ============================================================================

/**
 * URL parameter keys for pagination and filters
 */
const URL_PARAMS = {
  PAGE: 'page',
  PAGE_SIZE: 'pageSize',
  SEARCH: 'search',
  PAGE_TYPE: 'pageType',
  STATUS: 'status',
  OWNER: 'owner',
  VIEW_MODE: 'view'
} as const;

/**
 * Extract pagination state from URL search parameters
 */
const getPaginationFromUrl = (searchParams: URLSearchParams): Partial<UIStudioPaginationState> => {
  const page = parseInt(searchParams.get(URL_PARAMS.PAGE) || '1', 10);
  const pageSize = parseInt(searchParams.get(URL_PARAMS.PAGE_SIZE) || '12', 10);
  
  return {
    currentPage: Math.max(1, page),
    pageSize: [12, 24, 48, 96].includes(pageSize) ? pageSize : 12
  };
};

/**
 * Extract filters from URL search parameters
 */
const getFiltersFromUrl = (searchParams: URLSearchParams): Partial<UIStudioFilters> => {
  return {
    search: searchParams.get(URL_PARAMS.SEARCH) || '',
    pageType: (searchParams.get(URL_PARAMS.PAGE_TYPE) as UIStudioPageType) || 'all',
    status: (searchParams.get(URL_PARAMS.STATUS) as 'draft' | 'published' | 'archived' | 'all') || 'all',
    owner: (searchParams.get(URL_PARAMS.OWNER) as UIStudioEntityId | 'all' | 'me') || 'me'
  };
};

/**
 * Extract view mode from URL search parameters
 */
const getViewModeFromUrl = (searchParams: URLSearchParams): 'grid' | 'list' | 'card' => {
  const viewMode = searchParams.get(URL_PARAMS.VIEW_MODE);
  return (['grid', 'list', 'card'].includes(viewMode as string) ? viewMode : 'grid') as 'grid' | 'list' | 'card';
};

/**
 * Update URL with current state without triggering navigation
 */
const updateUrlParams = (
  navigate: ReturnType<typeof useNavigate>,
  location: ReturnType<typeof useLocation>,
  updates: Partial<{
    page: number;
    pageSize: number;
    search: string;
    pageType: string;
    status: string;
    owner: string;
    view: string;
  }>
) => {
  const searchParams = new URLSearchParams(location.search);
  
  // Update parameters
  Object.entries(updates).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      const paramKey = URL_PARAMS[key.toUpperCase() as keyof typeof URL_PARAMS];
      if (paramKey) {
        if (key === 'page' && value === 1) {
          // Don't show page=1 in URL (default)
          searchParams.delete(paramKey);
        } else if (key === 'pageSize' && value === 12) {
          // Don't show pageSize=12 in URL (default)
          searchParams.delete(paramKey);
        } else {
          searchParams.set(paramKey, String(value));
        }
      }
    } else {
      const paramKey = URL_PARAMS[key.toUpperCase() as keyof typeof URL_PARAMS];
      if (paramKey) {
        searchParams.delete(paramKey);
      }
    }
  });
  
  // Navigate with new parameters without adding to history
  navigate({ search: searchParams.toString() }, { replace: true });
};

// ============================================================================
// Filter Persistence Utilities
// ============================================================================

const FILTER_STORAGE_KEY = 'jarvis-ui-studio-filters';

interface StoredFilterState {
  filters: Partial<UIStudioFilters>;
  lastUpdated: string;
}

const getStoredFilters = (): StoredFilterState & { hasData: boolean } => {
  try {
    const stored = localStorage.getItem(FILTER_STORAGE_KEY);
    if (stored) {
      const parsed = JSON.parse(stored);
      return {
        filters: parsed.filters || {},
        lastUpdated: parsed.lastUpdated || new Date().toISOString(),
        hasData: true
      };
    }
  } catch (error) {
    console.warn('Failed to parse stored filter state:', error);
  }

  return {
    filters: {},
    lastUpdated: new Date().toISOString(),
    hasData: false
  };
};

const setStoredFilters = (filters: Partial<UIStudioFilters>): void => {
  try {
    localStorage.setItem(FILTER_STORAGE_KEY, JSON.stringify({
      filters,
      lastUpdated: new Date().toISOString()
    }));
  } catch (error) {
    console.warn('Failed to store filter state:', error);
  }
};

// ============================================================================
// Component State Management Interface
// ============================================================================

/**
 * Complete state interface for UIStudioInterface component
 */
export interface UIStudioInterfaceState {
  /** View configuration */
  view: UIStudioViewState;
  
  /** Filter settings */
  filters: UIStudioFilters;
  
  /** Pagination state */
  pagination: UIStudioPaginationState;
  
  /** Selection state */
  selection: UIStudioSelectionState;
  
  /** Modal states */
  modals: UIStudioModalState;
  
  /** Loading states */
  loading: {
    pages: boolean;
    templates: boolean;
    creating: boolean;
    deleting: boolean;
  };
  
  /** Error states */
  errors: {
    pages: string | null;
    templates: string | null;
    creation: string | null;
    deletion: string | null;
  };
  
  /** Cache management */
  cache: {
    pages: UIStudioCache<UIStudioPage[]>;
    templates: UIStudioCache<UIStudioTemplate[]>;
    recentPages: UIStudioCache<UIStudioPage[]>;
  };
}

// ============================================================================
// Component Implementation
// ============================================================================

/**
 * UIStudioInterface - Main discovery dashboard component
 */
export const UIStudioInterface: React.FC<UIStudioInterfaceProps> = ({
  userEntityId: propUserEntityId,
  initialView = 'grid',
  initialFilters = {},
  onPageSelect,
  onNavigate,
  className = '',
  isLoading: loadingOverride,
  error: errorOverride
}) => {
  // Get current user from auth context
  const { user } = useAuth();
  const userEntityId = propUserEntityId || user?.id || 'anonymous';
  
  const navigate = useNavigate();
  const location = useLocation();
  const { handleError } = useUIStudioErrorHandler();
  const containerRef = useRef<HTMLDivElement | null>(null);
  const { actions } = useKeyboardNavigationContext();
  const [focusedPageIndex, setFocusedPageIndex] = useState<number | null>(null);

  // ============================================================================
  // Search State Management with Debouncing
  // ============================================================================

  // Initialize search state with comprehensive functionality
  const searchState = useSearchState<UIStudioPage>(initialFilters?.search || '', {
    delay: 300,
    enableHistory: true,
    enableSuggestions: true,
    enableHighlighting: true,
    maxHistorySize: 20,
    maxSuggestions: 8,
    storageKey: 'jarvis-ui-studio-search-history',
    popularTerms: ['dashboard', 'analytics', 'form', 'table', 'chart', 'report'],
    filters: {
      fields: ['pageName', 'description', 'pageSlug'],
      caseSensitive: false,
      partialMatch: true,
      fieldBoosts: {
        pageName: 2.0,
        pageSlug: 1.5,
        description: 1.0
      }
    },
    onSearch: (term) => {
      console.log('Search triggered:', term);
    }
  });

  // ============================================================================
  // Component State Management
  // ============================================================================

  const [state, setState] = useState<UIStudioInterfaceState>(() => {
    // Load stored filter preferences and URL state
    const storedFilters = getStoredFilters();
    const urlSearchParams = new URLSearchParams(location.search);
    const urlPagination = getPaginationFromUrl(urlSearchParams);
    const urlFilters = getFiltersFromUrl(urlSearchParams);
    const urlViewMode = getViewModeFromUrl(urlSearchParams);
    
    // Calculate pagination values
    const pageSize = urlPagination.pageSize || 12;
    const currentPage = urlPagination.currentPage || 1;
    const offset = (currentPage - 1) * pageSize;
    
    return {
      view: {
        mode: urlViewMode || initialView,
        sort: {
          field: 'lastUpdated',
          direction: 'desc'
        }
      },
      filters: {
        search: urlFilters.search || searchState.searchTerm,
        pageType: urlFilters.pageType || 'all',
        status: urlFilters.status || 'all',
        owner: urlFilters.owner || 'me',
        dateRange: {},
        tags: [] as string[],
        ...(storedFilters.hasData ? storedFilters.filters : {}),
        ...(initialFilters as Partial<UIStudioFilters>),
        ...urlFilters // URL takes precedence
      },
      pagination: {
        currentPage,
        pageSize,
        totalItems: 0, // Will be calculated when data loads
        totalPages: 0, // Will be calculated when data loads
        hasNextPage: false, // Will be calculated when data loads
        hasPreviousPage: currentPage > 1,
        offset,
        pageSizeOptions: [12, 24, 48, 96]
      },
      selection: {
        selectedPages: new Set(),
        isSelectionMode: false,
        bulkOperation: null
      },
      modals: {
        createPageOpen: false,
        templateGalleryOpen: false,
        importExportOpen: false,
        mobileSidebarOpen: false,
        confirmDialog: {
          open: false,
          title: '',
          description: '',
          action: null
        }
      },
      loading: {
        pages: false,
        templates: false,
        creating: false,
        deleting: false
      },
      errors: {
        pages: null,
        templates: null,
        creation: null,
        deletion: null
      },
      cache: {
        pages: { data: null, timestamp: 0, ttl: 300000, status: 'expired' },
        templates: { data: null, timestamp: 0, ttl: 600000, status: 'expired' },
        recentPages: { data: null, timestamp: 0, ttl: 60000, status: 'expired' }
      }
    };
  });

  // ============================================================================
  // Data Fetching with TypeScript Generics
  // ============================================================================

  // Published pages query with pagination
  const publishedPagesQuery: GetPublishedPagesQuery = useMemo(() => ({
    limit: state.pagination.pageSize,
    offset: state.pagination.offset,
    pageType: state.filters.pageType !== 'all' ? state.filters.pageType : undefined,
    search: state.filters.search || undefined
  }), [state.pagination.pageSize, state.pagination.offset, state.filters.pageType, state.filters.search]);

  const publishedPagesResult = useUIStudioPublishedPages(publishedPagesQuery);
  const publishedPagesData: UIStudioApiResult<UIStudioPage[]> = useMemo(() => ({
    data: (publishedPagesResult.data as UIStudioPage[]) || null,
    loading: publishedPagesResult.isLoading,
    error: publishedPagesResult.error?.message || null,
    refetch: publishedPagesResult.refetch
  }), [publishedPagesResult.data, publishedPagesResult.isLoading, publishedPagesResult.error?.message, publishedPagesResult.refetch]);

  // User pages query
  const userPagesResult = useUIStudioPagesByOwner(userEntityId);
  const userPagesData: UIStudioApiResult<UIStudioPage[]> = useMemo(() => ({
    data: (userPagesResult.data as UIStudioPage[]) || null,
    loading: userPagesResult.isLoading,
    error: userPagesResult.error?.message || null,
    refetch: userPagesResult.refetch
  }), [userPagesResult.data, userPagesResult.isLoading, userPagesResult.error?.message, userPagesResult.refetch]);

  // User templates query
  const userTemplatesResult = useUIStudioTemplatesByOwner(userEntityId);
  const userTemplatesData: UIStudioApiResult<UIStudioTemplate[]> = useMemo(() => ({
    data: (userTemplatesResult.data as UIStudioTemplate[]) || null,
    loading: userTemplatesResult.isLoading,
    error: userTemplatesResult.error?.message || null,
    refetch: userTemplatesResult.refetch
  }), [userTemplatesResult.data, userTemplatesResult.isLoading, userTemplatesResult.error?.message, userTemplatesResult.refetch]);

  // ============================================================================
  // Filtered Pages with Enhanced Search and Filtering Logic
  // ============================================================================

  // Get all pages for searching and filtering
  const allPages = useMemo(() => [
    ...(publishedPagesData.data || []),
    ...(userPagesData.data || [])
  ], [publishedPagesData.data, userPagesData.data]);

  // Apply search with highlighting and scoring
  const searchResults = useMemo(() => {
    return searchState.searchItems(allPages, searchState.debouncedSearchTerm);
  }, [allPages, searchState, searchState.debouncedSearchTerm]);

  // Computed filtered pages with all filter logic and pagination calculation
  const filteredPagesData = useMemo(() => {
    // Start with search results if there's a search term, otherwise all pages
    const pagesWithSearch = searchState.debouncedSearchTerm.trim() 
      ? searchResults.map(result => result.item)
      : allPages;

    const allFilteredPages = pagesWithSearch.filter(page => {
      // Apply page type filter
      if (state.filters.pageType !== 'all' && page.pageType !== state.filters.pageType) {
        return false;
      }
      
      // Apply status filter (enhanced to handle archived via metadata)
      if (state.filters.status !== 'all') {
        let pageStatus: string;
        // Check if page is archived via metadata (future enhancement)
        const isArchived = page.metadata?.archived === true;
        
        if (isArchived) {
          pageStatus = 'archived';
        } else if (page.isPublished) {
          pageStatus = 'published';
        } else {
          pageStatus = 'draft';
        }
        
        if (pageStatus !== state.filters.status) {
          return false;
        }
      }
      
      // Apply owner filter
      if (state.filters.owner !== 'all') {
        if (state.filters.owner === 'me') {
          // Filter to show only pages owned by current user
          if (page.createdByEntityId !== userEntityId) {
            return false;
          }
        } else {
          // Filter to show pages owned by specific user
          if (page.createdByEntityId !== state.filters.owner) {
            return false;
          }
        }
      }
      
      // Apply date range filter
      if (state.filters.dateRange.from || state.filters.dateRange.to) {
        const pageDate = new Date(page.lastUpdated || page.createdAt);
        
        if (state.filters.dateRange.from) {
          const fromDate = new Date(state.filters.dateRange.from);
          if (pageDate < fromDate) {
            return false;
          }
        }
        
        if (state.filters.dateRange.to) {
          const toDate = new Date(state.filters.dateRange.to);
          if (pageDate > toDate) {
            return false;
          }
        }
      }
      
      // Apply tag filter
      if (state.filters.tags.length > 0) {
        // Parse tags from the tags field (JSON string) or use metadata
        let pageTags: string[] = [];
        
        try {
          if (page.tags) {
            // Tags are stored as JSON string in the API
            pageTags = typeof page.tags === 'string' 
              ? JSON.parse(page.tags) 
              : page.tags;
          }
          // Also check metadata for tags
          if (page.metadata?.tags) {
            const metaTags = Array.isArray(page.metadata.tags) 
              ? page.metadata.tags 
              : [page.metadata.tags];
            pageTags = [...pageTags, ...metaTags.map(String)];
          }
        } catch {
          // If tags parsing fails, fallback to empty array
          pageTags = [];
        }
        
        const hasMatchingTag = state.filters.tags.some(filterTag => 
          pageTags.includes(filterTag)
        );
        if (!hasMatchingTag) {
          return false;
        }
      }
      
      return true;
    });

    // Calculate pagination
    const totalItems = allFilteredPages.length;
    const totalPages = Math.ceil(totalItems / state.pagination.pageSize);
    const startIndex = state.pagination.offset;
    const endIndex = startIndex + state.pagination.pageSize;
    const currentPageItems = allFilteredPages.slice(startIndex, endIndex);

    return {
      allItems: allFilteredPages,
      currentPageItems,
      totalItems,
      totalPages,
      hasNextPage: state.pagination.currentPage < totalPages,
      hasPreviousPage: state.pagination.currentPage > 1
    };
  }, [searchResults, allPages, searchState.debouncedSearchTerm, state.filters, state.pagination.pageSize, state.pagination.offset, userEntityId]); // Remove currentPage dependency to avoid circular updates

  // TEMPORARILY DISABLED: Update pagination state when data changes
  // This was causing infinite loops with React 19
  // useEffect(() => {
  //   // Only update if values have actually changed to prevent infinite loops
  //   setState(prev => {
  //     if (
  //       prev.pagination.totalItems !== filteredPagesData.totalItems ||
  //       prev.pagination.totalPages !== filteredPagesData.totalPages
  //     ) {
  //       return {
  //         ...prev,
  //         pagination: {
  //           ...prev.pagination,
  //           totalItems: filteredPagesData.totalItems,
  //           totalPages: filteredPagesData.totalPages,
  //           hasNextPage: prev.pagination.currentPage < filteredPagesData.totalPages,
  //           hasPreviousPage: prev.pagination.currentPage > 1
  //         }
  //       };
  //     }
  //     return prev;
  //   });
  // }, [filteredPagesData.totalItems, filteredPagesData.totalPages]);

  // Extract current page items for display
  const filteredPages = filteredPagesData.currentPageItems;

  // ============================================================================
  // Mutations with TypeScript Generics
  // ============================================================================

  const createPageMutation = useCreateUIStudioPage();
  
  const createPageWrapper: UIStudioMutationResult<UIStudioPage, CreatePageRequest> = useMemo(() => ({
    mutate: async (variables: CreatePageRequest) => {
      setState(prev => ({
        ...prev,
        loading: { ...prev.loading, creating: true },
        errors: { ...prev.errors, creation: null }
      }));
      
      try {
        const result = await createPageMutation.mutateAsync(variables);
        
        setState(prev => ({
          ...prev,
          loading: { ...prev.loading, creating: false },
          modals: { ...prev.modals, createPageOpen: false }
        }));
        
        // Invalidate cache and refresh data
        publishedPagesData.refetch();
        userPagesData.refetch();
        
        return Array.isArray(result) ? result[0] as UIStudioPage : result as UIStudioPage;
      } catch (error) {
        const errorResult = handleError(error, 'create_page');
        setState(prev => ({
          ...prev,
          loading: { ...prev.loading, creating: false },
          errors: { ...prev.errors, creation: errorResult.userMessage }
        }));
        throw error;
      }
    },
    loading: createPageMutation.isPending,
    error: createPageMutation.error?.message || null,
    data: (Array.isArray(createPageMutation.data) ? createPageMutation.data[0] : createPageMutation.data) as UIStudioPage || null,
    reset: createPageMutation.reset
  }), [createPageMutation, handleError, publishedPagesData, userPagesData, setState]);

  const deletePageMutation = useDeleteUIStudioPage();
  
  const deletePageWrapper: UIStudioMutationResult<UIStudioPage, { pageEntityId: UIStudioEntityId; deletedByEntityId: UIStudioEntityId }> = useMemo(() => ({
    mutate: async (variables: { pageEntityId: UIStudioEntityId; deletedByEntityId: UIStudioEntityId }) => {
      setState(prev => ({
        ...prev,
        loading: { ...prev.loading, deleting: true },
        errors: { ...prev.errors, deletion: null }
      }));
      
      try {
        const result = await deletePageMutation.mutateAsync(variables);
        
        setState(prev => ({
          ...prev,
          loading: { ...prev.loading, deleting: false },
          selection: {
            ...prev.selection,
            selectedPages: new Set([...prev.selection.selectedPages].filter(id => id !== variables.pageEntityId))
          }
        }));
        
        // Invalidate cache and refresh data
        publishedPagesData.refetch();
        userPagesData.refetch();
        
        return Array.isArray(result) ? result[0] as UIStudioPage : result as UIStudioPage;
      } catch (error) {
        const errorResult = handleError(error, 'delete_page');
        setState(prev => ({
          ...prev,
          loading: { ...prev.loading, deleting: false },
          errors: { ...prev.errors, deletion: errorResult.userMessage }
        }));
        throw error;
      }
    },
    loading: deletePageMutation.isPending,
    error: deletePageMutation.error?.message || null,
    data: (Array.isArray(deletePageMutation.data) ? deletePageMutation.data[0] : deletePageMutation.data) as UIStudioPage || null,
    reset: deletePageMutation.reset
  }), [deletePageMutation, handleError, publishedPagesData, userPagesData, setState]);

  // ============================================================================
  // Event Handlers
  // ============================================================================

  const handleViewChange = useCallback((mode: 'grid' | 'list' | 'card') => {
    setState(prev => ({
      ...prev,
      view: { ...prev.view, mode }
    }));
    
    // Update URL - temporarily disabled to debug infinite loop
    // updateUrlParams(navigate, location, { view: mode });
  }, [navigate, location]);

  const handleFilterChange = useCallback(<K extends keyof UIStudioFilters>(
    key: K,
    value: UIStudioFilters[K]
  ) => {
    // Handle search separately through search state
    if (key === 'search') {
      searchState.setSearchTerm(value as string);
      // Update URL for search
      // updateUrlParams(navigate, location, { search: value as string, page: 1 });
      return;
    }
    
    setState(prev => {
      const newFilters = { ...prev.filters, [key]: value };
      const newPagination = { ...prev.pagination, currentPage: 1, offset: 0 }; // Reset to first page
      
      // Persist filter preferences to localStorage
      setStoredFilters(newFilters);
      
      // Update URL
      // updateUrlParams(navigate, location, {
      //   [key]: value as string,
      //   page: 1
      // });
      
      return {
        ...prev,
        filters: newFilters,
        pagination: newPagination
      };
    });
  }, [searchState, navigate, location]);

  const handlePageChange = useCallback((newPage: number) => {
    setState(prev => {
      const clampedPage = Math.max(1, Math.min(newPage, prev.pagination.totalPages));
      const newOffset = (clampedPage - 1) * prev.pagination.pageSize;
      
      const newPagination = {
        ...prev.pagination,
        currentPage: clampedPage,
        offset: newOffset,
        hasPreviousPage: clampedPage > 1,
        hasNextPage: clampedPage < prev.pagination.totalPages
      };
      
      // Update URL
      // updateUrlParams(navigate, location, { page: clampedPage });
      
      return {
        ...prev,
        pagination: newPagination
      };
    });
  }, [navigate, location]);

  const handlePageSizeChange = useCallback((newPageSize: number) => {
    setState(prev => {
      // Calculate new page to maintain roughly the same position
      const currentItem = (prev.pagination.currentPage - 1) * prev.pagination.pageSize;
      const newPage = Math.floor(currentItem / newPageSize) + 1;
      const newOffset = (newPage - 1) * newPageSize;
      
      const newPagination = {
        ...prev.pagination,
        pageSize: newPageSize,
        currentPage: newPage,
        offset: newOffset,
        totalPages: Math.ceil(prev.pagination.totalItems / newPageSize),
        hasPreviousPage: newPage > 1
      };
      
      // Recalculate hasNextPage with new total pages
      newPagination.hasNextPage = newPage < newPagination.totalPages;
      
      // Update URL
      // updateUrlParams(navigate, location, { 
      //   pageSize: newPageSize,
      //   page: newPage
      // });
      
      return {
        ...prev,
        pagination: newPagination
      };
    });
  }, [navigate, location]);

  const handlePageSelect = useCallback((page: UIStudioPage) => {
    onPageSelect?.(page);
    onNavigate?.(`/studio/page/${page.id}`);
    navigate(`/studio/page/${page.id}`);
  }, [onPageSelect, onNavigate, navigate]);

  const handleCreatePage = useCallback(async (request: CreatePageRequest) => {
    try {
      const newPage = await createPageWrapper.mutate(request);
      handlePageSelect(newPage);
    } catch (error) {
      // Error is already handled in mutation
      console.error('Failed to create page:', error);
    }
  }, [createPageWrapper, handlePageSelect]);

  const handleDeletePage = useCallback(async (pageId: UIStudioEntityId) => {
    try {
      await deletePageWrapper.mutate({
        pageEntityId: pageId,
        deletedByEntityId: userEntityId
      });
    } catch (error) {
      // Error is already handled in mutation
      console.error('Failed to delete page:', error);
    }
  }, [deletePageWrapper, userEntityId]);

  // ============================================================================
  // Keyboard Event Handlers
  // ============================================================================

  // Keyboard event handler that can access filteredPages
  const handleCustomKeyDown = useCallback((event: KeyboardEvent): boolean | void => {
    const key = event.key.toLowerCase();
    
    // Handle global shortcuts first
    if (event.ctrlKey || event.metaKey) {
      switch (key) {
        case 'n':
          event.preventDefault();
          setState(prev => ({ ...prev, modals: { ...prev.modals, createPageOpen: true } }));
          return true;
        case 'f': {
          event.preventDefault();
          const searchInput = document.querySelector('input[type="text"][placeholder*="Search"]') as HTMLInputElement;
          searchInput?.focus();
          return true;
        }
        case 'k':
          event.preventDefault();
          actions.openCommandPalette();
          return true;
      }
    }

    // Handle other shortcuts
    switch (key) {
      case '/': {
        if (!event.ctrlKey && !event.metaKey) {
          event.preventDefault();
          const searchInput = document.querySelector('input[type="text"][placeholder*="Search"]') as HTMLInputElement;
          searchInput?.focus();
          return true;
        }
        break;
      }
      case 'escape': {
        // Close any open modals
        setState(prev => ({
          ...prev,
          modals: {
            ...prev.modals,
            createPageOpen: false,
            templateGalleryOpen: false,
            importExportOpen: false,
            mobileSidebarOpen: false,
            confirmDialog: { ...prev.modals.confirmDialog, open: false }
          }
        }));
        setFocusedPageIndex(null);
        return true;
      }
      case 'enter':
      case ' ': {
        if (focusedPageIndex !== null && filteredPages[focusedPageIndex]) {
          event.preventDefault();
          handlePageSelect(filteredPages[focusedPageIndex]);
          return true;
        }
        break;
      }
    }

    return false;
  }, [filteredPages, focusedPageIndex, handlePageSelect, actions]);

  // Keyboard navigation setup
  const {
    registerShortcut,
    registerItem,
    isActive
  } = useKeyboardNavigation(containerRef, {
    enableArrowKeys: true,
    enableHomeEnd: true,
    enableEscape: true,
    trapFocus: false,
    onKeyDown: handleCustomKeyDown
  });

  // ============================================================================
  // Page Navigation Handlers  
  // ============================================================================

  const handlePageNavigation = useCallback((direction: 'next' | 'previous' | 'first' | 'last') => {
    if (filteredPages.length === 0) return;

    let newIndex: number;
    switch (direction) {
      case 'next':
        newIndex = focusedPageIndex === null ? 0 : (focusedPageIndex + 1) % filteredPages.length;
        break;
      case 'previous':
        newIndex = focusedPageIndex === null ? filteredPages.length - 1 : 
                  focusedPageIndex <= 0 ? filteredPages.length - 1 : focusedPageIndex - 1;
        break;
      case 'first':
        newIndex = 0;
        break;
      case 'last':
        newIndex = filteredPages.length - 1;
        break;
    }
    
    setFocusedPageIndex(newIndex);
    
    // Focus the page card element
    const pageCard = containerRef.current?.querySelector(`[data-page-index="${newIndex}"]`) as HTMLElement;
    pageCard?.focus();
  }, [filteredPages, focusedPageIndex]);

  // ============================================================================
  // Computed Values
  // ============================================================================

  const isLoading = useMemo(() => {
    return loadingOverride ?? (
      publishedPagesData.loading ||
      userPagesData.loading ||
      userTemplatesData.loading ||
      state.loading.creating ||
      state.loading.deleting
    );
  }, [loadingOverride, publishedPagesData.loading, userPagesData.loading, userTemplatesData.loading, state.loading]);

  const hasError = useMemo(() => {
    return errorOverride ?? (
      publishedPagesData.error ||
      userPagesData.error ||
      userTemplatesData.error ||
      state.errors.creation ||
      state.errors.deletion
    );
  }, [errorOverride, publishedPagesData.error, userPagesData.error, userTemplatesData.error, state.errors]);

  // filteredPages is already defined above in the keyboard navigation section

  // ============================================================================
  // Keyboard Shortcuts Registration
  // ============================================================================

  const handleRefresh = useCallback(() => {
    publishedPagesData.refetch();
    userPagesData.refetch();
    userTemplatesData.refetch();
  }, [publishedPagesData, userPagesData, userTemplatesData]);

  useEffect(() => {
    const shortcuts: KeyboardShortcut[] = [
      {
        key: 'n',
        ctrlKey: true,
        action: () => setState(prev => ({ ...prev, modals: { ...prev.modals, createPageOpen: true } })),
        description: 'Create new page'
      },
      {
        key: 'n',
        metaKey: true,
        action: () => setState(prev => ({ ...prev, modals: { ...prev.modals, createPageOpen: true } })),
        description: 'Create new page'
      },
      {
        key: 'f',
        ctrlKey: true,
        action: () => {
          const searchInput = document.querySelector('input[type="text"][placeholder*="Search"]') as HTMLInputElement;
          searchInput?.focus();
        },
        description: 'Focus search'
      },
      {
        key: 'f',
        metaKey: true,
        action: () => {
          const searchInput = document.querySelector('input[type="text"][placeholder*="Search"]') as HTMLInputElement;
          searchInput?.focus();
        },
        description: 'Focus search'
      },
      {
        key: '/',
        action: () => {
          const searchInput = document.querySelector('input[type="text"][placeholder*="Search"]') as HTMLInputElement;
          searchInput?.focus();
        },
        description: 'Focus search'
      },
      {
        key: 'j',
        action: () => handlePageNavigation('next'),
        description: 'Navigate to next page'
      },
      {
        key: 'k',
        action: () => handlePageNavigation('previous'),
        description: 'Navigate to previous page'
      },
      {
        key: 'g',
        action: () => handlePageNavigation('first'),
        description: 'Go to first page'
      },
      {
        key: 'G',
        shiftKey: true,
        action: () => handlePageNavigation('last'),
        description: 'Go to last page'
      },
      {
        key: '1',
        action: () => handleViewChange('grid'),
        description: 'Switch to grid view'
      },
      {
        key: '2',
        action: () => handleViewChange('list'),
        description: 'Switch to list view'
      },
      {
        key: '3',
        action: () => handleViewChange('card'),
        description: 'Switch to card view'
      },
      {
        key: 'r',
        action: handleRefresh,
        description: 'Refresh data'
      },
      // Pagination shortcuts
      {
        key: 'p',
        action: () => handlePageChange(Math.max(1, state.pagination.currentPage - 1)),
        description: 'Previous page'
      },
      {
        key: 'n',
        action: () => handlePageChange(Math.min(state.pagination.totalPages, state.pagination.currentPage + 1)),
        description: 'Next page'
      },
      {
        key: '[',
        action: () => handlePageChange(1),
        description: 'First page'
      },
      {
        key: ']',
        action: () => handlePageChange(state.pagination.totalPages),
        description: 'Last page'
      }
    ];

    const unregisterFunctions = shortcuts.map(shortcut => registerShortcut(shortcut));

    return () => {
      unregisterFunctions.forEach(fn => fn());
    };
  }, [registerShortcut, handlePageNavigation, handleViewChange, handlePageChange, handleRefresh, state.pagination.currentPage, state.pagination.totalPages]);

  // ============================================================================
  // URL Synchronization Effect
  // ============================================================================

  // TEMPORARILY DISABLED TO DEBUG INFINITE LOOP
  // useEffect(() => {
  //   // Sync URL parameters when location changes (browser back/forward)
  //   const urlSearchParams = new URLSearchParams(location.search);
  //   const urlPagination = getPaginationFromUrl(urlSearchParams);
  //   const urlFilters = getFiltersFromUrl(urlSearchParams);
  //   const urlViewMode = getViewModeFromUrl(urlSearchParams);

  //   // Only update state if URL parameters are different from current state
  //   const needsUpdate = (
  //     urlPagination.currentPage !== state.pagination.currentPage ||
  //     urlPagination.pageSize !== state.pagination.pageSize ||
  //     urlFilters.search !== state.filters.search ||
  //     urlFilters.pageType !== state.filters.pageType ||
  //     urlFilters.status !== state.filters.status ||
  //     urlFilters.owner !== state.filters.owner ||
  //     urlViewMode !== state.view.mode
  //   );

  //   if (needsUpdate) {
  //     setState(prev => {
  //       const newPageSize = urlPagination.pageSize || prev.pagination.pageSize;
  //       const newCurrentPage = urlPagination.currentPage || prev.pagination.currentPage;
  //       const newOffset = (newCurrentPage - 1) * newPageSize;

  //       return {
  //         ...prev,
  //         view: {
  //           ...prev.view,
  //           mode: urlViewMode
  //         },
  //         filters: {
  //           ...prev.filters,
  //           ...urlFilters
  //         },
  //         pagination: {
  //           ...prev.pagination,
  //           currentPage: newCurrentPage,
  //           pageSize: newPageSize,
  //           offset: newOffset
  //         }
  //       };
  //     });

  //     // Update search state if needed
  //     if (urlFilters.search !== searchState.searchTerm) {
  //       searchState.setSearchTerm(urlFilters.search || '');
  //     }
  //   }
  // }, [location.search]); // Only depend on location.search to avoid infinite loops

  // ============================================================================
  // Search State Synchronization Effect
  // ============================================================================

  // TEMPORARILY DISABLED TO DEBUG INFINITE LOOP
  // useEffect(() => {
  //   // Update filters when search state changes (to keep them in sync)
  //   if (searchState.debouncedSearchTerm !== state.filters.search) {
  //     setState(prev => ({
  //       ...prev,
  //       filters: {
  //         ...prev.filters,
  //         search: searchState.debouncedSearchTerm
  //       },
  //       pagination: {
  //         ...prev.pagination,
  //         currentPage: 1,
  //         offset: 0
  //       }
  //     }));
  //   }
  // }, [searchState.debouncedSearchTerm]); // Remove state.filters.search to avoid circular dependency

  // ============================================================================
  // Navigation Items Registration
  // ============================================================================

  useEffect(() => {
    if (!filteredPages.length) return;

    const unregisterFunctions: (() => void)[] = [];

    filteredPages.forEach((page, index) => {
      const navigationItem: NavigationItem = {
        id: `page-${page.id}`,
        element: containerRef.current?.querySelector(`[data-page-index="${index}"]`) as HTMLElement,
        group: 'pages',
        onActivate: () => {
          setFocusedPageIndex(index);
          handlePageSelect(page);
        },
        onEscape: () => {
          setFocusedPageIndex(null);
        }
      };

      const unregister = registerItem(navigationItem);
      unregisterFunctions.push(unregister);
    });

    return () => {
      unregisterFunctions.forEach(fn => fn());
    };
  }, [filteredPages, registerItem, handlePageSelect]);

  // ============================================================================
  // Render Component
  // ============================================================================

  if (isLoading) {
    return (
      <div className={`flex items-center justify-center min-h-screen ${className}`}>
        <LoadingSpinner size="lg" />
        <span className="ml-2 text-lg">Loading UIStudio Interface...</span>
      </div>
    );
  }

  if (hasError) {
    return (
      <div className={`flex flex-col items-center justify-center min-h-screen ${className}`}>
        <div className="text-red-500 text-lg mb-4">Error loading UIStudio Interface</div>
        <div className="text-gray-600 mb-4">{hasError}</div>
        <Button 
          onClick={() => {
            publishedPagesData.refetch();
            userPagesData.refetch();
            userTemplatesData.refetch();
          }}
        >
          Retry
        </Button>
      </div>
    );
  }

  return (
    <div 
      ref={containerRef}
      className={`min-h-screen bg-background ${className}`} 
      role="application" 
      aria-label="UIStudio Interface"
      tabIndex={-1}
    >
      {/* Skip to main content link for keyboard users */}
      <a 
        href="#main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-50 focus:px-4 focus:py-2 focus:bg-primary focus:text-primary-foreground focus:rounded-md"
      >
        Skip to main content
      </a>
      
      {/* Layout with Sidebar - Mobile First */}
      <div className="flex h-screen overflow-hidden">
        {/* Desktop Sidebar - Hidden on Mobile */}
        <aside 
          className="hidden lg:flex lg:flex-col lg:w-64 lg:border-r lg:bg-card/50"
          role="complementary"
          aria-label="Navigation sidebar"
        >
          <SidebarNavigation 
            userEntityId={userEntityId}
            onCreatePage={() => setState(prev => ({ ...prev, modals: { ...prev.modals, createPageOpen: true } }))}
            onOpenTemplates={() => setState(prev => ({ ...prev, modals: { ...prev.modals, templateGalleryOpen: true } }))}
            recentPages={userPagesData.data?.slice(0, 5) || []}
          />
        </aside>

        {/* Mobile Sidebar - Sheet - TEMPORARILY DISABLED DUE TO REACT 19 ISSUE */}
        {/* <Sheet 
          open={state.modals.mobileSidebarOpen} 
          onOpenChange={(open) => setState(prev => ({ ...prev, modals: { ...prev.modals, mobileSidebarOpen: open } }))}
        >
          <SheetContent 
            side="left" 
            className="w-[280px] p-0"
            aria-label="Mobile navigation menu"
          >
            <SidebarNavigation 
              userEntityId={userEntityId}
              onCreatePage={() => {
                setState(prev => ({ ...prev, modals: { ...prev.modals, createPageOpen: true, mobileSidebarOpen: false } }));
              }}
              onOpenTemplates={() => {
                setState(prev => ({ ...prev, modals: { ...prev.modals, templateGalleryOpen: true, mobileSidebarOpen: false } }));
              }}
              recentPages={userPagesData.data?.slice(0, 5) || []}
              isMobile={true}
            />
          </SheetContent>
        </Sheet>

        {/* Main Content Area */}
        <div className="flex-1 flex flex-col overflow-hidden" role="main">
          {/* Header - UIStudio Header Component */}
          <header role="banner" aria-label="UIStudio header">
            <UIStudioHeader
            userEntityId={userEntityId}
            onOpenMobileSidebar={() => setState(prev => ({ ...prev, modals: { ...prev.modals, mobileSidebarOpen: true } }))}
            onCreatePage={() => setState(prev => ({ ...prev, modals: { ...prev.modals, createPageOpen: true } }))}
            onOpenTemplates={() => setState(prev => ({ ...prev, modals: { ...prev.modals, templateGalleryOpen: true } }))}
            onOpenSearch={() => console.log('Search functionality not yet implemented')}
            showActions={{
              createPage: true,
              templates: true,
              search: true,
              notifications: true,
              settings: true,
              help: true,
            }}
            title="UIStudio"
            subtitle="Dashboard"
            showProjectSelector={false}
            />
          </header>

          {/* Main Content Area - Mobile First Responsive Layout */}
          <main 
            className="flex-1 overflow-auto p-md sm:p-lg lg:p-xl"
            role="main"
            aria-label="Main content area"
            id="main-content"
            tabIndex={-1}
          >
            {/* Content Container with Proper Spacing Hierarchy */}
            <div className="max-w-7xl mx-auto space-y-lg sm:space-y-xl">
              {/* Page Header Section */}
              <section 
                className="mb-lg sm:mb-xl"
                role="region"
                aria-labelledby="page-title"
              >
                <div className="mb-md sm:mb-lg">
                  <div className="flex items-start justify-between gap-4">
                    <div>
                      <h1 
                        id="page-title"
                        className="text-2xl font-bold text-foreground mb-xs sm:text-3xl lg:text-4xl"
                      >
                        Dashboard
                      </h1>
                      <p className="text-sm text-muted-foreground sm:text-base">
                        Manage your pages, templates, and content
                      </p>
                    </div>
                    
                    {/* Keyboard Navigation Help */}
                    <div className="flex items-center gap-2">
                      {isActive && (
                        <div className="bg-blue-500 text-white text-xs px-2 py-1 rounded font-mono">
                          Keyboard Navigation Active
                        </div>
                      )}
                      <QuickHelpButton className="shrink-0" />
                    </div>
                  </div>
                </div>
              </section>

              {/* Quick Actions Panel - Prominently displayed for new users and quick access */}
              <section 
                className="mb-lg sm:mb-xl"
                role="region"
                aria-labelledby="quick-actions-panel"
              >
                <QuickActionsPanel
                  userEntityId={userEntityId}
                  onCreatePage={() => setState(prev => ({ ...prev, modals: { ...prev.modals, createPageOpen: true } }))}
                  onOpenTemplates={() => setState(prev => ({ ...prev, modals: { ...prev.modals, templateGalleryOpen: true } }))}
                  onImport={async (file: File) => {
                    // TODO: Implement import functionality
                    console.log('Import file:', file.name);
                    // Placeholder implementation - should integrate with actual import logic
                    return new Promise((resolve) => {
                      setTimeout(() => {
                        console.log('Import completed for:', file.name);
                        resolve();
                      }, 2000);
                    });
                  }}
                  onExport={async (format: 'json' | 'zip') => {
                    // TODO: Implement export functionality
                    console.log('Export format:', format);
                    // Placeholder implementation - should integrate with actual export logic
                    return new Promise((resolve) => {
                      setTimeout(() => {
                        console.log('Export completed in format:', format);
                        resolve();
                      }, 2000);
                    });
                  }}
                  variant="grid"
                  size="normal"
                  showShortcuts={true}
                  loading={{
                    creating: state.loading.creating,
                    importing: false, // TODO: Add import loading state
                    exporting: false  // TODO: Add export loading state
                  }}
                  errors={{
                    import: null, // TODO: Add import error state
                    export: null  // TODO: Add export error state
                  }}
                  className=""
                />
              </section>

              {/* Filters and Search Section - Mobile First Responsive Grid */}
              <section 
                className="space-y-md sm:space-y-lg"
                role="search"
                aria-labelledby="filters-title"
              >
                <div className="space-y-sm">
                  <h2 
                    id="filters-title"
                    className="text-lg font-semibold text-foreground sm:text-xl"
                  >
                    Filter & Search
                  </h2>
                  <p className="text-xs text-muted-foreground sm:text-sm">Find and organize your content</p>
                </div>
                {/* Mobile: Stack vertically, Desktop: Horizontal layout */}
                <div 
                  className="grid grid-cols-1 gap-sm sm:grid-cols-2 lg:grid-cols-5 lg:gap-md xl:gap-lg"
                  role="group"
                  aria-label="Search and filter controls"
                >
                  <div className="sm:col-span-2 lg:col-span-1">
                    <div className="space-y-xs">
                      <label htmlFor="search-input" className="text-xs font-medium text-foreground sr-only">
                        Search pages
                      </label>
                      <SearchInputWithSuggestions
                        value={searchState.searchTerm}
                        onChange={searchState.setSearchTerm}
                        suggestions={searchState.suggestions}
                        showSuggestions={searchState.showSuggestions}
                        setShowSuggestions={searchState.setShowSuggestions}
                        onSelectSuggestion={searchState.selectSuggestion}
                        onRemoveSuggestion={(suggestion) => {
                          if (suggestion.type === 'history') {
                            searchState.removeFromHistory(suggestion.id.replace('history-', ''));
                          }
                        }}
                        placeholder="Search pages... (Press / to focus)"
                        isLoading={searchState.isSearching}
                        inputClassName="w-full h-10 px-sm"
                      />
                      <div id="search-description" className="sr-only">
                        Type to search through your pages by name, description, or slug. Use arrow keys to navigate suggestions.
                      </div>
                    </div>
                  </div>
                  <div className="space-y-xs">
                    <label htmlFor="page-type-select" className="text-xs font-medium text-foreground sr-only">
                      Filter by page type
                    </label>
                    <Select
                      value={state.filters.pageType}
                      onValueChange={(value) => handleFilterChange('pageType', value as UIStudioPageType | 'all')}
                    >
                      <SelectTrigger 
                        id="page-type-select" 
                        className="w-full h-10"
                        aria-label="Filter by page type"
                      >
                        <SelectValue placeholder="Page Type" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all">All Types</SelectItem>
                        <SelectItem value="static">Static</SelectItem>
                        <SelectItem value="dynamic">Dynamic</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="space-y-xs">
                    <label htmlFor="status-select" className="text-xs font-medium text-foreground sr-only">
                      Filter by status
                    </label>
                    <Select
                      value={state.filters.status}
                      onValueChange={(value) => handleFilterChange('status', value as 'draft' | 'published' | 'archived' | 'all')}
                    >
                      <SelectTrigger 
                        id="status-select" 
                        className="w-full h-10"
                        aria-label="Filter by status"
                      >
                        <SelectValue placeholder="Status" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all">All Status</SelectItem>
                        <SelectItem value="draft">Draft</SelectItem>
                        <SelectItem value="published">Published</SelectItem>
                        <SelectItem value="archived">Archived</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="space-y-xs">
                    <label htmlFor="owner-select" className="text-xs font-medium text-foreground sr-only">
                      Filter by owner
                    </label>
                    <Select
                      value={state.filters.owner}
                      onValueChange={(value) => handleFilterChange('owner', value as UIStudioEntityId | 'all' | 'me')}
                    >
                      <SelectTrigger 
                        id="owner-select" 
                        className="w-full h-10"
                        aria-label="Filter by owner"
                      >
                        <SelectValue placeholder="Owner" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all">All Owners</SelectItem>
                        <SelectItem value="me">My Pages</SelectItem>
                        {/* TODO: Add dynamic user list when user management is available */}
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="sm:col-span-2 lg:col-span-1 space-y-xs">
                    <label className="text-xs font-medium text-foreground sr-only">
                      Change view mode
                    </label>
                    <Tabs 
                      value={state.view.mode} 
                      onValueChange={(value) => handleViewChange(value as 'grid' | 'list' | 'card')} 
                      className="w-full"
                      aria-label="View mode selection"
                    >
                      <TabsList className="grid grid-cols-3 w-full h-10">
                        <TabsTrigger value="grid" className="text-xs sm:text-sm px-sm">Grid</TabsTrigger>
                        <TabsTrigger value="list" className="text-xs sm:text-sm px-sm">List</TabsTrigger>
                        <TabsTrigger value="card" className="text-xs sm:text-sm px-sm">Card</TabsTrigger>
                      </TabsList>
                    </Tabs>
                  </div>
                </div>
                
                {/* Results Summary Bar - Mobile optimized */}
                <div 
                  className="flex flex-col space-y-sm sm:flex-row sm:items-center sm:justify-between sm:space-y-0 pt-sm border-t border-border"
                  role="status"
                  aria-live="polite"
                  aria-label="Search results summary"
                >
                  <div className="text-sm text-muted-foreground sm:text-base">
                    <span className="font-medium text-foreground">{filteredPages.length}</span> 
                    <span>pages found</span>
                    {searchState.debouncedSearchTerm && (
                      <span className="ml-sm">
                        for "<SearchHighlight 
                          text={searchState.debouncedSearchTerm} 
                          className="font-medium text-foreground"
                        />"
                      </span>
                    )}
                    {searchState.isSearching && (
                      <span className="ml-sm text-primary">
                        (searching...)
                      </span>
                    )}
                  </div>
                  {filteredPages.length > 0 && (
                    <div className="text-xs text-muted-foreground">
                      Last updated: {new Date().toLocaleDateString()}
                    </div>
                  )}
                </div>
              </section>

              {/* Main Content Grid Section - Mobile First Responsive */}
              <section 
                className="min-h-96"
                role="region"
                aria-labelledby="pages-title"
              >
                <div className="mb-md sm:mb-lg">
                  <h2 
                    id="pages-title"
                    className="text-lg font-semibold text-foreground sm:text-xl"
                  >
                    Your Pages
                  </h2>
                  <p className="text-xs text-muted-foreground sm:text-sm">Manage and organize your content</p>
                </div>
                
                {filteredPages.length === 0 ? (
                  <>
                    {/* Empty State with Proper Spacing */}
                    <div 
                      className="flex flex-col items-center justify-center py-xl sm:py-2xl lg:py-3xl bg-muted/30 rounded-lg border-2 border-dashed border-border"
                      role="region"
                      aria-label="Empty state - no pages found"
                    >
                    <div className="text-center space-y-md max-w-md mx-auto px-md">
                      <div className="text-base font-medium mb-xs sm:text-lg lg:text-xl text-foreground">No pages found</div>
                      <div className="text-sm text-muted-foreground sm:text-base mb-lg">Create your first page to get started with UIStudio</div>
                      <Button
                        size="sm"
                        className="w-full max-w-xs sm:w-auto px-lg py-sm"
                        onClick={() => setState(prev => ({ ...prev, modals: { ...prev.modals, createPageOpen: true } }))}
                      >
                        Create Your First Page
                      </Button>
                    </div>
                    </div>
                  </>
                ) : (
                  <>
                    {/* Pages Grid Container with Improved Spacing */}
                    <div className="space-y-md">
                    <ScrollArea 
                      className="h-[calc(100vh-24rem)] sm:h-[calc(100vh-26rem)] lg:h-[calc(100vh-28rem)]"
                      role="grid"
                      aria-label="Pages grid"
                    >
                      {/* Responsive Grid - Mobile First Approach with Enhanced Spacing */}
                      <div 
                        className="grid gap-md sm:gap-lg lg:gap-xl pb-lg
                          grid-cols-1
                          sm:grid-cols-2
                          lg:grid-cols-3
                          xl:grid-cols-4
                          2xl:grid-cols-5"
                        role="grid"
                        aria-label={`${filteredPages.length} pages found`}
                      >
                        {filteredPages.map((page, index) => {
                          // Find search result for highlighting
                          const searchResult = searchState.debouncedSearchTerm.trim() 
                            ? searchResults.find(result => result.item.id === page.id)
                            : null;
                          
                          return (
                          <Card 
                            key={page.id} 
                            data-page-index={index}
                            className={`cursor-pointer hover:shadow-md transition-all duration-200 hover:scale-[1.02] group
                              flex flex-col h-full
                              border-2 hover:border-primary/20 
                              bg-card/50 backdrop-blur-sm
                              ${focusedPageIndex === index ? 'ring-2 ring-blue-500 ring-offset-2' : ''}
                            `}
                            role="gridcell"
                            tabIndex={0}
                            aria-label={`Page: ${page.pageName}, ${page.isPublished ? 'Published' : 'Draft'}`}
                            aria-selected={focusedPageIndex === index}
                            onClick={() => {
                              setFocusedPageIndex(index);
                              handlePageSelect(page);
                            }}
                            onFocus={() => setFocusedPageIndex(index)}
                            onKeyDown={(e) => {
                              if (e.key === 'Enter' || e.key === ' ') {
                                e.preventDefault();
                                handlePageSelect(page);
                              } else if (e.key === 'ArrowRight' || e.key === 'ArrowDown') {
                                e.preventDefault();
                                handlePageNavigation('next');
                              } else if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') {
                                e.preventDefault();
                                handlePageNavigation('previous');
                              } else if (e.key === 'Home') {
                                e.preventDefault();
                                handlePageNavigation('first');
                              } else if (e.key === 'End') {
                                e.preventDefault();
                                handlePageNavigation('last');
                              } else if (e.key === 'Delete' || e.key === 'Backspace') {
                                e.preventDefault();
                                handleDeletePage(page.id);
                              }
                            }}
                          >
                            <CardHeader className="pb-sm space-y-sm p-md">
                              <div className="flex items-start justify-between gap-sm">
                                <CardTitle className="text-sm font-medium leading-tight line-clamp-2 group-hover:text-primary transition-colors sm:text-base">
                                  {searchResult?.highlights.pageName ? (
                                    <SearchHighlight
                                      text={page.pageName}
                                      highlights={searchResult.highlights.pageName}
                                      className="inline"
                                    />
                                  ) : (
                                    page.pageName
                                  )}
                                </CardTitle>
                                <Badge 
                                  variant={page.isPublished ? 'default' : 'secondary'}
                                  className="shrink-0 text-xs px-xs py-0.5"
                                >
                                  {page.isPublished ? 'Published' : 'Draft'}
                                </Badge>
                              </div>
                              <CardDescription className="text-xs text-muted-foreground truncate sm:text-sm">
                                /{searchResult?.highlights.pageSlug ? (
                                  <SearchHighlight
                                    text={page.pageSlug}
                                    highlights={searchResult.highlights.pageSlug}
                                    className="inline"
                                  />
                                ) : (
                                  page.pageSlug
                                )}
                              </CardDescription>
                            </CardHeader>
                            <CardContent className="flex-1 pb-sm px-md">
                              <div className="text-xs text-muted-foreground line-clamp-3 sm:text-sm leading-relaxed">
                                {searchResult?.highlights.description ? (
                                  <SearchHighlight
                                    text={page.description || 'No description provided'}
                                    highlights={searchResult.highlights.description}
                                    className="inline"
                                  />
                                ) : (
                                  page.description || 'No description provided'
                                )}
                              </div>
                            </CardContent>
                            <CardFooter className="pt-sm flex flex-col space-y-sm sm:flex-row sm:justify-between sm:space-y-0 p-md">
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => handlePageSelect(page)}
                                className="w-full sm:w-auto text-xs sm:text-sm px-sm py-xs"
                              >
                                Edit Page
                              </Button>
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => handleDeletePage(page.id)}
                                disabled={deletePageWrapper.loading}
                                className="w-full sm:w-auto text-xs sm:text-sm text-destructive hover:text-destructive px-sm py-xs"
                              >
                                {deletePageWrapper.loading ? 'Deleting...' : 'Delete'}
                              </Button>
                            </CardFooter>
                          </Card>
                          );
                        })}
                      </div>
                    </ScrollArea>
                    
                    {/* Pagination Controls */}
                    {state.pagination.totalPages > 1 && (
                      <div className="mt-lg pt-md border-t border-border">
                        <Pagination
                          currentPage={state.pagination.currentPage}
                          totalPages={state.pagination.totalPages}
                          pageSize={state.pagination.pageSize}
                          totalItems={state.pagination.totalItems}
                          pageSizeOptions={state.pagination.pageSizeOptions}
                          hasNextPage={state.pagination.hasNextPage}
                          hasPreviousPage={state.pagination.hasPreviousPage}
                          onPageChange={handlePageChange}
                          onPageSizeChange={handlePageSizeChange}
                          showPageSizeSelector={true}
                          showPageInfo={true}
                          loading={isLoading}
                          disabled={isLoading}
                          size="md"
                          className="justify-center sm:justify-between"
                        />
                      </div>
                    )}
                    </div>
                  </>
                )}
              </section>
            </div>
          </main>

          {/* Footer with Status Indicators */}
          <footer role="contentinfo" aria-label="UIStudio footer">
            <UIStudioFooter
            userEntityId={userEntityId}
            showIndicators={{
              build: true,
              connection: true,
              database: true,
              api: true,
              session: true,
              performance: true
            }}
            version="1.0.0"
            buildInfo={{
              number: "241",
              timestamp: new Date(),
              branch: "feature/ftr_bento_cms",
              commit: "5ebc6a9"
            }}
            onRefreshStatus={() => {
              publishedPagesData.refetch();
              userPagesData.refetch();
              userTemplatesData.refetch();
            }}
            onOpenSystemInfo={() => console.log('System info not yet implemented')}
            className="mt-auto"
            compact={false}
            />
          </footer>
        </div>
      </div>

      {/* Create Page Dialog - Mobile Responsive */}
      <Dialog
        open={state.modals.createPageOpen}
        onOpenChange={(open) => setState(prev => ({ ...prev, modals: { ...prev.modals, createPageOpen: open } }))}
      >
        <DialogContent 
          className="w-[95vw] max-w-md sm:max-w-lg"
          aria-describedby="create-page-description"
        >
          <DialogHeader>
            <DialogTitle className="text-lg sm:text-xl">Create New Page</DialogTitle>
            <DialogDescription 
              id="create-page-description"
              className="text-sm sm:text-base"
            >
              Create a new page in your UIStudio workspace
            </DialogDescription>
          </DialogHeader>
          <CreatePageForm
            onSubmit={handleCreatePage}
            userEntityId={userEntityId}
            loading={createPageWrapper.loading}
            error={createPageWrapper.error}
          />
        </DialogContent>
      </Dialog>

      {/* Template Gallery Dialog - Mobile Responsive */}
      <Dialog
        open={state.modals.templateGalleryOpen}
        onOpenChange={(open) => setState(prev => ({ ...prev, modals: { ...prev.modals, templateGalleryOpen: open } }))}
      >
        <DialogContent 
          className="w-[95vw] max-w-7xl h-[90vh] p-0"
          aria-describedby="template-gallery-description"
        >
          <TemplateGalleryGrid
            userEntityId={userEntityId}
            isOpen={state.modals.templateGalleryOpen}
            onClose={() => setState(prev => ({ ...prev, modals: { ...prev.modals, templateGalleryOpen: false } }))}
            onTemplateApply={(template, pageName) => {
              console.log('Template applied:', template.templateName, 'Page:', pageName);
              // Template application navigation is handled within the component
            }}
            className="h-full p-6"
          />
        </DialogContent>
      </Dialog>
    </div>
  );
};

// ============================================================================
// Helper Components
// ============================================================================

/**
 * Sidebar Navigation Component
 */
interface SidebarNavigationProps {
  userEntityId: UIStudioEntityId;
  onCreatePage: () => void;
  onOpenTemplates: () => void;
  recentPages: UIStudioPage[];
  isMobile?: boolean;
}

const SidebarNavigation: React.FC<SidebarNavigationProps> = ({
  onCreatePage,
  onOpenTemplates,
  recentPages
}) => {
  const navigate = useNavigate();

  const navigationItems = [
    {
      icon: Home,
      label: 'Dashboard',
      href: '/studio',
      active: true
    },
    {
      icon: FileText,
      label: 'Pages',
      href: '/studio/pages',
      active: false
    },
    {
      icon: Layers,
      label: 'Templates',
      action: onOpenTemplates,
      active: false
    },
    {
      icon: BarChart3,
      label: 'Analytics',
      href: '/studio/analytics',
      active: false
    },
    {
      icon: Users,
      label: 'Team',
      href: '/studio/team',
      active: false
    },
    {
      icon: Settings,
      label: 'Settings',
      href: '/studio/settings',
      active: false
    }
  ];

  const handleNavigation = (item: typeof navigationItems[0]) => {
    if (item.action) {
      item.action();
    } else if (item.href) {
      navigate(item.href);
    }
  };

  const handlePageSelect = (page: UIStudioPage) => {
    navigate(`/studio/page/${page.id}`);
  };

  return (
    <nav 
      className="flex flex-col h-full"
      role="navigation"
      aria-label="UIStudio navigation"
    >
      {/* Sidebar Header */}
      <div 
        className="p-4 border-b"
        role="banner"
        aria-label="UIStudio branding"
      >
        <div className="flex items-center space-x-2">
          <div className="w-8 h-8 bg-primary rounded-lg flex items-center justify-center">
            <FolderOpen className="h-4 w-4 text-primary-foreground" />
          </div>
          <div className="flex flex-col">
            <h2 className="text-sm font-semibold">UIStudio</h2>
            <p className="text-xs text-muted-foreground">Page Builder</p>
          </div>
        </div>
      </div>

      {/* Quick Actions */}
      <section 
        className="p-4 space-y-2"
        role="region"
        aria-labelledby="quick-actions-title"
      >
        <h3 id="quick-actions-title" className="sr-only">Quick Actions</h3>
        <Button
          onClick={onCreatePage}
          className="w-full justify-start"
          size="sm"
        >
          <Plus className="h-4 w-4 mr-2" />
          Create New Page
        </Button>
        <Button
          onClick={onOpenTemplates}
          variant="outline"
          className="w-full justify-start"
          size="sm"
        >
          <Layers className="h-4 w-4 mr-2" />
          Browse Templates
        </Button>
      </section>

      <Separator />

      {/* Navigation Items */}
      <nav className="flex-1 p-4">
        <div className="space-y-1">
          <div className="text-xs font-medium text-muted-foreground uppercase tracking-wider mb-2">
            Navigation
          </div>
          {navigationItems.map((item) => {
            const Icon = item.icon;
            return (
              <Button
                key={item.label}
                variant={item.active ? 'secondary' : 'ghost'}
                className="w-full justify-start h-9"
                onClick={() => handleNavigation(item)}
              >
                <Icon className="h-4 w-4 mr-3" />
                <span className="text-sm">{item.label}</span>
                {!item.action && (
                  <ChevronRight className="h-3 w-3 ml-auto opacity-50" />
                )}
              </Button>
            );
          })}
        </div>

        {/* Recent Pages Section */}
        {recentPages.length > 0 && (
          <>
            <Separator className="my-4" />
            <div className="space-y-1">
              <div className="flex items-center justify-between">
                <div className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Recent Pages
                </div>
                <Star className="h-3 w-3 text-muted-foreground" />
              </div>
              <div className="space-y-1 mt-2">
                {recentPages.map((page) => (
                  <Button
                    key={page.id}
                    variant="ghost"
                    className="w-full justify-start h-auto p-2 text-left"
                    onClick={() => handlePageSelect(page)}
                  >
                    <div className="flex items-center space-x-2 w-full">
                      <FileText className="h-3 w-3 text-muted-foreground shrink-0" />
                      <div className="flex-1 min-w-0">
                        <div className="text-xs font-medium truncate">
                          {page.pageName}
                        </div>
                        <div className="text-xs text-muted-foreground truncate">
                          /{page.pageSlug}
                        </div>
                      </div>
                      <Badge 
                        variant={page.isPublished ? 'default' : 'secondary'}
                        className="text-xs h-4 px-1"
                      >
                        {page.isPublished ? 'Pub' : 'Draft'}
                      </Badge>
                    </div>
                  </Button>
                ))}
              </div>
              
              {/* View All Recent Pages */}
              <Button
                variant="ghost"
                className="w-full justify-start h-8 text-xs text-muted-foreground"
                onClick={() => navigate('/studio/recent')}
              >
                <Clock className="h-3 w-3 mr-2" />
                View all recent
              </Button>
            </div>
          </>
        )}
      </nav>

      {/* Footer */}
      <footer 
        className="p-4 border-t"
        role="contentinfo"
        aria-label="Sidebar footer"
      >
        <div className="text-xs text-muted-foreground text-center">
          UIStudio v1.0
        </div>
      </footer>
    </nav>
  );
};

/**
 * Create Page Form component
 */
interface CreatePageFormProps {
  onSubmit: (request: CreatePageRequest) => void;
  userEntityId: UIStudioEntityId;
  loading: boolean;
  error: string | null;
}

const CreatePageForm: React.FC<CreatePageFormProps> = ({
  onSubmit,
  userEntityId,
  loading,
  error
}) => {
  const [formData, setFormData] = useState({
    pageName: '',
    pageSlug: '',
    pageType: 'static' as UIStudioPageType,
    description: ''
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({
      ...formData,
      createdByEntityId: userEntityId
    });
  };

  const generateSlug = (name: string) => {
    return name
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
  };

  return (
    <form 
      onSubmit={handleSubmit} 
      className="space-y-4 sm:space-y-6"
      aria-label="Create new page form"
      noValidate
    >
      {/* Mobile-first responsive form grid */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 sm:gap-6">
        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor="pageName" className="text-sm font-medium sm:text-base">Page Name *</Label>
          <Input
            id="pageName"
            value={formData.pageName}
            onChange={(e) => {
              const name = e.target.value;
              setFormData(prev => ({
                ...prev,
                pageName: name,
                pageSlug: generateSlug(name)
              }));
            }}
            placeholder="Enter page name"
            className="w-full"
            required
            aria-required="true"
            aria-invalid={!formData.pageName && formData.pageName !== '' ? 'true' : 'false'}
            aria-describedby="pageName-description"
          />
          <div id="pageName-description" className="sr-only">
            Enter a descriptive name for your new page
          </div>
        </div>

        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor="pageSlug" className="text-sm font-medium sm:text-base">Page Slug *</Label>
          <Input
            id="pageSlug"
            value={formData.pageSlug}
            onChange={(e) => setFormData(prev => ({ ...prev, pageSlug: e.target.value }))}
            placeholder="page-slug"
            className="w-full"
            required
            aria-required="true"
            aria-invalid={!formData.pageSlug && formData.pageSlug !== '' ? 'true' : 'false'}
            aria-describedby="pageSlug-description"
          />
          <div id="pageSlug-description" className="sr-only">
            URL-friendly version of the page name, used in the page URL
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="pageType" className="text-sm font-medium sm:text-base">Page Type</Label>
          <Select
            value={formData.pageType}
            onValueChange={(value) => setFormData(prev => ({ ...prev, pageType: value as UIStudioPageType }))}
          >
            <SelectTrigger 
              className="w-full"
              aria-label="Select page type"
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="static">Static</SelectItem>
              <SelectItem value="dynamic">Dynamic</SelectItem>
            </SelectContent>
          </Select>
        </div>
        
        <div className="space-y-2">
          {/* Placeholder for future options */}
          <div className="h-[68px] flex items-end">
            <div className="text-xs text-muted-foreground">
              More options coming soon
            </div>
          </div>
        </div>

        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor="description" className="text-sm font-medium sm:text-base">Description (Optional)</Label>
          <Textarea
            id="description"
            value={formData.description}
            onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
            placeholder="Brief description of the page"
            rows={3}
            className="w-full resize-none"
            aria-describedby="description-description"
          />
          <div id="description-description" className="sr-only">
            Optional description to help identify and organize this page
          </div>
        </div>
      </div>

      {error && (
        <div 
          className="text-destructive text-sm p-3 bg-destructive/10 rounded-md border border-destructive/20"
          role="alert"
          aria-live="assertive"
        >
          {error}
        </div>
      )}

      <DialogFooter className="flex-col space-y-2 sm:flex-row sm:space-y-0 sm:space-x-2">
        <Button 
          type="submit" 
          disabled={loading || !formData.pageName || !formData.pageSlug}
          className="w-full sm:w-auto"
          size="sm"
        >
          {loading ? 'Creating...' : 'Create Page'}
        </Button>
      </DialogFooter>
    </form>
  );
};

// ============================================================================
// Default Export
// ============================================================================

export default UIStudioInterface;
export { SidebarNavigation };