/**
 * SelectionCheckbox - Enhanced checkbox for selection state management
 * 
 * A specialized checkbox component that integrates seamlessly with the
 * useSelectionState hook, providing proper accessibility and visual feedback.
 * 
 * Features:
 * - Indeterminate state support for "select all"
 * - Keyboard navigation (Space, Enter)
 * - Proper ARIA labels and states
 * - Visual feedback for focus and hover
 * - Supports modifier keys (Ctrl, Shift)
 * - Custom styling with Tailwind
 */

import React, { forwardRef, useCallback } from 'react';
import { Check, Minus } from 'lucide-react';
import { LucideIcon as Icon } from './icon';
import { cn } from '@/lib/utils';
import type { SelectionModifiers } from '@/hooks/useSelectionState';

// ============================================================================
// Types
// ============================================================================

export interface SelectionCheckboxProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'onChange' | 'size'> {
  /** Whether the checkbox is checked */
  checked: boolean;
  /** Whether the checkbox is in indeterminate state (for select all) */
  indeterminate?: boolean;
  /** Called when checkbox state changes */
  onChange: (checked: boolean, modifiers?: SelectionModifiers, event?: React.MouseEvent | React.KeyboardEvent) => void;
  /** Custom label for accessibility */
  label?: string;
  /** Size variant */
  size?: 'sm' | 'md' | 'lg';
  /** Visual variant */
  variant?: 'default' | 'primary' | 'destructive';
  /** Whether to show visual feedback on hover/focus */
  showFeedback?: boolean;
  /** Custom className */
  className?: string;
  /** Whether checkbox is disabled */
  disabled?: boolean;
}

// ============================================================================
// Utility Functions
// ============================================================================

function extractModifiers(event: React.MouseEvent | React.KeyboardEvent): SelectionModifiers {
  return {
    ctrlKey: event.ctrlKey || event.metaKey,
    shiftKey: event.shiftKey,
    altKey: event.altKey
  };
}

// ============================================================================
// Size Configuration
// ============================================================================

const sizeConfig = {
  sm: {
    container: 'w-xs h-xs',
    text: 'text-xs'
  },
  md: {
    container: 'w-xs h-xs',
    text: 'text-sm'
  },
  lg: {
    container: 'w-sm h-sm',
    text: 'text-base'
  }
};

const variantConfig = {
  default: {
    unchecked: 'border-input bg-background hover:bg-accent',
    checked: 'border-primary bg-primary text-primary-foreground',
    indeterminate: 'border-primary bg-primary text-primary-foreground',
    disabled: 'opacity-50 cursor-not-allowed'
  },
  primary: {
    unchecked: 'border-primary/30 bg-background hover:bg-primary/5',
    checked: 'border-primary bg-primary text-primary-foreground',
    indeterminate: 'border-primary bg-primary text-primary-foreground',
    disabled: 'opacity-50 cursor-not-allowed'
  },
  destructive: {
    unchecked: 'border-destructive/30 bg-background hover:bg-destructive/5',
    checked: 'border-destructive bg-destructive text-destructive-foreground',
    indeterminate: 'border-destructive bg-destructive text-destructive-foreground',
    disabled: 'opacity-50 cursor-not-allowed'
  }
};

// ============================================================================
// Main Component
// ============================================================================

export const SelectionCheckbox = forwardRef<HTMLInputElement, SelectionCheckboxProps>(({
  checked,
  indeterminate = false,
  onChange,
  label,
  size = 'md',
  variant = 'default',
  showFeedback = true,
  className,
  disabled,
  'aria-label': ariaLabel,
  ...props
}, ref) => {
  // ============================================================================
  // Event Handlers
  // ============================================================================

  const handleClick = useCallback((event: React.MouseEvent<HTMLDivElement>) => {
    if (disabled) return;
    
    event.preventDefault();
    event.stopPropagation();
    
    const modifiers = extractModifiers(event);
    const newChecked = indeterminate ? true : !checked;
    
    onChange(newChecked, modifiers, event);
  }, [checked, indeterminate, disabled, onChange]);

  const handleKeyDown = useCallback((event: React.KeyboardEvent<HTMLDivElement>) => {
    if (disabled) return;
    
    if (event.key === ' ' || event.key === 'Enter') {
      event.preventDefault();
      event.stopPropagation();
      
      const modifiers = extractModifiers(event);
      const newChecked = indeterminate ? true : !checked;
      
      onChange(newChecked, modifiers, event);
    }
  }, [checked, indeterminate, disabled, onChange]);

  const handleInputChange = useCallback((event: React.ChangeEvent<HTMLInputElement>) => {
    // This is handled by the div click/keydown handlers
    // We prevent the default input behavior
    event.preventDefault();
  }, []);

  // ============================================================================
  // Style Computation
  // ============================================================================

  const sizeStyles = sizeConfig[size];
  const variantStyles = variantConfig[variant];

  const getStateStyles = () => {
    if (disabled) return variantStyles.disabled;
    if (indeterminate) return variantStyles.indeterminate;
    if (checked) return variantStyles.checked;
    return variantStyles.unchecked;
  };

  const containerClassName = cn(
    // Base styles
    'relative inline-flex items-center justify-center',
    'border rounded transition-all duration-150',
    'cursor-pointer select-none',
    
    // Size styles
    sizeStyles.container,
    
    // State styles
    getStateStyles(),
    
    // Focus styles
    'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
    
    // Feedback styles
    showFeedback && !disabled && 'hover:scale-105 active:scale-95',
    
    // Disabled styles
    disabled && 'cursor-not-allowed',
    
    className
  );

  const effectiveAriaLabel = ariaLabel || label || (indeterminate ? 'Select all' : checked ? 'Selected' : 'Not selected');

  // ============================================================================
  // Render
  // ============================================================================

  return (
    <div className="inline-flex items-center">
      {/* Hidden input for form compatibility */}
      <input
        ref={ref}
        type="checkbox"
        checked={checked}
        onChange={handleInputChange}
        disabled={disabled}
        className="sr-only"
        aria-hidden="true"
        tabIndex={-1}
        {...props}
      />
      
      {/* Visual checkbox */}
      <div
        role="checkbox"
        aria-checked={indeterminate ? 'mixed' : checked}
        aria-label={effectiveAriaLabel}
        tabIndex={disabled ? -1 : 0}
        className={containerClassName}
        onClick={handleClick}
        onKeyDown={handleKeyDown}
      >
        {/* Check or indeterminate icon */}
        {checked && !indeterminate && (
          <Icon icon={Check} size="xs" className="stroke-[3]" />
        )}
        {indeterminate && (
          <Icon icon={Minus} size="xs" className="stroke-[3]" />
        )}
      </div>
      
      {/* Optional label */}
      {label && (
        <span 
          className={cn(
            'ml-2 select-none',
            sizeStyles.text,
            disabled ? 'text-muted-foreground' : 'text-foreground',
            !disabled && 'cursor-pointer'
          )}
          onClick={!disabled ? handleClick : undefined}
        >
          {label}
        </span>
      )}
    </div>
  );
});

SelectionCheckbox.displayName = 'SelectionCheckbox';

// ============================================================================
// Export
// ============================================================================

export default SelectionCheckbox;