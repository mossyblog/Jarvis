/**
 * UIStudio React Query Hooks
 * 
 * Custom hooks for all UIStudio operations using React Query for
 * optimistic updates, caching, and real-time capabilities.
 * 
 * @module UseUIStudio
 */

import { 
  useQuery, 
  useMutation, 
  useQueryClient,
  type UseQueryOptions,
  type UseMutationOptions,
  type QueryKey 
} from '@tanstack/react-query';

import { uistudioApiClient } from '../services/api/uistudioApiClient';
import { graphqlService } from '../services/graphql/graphqlService';
import { 
  UIStudioError,
  getUserFriendlyMessage,
  getRecoveryConfig,
  logError,
  createErrorContext
} from '../utils/uistudioErrors';
import {
  cacheKeys,
  invalidationPatterns,
  createCacheManager,
  type CacheInvalidationConfig
} from '../utils/cacheManager';
import {
  pageDataQueryOptions,
  layoutDataQueryOptions,
  templateDataQueryOptions,
  bindingDataQueryOptions,
  versionHistoryQueryOptions,
  staticQueryOptions
} from '../providers/QueryProvider';

import type {
  UIStudioPage,
  UIStudioLayout,
  UIStudioComponentBinding,
  UIStudioTemplate,
  UIStudioPermission,
  UIStudioVersion,
  UIStudioApiResponse,
  CreatePageRequest,
  UpdatePageRequest,
  CreateLayoutRequest,
  UpdateLayoutRequest,
  CreateBindingRequest,
  UpdateBindingRequest,
  CreateTemplateRequest,
  GrantPermissionRequest,
  CreateVersionRequest,
  DuplicatePageRequest,
  ApplyTemplateRequest,
  GetPublishedPagesQuery,
  GetResourcePermissionsQuery,
  GetVersionHistoryQuery,
  UIStudioEntityId
} from '../types/uistudio';

// ============================================================================
// Re-export cache keys for backward compatibility
// ============================================================================

/** @deprecated Use cacheKeys from cacheManager instead */
export const uistudioKeys = cacheKeys;

// ============================================================================
// Error Handling Hook
// ============================================================================

/** Hook for consistent error handling across UIStudio operations */
export function useUIStudioErrorHandler() {
  return {
    handleError: (error: unknown, operation: string) => {
      if (error instanceof UIStudioError) {
        const context = createErrorContext(operation);
        logError(error, context);
        
        const recoveryConfig = getRecoveryConfig(error);
        const userMessage = getUserFriendlyMessage(error);
        
        // In a real app, you might show a toast notification here
        console.error(`UIStudio Error [${operation}]:`, userMessage);
        
        return { error, userMessage, recoveryConfig };
      }
      
      // Handle non-UIStudio errors
      const fallbackMessage = 'An unexpected error occurred';
      console.error(`Unexpected Error [${operation}]:`, error);
      
      return { 
        error, 
        userMessage: fallbackMessage, 
        recoveryConfig: { strategy: 'ignore' as const, showToast: true, toastMessage: fallbackMessage }
      };
    }
  };
}

// ============================================================================
// Cache Management Hook
// ============================================================================

/** Hook to access cache manager for advanced cache operations */
export function useUIStudioCacheManager() {
  const queryClient = useQueryClient();
  const cacheManager = createCacheManager(queryClient);
  
  return {
    // Core cache manager methods
    applyInvalidation: cacheManager.applyInvalidation.bind(cacheManager),
    invalidateAll: cacheManager.invalidateAll.bind(cacheManager),
    clearAll: cacheManager.clearAll.bind(cacheManager),
    backgroundRefresh: cacheManager.backgroundRefresh.bind(cacheManager),
    warmupCache: cacheManager.warmupCache.bind(cacheManager),
    getCacheStats: cacheManager.getCacheStats.bind(cacheManager),
    getCacheHealth: cacheManager.getCacheHealth.bind(cacheManager),
    logCacheMetrics: cacheManager.logCacheMetrics.bind(cacheManager),
    optimisticUpdate: cacheManager.optimisticUpdate.bind(cacheManager),
    prefetchRelatedData: cacheManager.prefetchRelatedData.bind(cacheManager),
    
    // Convenience methods for common operations
    invalidatePages: () => cacheManager.applyInvalidation({ invalidate: [cacheKeys.pages()] }),
    invalidateLayouts: () => cacheManager.applyInvalidation({ invalidate: [cacheKeys.layouts()] }),
    invalidateBindings: () => cacheManager.applyInvalidation({ invalidate: [cacheKeys.bindings()] }),
    invalidateTemplates: () => cacheManager.applyInvalidation({ invalidate: [cacheKeys.templates()] }),
    
    // Background refresh methods
    refreshStaleData: () => cacheManager.backgroundRefresh(),
    refreshPageData: () => cacheManager.backgroundRefresh('page'),
    refreshLayoutData: () => cacheManager.backgroundRefresh('layout'),
    
    // Cache warming
    warmupForUser: (userId: UIStudioEntityId) => cacheManager.warmupCache(userId),
    
    // Performance monitoring
    logMetrics: () => cacheManager.logCacheMetrics(),
  };
}

