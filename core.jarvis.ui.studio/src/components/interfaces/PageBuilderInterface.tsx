/**
 * PageBuilderInterface Component
 * 
 * Wrapper interface for the PageBuilder component that integrates with UIStudio
 * for editing specific pages. This component handles page loading, saving, and
 * provides the main content area with proper spacing for the page editing experience.
 * 
 * Features:
 * - Mobile-first responsive design
 * - Proper spacing and layout management
 * - Integration with UIStudio backend
 * - Page loading and error states
 * - Save/publish functionality
 * 
 * @module PageBuilderInterface
 */

import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, AlertCircle, Loader2, Users, Wifi, WifiOff } from 'lucide-react';

// UIStudio hooks and services
import {
  useUIStudioPage,
  useUpdateUIStudioPage,
  useUIStudioErrorHandler
} from '../../hooks/useUIStudio';

// Mobile and collaboration hooks
import { useTouchGestures } from '../../hooks/useTouchGestures';
import { useBottomSheet } from '../mobile/BottomSheet';
import { useCollaboration } from '../../hooks/useCollaboration';
import { useVersionControl } from '../../hooks/useVersionControl';

// UI Components
import { Button } from '../ui/button';
import { Alert, AlertDescription } from '../ui/alert';
import { LoadingSpinner } from '../ui/loading-spinner';
import { Separator } from '../ui/separator';
import { Badge } from '../ui/badge';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '../ui/tooltip';
import { BottomSheet } from '../mobile/BottomSheet';

// Bento components
import { PageBuilder } from '../bento/page-builder/PageBuilder';
import { ComponentPalette } from '../bento/page-builder/ComponentPalette';

// Types
import type {
  BentoPage,
  BentoLayout,
  DeviceType,
  GridComponent
} from '../../types/bento';
import type {
  UIStudioPage,
  UIStudioEntityId,
  UpdatePageRequest
} from '../../types/uistudio';
import { PageStatus } from '../../types/bento';

// Recent Pages Tracking
import { addToRecentPages, type RecentPageMetadata } from '../../utils/recentPagesManager';

// Keyboard Navigation
import { useKeyboardNavigation } from '../../hooks/useKeyboardNavigation';
import { useKeyboardNavigationContext } from '../keyboard/KeyboardNavigationProvider';
import { QuickHelpButton } from '../keyboard/KeyboardShortcutDisplay';
import type { KeyboardShortcut } from '../../hooks/useKeyboardNavigation';

// ============================================================================
// Component Props Interface
// ============================================================================

/**
 * Props for the PageBuilderInterface component
 */
export interface PageBuilderInterfaceProps {
  /** Optional page ID override */
  pageId?: UIStudioEntityId;
  
  /** Optional callback when page is updated */
  onPageUpdate?: (page: UIStudioPage) => void;
  
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
 * Page builder state management
 */
interface PageBuilderState {
  /** Whether the page has unsaved changes */
  hasUnsavedChanges: boolean;
  
  /** Current save operation status */
  isSaving: boolean;
  
  /** Current publish operation status */
  isPublishing: boolean;
  
  /** Last save timestamp */
  lastSaved: string | null;
  
  /** Auto-save enabled */
  autoSaveEnabled: boolean;
  
  /** Current editing device */
  currentDevice: DeviceType;
  
  /** Mobile component palette visibility */
  showMobilePalette: boolean;
  
  /** Real-time collaboration state */
  collaborationEnabled: boolean;
  
  /** Connected users count */
  connectedUsers: number;
  
  /** Connection status */
  isConnected: boolean;
  
