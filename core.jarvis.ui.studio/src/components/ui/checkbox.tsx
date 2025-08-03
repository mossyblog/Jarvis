import React from 'react';
import { cn } from '../../lib/utils';

export interface CheckboxProps {
  checked?: boolean;
  onChange?: (checked: boolean) => void;
  disabled?: boolean;
  label?: string;
  className?: string;
  indeterminate?: boolean;
}

export const Checkbox: React.FC<CheckboxProps> = ({
  checked = false,
  onChange,
  disabled = false,
  label,
  className,
  indeterminate = false
}) => {
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onChange?.(e.target.checked);
  };

  return (
    <label className={cn(
      "flex items-center gap-xs cursor-pointer",
      disabled && "cursor-not-allowed opacity-50",
      className
    )}>
      <input
        type="checkbox"
        checked={checked}
        onChange={handleChange}
        disabled={disabled}
        ref={(el) => {
          if (el) el.indeterminate = indeterminate;
        }}
        className={cn(
          "w-xs h-xs rounded border border-input bg-background",
          "checked:bg-primary checked:border-primary",
          "focus:ring-2 focus:ring-primary focus:ring-offset-2",
          disabled && "cursor-not-allowed"
        )}
      />
      {label && (
        <span className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">
          {label}
        </span>
      )}
    </label>
  );
};