// ============================================================================
// Page Management Hooks
// ============================================================================

/** Hook to get a specific page (using GraphQL for reads) */
export function useUIStudioPage(
  pageEntityId: UIStudioEntityId,
  options?: Omit<UseQueryOptions<unknown, UIStudioError>, 'queryKey' | 'queryFn'>
) {
  const queryClient = useQueryClient();
  const cacheManager = createCacheManager(queryClient);
  
  return useQuery({
    queryKey: cacheKeys.page(pageEntityId),
    queryFn: async () => {
      const result = await graphqlService.getPageMetadata(pageEntityId);
      
      // Prefetch related data in the background
      cacheManager.prefetchRelatedData('page', pageEntityId);
      
      return result;
    },
    ...pageDataQueryOptions,
    ...options
  });
}

/** Hook to get pages by owner (using GraphQL for reads) */
export function useUIStudioPagesByOwner(
  ownerEntityId: UIStudioEntityId,
  options?: Omit<UseQueryOptions<unknown[], UIStudioError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: cacheKeys.pagesByOwner(ownerEntityId),
    queryFn: () => graphqlService.getPagesByOwner(ownerEntityId),
    ...pageDataQueryOptions,
    ...options
  });
}

/** Hook to get published pages (using GraphQL for reads) */
export function useUIStudioPublishedPages(
  query: GetPublishedPagesQuery = {},
  options?: Omit<UseQueryOptions<unknown[], UIStudioError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: cacheKeys.publishedPages(query),
    queryFn: () => graphqlService.getPublishedPages(query),
    ...pageDataQueryOptions,
    // Published pages are more stable, so longer stale time
    staleTime: 10 * 60 * 1000, // 10 minutes
    ...options
  });
}

/** Hook to create a page */
export function useCreateUIStudioPage(
  options?: UseMutationOptions<UIStudioApiResponse<UIStudioPage>, UIStudioError, CreatePageRequest>
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();
  const cacheManager = createCacheManager(queryClient);

  return useMutation({
    mutationFn: (request: CreatePageRequest) => uistudioApiClient.createPage(request),
    onSuccess: async (data, variables) => {
      // Apply smart cache invalidation
      const invalidationConfig = invalidationPatterns.createPage(variables.createdByEntityId);
      await cacheManager.applyInvalidation(invalidationConfig);
      
      // Warm up cache for the new page if we got an ID back
      if (data.length > 0 && data[0].id) {
        await cacheManager.prefetchRelatedData('page', data[0].id);
      }
    },
    onError: (error) => {
      handleError(error, 'create_page');
    },
    ...options
  });
}