  /** Conflict resolution mode */
  hasConflicts: boolean;
}

// ============================================================================
// Helper Functions
// ============================================================================

/**
 * Convert UIStudio page to Bento page format
 */
const convertToBentoPage = (uiStudioPage: UIStudioPage): BentoPage => {
  return {
    id: uiStudioPage.id,
    displayName: uiStudioPage.pageName,
    route: `/${uiStudioPage.pageSlug}`,
    layoutId: 'default', // TODO: Map from UIStudio layout
    status: uiStudioPage.isPublished ? PageStatus.Published : PageStatus.Draft,
    version: 1 as const, // TODO: Map from UIStudio version
    bindings: {
      security: {
        isPublic: false, // TODO: Map from UIStudio security settings
        requiredRoles: [],
        requiredPermissions: []
      },
      visibility: {
        showInNavigation: true, // TODO: Map from UIStudio visibility settings
        navigationOrder: 0
      }
    },
    createdAt: uiStudioPage.createdAt,
    updatedAt: uiStudioPage.lastUpdated,
    createdBy: uiStudioPage.createdByEntityId,
    updatedBy: uiStudioPage.updatedByEntityId || uiStudioPage.createdByEntityId
  };
};

/**
 * Convert Bento page back to UIStudio update format
 */
const convertToUIStudioUpdate = (
  bentoPage: BentoPage, 
  originalPage: UIStudioPage
): UpdatePageRequest => {
  return {
    pageName: bentoPage.displayName,
    pageSlug: bentoPage.route.startsWith('/') ? bentoPage.route.slice(1) : bentoPage.route,
    updatedByEntityId: originalPage.createdByEntityId // Use current user
  };
};

// ============================================================================
// Component Implementation
// ============================================================================

/**
 * PageBuilderInterface - Main page editing interface
 */
export const PageBuilderInterface: React.FC<PageBuilderInterfaceProps> = ({
  pageId: pageIdProp,
  onPageUpdate,
  onNavigate,
  className = '',
  isLoading: loadingOverride,
  error: errorOverride
}) => {
  const { pageId: pageIdParam } = useParams<{ pageId: string }>();
  const navigate = useNavigate();
  const { handleError } = useUIStudioErrorHandler();

  // Determine the page ID to use
  const pageId = pageIdProp || pageIdParam;

  // ============================================================================
  // State Management
  // ============================================================================

  const [state, setState] = useState<PageBuilderState>({
    hasUnsavedChanges: false,
    isSaving: false,
    isPublishing: false,
    lastSaved: null,
    autoSaveEnabled: true,
    currentDevice: 'desktop' as DeviceType,
    showMobilePalette: false,
    collaborationEnabled: true,
    connectedUsers: 1,
    isConnected: true,
    hasConflicts: false
  });

  // ============================================================================
  // Data Fetching
  // ============================================================================

  // Load the UIStudio page
  const {
    data: uiStudioPage,
    isLoading: pageLoading,
    error: pageError,
    refetch: refetchPage
  } = useUIStudioPage(pageId || '');

  // Update page mutation
  const updatePageMutation = useUpdateUIStudioPage(pageId || '');
  
  // ============================================================================
  // Mobile and Touch Support
  // ============================================================================
  
  // Bottom sheet for mobile component palette
  const componentPalette = useBottomSheet();
  
  // Touch gesture support
  const touchGestures = useTouchGestures(
    {
      enableLongPress: true,
      enableSwipe: true,
      enablePinch: state.currentDevice !== 'desktop',
      longPressDelay: 600
    },
    {
      onLongPress: (detail) => {
        if (state.currentDevice === 'mobile') {
          componentPalette.open();
        }
      },
      onSwipe: (detail) => {
        if (detail.direction === 'up' && state.currentDevice === 'mobile') {
          componentPalette.open();
        }
      }
    }
  );
  
  // ============================================================================
  // Real-time Collaboration
  // ============================================================================
  
  // Collaboration hooks
  const collaboration = useCollaboration(pageId || '', {
    enabled: state.collaborationEnabled,
    onUserJoined: (user) => {
      setState(prev => ({ ...prev, connectedUsers: prev.connectedUsers + 1 }));
    },
    onUserLeft: (user) => {
      setState(prev => ({ ...prev, connectedUsers: Math.max(1, prev.connectedUsers - 1) }));
    },
    onPageUpdate: (update) => {
      // Handle real-time page updates from other users
      if (update.userId !== collaboration.currentUserId) {
        setState(prev => ({ ...prev, hasConflicts: true }));
      }
    },
    onConnectionChange: (connected) => {
      setState(prev => ({ ...prev, isConnected: connected }));
    }
  });
  
  // Version control for collaboration
  const versionControl = useVersionControl(pageId || '', {
    autoSnapshot: true,
    snapshotInterval: 30000, // 30 seconds
    onConflict: (conflict) => {
      setState(prev => ({ ...prev, hasConflicts: true }));
    }
  });

  // ============================================================================
  // Computed Values
  // ============================================================================

  const isLoading = useMemo(() => {
    return loadingOverride ?? (pageLoading || state.isSaving || state.isPublishing);
  }, [loadingOverride, pageLoading, state.isSaving, state.isPublishing]);

  const hasError = useMemo(() => {
    return errorOverride ?? (pageError?.message || updatePageMutation.error?.message);
  }, [errorOverride, pageError?.message, updatePageMutation.error?.message]);

  // Convert UIStudio page to Bento format
  const bentoPage = useMemo(() => {
    if (!uiStudioPage || typeof uiStudioPage !== 'object') return undefined;
    return convertToBentoPage(uiStudioPage as UIStudioPage);
  }, [uiStudioPage]);

  // Available layouts (placeholder - would come from backend)
  const availableLayouts = useMemo<BentoLayout[]>(() => [
    // TODO: Load from UIStudio layouts API
  ], []);

  // ============================================================================
  // Event Handlers
  // ============================================================================

  const handleBackNavigation = useCallback(() => {
    const backPath = '/studio';
    onNavigate?.(backPath);
    navigate(backPath);
  }, [onNavigate, navigate]);

  const handlePageUpdateInternal = useCallback((updatedBentoPage: BentoPage) => {
    setState(prev => ({ ...prev, hasUnsavedChanges: true }));
    
    // Broadcast change to other collaborators
    if (state.collaborationEnabled && collaboration.isConnected) {
      collaboration.broadcastPageUpdate(updatedBentoPage);
    }
    
    // Auto-save after 2 seconds of no changes
    if (state.autoSaveEnabled) {
      const timeoutId = setTimeout(() => {
        handleSave(updatedBentoPage);
      }, 2000);
      
      return () => clearTimeout(timeoutId);
    }
  }, [state.autoSaveEnabled, state.collaborationEnabled, collaboration]);

  const handleSave = useCallback(async (pageToSave?: BentoPage) => {
    if (!uiStudioPage || (!pageToSave && !bentoPage)) return;

    const targetPage = pageToSave || bentoPage!;
    
    setState(prev => ({ ...prev, isSaving: true }));

    try {
      const updateRequest = convertToUIStudioUpdate(targetPage, uiStudioPage as UIStudioPage);
      const result = await updatePageMutation.mutateAsync(updateRequest);
      
      setState(prev => ({
        ...prev,
        isSaving: false,
        hasUnsavedChanges: false,
        lastSaved: new Date().toISOString()
      }));

      // Refresh page data
      refetchPage();
      
      // Notify parent
      if (Array.isArray(result) && result.length > 0) {
        onPageUpdate?.(result[0] as UIStudioPage);
      }
      
    } catch (error) {
      const errorResult = handleError(error, 'save_page');
      setState(prev => ({ ...prev, isSaving: false }));
      console.error('Failed to save page:', errorResult.userMessage);
    }
  }, [uiStudioPage, bentoPage, updatePageMutation, refetchPage, onPageUpdate, handleError]);

  const handlePublish = useCallback(async (pageToPublish: BentoPage) => {
    if (!uiStudioPage) return;

    setState(prev => ({ ...prev, isPublishing: true }));

    try {
      // First ensure the page is saved
      await handleSave(pageToPublish);
      
      // Then publish it
      const publishRequest = convertToUIStudioUpdate(
        { ...pageToPublish, status: PageStatus.Published },
        uiStudioPage as UIStudioPage
      );
      
      const result = await updatePageMutation.mutateAsync(publishRequest);
      
      setState(prev => ({
        ...prev,
        isPublishing: false,
        hasUnsavedChanges: false,
        lastSaved: new Date().toISOString()
      }));

      // Refresh page data
      refetchPage();
      
      // Notify parent
      if (Array.isArray(result) && result.length > 0) {
        onPageUpdate?.(result[0] as UIStudioPage);
      }
      
    } catch (error) {
      const errorResult = handleError(error, 'publish_page');
      setState(prev => ({ ...prev, isPublishing: false }));
      console.error('Failed to publish page:', errorResult.userMessage);
    }
  }, [uiStudioPage, handleSave, updatePageMutation, refetchPage, onPageUpdate, handleError]);

  const handlePreview = useCallback((page: BentoPage, device: DeviceType) => {
    setState(prev => ({ ...prev, currentDevice: device }));
    // TODO: Implement preview functionality
    console.log('Preview page:', page.displayName, 'on', device);
  }, []);
  
  // Mobile-specific handlers
  const handleDeviceChange = useCallback((device: DeviceType) => {
    setState(prev => ({ ...prev, currentDevice: device }));
    
    // Close mobile palette when switching away from mobile
    if (device !== 'mobile' && componentPalette.isOpen) {
      componentPalette.close();
    }
  }, [componentPalette]);
  
  const handleMobilePaletteToggle = useCallback(() => {
    setState(prev => ({ ...prev, showMobilePalette: !prev.showMobilePalette }));
    componentPalette.toggle();
  }, [componentPalette]);
  
  const handleCollaborationToggle = useCallback(() => {
    setState(prev => ({ ...prev, collaborationEnabled: !prev.collaborationEnabled }));
    if (state.collaborationEnabled) {
      collaboration.disconnect();
    } else {
      collaboration.connect();
    }
  }, [state.collaborationEnabled, collaboration]);
  
  const handleConflictResolve = useCallback(() => {
    setState(prev => ({ ...prev, hasConflicts: false }));
    // Implement conflict resolution UI
  }, []);

  // ============================================================================
  // Effects
  // ============================================================================

  // Handle page ID changes and track page access
  useEffect(() => {
    if (pageId) {
      refetchPage();
    }
  }, [pageId, refetchPage]);

  // Track page access for recent pages
  useEffect(() => {
    if (uiStudioPage && bentoPage) {
      // Add to recent pages when the page is successfully loaded
      const recentPageData: RecentPageMetadata = {
        id: bentoPage.id,
        displayName: bentoPage.displayName,
        route: bentoPage.route,
        pageSlug: (uiStudioPage as UIStudioPage).pageSlug,
        status: bentoPage.status as 'draft' | 'published' | 'archived',
        lastAccessed: new Date().toISOString(),
        accessCount: 1, // Will be incremented by the manager if it already exists
        description: bentoPage.description,
        tags: bentoPage.tags,
        createdAt: bentoPage.createdAt,
        updatedAt: bentoPage.updatedAt,
        createdBy: bentoPage.createdBy
      };
      
      addToRecentPages(recentPageData);
    }
  }, [uiStudioPage, bentoPage]);

  // ============================================================================
  // Render States
  // ============================================================================

  // Loading state
  if (isLoading) {
    return (
      <div className={`min-h-screen bg-background flex items-center justify-center ${className}`}>
        <div className="flex flex-col items-center space-y-4">
          <LoadingSpinner size="lg" />
          <div className="text-lg font-medium">Loading Page Builder...</div>
          <div className="text-sm text-muted-foreground">
            {state.isSaving && 'Saving changes...'}
            {state.isPublishing && 'Publishing page...'}
            {pageLoading && 'Loading page data...'}
          </div>
        </div>
      </div>
    );
  }

  // Error state
  if (hasError) {
    return (
      <div className={`min-h-screen bg-background p-4 sm:p-6 lg:p-8 ${className}`}>
        <div className="max-w-2xl mx-auto">
          {/* Header with Back Button */}
          <div className="flex items-center space-x-4 mb-6">
            <Button
              variant="outline"
              size="sm"
              onClick={handleBackNavigation}
              className="flex items-center space-x-2"
            >
              <ArrowLeft className="h-4 w-4" />
              <span>Back to Studio</span>
            </Button>
            <Separator orientation="vertical" className="h-6" />
            <div>
              <h1 className="text-xl font-bold sm:text-2xl">Page Builder</h1>
              <p className="text-sm text-muted-foreground sm:text-base">Error loading page</p>
            </div>
          </div>

          {/* Error Alert */}
          <Alert variant="destructive">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription className="text-sm sm:text-base">
              <div className="font-medium mb-2">Unable to load page</div>
              <div className="text-xs sm:text-sm opacity-90">{hasError}</div>
            </AlertDescription>
          </Alert>

          {/* Retry Button */}
          <div className="mt-6 flex justify-center">
            <Button
              onClick={() => refetchPage()}
              disabled={pageLoading}
              className="w-full max-w-xs sm:w-auto"
            >
              {pageLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Try Again
            </Button>
          </div>
        </div>
      </div>
    );
  }

  // No page found
  if (!uiStudioPage || !bentoPage) {
    return (
      <div className={`min-h-screen bg-background p-4 sm:p-6 lg:p-8 ${className}`}>
        <div className="max-w-2xl mx-auto">
          {/* Header with Back Button */}
          <div className="flex items-center space-x-4 mb-6">
            <Button
              variant="outline"
              size="sm"
              onClick={handleBackNavigation}
              className="flex items-center space-x-2"
            >
              <ArrowLeft className="h-4 w-4" />
              <span>Back to Studio</span>
            </Button>
            <Separator orientation="vertical" className="h-6" />
            <div>
              <h1 className="text-xl font-bold sm:text-2xl">Page Builder</h1>
              <p className="text-sm text-muted-foreground sm:text-base">Page not found</p>
            </div>
          </div>

          {/* Not Found Alert */}
          <Alert>
            <AlertCircle className="h-4 w-4" />
            <AlertDescription className="text-sm sm:text-base">
              <div className="font-medium mb-2">Page not found</div>
              <div className="text-xs sm:text-sm opacity-90">
                The page you're looking for doesn't exist or you don't have permission to access it.
              </div>
            </AlertDescription>
          </Alert>

          {/* Back Button */}
          <div className="mt-6 flex justify-center">
            <Button
              onClick={handleBackNavigation}
              className="w-full max-w-xs sm:w-auto"
            >
              Back to Studio
            </Button>
          </div>
        </div>
      </div>
    );
  }

  // ============================================================================
  // Main Content Area with Proper Spacing
  // ============================================================================

  return (
    <div className={`min-h-screen bg-background ${className}`}>
      {/* Page Builder Container - Mobile First Responsive Layout */}
      <div className="h-screen flex flex-col overflow-hidden">
        {/* Header - Mobile First with Proper Spacing */}
        <header className="border-b bg-card/50 backdrop-blur-sm sticky top-0 z-50">
          <div className="px-4 sm:px-6 lg:px-8">
            <div className="flex items-center justify-between h-14 sm:h-16">
              {/* Navigation and Title - Mobile Optimized */}
              <div className="flex items-center space-x-3 sm:space-x-4">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleBackNavigation}
                  className="flex items-center space-x-1 sm:space-x-2"
                >
                  <ArrowLeft className="h-3 w-3 sm:h-4 sm:w-4" />
                  <span className="hidden sm:inline">Back</span>
                </Button>
                
                <Separator orientation="vertical" className="h-4 sm:h-6" />
                
                <div className="flex flex-col min-w-0">
                  <h1 className="text-sm font-semibold truncate sm:text-base lg:text-lg">
                    {bentoPage.displayName}
                  </h1>
                  <div className="flex items-center space-x-2">
                    <p className="text-xs text-muted-foreground truncate sm:text-sm">
                      /{(uiStudioPage as UIStudioPage)?.pageSlug || 'unknown'}
                    </p>
                    {state.hasUnsavedChanges && (
                      <span className="inline-flex items-center text-xs text-orange-600">
                        • Unsaved
                      </span>
                    )}
                    {state.lastSaved && !state.hasUnsavedChanges && (
                      <span className="hidden sm:inline-flex items-center text-xs text-green-600">
                        • Saved
                      </span>
                    )}
                  </div>
                </div>
              </div>

              {/* Collaboration Status */}
              <div className="flex items-center space-x-2 sm:space-x-3">
                <TooltipProvider>
                  {/* Connection Status */}
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <div className="flex items-center space-x-1">
                        {state.isConnected ? (
                          <Wifi className="h-3 w-3 text-green-600" />
                        ) : (
                          <WifiOff className="h-3 w-3 text-red-600" />
                        )}
                        {state.collaborationEnabled && (
                          <>
                            <Users className="h-3 w-3 text-muted-foreground" />
                            <span className="text-xs text-muted-foreground">
                              {state.connectedUsers}
                            </span>
                          </>
                        )}
                      </div>
                    </TooltipTrigger>
                    <TooltipContent>
                      <p>{state.isConnected ? 'Connected' : 'Disconnected'}</p>
                      {state.collaborationEnabled && (
                        <p>{state.connectedUsers} user(s) editing</p>
                      )}
                    </TooltipContent>
                  </Tooltip>
                  
                  {/* Conflict Indicator */}
                  {state.hasConflicts && (
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Badge variant="destructive" className="text-xs">
                          Conflicts
                        </Badge>
                      </TooltipTrigger>
                      <TooltipContent>
                        <p>Page has conflicts that need resolution</p>
                      </TooltipContent>
                    </Tooltip>
                  )}
                </TooltipProvider>
                
                {/* Mobile Component Palette Toggle */}
                {state.currentDevice === 'mobile' && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handleMobilePaletteToggle}
                    className="sm:hidden"
                  >
                    Components
                  </Button>
                )}
              </div>
              
