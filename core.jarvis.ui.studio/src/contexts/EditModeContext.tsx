/**
 * EditModeContext - Global edit mode state management
 * 
 * Manages the global edit mode state that controls whether users can
 * edit page layouts using the Bento Grid system. Every page in the studio
 * becomes editable when edit mode is active.
 */

import React, { createContext, useContext, useState, useCallback, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { PageStatus } from '@/types/bento';
import type { BentoPage, GridComponent } from '@/types/bento';
import { uiStudioAPI, type PageSaveRequest, type PagePublishRequest } from '@/services/uiStudioApiService';
import { toast } from 'sonner';

// ============================================================================
// Types
// ============================================================================

export interface EditModeState {
  /** Whether edit mode is currently active */
  isEditMode: boolean;
  /** Current page being edited */
  currentPage: BentoPage | null;
  /** Whether there are unsaved changes */
  hasUnsavedChanges: boolean;
  /** Current device view (desktop, tablet, mobile) */
  currentDevice: 'desktop' | 'tablet' | 'mobile';
  /** Whether the grid overlay is visible */
  showGrid: boolean;
  /** Currently selected component ID */
  selectedComponentId: string | null;
}

export interface EditModeActions {
  /** Toggle edit mode on/off */
  toggleEditMode: () => void;
  /** Set edit mode explicitly */
  setEditMode: (enabled: boolean) => void;
  /** Update current page */
  updatePage: (page: Partial<BentoPage>) => void;
  /** Mark changes as saved */
  markSaved: () => void;
  /** Change device view */
  setDevice: (device: 'desktop' | 'tablet' | 'mobile') => void;
  /** Toggle grid visibility */
  toggleGrid: () => void;
  /** Select a component */
  selectComponent: (componentId: string | null) => void;
  /** Add component to current page */
  addComponent: (componentType: string, position: { x: number; y: number; w: number; h: number }) => void;
  /** Move component */
  moveComponent: (componentId: string, newPosition: { x: number; y: number; w: number; h: number }) => void;
  /** Delete component */
  deleteComponent: (componentId: string) => void;
  /** Save current page */
  savePage: () => Promise<void>;
  /** Create new page */
  createPage: (pageData: Partial<BentoPage>) => Promise<void>;
  /** Publish current page */
  publishPage: (environment: 'development' | 'staging' | 'production', options?: Partial<PagePublishRequest['options']>) => Promise<void>;
  /** Load page from template */
  loadTemplate: (templateId: string, pageName: string, route: string) => Promise<void>;
  /** Get page versions */
  getPageVersions: () => Promise<any[]>;
}

export interface EditModeContextValue extends EditModeState, EditModeActions {}

// ============================================================================
// Context
// ============================================================================

const EditModeContext = createContext<EditModeContextValue | null>(null);

// ============================================================================
// Provider Component
// ============================================================================

interface EditModeProviderProps {
  children: React.ReactNode;
}

export const EditModeProvider: React.FC<EditModeProviderProps> = ({ children }) => {
  const location = useLocation();
  const navigate = useNavigate();

  // State
  const [state, setState] = useState<EditModeState>({
    isEditMode: false,
    currentPage: null,
    hasUnsavedChanges: false,
    currentDevice: 'desktop',
    showGrid: true,
    selectedComponentId: null
  });

  // Read edit mode from URL on mount and location changes
  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const editParam = params.get('edit');
    const isEditFromUrl = editParam === 'true';
    
    if (isEditFromUrl !== state.isEditMode) {
      setState(prev => ({ ...prev, isEditMode: isEditFromUrl }));
    }
  }, [location.search, state.isEditMode]);

  // Mock function to load page data (replace with actual API call)
  const loadPageForRoute = useCallback(async (route: string) => {
    // Mock page data - in real implementation, this would fetch from your backend
    const mockPage: BentoPage = {
      id: `page-${route.replace(/\//g, '-')}`,
      displayName: getPageNameFromRoute(route),
      route,
      layoutId: 'default',
      status: PageStatus.Draft,
      version: 1,
      bindings: {
        security: {
          isPublic: false,
          requiredRoles: [],
          requiredPermissions: []
        },
        visibility: {
          showInNavigation: true,
          navigationOrder: 0
        }
      },
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      createdBy: 'current-user',
      updatedBy: 'current-user'
    };

    setState(prev => ({ ...prev, currentPage: mockPage }));
  }, []);
  
  // Load page data when edit mode is enabled or route changes
  useEffect(() => {
    if (state.isEditMode) {
      loadPageForRoute(location.pathname);
    }
  }, [state.isEditMode, location.pathname, loadPageForRoute]);

  // Helper to get page name from route
  const getPageNameFromRoute = (route: string): string => {
    const routeMap: Record<string, string> = {
      '/': 'Dashboard',
      '/accounts': 'User Management',
      '/editor': 'Table Editor',
      '/schema': 'Schema Visualizer',
      '/bento': 'Bento Demo',
      '/settings': 'Settings'
    };
    return routeMap[route] || route.replace(/\//g, ' ').trim() || 'Home';
  };

  // Actions
  const toggleEditMode = useCallback(() => {
    const newEditMode = !state.isEditMode;
    const params = new URLSearchParams(location.search);
    
    if (newEditMode) {
      params.set('edit', 'true');
    } else {
      params.delete('edit');
    }
    
    const newSearch = params.toString();
    const newUrl = `${location.pathname}${newSearch ? `?${newSearch}` : ''}`;
    
    navigate(newUrl, { replace: true });
  }, [state.isEditMode, location.search, location.pathname, navigate]);

  const setEditMode = useCallback((enabled: boolean) => {
    if (enabled !== state.isEditMode) {
      toggleEditMode();
    }
  }, [state.isEditMode, toggleEditMode]);

  const updatePage = useCallback((updates: Partial<BentoPage>) => {
    if (!state.currentPage) return;
    
    const updatedPage = { ...state.currentPage, ...updates, updatedAt: new Date().toISOString() };
    setState(prev => ({ 
      ...prev, 
      currentPage: updatedPage,
      hasUnsavedChanges: true 
    }));
  }, [state.currentPage]);

  const markSaved = useCallback(() => {
    setState(prev => ({ ...prev, hasUnsavedChanges: false }));
  }, []);

  const setDevice = useCallback((device: 'desktop' | 'tablet' | 'mobile') => {
    setState(prev => ({ ...prev, currentDevice: device }));
  }, []);

  const toggleGrid = useCallback(() => {
    setState(prev => ({ ...prev, showGrid: !prev.showGrid }));
  }, []);

  const selectComponent = useCallback((componentId: string | null) => {
    setState(prev => ({ ...prev, selectedComponentId: componentId }));
  }, []);

  const addComponent = useCallback((componentType: string, position: { x: number; y: number; w: number; h: number }) => {
    if (!state.currentPage) return;

    const newComponent: GridComponent = {
      id: crypto.randomUUID(),
      componentType,
      position,
      props: {
        title: componentType.replace(/-/g, ' ').replace(/\b\w/g, l => l.toUpperCase()),
        value: '0',
        change: '+0%',
        trend: 'up'
      },
      bindings: {},
      display: {
        className: '',
        style: {}
      }
    };

    // This would update the grid for the current device
    // For now, just mark as having unsaved changes
    setState(prev => ({ ...prev, hasUnsavedChanges: true }));
    
    console.log('Adding component:', newComponent);
    
    // Trigger a custom event that the BentoGrid can listen to
    window.dispatchEvent(new CustomEvent('bento-add-component', { 
      detail: { component: newComponent, device: state.currentDevice } 
    }));
  }, [state.currentPage, state.currentDevice]);

  const moveComponent = useCallback((componentId: string, newPosition: { x: number; y: number; w: number; h: number }) => {
    setState(prev => ({ ...prev, hasUnsavedChanges: true }));
    console.log('Moving component:', componentId, newPosition);
  }, []);

  const deleteComponent = useCallback((componentId: string) => {
    setState(prev => ({ 
      ...prev, 
      hasUnsavedChanges: true,
      selectedComponentId: prev.selectedComponentId === componentId ? null : prev.selectedComponentId
    }));
    console.log('Deleting component:', componentId);
  }, []);

  const savePage = useCallback(async () => {
    if (!state.currentPage) return;
    
    try {
      // Prepare save request
      const saveRequest: PageSaveRequest = {
        page: state.currentPage,
        components: [], // Get components from current grid state
        bindings: [], // Get bindings from component configurations
        metadata: {
          comment: 'Auto-save',
          tags: ['auto-generated'],
          category: 'page'
        }
      };
      
      // Save via API
      const response = await uiStudioAPI.savePage(saveRequest);
      
      if (response.success && response.data) {
        // Update current page with saved version
        setState(prev => ({
          ...prev,
          currentPage: response.data!
        }));
        
        markSaved();
        toast.success('Page saved successfully');
      } else {
        throw new Error('Failed to save page');
      }
    } catch (error) {
      console.error('Failed to save page:', error);
      toast.error('Failed to save page');
      throw error;
    }
  }, [state.currentPage, markSaved]);

  const createPage = useCallback(async (pageData: Partial<BentoPage>) => {
    try {
      const newPage: BentoPage = {
        id: crypto.randomUUID(),
        displayName: pageData.displayName || 'New Page',
        route: pageData.route || '/new-page',
        layoutId: pageData.layoutId || 'default',
        status: PageStatus.Draft,
        version: 1,
        bindings: pageData.bindings || {
          security: {
            isPublic: false,
            requiredRoles: [],
            requiredPermissions: []
          },
          visibility: {
            showInNavigation: true,
            navigationOrder: 0
          }
        },
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        createdBy: 'current-user',
        updatedBy: 'current-user'
      };

      // Save the new page
      const saveRequest: PageSaveRequest = {
        page: newPage,
        components: [],
        bindings: [],
        metadata: {
          comment: 'Initial page creation',
          tags: ['new-page'],
          category: 'page'
        }
      };
      
      const response = await uiStudioAPI.savePage(saveRequest);
      
      if (response.success && response.data) {
        setState(prev => ({ ...prev, currentPage: response.data! }));
        toast.success('Page created successfully');
        
        // Navigate to the new page
        navigate(newPage.route);
      } else {
        throw new Error('Failed to create page');
      }
    } catch (error) {
      console.error('Failed to create page:', error);
      toast.error('Failed to create page');
      throw error;
    }
  }, [navigate]);

  // Publish page action
  const publishPage = useCallback(async (
    environment: 'development' | 'staging' | 'production',
    options: Partial<PagePublishRequest['options']> = {}
  ) => {
    if (!state.currentPage) return;
    
    try {
      // Ensure page is saved first
      if (state.hasUnsavedChanges) {
        await savePage();
      }
      
      const publishRequest: PagePublishRequest = {
        pageId: state.currentPage.id,
        environment,
        options: {
          makePublic: false,
          notifyUsers: false,
          generateSitemap: true,
          optimizeAssets: true,
          ...options
        }
      };
      
      const response = await uiStudioAPI.publishPage(publishRequest);
      
      if (response.success && response.data) {
        toast.success(`Page published to ${environment} successfully!`);
      } else {
        throw new Error('Failed to publish page');
      }
    } catch (error) {
      console.error('Failed to publish page:', error);
      toast.error('Failed to publish page');
      throw error;
    }
  }, [state.currentPage, state.hasUnsavedChanges, savePage]);
  
  // Load template action
  const loadTemplate = useCallback(async (templateId: string, pageName: string, route: string) => {
    try {
      const response = await uiStudioAPI.createPageFromTemplate(templateId, pageName, route);
      
      if (response.success && response.data) {
        setState(prev => ({ 
          ...prev, 
          currentPage: response.data!.page,
          hasUnsavedChanges: true
        }));
        
        toast.success('Template loaded successfully');
        navigate(route);
      } else {
        throw new Error('Failed to load template');
      }
    } catch (error) {
      console.error('Failed to load template:', error);
      toast.error('Failed to load template');
      throw error;
    }
  }, [navigate]);
  
  // Get page versions action
  const getPageVersions = useCallback(async () => {
    if (!state.currentPage) return [];
    
    try {
      const response = await uiStudioAPI.getPageVersions(state.currentPage.id);
      
      if (response.success && response.data) {
        return response.data;
      } else {
        throw new Error('Failed to get page versions');
      }
    } catch (error) {
      console.error('Failed to get page versions:', error);
      toast.error('Failed to get page versions');
      return [];
    }
  }, [state.currentPage]);

  // Context value
  const contextValue: EditModeContextValue = {
    ...state,
    toggleEditMode,
    setEditMode,
    updatePage,
    markSaved,
    setDevice,
    toggleGrid,
    selectComponent,
    addComponent,
    moveComponent,
    deleteComponent,
    savePage,
    createPage,
    publishPage,
    loadTemplate,
    getPageVersions
  };

  return (
    <EditModeContext.Provider value={contextValue}>
      {children}
    </EditModeContext.Provider>
  );
};

// ============================================================================
// Hook
// ============================================================================

// eslint-disable-next-line react-refresh/only-export-components
export const useEditMode = (): EditModeContextValue => {
  const context = useContext(EditModeContext);
  if (!context) {
    throw new Error('useEditMode must be used within an EditModeProvider');
  }
  return context;
};

export default EditModeContext;