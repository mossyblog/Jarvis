/**
 * Typography - Semantic typography components for consistent text styling
 * 
 * This component provides semantic typography elements that follow the
 * Supabase design system with Inter as the primary font.
 */

import React from 'react';
import { cn } from '@/lib/utils';

// ============================================================================
// Types
// ============================================================================

interface TypographyProps {
  children: React.ReactNode;
  className?: string;
  as?: keyof JSX.IntrinsicElements;
}

// ============================================================================
// Typography Components
// ============================================================================

export const Display = ({ children, className, as: Component = 'h1', ...props }: TypographyProps & React.HTMLAttributes<HTMLElement>) => {
  return (
    <Component 
      className={cn('typography-display', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

export const H1 = ({ children, className, as: Component = 'h1', ...props }: TypographyProps & React.HTMLAttributes<HTMLHeadingElement>) => {
  return (
    <Component 
      className={cn('typography-h1', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

export const H2 = ({ children, className, as: Component = 'h2', ...props }: TypographyProps & React.HTMLAttributes<HTMLHeadingElement>) => {
  return (
    <Component 
      className={cn('typography-h2', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

export const H3 = ({ children, className, as: Component = 'h3', ...props }: TypographyProps & React.HTMLAttributes<HTMLHeadingElement>) => {
  return (
    <Component 
      className={cn('typography-h3', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

export const H4 = ({ children, className, as: Component = 'h4', ...props }: TypographyProps & React.HTMLAttributes<HTMLHeadingElement>) => {
  return (
    <Component 
      className={cn('typography-h4', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

export const H5 = ({ children, className, as: Component = 'h5', ...props }: TypographyProps & React.HTMLAttributes<HTMLHeadingElement>) => {
  return (
    <Component 
      className={cn('typography-h5', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

export const H6 = ({ children, className, as: Component = 'h6', ...props }: TypographyProps & React.HTMLAttributes<HTMLHeadingElement>) => {
  return (
    <Component 
      className={cn('typography-h6', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

export const Body = ({ children, className, as: Component = 'p', ...props }: TypographyProps & React.HTMLAttributes<HTMLElement>) => {
  return (
    <Component 
      className={cn('typography-body', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

export const BodySmall = ({ children, className, as: Component = 'p', ...props }: TypographyProps & React.HTMLAttributes<HTMLElement>) => {
  return (
    <Component 
      className={cn('typography-body-small', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

export const UI = ({ children, className, as: Component = 'span', ...props }: TypographyProps & React.HTMLAttributes<HTMLElement>) => {
  return (
    <Component 
      className={cn('typography-ui', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

export const UISmall = ({ children, className, as: Component = 'span', ...props }: TypographyProps & React.HTMLAttributes<HTMLElement>) => {
  return (
    <Component 
      className={cn('typography-ui-small', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

export const Code = ({ children, className, as: Component = 'code', ...props }: TypographyProps & React.HTMLAttributes<HTMLElement>) => {
  return (
    <Component 
      className={cn('typography-code', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

export const CodeSmall = ({ children, className, as: Component = 'code', ...props }: TypographyProps & React.HTMLAttributes<HTMLElement>) => {
  return (
    <Component 
      className={cn('typography-code-small', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

export const Caption = ({ children, className, as: Component = 'span', ...props }: TypographyProps & React.HTMLAttributes<HTMLElement>) => {
  return (
    <Component 
      className={cn('typography-caption', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

export const Label = ({ children, className, as: Component = 'label', ...props }: TypographyProps & React.HTMLAttributes<HTMLLabelElement>) => {
  return (
    <Component 
      className={cn('typography-label', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

export const Brand = ({ children, className, as: Component = 'span', ...props }: TypographyProps & React.HTMLAttributes<HTMLElement>) => {
  return (
    <Component 
      className={cn('typography-brand', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

export const Number = ({ children, className, as: Component = 'span', tabular = true, ...props }: TypographyProps & React.HTMLAttributes<HTMLElement> & { tabular?: boolean }) => {
  return (
    <Component 
      className={cn(tabular && 'typography-number', className)} 
      {...props}
    >
      {children}
    </Component>
  );
};

// ============================================================================
// Typography Namespace
// ============================================================================

// eslint-disable-next-line react-refresh/only-export-components
export const Typography = {
  Display,
  H1,
  H2,
  H3,
  H4,
  H5,
  H6,
  Body,
  BodySmall,
  UI,
  UISmall,
  Code,
  CodeSmall,
  Caption,
  Label,
  Brand,
  Number,
};