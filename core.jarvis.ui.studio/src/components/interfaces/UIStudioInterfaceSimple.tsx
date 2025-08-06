import React, { useState, useCallback } from 'react';
import { Button } from '../ui/button';
import { Alert, AlertDescription } from '../ui/alert';
import { Plus, Save, Check, AlertCircle, CheckCircle2, X, Loader2 } from 'lucide-react';
import { LucideIcon as Icon } from '../ui/icon';
import { useCreateUIStudioPage } from '../../hooks/useUIStudio';

export interface UIStudioInterfaceSimpleProps {
  userEntityId?: string;
}

export const UIStudioInterfaceSimple: React.FC<UIStudioInterfaceSimpleProps> = ({ userEntityId }) => {
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [pageName, setPageName] = useState('');
  const [pageUrl, setPageUrl] = useState('');
  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<{pageName?: string, pageUrl?: string}>({});
  const [createdPages, setCreatedPages] = useState<Array<{id: string, name: string, url: string, createdAt: string}>>([]);
  
  const createPage = useCreateUIStudioPage();

  const handleCreatePage = useCallback(() => {
    console.log('Create page clicked');
    setShowCreateModal(true);
    setPageName('');
    setPageUrl('');
    setError(null);
    setSuccess(null);
    setValidationErrors({});
  }, []);

  const handlePageNameChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    console.log('Page name changed to:', value);
    setPageName(value);
    
    // Clear validation errors as user types
    if (validationErrors.pageName && value.trim()) {
      setValidationErrors(prev => ({ ...prev, pageName: undefined }));
    }
    
    // Clear general error when user makes changes
    if (error) {
      setError(null);
    }
  }, [validationErrors.pageName, error]);

  const handlePageUrlChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    let value = e.target.value;
    
    // Auto-format URL (ensure it starts with /)
    if (value && !value.startsWith('/')) {
      value = '/' + value;
    }
    
    console.log('Page URL changed to:', value);
    setPageUrl(value);
    
    // Clear validation errors as user types
    if (validationErrors.pageUrl && value.trim()) {
      setValidationErrors(prev => ({ ...prev, pageUrl: undefined }));
    }
    
    // Clear general error when user makes changes
    if (error) {
      setError(null);
    }
  }, [validationErrors.pageUrl, error]);

  const handleCloseModal = useCallback(() => {
    if (isCreating) {
      // Don't allow closing while creating
      return;
    }
    
    setShowCreateModal(false);
    setPageName('');
    setPageUrl('');
    setError(null);
    setSuccess(null);
    setValidationErrors({});
  }, [isCreating]);

  const validateInputs = useCallback(() => {
    const errors: {pageName?: string, pageUrl?: string} = {};
    
    if (!pageName.trim()) {
      errors.pageName = 'Page name is required';
    } else if (pageName.trim().length < 2) {
      errors.pageName = 'Page name must be at least 2 characters';
    }
    
    if (!pageUrl.trim()) {
      errors.pageUrl = 'Page URL is required';
    } else if (!pageUrl.startsWith('/')) {
      errors.pageUrl = 'Page URL must start with /';
    } else if (pageUrl.length < 2) {
      errors.pageUrl = 'Page URL must be at least 2 characters';
    } else if (!/^\/[a-zA-Z0-9\-_\/]*$/.test(pageUrl)) {
      errors.pageUrl = 'Page URL can only contain letters, numbers, hyphens, underscores, and slashes';
    }
    
    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  }, [pageName, pageUrl]);

  const handleConfirmCreate = useCallback(async () => {
    // Clear previous states
    setError(null);
    setSuccess(null);
    
    // Validate inputs
    if (!validateInputs()) {
      setError('Please fix the validation errors above');
      return;
    }

    console.log('Creating new page...', { pageName, pageUrl });
    setIsCreating(true);
    
    try {
      // Create page using UIStudio API
      const newPage = await createPage.mutateAsync({
        pageName: pageName.trim(),
        pageSlug: pageUrl.trim(),
        pageType: 'static',
        description: `Page created via UIStudio: ${pageName.trim()}`,
        createdByEntityId: userEntityId || 'temp-user-id',
        metadata: {
          createdVia: 'UIStudio',
          version: '1.0.0'
        },
        tags: 'uistudio,dashboard'
      });

      console.log('✅ Page created successfully:', newPage);
      
      // Add to local state for display
      const pageRecord = {
        id: (newPage as any)?.id || `temp-${Date.now()}`,
        name: pageName.trim(),
        url: pageUrl.trim(),
        createdAt: new Date().toISOString()
      };
      setCreatedPages(prev => [pageRecord, ...prev]);
      
      // Show success message
      setSuccess(`Page "${pageName.trim()}" created successfully!`);
      
      // Close modal after short delay to show success
      setTimeout(() => {
        setShowCreateModal(false);
        setPageName('');
        setPageUrl('');
        setError(null);
        setSuccess(null);
        setValidationErrors({});
      }, 1500);
      
    } catch (error: any) {
      console.error('❌ Failed to create page:', error);
      
      // Extract meaningful error message
      let errorMessage = 'Failed to create page. Please try again.';
      
      if (error?.response?.data?.message) {
        errorMessage = error.response.data.message;
      } else if (error?.message) {
        errorMessage = error.message;
      } else if (typeof error === 'string') {
        errorMessage = error;
      }
      
      // Handle specific error cases
      if (errorMessage.toLowerCase().includes('duplicate') || errorMessage.toLowerCase().includes('already exists')) {
        errorMessage = 'A page with this name or URL already exists. Please choose different values.';
      } else if (errorMessage.toLowerCase().includes('network') || errorMessage.toLowerCase().includes('connection')) {
        errorMessage = 'Network error. Please check your connection and try again.';
      } else if (errorMessage.toLowerCase().includes('unauthorized') || errorMessage.toLowerCase().includes('permission')) {
        errorMessage = 'You do not have permission to create pages. Please contact your administrator.';
      }
      
      setError(errorMessage);
    } finally {
      setIsCreating(false);
    }
  }, [pageName, pageUrl, validateInputs, createPage, userEntityId]);

  return (
    <div className="flex flex-col h-screen bg-background">
      {/* Header */}
      <header className="border-b">
        <div className="flex items-center justify-between px-6 py-4">
          <h1 className="text-2xl font-bold">UIStudio</h1>
          <Button onClick={handleCreatePage} className="gap-2">
            <Icon icon={Plus} size="sm" />
            Create New Page
          </Button>
        </div>
      </header>

      {/* Main Content */}
      <main className="flex-1 p-6">
        <div className="max-w-4xl mx-auto">
          <div className="text-center py-12">
            <h2 className="text-xl font-semibold mb-4">Welcome to UIStudio</h2>
            <p className="text-muted-foreground mb-8">
              Create beautiful pages with our visual editor
            </p>
            <Button onClick={handleCreatePage} size="lg" className="gap-2">
              <Icon icon={Plus} size="md" />
              Create Your First Page
            </Button>
          </div>

          {/* Created Pages */}
          <div className="mt-12">
            <h3 className="text-lg font-semibold mb-4">Created Pages</h3>
            {createdPages.length > 0 ? (
              <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                {createdPages.map((page) => (
                  <div key={page.id} className="border rounded-lg p-4 bg-green-50 border-green-200">
                    <div className="h-232 bg-green-100 rounded mb-3 flex items-center justify-center">
                      <Icon icon={Check} size="xl" className="text-green-600" />
                    </div>
                    <h4 className="font-medium mb-1 text-green-800">{page.name}</h4>
                    <p className="text-sm text-green-600 mb-1">{page.url}</p>
                    <p className="text-sm text-muted-foreground">
                      Created {new Date(page.createdAt).toLocaleTimeString()}
                    </p>
                  </div>
                ))}
              </div>
            ) : (
              <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                {/* Placeholder cards */}
                {[1, 2, 3].map((i) => (
                  <div key={i} className="border rounded-lg p-4">
                    <div className="h-232 bg-muted rounded mb-3"></div>
                    <h4 className="font-medium mb-1">Page {i}</h4>
                    <p className="text-sm text-muted-foreground">Last edited 2 hours ago</p>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </main>

      {/* Simple Modal */}
      {showCreateModal && (
        <>
          {/* Modal Backdrop */}
          <div 
            className="fixed inset-0 bg-black/50 z-40 transition-opacity" 
            onClick={handleCloseModal}
            aria-label="Close modal"
          />
          <div className="fixed inset-0 flex items-center justify-center z-50 p-4">
            <div 
              className="bg-background border rounded-lg p-6 max-w-md w-full shadow-xl transform transition-all"
              role="dialog"
              aria-labelledby="modal-title"
              aria-describedby="modal-description"
            >
              <div className="flex items-center justify-between mb-4">
                <h2 id="modal-title" className="text-xl font-semibold">Create New Page</h2>
                {!isCreating && (
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={handleCloseModal}
                    className="h-md w-md rounded-md hover:bg-gray-100 dark:hover:bg-gray-800"
                    aria-label="Close modal"
                  >
                    <Icon icon={X} size="sm" />
                  </Button>
                )}
              </div>
              <p id="modal-description" className="text-sm text-muted-foreground mb-4">
                Create a new page for your UIStudio dashboard. Fill in the required information below.
              </p>
              {/* Error Alert */}
              {error && (
                <Alert variant="destructive" className="mb-4">
                  <Icon icon={AlertCircle} size="sm" />
                  <AlertDescription>{error}</AlertDescription>
                </Alert>
              )}
              
              {/* Success Alert */}
              {success && (
                <Alert className="mb-4 border-green-200 bg-green-50 text-green-800">
                  <Icon icon={CheckCircle2} size="sm" />
                  <AlertDescription>{success}</AlertDescription>
                </Alert>
              )}
              
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium mb-2 text-foreground">
                    Page Name <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    className={`w-full px-3 py-2 border rounded-md transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed ${
                      validationErrors.pageName 
                        ? 'border-red-500 bg-red-50 text-red-900 placeholder-red-400 focus:ring-red-500 focus:border-red-500' 
                        : 'border-gray-300 bg-white text-gray-900 placeholder-gray-500 dark:border-gray-600 dark:bg-gray-700 dark:text-white dark:placeholder-gray-400'
                    }`}
                    placeholder="Enter page name (e.g., My Dashboard)"
                    value={pageName}
                    onChange={handlePageNameChange}
                    disabled={isCreating}
                    autoFocus
                  />
                  {validationErrors.pageName && (
                    <p className="mt-1 text-sm text-red-600">{validationErrors.pageName}</p>
                  )}
                </div>
                <div>
                  <label className="block text-sm font-medium mb-2 text-foreground">
                    Page URL <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    className={`w-full px-3 py-2 border rounded-md transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed ${
                      validationErrors.pageUrl 
                        ? 'border-red-500 bg-red-50 text-red-900 placeholder-red-400 focus:ring-red-500 focus:border-red-500' 
                        : 'border-gray-300 bg-white text-gray-900 placeholder-gray-500 dark:border-gray-600 dark:bg-gray-700 dark:text-white dark:placeholder-gray-400'
                    }`}
                    placeholder="/my-dashboard"
                    value={pageUrl}
                    onChange={handlePageUrlChange}
                    disabled={isCreating}
                  />
                  {validationErrors.pageUrl && (
                    <p className="mt-1 text-sm text-red-600">{validationErrors.pageUrl}</p>
                  )}
                  <p className="mt-1 text-xs text-gray-500">
                    Must start with / and contain only letters, numbers, hyphens, underscores, and slashes
                  </p>
                </div>
              </div>
              <div className="flex gap-3 mt-6">
                <Button 
                  variant="outline" 
                  onClick={handleCloseModal} 
                  className="flex-1"
                  disabled={isCreating}
                >
                  {isCreating ? 'Creating...' : 'Cancel'}
                </Button>
                <Button 
                  onClick={handleConfirmCreate} 
                  className={`flex-1 gap-2 transition-all ${
                    success ? 'bg-green-600 hover:bg-green-700' : ''
                  }`}
                  disabled={isCreating || !pageName.trim() || !pageUrl.trim()}
                >
                  {isCreating ? (
                    <>
                      <Icon icon={Loader2} size="sm" className="animate-spin" />
                      Creating Page...
                    </>
                  ) : success ? (
                    <>
                      <Icon icon={CheckCircle2} size="sm" />
                      Created Successfully!
                    </>
                  ) : (
                    <>
                      <Icon icon={Save} size="sm" />
                      Create & Save Page
                    </>
                  )}
                </Button>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
};