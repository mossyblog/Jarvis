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
import { 
  UIStudioError,
  getUserFriendlyMessage,
  getRecoveryConfig,
  logError,
  createErrorContext
} from '../utils/uistudioErrors';

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
// Query Keys Factory
// ============================================================================

/** Query keys for consistent caching and invalidation */
export const uistudioKeys = {
  all: ['uistudio'] as const,
  
  // Pages
  pages: () => [...uistudioKeys.all, 'pages'] as const,
  page: (id: UIStudioEntityId) => [...uistudioKeys.pages(), id] as const,
  pagesByOwner: (ownerId: UIStudioEntityId) => [...uistudioKeys.pages(), 'by-owner', ownerId] as const,
  publishedPages: (query?: GetPublishedPagesQuery) => [...uistudioKeys.pages(), 'published', query] as const,
  pageBindings: (pageId: UIStudioEntityId) => [...uistudioKeys.pages(), pageId, 'bindings'] as const,
  
  // Layouts
  layouts: () => [...uistudioKeys.all, 'layouts'] as const,
  layout: (id: UIStudioEntityId) => [...uistudioKeys.layouts(), id] as const,
  
  // Bindings
  bindings: () => [...uistudioKeys.all, 'bindings'] as const,
  binding: (id: UIStudioEntityId) => [...uistudioKeys.bindings(), id] as const,
  
  // Templates
  templates: () => [...uistudioKeys.all, 'templates'] as const,
  template: (id: UIStudioEntityId) => [...uistudioKeys.templates(), id] as const,
  templatesByOwner: (ownerId: UIStudioEntityId) => [...uistudioKeys.templates(), 'by-owner', ownerId] as const,
  
  // Permissions
  permissions: () => [...uistudioKeys.all, 'permissions'] as const,
  resourcePermissions: (resourceId: UIStudioEntityId, resourceType: string) => 
    [...uistudioKeys.permissions(), 'resource', resourceId, resourceType] as const,
  
  // Versions
  versions: () => [...uistudioKeys.all, 'versions'] as const,
  versionHistory: (resourceId: UIStudioEntityId, query?: GetVersionHistoryQuery) => 
    [...uistudioKeys.versions(), 'history', resourceId, query] as const,
};

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
// Page Management Hooks
// ============================================================================

/** Hook to get a specific page */
export function useUIStudioPage(
  pageEntityId: UIStudioEntityId,
  options?: Omit<UseQueryOptions<UIStudioApiResponse<UIStudioPage>, UIStudioError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: uistudioKeys.page(pageEntityId),
    queryFn: () => uistudioApiClient.getPage(pageEntityId),
    ...options
  });
}

/** Hook to get pages by owner */
export function useUIStudioPagesByOwner(
  ownerEntityId: UIStudioEntityId,
  options?: Omit<UseQueryOptions<UIStudioApiResponse<UIStudioPage>, UIStudioError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: uistudioKeys.pagesByOwner(ownerEntityId),
    queryFn: () => uistudioApiClient.getPagesByOwner(ownerEntityId),
    ...options
  });
}

/** Hook to get published pages */
export function useUIStudioPublishedPages(
  query: GetPublishedPagesQuery = {},
  options?: Omit<UseQueryOptions<UIStudioApiResponse<UIStudioPage>, UIStudioError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: uistudioKeys.publishedPages(query),
    queryFn: () => uistudioApiClient.getPublishedPages(query),
    ...options
  });
}

