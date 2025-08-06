/**
 * UIStudio Integration Examples
 * 
 * Demonstrates how to use the UIStudio API client and React Query hooks
 * for building Bento Grid components and page management.
 * 
 * @module UIStudioExamples
 */

import React, { useState } from 'react';
import { 
  useUIStudioPagesByOwner,
  useCreateUIStudioPage,
  useUpdateUIStudioPage,
  useUIStudioPageBindings,
  useCreateUIStudioBinding,
  useUpdateUIStudioLayoutGrid,
  useCreateUIStudioTemplate,
  useApplyUIStudioTemplate,
  useUIStudioVersionHistory,
  useCreateUIStudioVersionSnapshot,
  uistudioKeys
} from '../hooks/useUIStudio';
import { useQueryClient } from '@tanstack/react-query';
import type { 
  CreatePageRequest, 
  UpdatePageRequest,
  CreateBindingRequest,
  UIStudioLayout,
  CreateTemplateRequest,
  UIStudioPage,
  UIStudioComponentBinding,
  ApplyTemplateRequest,
  CreateVersionRequest 
} from '../types/uistudio';

// ============================================================================
// Page Management Example
// ============================================================================

interface PageManagerProps {
  userId: string;
}

export function PageManagerExample({ userId }: PageManagerProps) {
  const [selectedPageId, setSelectedPageId] = useState<string | null>(null);
  
  // Fetch user's pages
  const { 
    data: pages, 
    isLoading, 
    error,
    refetch 
  } = useUIStudioPagesByOwner(userId, {
    // Refetch when component mounts or user comes back to tab
    refetchOnMount: true,
    refetchOnWindowFocus: true,
  });

  // Create page mutation
  const createPageMutation = useCreateUIStudioPage({
    onSuccess: (data) => {
      console.log('Page created successfully:', data[0]);
      // Optionally select the newly created page
      if (data[0]) {
        setSelectedPageId(data[0].ownerEntityId);
      }
    },
    onError: (error) => {
      console.error('Failed to create page:', error.message);
    }
  });

  // Update page mutation
  const updatePageMutation = useUpdateUIStudioPage(selectedPageId!, {
    onSuccess: () => {
      console.log('Page updated successfully');
    }
  });

  const handleCreatePage = async () => {
    const pageData: CreatePageRequest = {
      pageName: 'New Dashboard',
      pageSlug: `dashboard-${Date.now()}`,
      pageType: 'dynamic',
      description: 'A new dashboard page',
      createdByEntityId: userId,
      metadata: {
        category: 'dashboard',
        version: '1.0'
      },
      tags: 'dashboard,analytics,overview'
    };

    await createPageMutation.mutateAsync(pageData);
  };

  const handleUpdatePage = async () => {
    if (!selectedPageId) return;

    const updateData: UpdatePageRequest = {
      pageName: 'Updated Dashboard',
      description: 'Updated dashboard description',
      updatedByEntityId: userId,
      metadata: {
        category: 'dashboard',
        version: '1.1',
        lastModified: new Date().toISOString()
      }
    };

    await updatePageMutation.mutateAsync(updateData);
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-8">
        <div className="animate-spin rounded-full h-sm w-sm border-b-2 border-blue-600"></div>
        <span className="ml-2">Loading pages...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
        <h3 className="text-red-800 font-medium">Error loading pages</h3>
        <p className="text-red-600 text-sm mt-1">{error.message}</p>
        <button 
          onClick={() => refetch()}
          className="mt-2 px-3 py-1 bg-red-600 text-white rounded text-sm hover:bg-red-700"
        >
          Retry
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h2 className="text-xl font-bold">My Pages</h2>
        <button
          onClick={handleCreatePage}
          disabled={createPageMutation.isPending}
          className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
        >
          {createPageMutation.isPending ? 'Creating...' : 'Create Page'}
        </button>
      </div>

      {pages && pages.length > 0 ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {(pages as UIStudioPage[])?.map((page: UIStudioPage) => (
            <div
              key={page.id}
              className={`p-4 border rounded-lg cursor-pointer transition-colors ${
                selectedPageId === page.ownerEntityId
                  ? 'border-blue-500 bg-blue-50'
                  : 'border-gray-200 hover:border-gray-300'
              }`}
              onClick={() => setSelectedPageId(page.ownerEntityId)}
            >
              <h3 className="font-medium">{page.pageName}</h3>
              <p className="text-sm text-gray-600 mt-1">{page.description}</p>
              <div className="flex justify-between items-center mt-3">
                <span className={`px-2 py-1 text-xs rounded ${
                  page.isPublished 
                    ? 'bg-green-100 text-green-800' 
                    : 'bg-yellow-xl0 text-yellow-800'
                }`}>
                  {page.isPublished ? 'Published' : 'Draft'}
                </span>
                <span className="text-xs text-gray-500">
                  {page.pageType}
                </span>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="text-center p-8 text-gray-500">
          <p>No pages found. Create your first page to get started.</p>
        </div>
      )}

      {selectedPageId && (
        <div className="mt-6 p-4 bg-gray-50 rounded-lg">
          <h3 className="font-medium mb-2">Page Actions</h3>
          <div className="space-x-2">
            <button
              onClick={handleUpdatePage}
              disabled={updatePageMutation.isPending}
              className="px-3 py-1 bg-green-600 text-white rounded text-sm hover:bg-green-700 disabled:opacity-50"
            >
              {updatePageMutation.isPending ? 'Updating...' : 'Update Page'}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

// ============================================================================
// Bento Grid Layout Editor Example
// ============================================================================

interface BentoGridEditorProps {
  pageEntityId: string;
  layoutEntityId: string;
  userId: string;
}

export function BentoGridEditorExample({ pageEntityId, layoutEntityId, userId }: BentoGridEditorProps) {
  const queryClient = useQueryClient();
  
  // Get page bindings
  const { 
    data: bindings, 
    isLoading: bindingsLoading 
  } = useUIStudioPageBindings(pageEntityId);

  // Layout grid update mutation
  const updateGridMutation = useUpdateUIStudioLayoutGrid(layoutEntityId, {
    onSuccess: () => {
      console.log('Grid layout updated successfully');
    }
  });

  // Create binding mutation
  const createBindingMutation = useCreateUIStudioBinding({
    onSuccess: () => {
      // Invalidate page bindings to refetch
      queryClient.invalidateQueries({ 
        queryKey: uistudioKeys.pageBindings(pageEntityId) 
      });
    }
  });

  const handleGridConfigChange = async (newConfig: NonNullable<UIStudioLayout['gridConfig']>) => {
    await updateGridMutation.mutateAsync(newConfig);
  };

  const handleAddComponent = async (componentType: string) => {
    const bindingData: CreateBindingRequest = {
      pageSlug: 'example-page', // In real usage, get this from page data
      componentType,
      componentInstanceId: `${componentType}-${Date.now()}`,
      boundComponentType: `${componentType}Component`,
      fieldMappings: {
        title: '$.name',
        description: '$.description',
        value: '$.value'
      },
      dataSourceConfig: {
        filters: [],
        sorting: [{ field: 'createdAt', direction: 'desc' }],
        pagination: { pageSize: 20, enabled: true }
      },
      positionConfig: {
        gridColumn: 'auto',
        gridRow: 'auto',
        minWidth: '200px',
        minHeight: '150px'
      },
      styleConfig: {
        theme: 'modern',
        borderRadius: '8px'
      },
      behaviorConfig: {
        sortable: true,
        filterable: true,
        selectable: 'single'
      },
      createdByEntityId: userId
    };

    await createBindingMutation.mutateAsync(bindingData);
  };

  if (bindingsLoading) {
    return (
      <div className="flex items-center justify-center p-8">
        <div className="animate-spin rounded-full h-sm w-sm border-b-2 border-blue-600"></div>
        <span className="ml-2">Loading components...</span>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h2 className="text-xl font-bold">Bento Grid Editor</h2>
        <div className="space-x-2">
          <button
            onClick={() => handleAddComponent('table')}
            disabled={createBindingMutation.isPending}
            className="px-3 py-1 bg-blue-600 text-white rounded text-sm hover:bg-blue-700 disabled:opacity-50"
          >
            Add Table
          </button>
          <button
            onClick={() => handleAddComponent('chart')}
            disabled={createBindingMutation.isPending}
            className="px-3 py-1 bg-green-600 text-white rounded text-sm hover:bg-green-700 disabled:opacity-50"
          >
            Add Chart
          </button>
          <button
            onClick={() => handleAddComponent('card')}
            disabled={createBindingMutation.isPending}
            className="px-3 py-1 bg-purple-600 text-white rounded text-sm hover:bg-purple-700 disabled:opacity-50"
          >
            Add Card
          </button>
        </div>
      </div>

      {/* Grid Configuration Panel */}
      <div className="p-4 bg-gray-50 rounded-lg">
        <h3 className="font-medium mb-3">Grid Configuration</h3>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div>
            <label className="block text-sm font-medium mb-1">Columns</label>
            <select
              onChange={(e) => handleGridConfigChange({
                columns: parseInt(e.target.value),
                gap: '16px',
                padding: '20px',
                minItemWidth: '200px'
              })}
              className="w-full px-3 py-1 border border-gray-300 rounded text-sm"
            >
              <option value="12">12 Columns</option>
              <option value="6">6 Columns</option>
              <option value="4">4 Columns</option>
              <option value="3">3 Columns</option>
            </select>
          </div>
          
          <div>
            <label className="block text-sm font-medium mb-1">Gap</label>
            <select
              onChange={(e) => handleGridConfigChange({
                columns: 12,
                gap: e.target.value,
                padding: '20px',
                minItemWidth: '200px'
              })}
              className="w-full px-3 py-1 border border-gray-300 rounded text-sm"
            >
              <option value="8px">Small (8px)</option>
              <option value="16px">Medium (16px)</option>
              <option value="24px">Large (24px)</option>
              <option value="32px">XL (32px)</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Padding</label>
            <select
              onChange={(e) => handleGridConfigChange({
                columns: 12,
                gap: '16px',
                padding: e.target.value,
                minItemWidth: '200px'
              })}
              className="w-full px-3 py-1 border border-gray-300 rounded text-sm"
            >
              <option value="0px">None</option>
              <option value="10px">Small (10px)</option>
              <option value="20px">Medium (20px)</option>
              <option value="30px">Large (30px)</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Min Width</label>
            <select
              onChange={(e) => handleGridConfigChange({
                columns: 12,
                gap: '16px',
                padding: '20px',
                minItemWidth: e.target.value
              })}
              className="w-full px-3 py-1 border border-gray-300 rounded text-sm"
            >
              <option value="150px">150px</option>
              <option value="200px">200px</option>
              <option value="250px">250px</option>
              <option value="300px">300px</option>
            </select>
          </div>
        </div>
      </div>

      {/* Components List */}
      <div className="p-4 border rounded-lg">
        <h3 className="font-medium mb-3">Page Components</h3>
        {bindings && bindings.length > 0 ? (
          <div className="grid gap-3 md:grid-cols-2 lg:grid-cols-3">
            {(bindings as UIStudioComponentBinding[])?.map((binding: UIStudioComponentBinding) => (
              <div
                key={binding.id}
                className="p-3 border border-gray-200 rounded-lg"
              >
                <div className="flex justify-between items-start">
                  <div>
                    <h4 className="font-medium text-sm">{binding.componentType}</h4>
                    <p className="text-xs text-gray-600 mt-1">
                      {binding.componentInstanceId}
                    </p>
                  </div>
                  <span className="px-2 py-1 bg-blue-100 text-blue-800 text-xs rounded">
                    {binding.boundComponentType}
                  </span>
                </div>
                
                {binding.positionConfig && (
                  <div className="mt-2 text-xs text-gray-500">
                    Position: {binding.positionConfig.gridColumn} / {binding.positionConfig.gridRow}
                  </div>
                )}
              </div>
            ))}
          </div>
        ) : (
          <div className="text-center p-8 text-gray-500">
            <p>No components added yet. Use the buttons above to add components.</p>
          </div>
        )}
      </div>

      {updateGridMutation.isPending && (
        <div className="fixed bottom-4 right-4 p-3 bg-blue-600 text-white rounded-lg shadow-lg">
          Updating grid layout...
        </div>
      )}
    </div>
  );
}

// ============================================================================
// Template Management Example
// ============================================================================

interface TemplateManagerProps {
  userId: string;
}

export function TemplateManagerExample({ userId }: TemplateManagerProps) {
  const [selectedTemplate, setSelectedTemplate] = useState<string | null>(null);

  // Create template mutation
  const createTemplateMutation = useCreateUIStudioTemplate({
    onSuccess: (data) => {
      console.log('Template created:', data[0]);
    }
  });

  // Apply template mutation
  const applyTemplateMutation = useApplyUIStudioTemplate(selectedTemplate!, {
    onSuccess: (data) => {
      console.log('Template applied, page created:', data[0]);
    }
  });

  const handleCreateTemplate = async () => {
    const templateData: CreateTemplateRequest = {
      templateName: 'Dashboard Template',
      description: 'Standard dashboard layout with metrics and charts',
      templateType: 'page',
      category: 'dashboards',
      templateData: {
        layout: { 
          type: 'grid', 
          columns: 12,
          gap: '16px'
        },
        components: [
          { 
            type: 'metrics', 
            position: { gridColumn: '1 / 7', gridRow: '1 / 2' },
            config: { theme: 'modern' }
          },
          { 
            type: 'chart', 
            position: { gridColumn: '7 / 13', gridRow: '1 / 3' },
            config: { chartType: 'line' }
          },
          { 
            type: 'table', 
            position: { gridColumn: '1 / 13', gridRow: '2 / 4' },
            config: { sortable: true, filterable: true }
          }
        ]
      },
      defaultValues: {
        title: 'New Dashboard',
        theme: 'light'
      },
      isPublic: false,
      tags: 'dashboard,metrics,charts,template',
      createdByEntityId: userId
    };

    await createTemplateMutation.mutateAsync(templateData);
  };

  const handleApplyTemplate = async () => {
    if (!selectedTemplate) return;

    const applyData: ApplyTemplateRequest = {
      pageName: 'Dashboard from Template',
      pageSlug: `dashboard-template-${Date.now()}`,
      createdByEntityId: userId
    };

    await applyTemplateMutation.mutateAsync(applyData);
  };

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h2 className="text-xl font-bold">Template Manager</h2>
        <button
          onClick={handleCreateTemplate}
          disabled={createTemplateMutation.isPending}
          className="px-4 py-2 bg-purple-600 text-white rounded hover:bg-purple-700 disabled:opacity-50"
        >
          {createTemplateMutation.isPending ? 'Creating...' : 'Create Template'}
        </button>
      </div>

      <div className="p-4 border rounded-lg">
        <h3 className="font-medium mb-3">Template Actions</h3>
        <div className="space-y-3">
          <div>
            <label className="block text-sm font-medium mb-1">Select Template ID</label>
            <input
              type="text"
              value={selectedTemplate || ''}
              onChange={(e) => setSelectedTemplate(e.target.value)}
              placeholder="Enter template entity ID"
              className="w-full px-3 py-2 border border-gray-300 rounded"
            />
          </div>
          
          <button
            onClick={handleApplyTemplate}
            disabled={!selectedTemplate || applyTemplateMutation.isPending}
            className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
          >
            {applyTemplateMutation.isPending ? 'Applying...' : 'Apply Template'}
          </button>
        </div>
      </div>
    </div>
  );
}

// ============================================================================
// Version Control Example
// ============================================================================

interface VersionControlProps {
  resourceId: string;
  userId: string;
}

export function VersionControlExample({ resourceId, userId }: VersionControlProps) {
  // Get version history
  const { 
    data: versions, 
    isLoading 
  } = useUIStudioVersionHistory(resourceId, { limit: 10 });

  // Create snapshot mutation
  const createSnapshotMutation = useCreateUIStudioVersionSnapshot({
    onSuccess: () => {
      console.log('Version snapshot created');
    }
  });

  const handleCreateSnapshot = async () => {
    const versionData: CreateVersionRequest = {
      resourceEntityId: resourceId,
      resourceType: 'page',
      versionLabel: `v${Date.now()}`,
      changeDescription: 'Manual snapshot created',
      changeReason: 'User requested backup',
      createdByEntityId: userId
    };

    await createSnapshotMutation.mutateAsync(versionData);
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-4">
        <div className="animate-spin rounded-full h-xs w-xs border-b-2 border-blue-600"></div>
        <span className="ml-2">Loading versions...</span>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h3 className="font-medium">Version History</h3>
        <button
          onClick={handleCreateSnapshot}
          disabled={createSnapshotMutation.isPending}
          className="px-3 py-1 bg-blue-600 text-white rounded text-sm hover:bg-blue-700 disabled:opacity-50"
        >
          {createSnapshotMutation.isPending ? 'Creating...' : 'Create Snapshot'}
        </button>
      </div>

      {versions && versions.length > 0 ? (
        <div className="space-y-2 max-h-412 overflow-y-auto">
          {versions.map((version) => (
            <div
              key={version.id}
              className="p-3 border border-gray-200 rounded-lg"
            >
              <div className="flex justify-between items-start">
                <div>
                  <div className="font-medium text-sm">{version.versionLabel}</div>
                  <div className="text-xs text-gray-600 mt-1">
                    {version.changeDescription}
                  </div>
                  <div className="text-xs text-gray-500 mt-1">
                    {new Date(version.lastUpdated).toLocaleString()}
                  </div>
                </div>
                
                {version.isPublished && (
                  <span className="px-2 py-1 bg-green-100 text-green-800 text-xs rounded">
                    Published
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="text-center p-4 text-gray-500 text-sm">
          No version history available
        </div>
      )}
    </div>
  );
}

// ============================================================================
// Complete Example App
// ============================================================================

export function UIStudioCompleteExample() {
  const [activeTab, setActiveTab] = useState<'pages' | 'editor' | 'templates' | 'versions'>('pages');
  const [selectedPageId, setSelectedPageId] = useState<string>('');
  const [selectedLayoutId, setSelectedLayoutId] = useState<string>('');
  const userId = 'example-user-123'; // In real app, get from auth context

  const tabs = [
    { id: 'pages', label: 'Pages' },
    { id: 'editor', label: 'Bento Editor' },
    { id: 'templates', label: 'Templates' },
    { id: 'versions', label: 'Versions' }
  ] as const;

  return (
    <div className="max-w-mdxl mx-auto p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">
          UIStudio Integration Examples
        </h1>
        <p className="text-gray-600">
          Comprehensive examples of UIStudio API client usage with React Query
        </p>
      </div>

      {/* Tab Navigation */}
      <div className="border-b border-gray-200 mb-6">
        <nav className="-mb-px flex space-x-8">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`py-2 px-1 border-b-2 font-medium text-sm ${
                activeTab === tab.id
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </nav>
      </div>

      {/* Tab Content */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        {activeTab === 'pages' && (
          <PageManagerExample userId={userId} />
        )}
        
        {activeTab === 'editor' && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium mb-1">Page Entity ID</label>
                <input
                  type="text"
                  value={selectedPageId}
                  onChange={(e) => setSelectedPageId(e.target.value)}
                  placeholder="Enter page entity ID"
                  className="w-full px-3 py-2 border border-gray-300 rounded"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Layout Entity ID</label>
                <input
                  type="text"
                  value={selectedLayoutId}
                  onChange={(e) => setSelectedLayoutId(e.target.value)}
                  placeholder="Enter layout entity ID"
                  className="w-full px-3 py-2 border border-gray-300 rounded"
                />
              </div>
            </div>
            
            {selectedPageId && selectedLayoutId ? (
              <BentoGridEditorExample 
                pageEntityId={selectedPageId}
                layoutEntityId={selectedLayoutId}
                userId={userId}
              />
            ) : (
              <div className="text-center p-8 text-gray-500">
                Enter page and layout entity IDs to use the Bento Grid Editor
              </div>
            )}
          </div>
        )}
        
        {activeTab === 'templates' && (
          <TemplateManagerExample userId={userId} />
        )}
        
        {activeTab === 'versions' && (
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-1">Resource ID for Version History</label>
              <input
                type="text"
                value={selectedPageId}
                onChange={(e) => setSelectedPageId(e.target.value)}
                placeholder="Enter resource entity ID"
                className="w-full px-3 py-2 border border-gray-300 rounded"
              />
            </div>
            
            {selectedPageId ? (
              <VersionControlExample 
                resourceId={selectedPageId}
                userId={userId}
              />
            ) : (
              <div className="text-center p-8 text-gray-500">
                Enter a resource entity ID to view version history
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}