/** Hook to update a page */
export function useUpdateUIStudioPage(
  pageEntityId: UIStudioEntityId,
  options?: UseMutationOptions<
    UIStudioApiResponse<UIStudioPage>, 
    UIStudioError, 
    UpdatePageRequest,
    { rollback: () => void }
  >
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();
  const cacheManager = createCacheManager(queryClient);

  return useMutation<
    UIStudioApiResponse<UIStudioPage>,
    UIStudioError,
    UpdatePageRequest,
    { rollback: () => void }
  >({
    mutationFn: (request: UpdatePageRequest) => uistudioApiClient.updatePage(pageEntityId, request),
    onMutate: async (variables): Promise<{ rollback: () => void }> => {
      // Use cache manager for optimistic update
      const rollback = await cacheManager.optimisticUpdate<UIStudioApiResponse<UIStudioPage>>(
        cacheKeys.page(pageEntityId),
        (oldData) => {
          if (!oldData || oldData.length === 0) return oldData || [];
          return [{ ...oldData[0], ...variables }];
        }
      );

      return { rollback };
    },
    onError: (error, variables, context) => {
      // Rollback optimistic update
      context?.rollback();
      handleError(error, 'update_page');
    },
    onSuccess: async () => {
      // Apply smart cache invalidation
      const invalidationConfig = invalidationPatterns.updatePage(pageEntityId);
      await cacheManager.applyInvalidation(invalidationConfig);
    },
    ...options
  });
}

/** Hook to publish a page */
export function usePublishUIStudioPage(
  options?: UseMutationOptions<
    UIStudioApiResponse<UIStudioPage>, 
    UIStudioError, 
    { pageEntityId: UIStudioEntityId; publishedByEntityId: UIStudioEntityId }
  >
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();

  return useMutation({
    mutationFn: ({ pageEntityId, publishedByEntityId }) => 
      uistudioApiClient.publishPage(pageEntityId, publishedByEntityId),
    onSuccess: (data, variables) => {
      // Invalidate and refetch affected queries
      queryClient.invalidateQueries({ queryKey: uistudioKeys.page(variables.pageEntityId) });
      queryClient.invalidateQueries({ queryKey: uistudioKeys.publishedPages() });
    },
    onError: (error) => {
      handleError(error, 'publish_page');
    },
    ...options
  });
}

/** Hook to delete a page */
export function useDeleteUIStudioPage(
  options?: UseMutationOptions<
    UIStudioApiResponse<UIStudioPage>, 
    UIStudioError, 
    { pageEntityId: UIStudioEntityId; deletedByEntityId: UIStudioEntityId }
  >
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();

  return useMutation({
    mutationFn: ({ pageEntityId, deletedByEntityId }) => 
      uistudioApiClient.deletePage(pageEntityId, deletedByEntityId),
    onSuccess: (data, variables) => {
      // Remove from cache and invalidate lists
      queryClient.removeQueries({ queryKey: uistudioKeys.page(variables.pageEntityId) });
      queryClient.invalidateQueries({ queryKey: uistudioKeys.pages() });
      queryClient.invalidateQueries({ queryKey: uistudioKeys.publishedPages() });
    },
    onError: (error) => {
      handleError(error, 'delete_page');
    },
    ...options
  });
}

/** Hook to duplicate a page */
export function useDuplicateUIStudioPage(
  pageEntityId: UIStudioEntityId,
  options?: UseMutationOptions<UIStudioApiResponse<UIStudioPage>, UIStudioError, DuplicatePageRequest>
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();

  return useMutation({
    mutationFn: (request: DuplicatePageRequest) => 
      uistudioApiClient.duplicatePage(pageEntityId, request),
    onSuccess: (data, variables) => {
      // Invalidate pages lists
      queryClient.invalidateQueries({ queryKey: uistudioKeys.pages() });
      queryClient.invalidateQueries({ queryKey: uistudioKeys.pagesByOwner(variables.createdByEntityId) });
    },
    onError: (error) => {
      handleError(error, 'duplicate_page');
    },
    ...options
  });
}

// ============================================================================
// Layout Management Hooks
// ============================================================================

/** Hook to get a specific layout (using GraphQL for reads) */
export function useUIStudioLayout(
  layoutEntityId: UIStudioEntityId,
  options?: Omit<UseQueryOptions<unknown, UIStudioError>, 'queryKey' | 'queryFn'>
) {
  const queryClient = useQueryClient();
  const cacheManager = createCacheManager(queryClient);
  
  return useQuery({
    queryKey: cacheKeys.layout(layoutEntityId),
    queryFn: async () => {
      const layouts = await graphqlService.getUIStudioLayouts();
      const layout = layouts.find((layout: any) => layout.id === layoutEntityId) || null;
      
      // Prefetch related data in the background
      if (layout) {
        cacheManager.prefetchRelatedData('layout', layoutEntityId);
      }
      
      return layout;
    },
    ...layoutDataQueryOptions,
    ...options
  });
}