/** Hook to create a page */
export function useCreateUIStudioPage(
  options?: UseMutationOptions<UIStudioApiResponse<UIStudioPage>, UIStudioError, CreatePageRequest>
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();

  return useMutation({
    mutationFn: (request: CreatePageRequest) => uistudioApiClient.createPage(request),
    onSuccess: (data, variables) => {
      // Invalidate and refetch pages
      queryClient.invalidateQueries({ queryKey: uistudioKeys.pages() });
      queryClient.invalidateQueries({ queryKey: uistudioKeys.pagesByOwner(variables.createdByEntityId) });
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
    { previousPage: UIStudioApiResponse<UIStudioPage> | undefined }
  >
) {
  const queryClient = useQueryClient();
  const { handleError } = useUIStudioErrorHandler();

  return useMutation<
    UIStudioApiResponse<UIStudioPage>,
    UIStudioError,
    UpdatePageRequest,
    { previousPage: UIStudioApiResponse<UIStudioPage> | undefined }
  >({
    mutationFn: (request: UpdatePageRequest) => uistudioApiClient.updatePage(pageEntityId, request),
    onMutate: async (variables): Promise<{ previousPage: UIStudioApiResponse<UIStudioPage> | undefined }> => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: uistudioKeys.page(pageEntityId) });

      // Snapshot the previous value
      const previousPage = queryClient.getQueryData<UIStudioApiResponse<UIStudioPage>>(
        uistudioKeys.page(pageEntityId)
      );

      // Optimistically update the page
      if (previousPage && previousPage.length > 0) {
        const updatedPage = { ...previousPage[0], ...variables };
        queryClient.setQueryData(uistudioKeys.page(pageEntityId), [updatedPage]);
      }

      return { previousPage };
    },
    onError: (error, variables, context) => {
      // Rollback optimistic update
      if (context?.previousPage) {
        queryClient.setQueryData(uistudioKeys.page(pageEntityId), context.previousPage);
      }
      handleError(error, 'update_page');
    },
    onSuccess: () => {
      // Invalidate related queries
      queryClient.invalidateQueries({ queryKey: uistudioKeys.page(pageEntityId) });
      queryClient.invalidateQueries({ queryKey: uistudioKeys.pages() });
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

/** Hook to get a specific layout */
export function useUIStudioLayout(
  layoutEntityId: UIStudioEntityId,
  options?: Omit<UseQueryOptions<UIStudioApiResponse<UIStudioLayout>, UIStudioError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: uistudioKeys.layout(layoutEntityId),
    queryFn: () => uistudioApiClient.getLayout(layoutEntityId),
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

/** Hook to get page bindings */
export function useUIStudioPageBindings(
  pageEntityId: UIStudioEntityId,
  options?: Omit<UseQueryOptions<UIStudioApiResponse<UIStudioComponentBinding>, UIStudioError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: uistudioKeys.pageBindings(pageEntityId),
    queryFn: () => uistudioApiClient.getPageBindings(pageEntityId),
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

/** Hook to get a specific template */
export function useUIStudioTemplate(
  templateEntityId: UIStudioEntityId,
  options?: Omit<UseQueryOptions<UIStudioApiResponse<UIStudioTemplate>, UIStudioError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: uistudioKeys.template(templateEntityId),
    queryFn: () => uistudioApiClient.getTemplate(templateEntityId),
    ...options
  });
}

/** Hook to get templates by owner */
export function useUIStudioTemplatesByOwner(
  ownerEntityId: UIStudioEntityId,
  options?: Omit<UseQueryOptions<UIStudioApiResponse<UIStudioTemplate>, UIStudioError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: uistudioKeys.templatesByOwner(ownerEntityId),
    queryFn: () => uistudioApiClient.getTemplatesByOwner(ownerEntityId),
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
    queryKey: uistudioKeys.versionHistory(resourceId, query),
    queryFn: () => uistudioApiClient.getVersionHistory(resourceId, query),
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
    queryKey: [...uistudioKeys.all, 'health'],
    queryFn: () => uistudioApiClient.healthCheck(),
    // Check health every 5 minutes
    refetchInterval: 5 * 60 * 1000,
    // Don't retry health checks as frequently
    retry: 1,
    ...options
  });
}

/** Hook to invalidate all UIStudio queries */
export function useInvalidateUIStudioQueries() {
  const queryClient = useQueryClient();
  
  return {
    invalidateAll: () => queryClient.invalidateQueries({ queryKey: uistudioKeys.all }),
    invalidatePages: () => queryClient.invalidateQueries({ queryKey: uistudioKeys.pages() }),
    invalidateLayouts: () => queryClient.invalidateQueries({ queryKey: uistudioKeys.layouts() }),
    invalidateBindings: () => queryClient.invalidateQueries({ queryKey: uistudioKeys.bindings() }),
    invalidateTemplates: () => queryClient.invalidateQueries({ queryKey: uistudioKeys.templates() }),
    invalidateVersions: () => queryClient.invalidateQueries({ queryKey: uistudioKeys.versions() }),
  };
}