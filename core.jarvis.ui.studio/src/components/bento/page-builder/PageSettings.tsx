/**
 * PageSettings - Page configuration panel
 * 
 * Provides a comprehensive interface for configuring page properties,
 * security settings, navigation options, and metadata.
 */

import React, { useState, useCallback, useMemo } from 'react';
import {
  Shield,
  Navigation,
  Tag,
  Settings,
  AlertCircle,
  X,
  Plus
} from 'lucide-react';

import type { BentoPage, PageStatus } from '@/types/bento';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Switch } from '@/components/ui/switch';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

// ============================================================================
// Types
// ============================================================================

export interface PageSettingsProps {
  /** Current page configuration */
  page: BentoPage;
  /** Called when page settings are updated */
  onUpdate?: (updates: Partial<BentoPage>) => void;
  /** Whether the settings are read-only */
  readOnly?: boolean;
  /** Whether to render in compact mobile mode */
  compact?: boolean;
  /** Additional CSS classes */
  className?: string;
}

interface ValidationError {
  field: string;
  message: string;
}

// ============================================================================
// Constants
// ============================================================================

const AVAILABLE_ROLES = [
  { id: 'user', name: 'User', description: 'Basic user access' },
  { id: 'admin', name: 'Admin', description: 'Administrative access' },
  { id: 'super-admin', name: 'Super Admin', description: 'Full system access' },
  { id: 'analyst', name: 'Analyst', description: 'Data analysis access' },
  { id: 'editor', name: 'Editor', description: 'Content editing access' },
  { id: 'viewer', name: 'Viewer', description: 'View-only access' }
];

const AVAILABLE_PERMISSIONS = [
  { id: 'view-dashboard', name: 'View Dashboard', category: 'General' },
  { id: 'manage-users', name: 'Manage Users', category: 'Administration' },
  { id: 'view-analytics', name: 'View Analytics', category: 'Data' },
  { id: 'export-data', name: 'Export Data', category: 'Data' },
  { id: 'manage-settings', name: 'Manage Settings', category: 'Administration' },
  { id: 'view-logs', name: 'View Logs', category: 'Monitoring' },
  { id: 'manage-integrations', name: 'Manage Integrations', category: 'Administration' }
];

const PAGE_STATUSES: { value: PageStatus; label: string; description: string }[] = [
  {
    value: 'draft' as PageStatus,
    label: 'Draft',
    description: 'Page is being worked on and not visible to users'
  },
  {
    value: 'published' as PageStatus,
    label: 'Published',
    description: 'Page is live and accessible to users'
  },
  {
    value: 'archived' as PageStatus,
    label: 'Archived',
    description: 'Page is hidden but preserved for historical reference'
  },
  {
    value: 'scheduled' as PageStatus,
    label: 'Scheduled',
    description: 'Page is scheduled to be published at a future date'
  }
];

// ============================================================================
// Validation Functions
// ============================================================================

const validatePageSettings = (page: BentoPage): ValidationError[] => {
  const errors: ValidationError[] = [];

  // Display name validation
  if (!page.displayName?.trim()) {
    errors.push({ field: 'displayName', message: 'Display name is required' });
  } else if (page.displayName.length < 3) {
    errors.push({ field: 'displayName', message: 'Display name must be at least 3 characters' });
  }

  // Route validation
  if (!page.route?.trim()) {
    errors.push({ field: 'route', message: 'Route is required' });
  } else {
    if (!page.route.startsWith('/')) {
      errors.push({ field: 'route', message: 'Route must start with /' });
    }
    if (!/^\/[a-z0-9-/]*$/.test(page.route)) {
      errors.push({ field: 'route', message: 'Route can only contain lowercase letters, numbers, hyphens, and slashes' });
    }
  }

  return errors;
};

// ============================================================================
// Main Component
// ============================================================================