/** Hook to create a layout */
export function useCreateUIStudioLayout(
  options?: UseMutationOptions<UIStudioApiResponse<UIStudioLayout>, UIStudioError, CreateLayoutRequest>
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();

  return useMutation({
    mutationFn: (request: CreateLayoutRequest) => uistudioApiClient.createLayout(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: uistudioKeys.layouts() });
    },
    onError: (error) => {
      handleError(error, 'create_layout');
    },
    ...options
  });
}

/** Hook to update a layout */
export function useUpdateUIStudioLayout(
  layoutEntityId: UIStudioEntityId,
  options?: UseMutationOptions<UIStudioApiResponse<UIStudioLayout>, UIStudioError, UpdateLayoutRequest>
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();

  return useMutation({
    mutationFn: (request: UpdateLayoutRequest) => uistudioApiClient.updateLayout(layoutEntityId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: uistudioKeys.layout(layoutEntityId) });
      queryClient.invalidateQueries({ queryKey: uistudioKeys.layouts() });
    },
    onError: (error) => {
      handleError(error, 'update_layout');
    },
    ...options
  });
}

/** Hook to update layout grid configuration */
export function useUpdateUIStudioLayoutGrid(
  layoutEntityId: UIStudioEntityId,
  options?: UseMutationOptions<
    UIStudioApiResponse<UIStudioLayout>, 
    UIStudioError, 
    NonNullable<UIStudioLayout['gridConfig']>,
    { previousLayout: UIStudioApiResponse<UIStudioLayout> | undefined }
  >
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();

  return useMutation<
    UIStudioApiResponse<UIStudioLayout>,
    UIStudioError,
    NonNullable<UIStudioLayout['gridConfig']>,
    { previousLayout: UIStudioApiResponse<UIStudioLayout> | undefined }
  >({
    mutationFn: (gridConfig) => uistudioApiClient.updateLayoutGrid(layoutEntityId, gridConfig),
    onMutate: async (variables): Promise<{ previousLayout: UIStudioApiResponse<UIStudioLayout> | undefined }> => {
      // Optimistic update for grid configuration
      await queryClient.cancelQueries({ queryKey: uistudioKeys.layout(layoutEntityId) });
      
      const previousLayout = queryClient.getQueryData<UIStudioApiResponse<UIStudioLayout>>(
        uistudioKeys.layout(layoutEntityId)
      );

      if (previousLayout && previousLayout.length > 0) {
        const updatedLayout = { ...previousLayout[0], gridConfig: variables };
        queryClient.setQueryData(uistudioKeys.layout(layoutEntityId), [updatedLayout]);
      }

      return { previousLayout };
    },
    onError: (error, variables, context) => {
      if (context?.previousLayout) {
        queryClient.setQueryData(uistudioKeys.layout(layoutEntityId), context.previousLayout);
      }
      handleError(error, 'update_layout_grid');
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: uistudioKeys.layout(layoutEntityId) });
    },
    ...options
  });
}

// ============================================================================
// Component Binding Hooks
// ============================================================================

/** Hook to get page bindings (using GraphQL for reads) */
export function useUIStudioPageBindings(
  pageSlug: string,
  options?: Omit<UseQueryOptions<unknown[], UIStudioError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: cacheKeys.pageBindingsBySlug(pageSlug),
    queryFn: () => graphqlService.getPageBindings(pageSlug),
    ...bindingDataQueryOptions,
    ...options
  });
}

/** Hook to create a component binding */
export function useCreateUIStudioBinding(
  options?: UseMutationOptions<UIStudioApiResponse<UIStudioComponentBinding>, UIStudioError, CreateBindingRequest>
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();

  return useMutation({
    mutationFn: (request: CreateBindingRequest) => uistudioApiClient.createBinding(request),
    onSuccess: (data, variables) => {
      // Invalidate bindings for the affected page
      queryClient.invalidateQueries({ queryKey: uistudioKeys.bindings() });
      // Note: We'd need to get the page entity ID from the slug to invalidate specific page bindings
    },
    onError: (error) => {
      handleError(error, 'create_binding');
    },
    ...options
  });
}