              {/* Actions - Responsive */}
              <div className="flex items-center space-x-1 sm:space-x-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handleSave()}
                  disabled={!state.hasUnsavedChanges || state.isSaving}
                  className="hidden sm:flex"
                >
                  {state.isSaving && <Loader2 className="mr-1 h-3 w-3 animate-spin" />}
                  Save
                </Button>
                
                <Button
                  size="sm"
                  onClick={() => handlePublish(bentoPage)}
                  disabled={state.isPublishing}
                  className="text-xs sm:text-sm"
                >
                  {state.isPublishing && <Loader2 className="mr-1 h-3 w-3 animate-spin" />}
                  {bentoPage.status === PageStatus.Published ? 'Update' : 'Publish'}
                </Button>
              </div>
            </div>
          </div>
        </header>

        {/* Main Content Area - Full Height with Proper Spacing */}
        <main 
          className="flex-1 overflow-hidden"
          ref={(el) => {
            if (el && touchGestures.isTouchDevice) {
              touchGestures.attachListeners(el);
            }
          }}
        >
          {/* PageBuilder Container with Mobile-First Responsive Spacing */}
          <div className="h-full">
            <PageBuilder
              page={bentoPage}
              layouts={availableLayouts}
              readOnly={false}
              onPageUpdate={handlePageUpdateInternal}
              onSave={handleSave}
              onPublish={handlePublish}
              onPreview={handlePreview}
              onDeviceChange={handleDeviceChange}
              currentDevice={state.currentDevice}
              collaborationEnabled={state.collaborationEnabled}
              connectedUsers={state.connectedUsers}
              isConnected={state.isConnected}
              className="h-full"
            />
          </div>
          
