/**
 * BulkActionsToolbar - Floating toolbar for bulk operations
 * 
 * A responsive toolbar that appears when items are selected, providing
 * quick access to bulk operations with proper accessibility support.
 * 
 * Features:
 * - Animated slide-up appearance
 * - Responsive design (stacks on mobile)
 * - Keyboard navigation support
 * - Destructive action confirmation
 * - Action tooltips and shortcuts
 * - Progress indication for async actions
 */

import React, { useState, useRef, useEffect } from 'react';
import { X, MoreHorizontal, Trash2, Edit2, Copy, Archive, Tag, Share2 } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from './button';
import { 
  DropdownMenu, 
  DropdownMenuContent, 
  DropdownMenuItem, 
  DropdownMenuTrigger,
  DropdownMenuSeparator 
} from './dropdown-menu';
import { 
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from './alert-dialog';
import { Tooltip, TooltipContent, TooltipTrigger, TooltipProvider } from './tooltip';
import type { BulkAction } from '@/hooks/useSelectionState';

// ============================================================================
// Types
// ============================================================================

export interface BulkActionsToolbarProps<T = unknown> {
  /** Number of selected items */
  selectedCount: number;
  /** Available bulk actions */
  actions: BulkAction<T>[];
  /** Whether to show the toolbar */
  visible: boolean;
  /** Called when an action is executed */
  onAction: (actionId: string) => void;
  /** Called when toolbar is dismissed */
  onDismiss: () => void;
  /** Custom className */
  className?: string;
  /** Maximum actions to show before "More" menu */
  maxVisibleActions?: number;
  /** Position of the toolbar */
  position?: 'bottom' | 'top';
  /** Whether to show selection count */
  showCount?: boolean;
  /** Custom close button label */
  closeLabel?: string;
}

interface DestructiveActionState<T = unknown> {
  action: BulkAction<T> | null;
  isOpen: boolean;
}

// ============================================================================
// Icon Mapping
// ============================================================================

const getActionIcon = (iconProp?: React.ComponentType<{ className?: string }> | string) => {
  if (!iconProp) return null;
  
  // If it's already a component, return it
  if (typeof iconProp !== 'string') {
    return iconProp;
  }
  
  // Map string names to icons
  const iconMap: Record<string, React.ComponentType<{ className?: string }>> = {
    delete: Trash2,
    trash: Trash2,
    edit: Edit2,
    copy: Copy,
    duplicate: Copy,
    archive: Archive,
    tag: Tag,
    share: Share2,
    more: MoreHorizontal
  };
  
  return iconMap[iconProp.toLowerCase()] || null;
};

// ============================================================================
// Main Component
// ============================================================================

export function BulkActionsToolbar<T = unknown>({
  selectedCount,
  actions,
  visible,
  onAction,
  onDismiss,
  className,
  maxVisibleActions = 4,
  position = 'bottom',
  showCount = true,
  closeLabel = 'Close'
}: BulkActionsToolbarProps<T>) {
  const [destructiveAction, setDestructiveAction] = useState<DestructiveActionState<T>>({
    action: null,
    isOpen: false
  });
  const [executingActions, setExecutingActions] = useState<Set<string>>(new Set());
  const toolbarRef = useRef<HTMLDivElement>(null);

  // ============================================================================
  // Action Handling
  // ============================================================================

  const handleActionClick = async (action: BulkAction<T>) => {
    if (action.destructive) {
      setDestructiveAction({ action, isOpen: true });
      return;
    }
    
    await executeAction(action);
  };

  const executeAction = async (action: BulkAction<T>) => {
    setExecutingActions(prev => new Set(prev).add(action.id));
    
    try {
      onAction(action.id);
    } finally {
      setExecutingActions(prev => {
        const next = new Set(prev);
        next.delete(action.id);
        return next;
      });
    }
  };

  const handleDestructiveConfirm = async () => {
    if (destructiveAction.action) {
      await executeAction(destructiveAction.action);
      setDestructiveAction({ action: null, isOpen: false });
    }
  };

  const handleDestructiveCancel = () => {
    setDestructiveAction({ action: null, isOpen: false });
  };

  // ============================================================================
  // Keyboard Navigation
  // ============================================================================

  useEffect(() => {
    if (!visible) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onDismiss();
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [visible, onDismiss]);

  // ============================================================================
  // Action Organization
  // ============================================================================

  const visibleActions = actions.slice(0, maxVisibleActions);
  const hiddenActions = actions.slice(maxVisibleActions);
  const hasHiddenActions = hiddenActions.length > 0;

  // ============================================================================
  // Render Helpers
  // ============================================================================

  const renderActionButton = (action: BulkAction<T>, isInDropdown = false) => {
    const IconComponent = getActionIcon(action.icon);
    const isExecuting = executingActions.has(action.id);
    
    const buttonContent = (
      <>
        {IconComponent && (
          <IconComponent className={cn(
            "h-4 w-4",
            isExecuting && "animate-spin"
          )} />
        )}
        <span className={cn(
          isInDropdown ? "ml-2" : "sr-only sm:not-sr-only sm:ml-2"
        )}>
          {action.label}
        </span>
        {action.shortcut && (
          <span className="ml-auto text-xs text-muted-foreground">
            {action.shortcut}
          </span>
        )}
      </>
    );

    if (isInDropdown) {
      return (
        <DropdownMenuItem
          key={action.id}
          onClick={() => handleActionClick(action)}
          disabled={isExecuting}
          className={cn(
            action.destructive && "text-destructive focus:text-destructive"
          )}
        >
          {buttonContent}
        </DropdownMenuItem>
      );
    }

    const button = (
      <Button
        key={action.id}
        variant={action.destructive ? "destructive" : "secondary"}
        size="sm"
        onClick={() => handleActionClick(action)}
        disabled={isExecuting}
        className="flex-shrink-0"
      >
        {buttonContent}
      </Button>
    );

    if (action.tooltip) {
      return (
        <Tooltip key={action.id}>
          <TooltipTrigger asChild>
            {button}
          </TooltipTrigger>
          <TooltipContent>
            <p>{action.tooltip}</p>
            {action.shortcut && (
              <p className="text-xs opacity-75">Shortcut: {action.shortcut}</p>
            )}
          </TooltipContent>
        </Tooltip>
      );
    }

    return button;
  };

  // ============================================================================
  // Render
  // ============================================================================

  if (!visible || actions.length === 0) {
    return null;
  }

  return (
    <TooltipProvider>
      <div
        ref={toolbarRef}
        className={cn(
          // Base styles
          "fixed left-1/2 transform -translate-x-1/2 z-50",
          "bg-background border border-border rounded-lg shadow-lg",
          "flex items-center gap-2 px-4 py-2",
          "transition-all duration-200 ease-in-out",
          // Position
          position === 'bottom' ? 'bottom-4' : 'top-4',
          // Animation
          visible ? 'opacity-100 scale-100' : 'opacity-0 scale-95 pointer-events-none',
          // Responsive
          "max-w-[calc(100vw-2rem)] overflow-hidden",
          "sm:max-w-none",
          className
        )}
        role="toolbar"
        aria-label={`Bulk actions for ${selectedCount} selected items`}
      >
        {/* Selection Count */}
        {showCount && (
          <div className="flex items-center gap-2 text-sm font-medium text-foreground flex-shrink-0">
            <span className="hidden sm:inline">
              {selectedCount} item{selectedCount !== 1 ? 's' : ''} selected
            </span>
            <span className="sm:hidden">
              {selectedCount} selected
            </span>
          </div>
        )}

        {/* Separator */}
        {showCount && <div className="w-px h-6 bg-border flex-shrink-0" />}

        {/* Action Buttons */}
        <div className="flex items-center gap-2 overflow-hidden">
          <div className="flex items-center gap-1 overflow-x-auto scrollbar-none">
            {visibleActions.map(action => renderActionButton(action))}
          </div>

          {/* More Actions Dropdown */}
          {hasHiddenActions && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="secondary" size="sm" className="flex-shrink-0">
                  <MoreHorizontal className="h-4 w-4" />
                  <span className="sr-only">More actions</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="center" className="min-w-[200px]">
                {hiddenActions.map((action, index) => (
                  <React.Fragment key={action.id}>
                    {index > 0 && action.destructive && !hiddenActions[index - 1].destructive && (
                      <DropdownMenuSeparator />
                    )}
                    {renderActionButton(action, true)}
                  </React.Fragment>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </div>

        {/* Close Button */}
        <Button
          variant="ghost"
          size="sm"
          onClick={onDismiss}
          className="flex-shrink-0 ml-auto"
          aria-label={closeLabel}
        >
          <X className="h-4 w-4" />
        </Button>

        {/* Destructive Action Confirmation Dialog */}
        <AlertDialog 
          open={destructiveAction.isOpen} 
          onOpenChange={(open) => {
            if (!open) handleDestructiveCancel();
          }}
        >
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>
                Confirm {destructiveAction.action?.label}
              </AlertDialogTitle>
              <AlertDialogDescription>
                Are you sure you want to {destructiveAction.action?.label.toLowerCase()} {selectedCount} item{selectedCount !== 1 ? 's' : ''}? 
                This action cannot be undone.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel onClick={handleDestructiveCancel}>
                Cancel
              </AlertDialogCancel>
              <AlertDialogAction 
                onClick={handleDestructiveConfirm}
                className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              >
                {destructiveAction.action?.label}
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>
    </TooltipProvider>
  );
}

// ============================================================================
// Export
// ============================================================================

export default BulkActionsToolbar;