/** Hook to create multiple bindings in bulk */
export function useCreateUIStudioBindingsBulk(
  options?: UseMutationOptions<UIStudioApiResponse<UIStudioComponentBinding>, UIStudioError, CreateBindingRequest[]>
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();

  return useMutation({
    mutationFn: (requests: CreateBindingRequest[]) => uistudioApiClient.createBindingsBulk(requests),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: uistudioKeys.bindings() });
    },
    onError: (error) => {
      handleError(error, 'create_bindings_bulk');
    },
    ...options
  });
}

/** Hook to update a component binding */
export function useUpdateUIStudioBinding(
  bindingEntityId: UIStudioEntityId,
  options?: UseMutationOptions<UIStudioApiResponse<UIStudioComponentBinding>, UIStudioError, UpdateBindingRequest>
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();

  return useMutation({
    mutationFn: (request: UpdateBindingRequest) => uistudioApiClient.updateBinding(bindingEntityId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: uistudioKeys.binding(bindingEntityId) });
      queryClient.invalidateQueries({ queryKey: uistudioKeys.bindings() });
    },
    onError: (error) => {
      handleError(error, 'update_binding');
    },
    ...options
  });
}

/** Hook to delete a component binding */
export function useDeleteUIStudioBinding(
  options?: UseMutationOptions<UIStudioApiResponse<UIStudioComponentBinding>, UIStudioError, UIStudioEntityId>
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();

  return useMutation({
    mutationFn: (bindingEntityId: UIStudioEntityId) => uistudioApiClient.deleteBinding(bindingEntityId),
    onSuccess: (data, bindingEntityId) => {
      queryClient.removeQueries({ queryKey: uistudioKeys.binding(bindingEntityId) });
      queryClient.invalidateQueries({ queryKey: uistudioKeys.bindings() });
    },
    onError: (error) => {
      handleError(error, 'delete_binding');
    },
    ...options
  });
}

// ============================================================================
// Template Management Hooks
// ============================================================================

/** Hook to get a specific template (using GraphQL for reads) */
export function useUIStudioTemplate(
  templateEntityId: UIStudioEntityId,
  options?: Omit<UseQueryOptions<unknown, UIStudioError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: uistudioKeys.template(templateEntityId),
    queryFn: () => graphqlService.getTemplateMetadata(templateEntityId),
    ...options
  });
}

/** Hook to get templates by owner (using GraphQL for reads) */
export function useUIStudioTemplatesByOwner(
  ownerEntityId: UIStudioEntityId,
  options?: Omit<UseQueryOptions<unknown[], UIStudioError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: uistudioKeys.templatesByOwner(ownerEntityId),
    queryFn: async () => {
      const templates = await graphqlService.getTemplates();
      return templates.filter((template: any) => template.owner_entity_id === ownerEntityId);
    },
    ...options
  });
}

/** Hook to create a template */
export function useCreateUIStudioTemplate(
  options?: UseMutationOptions<UIStudioApiResponse<UIStudioTemplate>, UIStudioError, CreateTemplateRequest>
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();

  return useMutation({
    mutationFn: (request: CreateTemplateRequest) => uistudioApiClient.createTemplate(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: uistudioKeys.templates() });
      queryClient.invalidateQueries({ queryKey: uistudioKeys.templatesByOwner(variables.createdByEntityId) });
    },
    onError: (error) => {
      handleError(error, 'create_template');
    },
    ...options
  });
}

/** Hook to apply a template */
export function useApplyUIStudioTemplate(
  templateEntityId: UIStudioEntityId,
  options?: UseMutationOptions<UIStudioApiResponse<UIStudioPage>, UIStudioError, ApplyTemplateRequest>
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();

  return useMutation({
    mutationFn: (request: ApplyTemplateRequest) => uistudioApiClient.applyTemplate(templateEntityId, request),
    onSuccess: (data, variables) => {
      // Invalidate pages since a new page was created
      queryClient.invalidateQueries({ queryKey: uistudioKeys.pages() });
      queryClient.invalidateQueries({ queryKey: uistudioKeys.pagesByOwner(variables.createdByEntityId) });
    },
    onError: (error) => {
      handleError(error, 'apply_template');
    },
    ...options
  });
}