export const PageSettings: React.FC<PageSettingsProps> = ({
  page,
  onUpdate,
  readOnly = false,
  className
}) => {
  const [localPage, setLocalPage] = useState<BentoPage>(page);
  const [newTag, setNewTag] = useState('');
  const [newPermission, setNewPermission] = useState('');

  // Validation
  const validationErrors = useMemo(() => {
    return validatePageSettings(localPage);
  }, [localPage]);

  const hasErrors = validationErrors.length > 0;

  // Handle page updates
  const handlePageUpdate = useCallback((updates: Partial<BentoPage>) => {
    const updatedPage = { ...localPage, ...updates };
    setLocalPage(updatedPage);
    onUpdate?.(updates);
  }, [localPage, onUpdate]);

  // Handle nested binding updates
  const handleSecurityUpdate = useCallback((updates: Partial<BentoPage['bindings']['security']>) => {
    const updatedBindings = {
      ...localPage.bindings,
      security: { ...localPage.bindings.security, ...updates }
    };
    handlePageUpdate({ bindings: updatedBindings });
  }, [localPage.bindings, handlePageUpdate]);

  const handleVisibilityUpdate = useCallback((updates: Partial<BentoPage['bindings']['visibility']>) => {
    const updatedBindings = {
      ...localPage.bindings,
      visibility: { ...localPage.bindings.visibility, ...updates }
    };
    handlePageUpdate({ bindings: updatedBindings });
  }, [localPage.bindings, handlePageUpdate]);

  // Handle role toggles
  const handleRoleToggle = useCallback((roleId: string, checked: boolean) => {
    const currentRoles = localPage.bindings.security.requiredRoles || [];
    const updatedRoles = checked
      ? [...currentRoles, roleId]
      : currentRoles.filter(role => role !== roleId);
    
    handleSecurityUpdate({ requiredRoles: updatedRoles });
  }, [localPage.bindings.security.requiredRoles, handleSecurityUpdate]);

  // Handle permission management
  const handleAddPermission = useCallback(() => {
    if (!newPermission.trim()) return;
    
    const currentPermissions = localPage.bindings.security.requiredPermissions || [];
    if (!currentPermissions.includes(newPermission)) {
      handleSecurityUpdate({ 
        requiredPermissions: [...currentPermissions, newPermission] 
      });
    }
    setNewPermission('');
  }, [newPermission, localPage.bindings.security.requiredPermissions, handleSecurityUpdate]);

  const handleRemovePermission = useCallback((permission: string) => {
    const currentPermissions = localPage.bindings.security.requiredPermissions || [];
    handleSecurityUpdate({ 
      requiredPermissions: currentPermissions.filter(p => p !== permission) 
    });
  }, [localPage.bindings.security.requiredPermissions, handleSecurityUpdate]);

  // Handle tag management
  const handleAddTag = useCallback(() => {
    if (!newTag.trim()) return;
    
    const currentTags = localPage.tags || [];
    if (!currentTags.includes(newTag.trim())) {
      handlePageUpdate({ tags: [...currentTags, newTag.trim()] });
    }
    setNewTag('');
  }, [newTag, localPage.tags, handlePageUpdate]);

  const handleRemoveTag = useCallback((tag: string) => {
    const currentTags = localPage.tags || [];
    handlePageUpdate({ tags: currentTags.filter(t => t !== tag) });
  }, [localPage.tags, handlePageUpdate]);

  const getFieldError = useCallback((field: string) => {
    return validationErrors.find(error => error.field === field);
  }, [validationErrors]);

  return (
    <div className={cn('page-settings space-y-6', className)}>
      {/* Validation Summary */}
      {hasErrors && (
        <Card className="border-destructive">
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-destructive">
              <AlertCircle className="h-xs w-xs" />
              Configuration Issues
            </CardTitle>
          </CardHeader>
          <CardContent className="pt-0">
            <ul className="text-sm space-y-1">
              {validationErrors.map((error, index) => (
                <li key={index} className="text-destructive">
                  • {error.message}
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      )}

      {/* Basic Information */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Settings className="h-xs w-xs" />
            Basic Information
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="displayName">
                Display Name <span className="text-destructive">*</span>
              </Label>
              <Input
                id="displayName"
                value={localPage.displayName}
                onChange={(e) => handlePageUpdate({ displayName: e.target.value })}
                disabled={readOnly}
                className={getFieldError('displayName') ? 'border-destructive' : ''}
              />
              {getFieldError('displayName') && (
                <p className="text-xs text-destructive">
                  {getFieldError('displayName')?.message}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="route">
                Route <span className="text-destructive">*</span>
              </Label>
              <Input
                id="route"
                value={localPage.route}
                onChange={(e) => handlePageUpdate({ route: e.target.value })}
                disabled={readOnly}
                placeholder="/example-page"
                className={getFieldError('route') ? 'border-destructive' : ''}
              />
              {getFieldError('route') && (
                <p className="text-xs text-destructive">
                  {getFieldError('route')?.message}
                </p>
              )}
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="description">Description</Label>
            <Textarea
              id="description"
              value={localPage.description || ''}
              onChange={(e) => handlePageUpdate({ description: e.target.value })}
              disabled={readOnly}
              placeholder="Brief description of the page..."
              rows={3}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="status">Status</Label>
            <Select
              value={localPage.status}
              onValueChange={(value) => handlePageUpdate({ status: value as PageStatus })}
              disabled={readOnly}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {PAGE_STATUSES.map((status) => (
                  <SelectItem key={status.value} value={status.value}>
                    <div>
                      <div className="font-medium">{status.label}</div>
                      <div className="text-xs text-muted-foreground">
                        {status.description}
                      </div>
                    </div>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Security Settings */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Shield className="h-xs w-xs" />
            Security Settings
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Public Access */}
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label className="text-base">Public Access</Label>
              <p className="text-sm text-muted-foreground">
                Allow access without authentication
              </p>
            </div>
            <Switch
              checked={localPage.bindings.security.isPublic}
              onCheckedChange={(checked) => handleSecurityUpdate({ isPublic: checked })}
              disabled={readOnly}
            />
          </div>

          {/* Role-Based Access */}
          {!localPage.bindings.security.isPublic && (
            <>
              <Separator />
              <div className="space-y-3">
                <Label className="text-base">Required Roles</Label>
                <div className="grid grid-cols-2 gap-2">
                  {AVAILABLE_ROLES.map((role) => (
                    <div key={role.id} className="flex items-center space-x-2">
                      <Switch
                        id={`role-${role.id}`}
                        checked={localPage.bindings.security.requiredRoles?.includes(role.id) || false}
                        onCheckedChange={(checked) => handleRoleToggle(role.id, checked)}
                        disabled={readOnly}
                      />
                      <Label htmlFor={`role-${role.id}`} className="text-sm">
                        {role.name}
                      </Label>
                    </div>
                  ))}
                </div>
              </div>

              {/* Permissions */}
              <Separator />
              <div className="space-y-3">
                <Label className="text-base">Required Permissions</Label>
                
                {/* Current Permissions */}
                {localPage.bindings.security.requiredPermissions && 
                 localPage.bindings.security.requiredPermissions.length > 0 && (
                  <div className="flex flex-wrap gap-2 mb-3">
                    {localPage.bindings.security.requiredPermissions.map((permission) => (
                      <Badge key={permission} variant="secondary" className="flex items-center gap-1">
                        {permission}
                        {!readOnly && (
                          <Button
                            variant="ghost"
                            size="sm"
                            className="h-auto p-0 ml-1"
                            onClick={() => handleRemovePermission(permission)}
                          >
                            <X className="h-2xs w-2xs" />
                          </Button>
                        )}
                      </Badge>
                    ))}
                  </div>
                )}

                {/* Add Permission */}
                {!readOnly && (
                  <div className="flex gap-2">
                    <Select value={newPermission} onValueChange={setNewPermission}>
                      <SelectTrigger className="flex-1">
                        <SelectValue placeholder="Select permission..." />
                      </SelectTrigger>
                      <SelectContent>
                        {AVAILABLE_PERMISSIONS.map((permission) => (
                          <SelectItem key={permission.id} value={permission.id}>
                            <div>
                              <div className="font-medium">{permission.name}</div>
                              <div className="text-xs text-muted-foreground">
                                {permission.category}
                              </div>
                            </div>
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <Button onClick={handleAddPermission} disabled={!newPermission}>
                      <Plus className="h-xs w-xs" />
                    </Button>
                  </div>
                )}
              </div>
            </>
          )}
        </CardContent>
      </Card>

      {/* Navigation Settings */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Navigation className="h-xs w-xs" />
            Navigation
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label className="text-base">Show in Navigation</Label>
              <p className="text-sm text-muted-foreground">
                Display this page in the main navigation menu
              </p>
            </div>
            <Switch
              checked={localPage.bindings.visibility.showInNavigation}
              onCheckedChange={(checked) => handleVisibilityUpdate({ showInNavigation: checked })}
              disabled={readOnly}
            />
          </div>

          {localPage.bindings.visibility.showInNavigation && (
            <>
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="navigationIcon">Navigation Icon</Label>
                  <Input
                    id="navigationIcon"
                    value={localPage.bindings.visibility.icon || ''}
                    onChange={(e) => handleVisibilityUpdate({ icon: e.target.value })}
                    disabled={readOnly}
                    placeholder="📊"
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="navigationOrder">Display Order</Label>
                  <Input
                    id="navigationOrder"
                    type="number"
                    value={localPage.bindings.visibility.navigationOrder || 0}
                    onChange={(e) => handleVisibilityUpdate({ navigationOrder: parseInt(e.target.value) || 0 })}
                    disabled={readOnly}
                    min="0"
                  />
                </div>
              </div>
            </>
          )}
        </CardContent>
      </Card>

      {/* Tags */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Tag className="h-xs w-xs" />
            Tags
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          {/* Current Tags */}
          {localPage.tags && localPage.tags.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {localPage.tags.map((tag) => (
                <Badge key={tag} variant="outline" className="flex items-center gap-1">
                  {tag}
                  {!readOnly && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-auto p-0 ml-1"
                      onClick={() => handleRemoveTag(tag)}
                    >
                      <X className="h-2xs w-2xs" />
                    </Button>
                  )}
                </Badge>
              ))}
            </div>
          )}

          {/* Add Tag */}
          {!readOnly && (
            <div className="flex gap-2">
              <Input
                placeholder="Add tag..."
                value={newTag}
                onChange={(e) => setNewTag(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    e.preventDefault();
                    handleAddTag();
                  }
                }}
              />
              <Button onClick={handleAddTag} disabled={!newTag.trim()}>
                <Plus className="h-xs w-xs" />
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Metadata */}
      <Card>
        <CardHeader>
          <CardTitle>Metadata</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2 text-sm text-muted-foreground">
          <div className="flex justify-between">
            <span>Page ID:</span>
            <span className="font-mono">{localPage.id}</span>
          </div>
          <div className="flex justify-between">
            <span>Version:</span>
            <span>{localPage.version}</span>
          </div>
          <div className="flex justify-between">
            <span>Created:</span>
            <span>{new Date(localPage.createdAt).toLocaleString()}</span>
          </div>
          <div className="flex justify-between">
            <span>Updated:</span>
            <span>{new Date(localPage.updatedAt).toLocaleString()}</span>
          </div>
          <div className="flex justify-between">
            <span>Created By:</span>
            <span>{localPage.createdBy}</span>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

PageSettings.displayName = 'PageSettings';