          {/* Mobile Component Palette */}
          <BottomSheet
            isOpen={componentPalette.isOpen}
            onClose={componentPalette.close}
            title="Components"
            initialHeight={0.6}
            maxHeight={0.9}
            minHeight={0.3}
            showHandle={true}
            dismissOnBackdrop={true}
          >
            <div className="p-4">
              <ComponentPalette
                onComponentAdd={(type, position) => {
                  // Handle component addition
                  console.log('Add component:', type, position);
                  componentPalette.close();
                }}
                selectedDevice={state.currentDevice}
                compact={true}
              />
            </div>
          </BottomSheet>
          
          {/* Conflict Resolution Modal */}
          {state.hasConflicts && (
            <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
              <div className="bg-white rounded-lg p-6 max-w-md mx-4">
                <h3 className="text-lg font-semibold mb-4">Page Conflicts Detected</h3>
                <p className="text-sm text-muted-foreground mb-4">
                  Another user has made changes to this page. Please review and resolve conflicts.
                </p>
                <div className="flex space-x-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handleConflictResolve}
                  >
                    Resolve Conflicts
                  </Button>
                  <Button
                    size="sm"
                    onClick={() => {
                      setState(prev => ({ ...prev, hasConflicts: false }));
                      // Reload page data
                      refetchPage();
                    }}
                  >
                    Reload Page
                  </Button>
                </div>
              </div>
            </div>
          )}
        </main>
      </div>
    </div>
  );
};

// ============================================================================
// Default Export
// ============================================================================

export default PageBuilderInterface;