// ============================================================================
// Version Control Hooks
// ============================================================================

/** Hook to get version history */
export function useUIStudioVersionHistory(
  resourceId: UIStudioEntityId,
  query: GetVersionHistoryQuery = {},
  options?: Omit<UseQueryOptions<UIStudioApiResponse<UIStudioVersion>, UIStudioError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: cacheKeys.versionHistory(resourceId, query),
    queryFn: () => uistudioApiClient.getVersionHistory(resourceId, query),
    ...versionHistoryQueryOptions,
    ...options
  });
}

/** Hook to create version snapshot */
export function useCreateUIStudioVersionSnapshot(
  options?: UseMutationOptions<UIStudioApiResponse<UIStudioVersion>, UIStudioError, CreateVersionRequest>
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();

  return useMutation({
    mutationFn: (request: CreateVersionRequest) => uistudioApiClient.createVersionSnapshot(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: uistudioKeys.versionHistory(variables.resourceEntityId) });
    },
    onError: (error) => {
      handleError(error, 'create_version_snapshot');
    },
    ...options
  });
}

/** Hook to rollback to version */
export function useRollbackUIStudioToVersion(
  options?: UseMutationOptions<
    UIStudioApiResponse<UIStudioVersion>, 
    UIStudioError, 
    { versionId: UIStudioEntityId; rolledBackById: UIStudioEntityId }
  >
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();

  return useMutation({
    mutationFn: ({ versionId, rolledBackById }) => 
      uistudioApiClient.rollbackToVersion(versionId, rolledBackById),
    onSuccess: () => {
      // Invalidate all caches since a rollback affects the current state
      queryClient.invalidateQueries({ queryKey: uistudioKeys.all });
    },
    onError: (error) => {
      handleError(error, 'rollback_to_version');
    },
    ...options
  });
}

// ============================================================================
// Utility Hooks
// ============================================================================

/** Hook for UIStudio health check */
export function useUIStudioHealthCheck(
  options?: Omit<UseQueryOptions<{ status: 'ok' | 'error'; timestamp: string }, UIStudioError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: cacheKeys.health(),
    queryFn: () => uistudioApiClient.healthCheck(),
    // Health checks use static query options with custom intervals
    ...staticQueryOptions,
    // Check health every 5 minutes
    refetchInterval: 5 * 60 * 1000,
    // Don't retry health checks as frequently
    retry: 1,
    ...options
  });
}

/** Hook to invalidate all UIStudio queries */
export function useInvalidateUIStudioQueries() {
  const cacheManagerHook = useUIStudioCacheManager();
  
  return {
    invalidateAll: () => cacheManagerHook.invalidateAll(),
    invalidatePages: () => cacheManagerHook.invalidatePages(),
    invalidateLayouts: () => cacheManagerHook.invalidateLayouts(),
    invalidateBindings: () => cacheManagerHook.invalidateBindings(),
    invalidateTemplates: () => cacheManagerHook.invalidateTemplates(),
    invalidateVersions: () => cacheManagerHook.applyInvalidation({ invalidate: [cacheKeys.versions()] }),
    
    // Advanced cache operations
    refreshStaleData: () => cacheManagerHook.refreshStaleData(),
    clearAllCache: () => cacheManagerHook.clearAll(),
    getCacheStats: () => cacheManagerHook.getCacheStats(),
    getCacheHealth: () => cacheManagerHook.getCacheHealth(),
    logMetrics: () => cacheManagerHook.logMetrics(),
  };
}

// ============================================================================
// Export Cache Keys and Utilities for External Use
// ============================================================================

/** Export cache keys for use in other parts of the application */
export { cacheKeys } from '../utils/cacheManager';

/** Export cache management utilities */
export { 
  createCacheManager,
  type CacheInvalidationConfig 
} from '../utils/cacheManager';

/** Export cache strategy hooks */
export {
  useCacheStrategy,
  useRealtimeCacheStrategy,
  useBackgroundCacheStrategy,
  useStaticCacheStrategy
} from './useCacheStrategy';