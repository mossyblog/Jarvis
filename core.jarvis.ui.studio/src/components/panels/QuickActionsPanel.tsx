/**
 * QuickActionsPanel Component
 * 
 * A comprehensive quick actions panel for UIStudio with keyboard shortcuts,
 * import/export functionality, template browsing, and new page creation.
 * 
 * Features:
 * - Create new page with Ctrl/Cmd+N keyboard shortcut
 * - Import/export functionality for pages
 * - Browse templates action
 * - Responsive design (mobile-first approach)
 * - Keyboard navigation support
 * - Integration with existing UIStudioInterface
 * - Accessibility features (ARIA, screen reader support)
 * 
 * @module QuickActionsPanel
 */

import React, { useState, useCallback, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { cn } from '../../lib/utils';

// UI Components
import { Button } from '../ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Separator } from '../ui/separator';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '../ui/dialog';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '../ui/dropdown-menu';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '../ui/tooltip';

// Icons
import {
  Plus,
  Layers,
  FileUp,
  FileDown,
  Copy,
  Zap,
  Sparkles,
  ChevronRight,
  Keyboard,
  Command,
  Import,
  MoreHorizontal,
  Rocket,
  BookOpen,
  Grid3x3,
  Palette
} from 'lucide-react';

// Contexts and Hooks
import { useKeyboardNavigationContext } from '../keyboard/KeyboardNavigationProvider';
import { useKeyboardNavigation } from '../../hooks/useKeyboardNavigation';
import type { KeyboardShortcut } from '../../hooks/useKeyboardNavigation';

// Types
import type { UIStudioEntityId, CreatePageRequest } from '../../types/uistudio';

// ============================================================================
// Component Props Interface
// ============================================================================

export interface QuickActionsPanelProps {
  /** Current user entity ID */
  userEntityId: UIStudioEntityId;
  
  /** Callback to create new page */
  onCreatePage?: () => void;
  
  /** Callback to open template gallery */
  onOpenTemplates?: () => void;
  
  /** Callback to handle import functionality */
  onImport?: (file: File) => Promise<void>;
  
  /** Callback to handle export functionality */
  onExport?: (format: 'json' | 'zip') => Promise<void>;
  
  /** Optional custom CSS classes */
  className?: string;
  
  /** Panel layout variant */
  variant?: 'horizontal' | 'vertical' | 'grid';
  
  /** Panel size */
  size?: 'compact' | 'normal' | 'expanded';
  
  /** Show keyboard shortcuts hints */
  showShortcuts?: boolean;
  
  /** Loading states for actions */
  loading?: {
    creating?: boolean;
    importing?: boolean;
    exporting?: boolean;
  };
  
  /** Error states */
  errors?: {
    import?: string | null;
    export?: string | null;
  };
}

// ============================================================================
// Quick Action Item Interface
// ============================================================================

export interface QuickActionItem {
  id: string;
  title: string;
  description: string;
  icon: React.ComponentType<{ className?: string }>;
  action: () => void;
  shortcut?: string;
  badge?: string;
  disabled?: boolean;
  loading?: boolean;
  variant?: 'primary' | 'secondary' | 'outline';
}

// ============================================================================
// Component Implementation
// ============================================================================

export const QuickActionsPanel: React.FC<QuickActionsPanelProps> = ({
  userEntityId,
  onCreatePage,
  onOpenTemplates,
  onImport,
  onExport,
  className,
  variant = 'grid',
  size = 'normal',
  showShortcuts = true,
  loading = {},
  errors = {}
}) => {
  const navigate = useNavigate();
  const { actions } = useKeyboardNavigationContext();
  const panelRef = useRef<HTMLDivElement>(null);
  
  // State management
  const [importDialogOpen, setImportDialogOpen] = useState(false);
  const [exportDialogOpen, setExportDialogOpen] = useState(false);
  const [focusedAction, setFocusedAction] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // ============================================================================
  // Action Handlers
  // ============================================================================

  const handleCreatePage = useCallback(() => {
    onCreatePage?.();
  }, [onCreatePage]);

  const handleOpenTemplates = useCallback(() => {
    onOpenTemplates?.();
  }, [onOpenTemplates]);

  const handleImport = useCallback(async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file && onImport) {
      try {
        await onImport(file);
        setImportDialogOpen(false);
      } catch (error) {
        console.error('Import failed:', error);
      }
    }
  }, [onImport]);

  const handleExport = useCallback(async (format: 'json' | 'zip') => {
    if (onExport) {
      try {
        await onExport(format);
        setExportDialogOpen(false);
      } catch (error) {
        console.error('Export failed:', error);
      }
    }
  }, [onExport]);

  const handleQuickStart = useCallback(() => {
    // Navigate to getting started guide or quick start wizard
    navigate('/studio/quick-start');
  }, [navigate]);

  const handleBrowseExamples = useCallback(() => {
    // Navigate to examples gallery
    navigate('/studio/examples');
  }, [navigate]);

  // ============================================================================
  // Quick Actions Configuration
  // ============================================================================

  const quickActions: QuickActionItem[] = [
    {
      id: 'create-page',
      title: 'Create New Page',
      description: 'Start building a new page from scratch',
      icon: Plus,
      action: handleCreatePage,
      shortcut: showShortcuts ? 'Ctrl+N' : undefined,
      variant: 'primary',
      loading: loading.creating
    },
    {
      id: 'browse-templates',
      title: 'Browse Templates',
      description: 'Choose from pre-built templates',
      icon: Layers,
      action: handleOpenTemplates,
      shortcut: showShortcuts ? 'Ctrl+T' : undefined,
      variant: 'secondary'
    },
    {
      id: 'import-pages',
      title: 'Import Pages',
      description: 'Import pages from file or URL',
      icon: FileUp,
      action: () => setImportDialogOpen(true),
      shortcut: showShortcuts ? 'Ctrl+I' : undefined,
      variant: 'outline',
      loading: loading.importing
    },
    {
      id: 'export-pages',
      title: 'Export Pages',
      description: 'Export your pages and data',
      icon: FileDown,
      action: () => setExportDialogOpen(true),
      shortcut: showShortcuts ? 'Ctrl+E' : undefined,
      variant: 'outline',
      loading: loading.exporting
    }
  ];

  const additionalActions: QuickActionItem[] = [
    {
      id: 'quick-start',
      title: 'Quick Start Guide',
      description: 'Learn the basics in 5 minutes',
      icon: Rocket,
      action: handleQuickStart,
      badge: 'New'
    },
    {
      id: 'examples',
      title: 'View Examples',
      description: 'Explore example pages and components',
      icon: BookOpen,
      action: handleBrowseExamples
    },
    {
      id: 'component-library',
      title: 'Component Library',
      description: 'Browse available UI components',
      icon: Grid3x3,
      action: () => navigate('/studio/components')
    },
    {
      id: 'design-system',
      title: 'Design System',
      description: 'Explore colors, typography, and styles',
      icon: Palette,
      action: () => navigate('/studio/design-system')
    }
  ];

  // ============================================================================
  // Keyboard Navigation Setup
  // ============================================================================

  const handleCustomKeyDown = useCallback((event: KeyboardEvent): boolean | void => {
    const key = event.key.toLowerCase();
    
    // Handle global shortcuts
    if (event.ctrlKey || event.metaKey) {
      switch (key) {
        case 'n':
          event.preventDefault();
          handleCreatePage();
          return true;
        case 't':
          event.preventDefault();
          handleOpenTemplates();
          return true;
        case 'i':
          event.preventDefault();
          setImportDialogOpen(true);
          return true;
        case 'e':
          event.preventDefault();
          setExportDialogOpen(true);
          return true;
      }
    }

    return false;
  }, [handleCreatePage, handleOpenTemplates]);

  const {
    registerShortcut,
    registerItem,
    isActive
  } = useKeyboardNavigation(panelRef, {
    enableArrowKeys: true,
    enableHomeEnd: true,
    enableEscape: true,
    onKeyDown: handleCustomKeyDown
  });

  // ============================================================================
  // Keyboard Shortcuts Registration
  // ============================================================================

  useEffect(() => {
    const shortcuts: KeyboardShortcut[] = [
      {
        key: 'n',
        ctrlKey: true,
        action: handleCreatePage,
        description: 'Create new page'
      },
      {
        key: 'n',
        metaKey: true,
        action: handleCreatePage,
        description: 'Create new page'
      },
      {
        key: 't',
        ctrlKey: true,
        action: handleOpenTemplates,
        description: 'Browse templates'
      },
      {
        key: 't',
        metaKey: true,
        action: handleOpenTemplates,
        description: 'Browse templates'
      },
      {
        key: 'i',
        ctrlKey: true,
        action: () => setImportDialogOpen(true),
        description: 'Import pages'
      },
      {
        key: 'i',
        metaKey: true,
        action: () => setImportDialogOpen(true),
        description: 'Import pages'
      },
      {
        key: 'e',
        ctrlKey: true,
        action: () => setExportDialogOpen(true),
        description: 'Export pages'
      },
      {
        key: 'e',
        metaKey: true,
        action: () => setExportDialogOpen(true),
        description: 'Export pages'
      }
    ];

    const unregisterFunctions = shortcuts.map(shortcut => registerShortcut(shortcut));

    return () => {
      unregisterFunctions.forEach(fn => fn());
    };
  }, [registerShortcut, handleCreatePage, handleOpenTemplates]);

  // ============================================================================
  // Layout Classes
  // ============================================================================

  const getLayoutClasses = () => {
    const base = 'space-y-lg';
    
    switch (variant) {
      case 'horizontal':
        return cn(base, 'flex flex-row space-y-0 space-x-lg overflow-x-auto');
      case 'vertical':
        return cn(base, 'flex flex-col');
      case 'grid':
      default:
        return cn(base);
    }
  };

  const getGridClasses = () => {
    switch (size) {
      case 'compact':
        return 'grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-sm';
      case 'expanded':
        return 'grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-lg';
      case 'normal':
      default:
        return 'grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-md';
    }
  };

  const getCardClasses = (action: QuickActionItem) => {
    const base = cn(
      'group cursor-pointer transition-all duration-200 hover:shadow-lg hover:scale-[1.02]',
      'border-2 hover:border-primary/20 bg-card/50 backdrop-blur-sm',
      action.disabled && 'opacity-50 cursor-not-allowed',
      focusedAction === action.id && 'ring-2 ring-blue-500 ring-offset-2'
    );

    switch (action.variant) {
      case 'primary':
        return cn(base, 'border-primary/20 bg-primary/5');
      case 'secondary':
        return cn(base, 'border-secondary/20 bg-secondary/5');
      case 'outline':
      default:
        return base;
    }
  };

  // ============================================================================
  // Render Component
  // ============================================================================

  return (
    <div
      ref={panelRef}
      className={cn(getLayoutClasses(), className)}
      role="region"
      aria-label="Quick Actions Panel"
      tabIndex={-1}
    >
      {/* Panel Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-foreground flex items-center gap-2">
            <Zap className="h-5 w-5 text-primary" />
            Quick Actions
          </h2>
          <p className="text-sm text-muted-foreground">
            Get started with common tasks and shortcuts
          </p>
        </div>
        
        {showShortcuts && (
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <Badge variant="outline" className="gap-1">
                  <Keyboard className="h-3 w-3" />
                  <span className="hidden sm:inline">Shortcuts enabled</span>
                </Badge>
              </TooltipTrigger>
              <TooltipContent>
                <p>Keyboard shortcuts are active</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        )}
      </div>

      {/* Main Actions Grid */}
      <section
        className={getGridClasses()}
        role="group"
        aria-label="Primary quick actions"
      >
        {quickActions.map((action) => {
          const Icon = action.icon;
          
          return (
            <Card
              key={action.id}
              className={getCardClasses(action)}
              role="button"
              tabIndex={0}
              aria-label={`${action.title}: ${action.description}`}
              aria-disabled={action.disabled}
              onClick={() => {
                if (!action.disabled && !action.loading) {
                  setFocusedAction(action.id);
                  action.action();
                }
              }}
              onFocus={() => setFocusedAction(action.id)}
              onKeyDown={(e) => {
                if ((e.key === 'Enter' || e.key === ' ') && !action.disabled && !action.loading) {
                  e.preventDefault();
                  action.action();
                }
              }}
            >
              <CardHeader className="pb-sm space-y-sm">
                <div className="flex items-start justify-between gap-sm">
                  <div className="flex items-center gap-sm">
                    <div className={cn(
                      'p-2 rounded-lg transition-colors',
                      action.variant === 'primary' 
                        ? 'bg-primary text-primary-foreground' 
                        : 'bg-muted text-muted-foreground group-hover:bg-primary group-hover:text-primary-foreground'
                    )}>
                      <Icon className="h-4 w-4" />
                    </div>
                    <CardTitle className="text-sm font-medium leading-tight">
                      {action.title}
                    </CardTitle>
                  </div>
                  
                  <div className="flex items-center gap-xs">
                    {action.badge && (
                      <Badge variant="secondary" className="text-xs px-1">
                        {action.badge}
                      </Badge>
                    )}
                    {action.shortcut && showShortcuts && (
                      <Badge variant="outline" className="text-xs gap-1 px-1">
                        <Command className="h-2 w-2" />
                        {action.shortcut.replace('Ctrl', '⌘')}
                      </Badge>
                    )}
                  </div>
                </div>
              </CardHeader>
              
              <CardContent className="pt-0">
                <CardDescription className="text-xs text-muted-foreground leading-relaxed">
                  {action.description}
                </CardDescription>
                
                {action.loading && (
                  <div className="mt-sm">
                    <div className="h-1 bg-muted rounded overflow-hidden">
                      <div className="h-full bg-primary rounded animate-pulse" />
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>
          );
        })}
      </section>

      <Separator />

      {/* Additional Actions */}
      <section>
        <div className="flex items-center justify-between mb-md">
          <h3 className="text-lg font-semibold text-foreground flex items-center gap-2">
            <Sparkles className="h-4 w-4 text-secondary" />
            Explore & Learn
          </h3>
          
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm">
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuLabel>More Actions</DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem>
                <Copy className="h-4 w-4 mr-2" />
                Clone Page
              </DropdownMenuItem>
              <DropdownMenuItem>
                <Import className="h-4 w-4 mr-2" />
                Batch Import
              </DropdownMenuItem>
              <DropdownMenuItem>
                <FileDown className="h-4 w-4 mr-2" />
                Bulk Export
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-sm">
          {additionalActions.map((action) => {
            const Icon = action.icon;
            
            return (
              <Button
                key={action.id}
                variant="ghost"
                className="h-auto p-md flex-col items-start text-left group"
                onClick={action.action}
                disabled={action.disabled}
              >
                <div className="flex items-center gap-sm w-full mb-xs">
                  <Icon className="h-4 w-4 text-muted-foreground group-hover:text-primary transition-colors" />
                  <span className="text-sm font-medium flex-1">{action.title}</span>
                  {action.badge && (
                    <Badge variant="secondary" className="text-xs">
                      {action.badge}
                    </Badge>
                  )}
                  <ChevronRight className="h-3 w-3 text-muted-foreground group-hover:text-primary transition-colors" />
                </div>
                <p className="text-xs text-muted-foreground leading-relaxed">
                  {action.description}
                </p>
              </Button>
            );
          })}
        </div>
      </section>

      {/* Import Dialog */}
      <Dialog open={importDialogOpen} onOpenChange={setImportDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Import Pages</DialogTitle>
            <DialogDescription>
              Select a file to import pages. Supported formats: JSON, ZIP
            </DialogDescription>
          </DialogHeader>
          
          <div className="space-y-md">
            <input
              ref={fileInputRef}
              type="file"
              accept=".json,.zip"
              onChange={handleImport}
              className="hidden"
            />
            
            <Button
              onClick={() => fileInputRef.current?.click()}
              className="w-full"
              disabled={loading.importing}
            >
              <FileUp className="h-4 w-4 mr-2" />
              {loading.importing ? 'Importing...' : 'Choose File'}
            </Button>
            
            {errors.import && (
              <div className="text-destructive text-sm p-2 bg-destructive/10 rounded">
                {errors.import}
              </div>
            )}
          </div>
        </DialogContent>
      </Dialog>

      {/* Export Dialog */}
      <Dialog open={exportDialogOpen} onOpenChange={setExportDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Export Pages</DialogTitle>
            <DialogDescription>
              Choose the export format for your pages and data
            </DialogDescription>
          </DialogHeader>
          
          <div className="space-y-md">
            <div className="grid grid-cols-2 gap-md">
              <Button
                variant="outline"
                onClick={() => handleExport('json')}
                disabled={loading.exporting}
                className="h-auto flex-col p-md"
              >
                <FileDown className="h-6 w-6 mb-2" />
                <span className="font-medium">JSON</span>
                <span className="text-xs text-muted-foreground">
                  Data and structure
                </span>
              </Button>
              
              <Button
                variant="outline"
                onClick={() => handleExport('zip')}
                disabled={loading.exporting}
                className="h-auto flex-col p-md"
              >
                <FileDown className="h-6 w-6 mb-2" />
                <span className="font-medium">ZIP</span>
                <span className="text-xs text-muted-foreground">
                  Complete package
                </span>
              </Button>
            </div>
            
            {errors.export && (
              <div className="text-destructive text-sm p-2 bg-destructive/10 rounded">
                {errors.export}
              </div>
            )}
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default QuickActionsPanel;