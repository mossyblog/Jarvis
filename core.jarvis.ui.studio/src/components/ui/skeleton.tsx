/**
 * Skeleton Components
 * 
 * Loading skeleton components for different content types
 * with specialized variants and consistent styling.
 * 
 * @module Skeleton
 */

import React from 'react';
import { cn } from "@/lib/utils";

// ============================================================================
// Base Skeleton Component
// ============================================================================

function Skeleton({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="skeleton"
      className={cn("animate-pulse rounded-md bg-muted", className)}
      {...props}
    />
  );
}

// ============================================================================
// Specialized Skeleton Components
// ============================================================================

interface SkeletonTextProps {
  lines?: number;
  className?: string;
}

function SkeletonText({ lines = 3, className }: SkeletonTextProps) {
  return (
    <div className={cn("space-y-2", className)}>
      {Array.from({ length: lines }).map((_, i) => (
        <Skeleton 
          key={i}
          className={cn(
            "h-xs",
            i === lines - 1 ? "w-2xs/4" : "w-full" // Last line is shorter
          )} 
        />
      ))}
    </div>
  );
}

interface SkeletonCardProps {
  className?: string;
  hasImage?: boolean;
  hasActions?: boolean;
}

function SkeletonCard({ className, hasImage = true, hasActions = true }: SkeletonCardProps) {
  return (
    <div className={cn("p-4 border rounded-lg space-y-4", className)}>
      {hasImage && (
        <Skeleton className="h-mdxl w-full" />
      )}
      <div className="space-y-2">
        <Skeleton className="h-xs w-2xs/4" />
        <SkeletonText lines={2} />
      </div>
      {hasActions && (
        <div className="flex gap-2">
          <Skeleton className="h-sm w-4xl" />
          <Skeleton className="h-sm w-3xl" />
        </div>
      )}
    </div>
  );
}

interface SkeletonTableProps {
  rows?: number;
  columns?: number;
  hasHeader?: boolean;
  className?: string;
}

function SkeletonTable({ 
  rows = 5, 
  columns = 4, 
  hasHeader = true, 
  className 
}: SkeletonTableProps) {
  return (
    <div className={cn("w-full", className)}>
      {hasHeader && (
        <div className="grid gap-4 pb-4 border-b mb-4" style={{ gridTemplateColumns: `repeat(${columns}, 1fr)` }}>
          {Array.from({ length: columns }).map((_, i) => (
            <Skeleton key={i} className="h-xs w-full" />
          ))}
        </div>
      )}
      <div className="space-y-3">
        {Array.from({ length: rows }).map((_, rowIndex) => (
          <div 
            key={rowIndex} 
            className="grid gap-4" 
            style={{ gridTemplateColumns: `repeat(${columns}, 1fr)` }}
          >
            {Array.from({ length: columns }).map((_, colIndex) => (
              <Skeleton 
                key={colIndex} 
                className={cn(
                  "h-xs",
                  colIndex === 0 ? "w-2xs/4" : "w-full" // First column shorter for names
                )} 
              />
            ))}
          </div>
        ))}
      </div>
    </div>
  );
}

interface SkeletonListProps {
  items?: number;
  hasAvatar?: boolean;
  hasActions?: boolean;
  className?: string;
}

function SkeletonList({ 
  items = 5, 
  hasAvatar = false, 
  hasActions = false, 
  className 
}: SkeletonListProps) {
  return (
    <div className={cn("space-y-3", className)}>
      {Array.from({ length: items }).map((_, i) => (
        <div key={i} className="flex items-center gap-3 p-3 border rounded">
          {hasAvatar && (
            <Skeleton className="h-sm w-sm rounded-full flex-shrink-0" />
          )}
          <div className="flex-1 space-y-1">
            <Skeleton className="h-xs w-xsxs/3" />
            <Skeleton className="h-xs w-xs/3" />
          </div>
          {hasActions && (
            <div className="flex gap-2 flex-shrink-0">
              <Skeleton className="h-xs w-xs rounded" />
              <Skeleton className="h-xs w-xs rounded" />
            </div>
          )}
        </div>
      ))}
    </div>
  );
}

interface SkeletonFormProps {
  fields?: number;
  hasSubmit?: boolean;
  className?: string;
}

function SkeletonForm({ fields = 4, hasSubmit = true, className }: SkeletonFormProps) {
  return (
    <div className={cn("space-y-4", className)}>
      {Array.from({ length: fields }).map((_, i) => (
        <div key={i} className="space-y-2">
          <Skeleton className="h-xs w-xsxs/4" />
          <Skeleton className="h-sm w-full" />
        </div>
      ))}
      {hasSubmit && (
        <div className="flex gap-2 pt-2">
          <Skeleton className="h-sm w-5xl" />
          <Skeleton className="h-sm w-4xl" />
        </div>
      )}
    </div>
  );
}

interface SkeletonGridProps {
  items?: number;
  columns?: number;
  className?: string;
}

function SkeletonGrid({ items = 6, columns = 3, className }: SkeletonGridProps) {
  return (
    <div 
      className={cn("grid gap-4", className)}
      style={{ gridTemplateColumns: `repeat(${columns}, 1fr)` }}
    >
      {Array.from({ length: items }).map((_, i) => (
        <SkeletonCard key={i} />
      ))}
    </div>
  );
}

interface SkeletonChartProps {
  type?: 'bar' | 'line' | 'pie';
  className?: string;
}

function SkeletonChart({ type = 'bar', className }: SkeletonChartProps) {
  if (type === 'pie') {
    return (
      <div className={cn("flex items-center justify-center p-8", className)}>
        <Skeleton className="h-mdxl w-mdxl rounded-full" />
      </div>
    );
  }

  return (
    <div className={cn("p-4", className)}>
      <div className="space-y-2 mb-4">
        <Skeleton className="h-xs w-xsxs/3" />
        <Skeleton className="h-xs w-xsxs/2" />
      </div>
      <div className="h-xlxl flex items-end gap-2">
        {Array.from({ length: 8 }).map((_, i) => (
          <Skeleton 
            key={i} 
            className="flex-1 rounded-t" 
            style={{ height: `${Math.random() * 60 + 40}%` }}
          />
        ))}
      </div>
    </div>
  );
}

interface SkeletonNavigationProps {
  items?: number;
  hasLogo?: boolean;
  hasProfile?: boolean;
  className?: string;
}

function SkeletonNavigation({ 
  items = 5, 
  hasLogo = true, 
  hasProfile = true, 
  className 
}: SkeletonNavigationProps) {
  return (
    <div className={cn("flex items-center justify-between p-4", className)}>
      {hasLogo && (
        <Skeleton className="h-xs w-252" />
      )}
      <div className="flex gap-6">
        {Array.from({ length: items }).map((_, i) => (
          <Skeleton key={i} className="h-xs w-3xl" />
        ))}
      </div>
      {hasProfile && (
        <div className="flex items-center gap-2">
          <Skeleton className="h-xs w-xs rounded-full" />
          <Skeleton className="h-xs w-4xl" />
        </div>
      )}
    </div>
  );
}

interface SkeletonPageProps {
  hasHeader?: boolean;
  hasSidebar?: boolean;
  hasFooter?: boolean;
  className?: string;
}

function SkeletonPage({ 
  hasHeader = true, 
  hasSidebar = false, 
  hasFooter = false, 
  className 
}: SkeletonPageProps) {
  return (
    <div className={cn("min-h-screen flex flex-col", className)}>
      {hasHeader && (
        <SkeletonNavigation className="border-b" />
      )}
      
      <div className="flex-1 flex">
        {hasSidebar && (
          <div className="w-xlxl border-r p-4 space-y-2">
            {Array.from({ length: 8 }).map((_, i) => (
              <Skeleton key={i} className="h-xs w-full" />
            ))}
          </div>
        )}
        
        <div className="flex-1 p-6 space-y-6">
          <div className="space-y-2">
            <Skeleton className="h-xs w-xsxs/3" />
            <Skeleton className="h-xs w-xs/3" />
          </div>
          
          <SkeletonGrid />
          
          <SkeletonTable />
        </div>
      </div>
      
      {hasFooter && (
        <div className="border-t p-4">
          <Skeleton className="h-xs w-full max-w-md mx-auto" />
        </div>
      )}
    </div>
  );
}

// ============================================================================
// Avatar Skeleton
// ============================================================================

interface SkeletonAvatarProps {
  size?: 'sm' | 'default' | 'lg';
  className?: string;
}

function SkeletonAvatar({ size = 'default', className }: SkeletonAvatarProps) {
  const sizeClasses = {
    sm: 'h-xs w-xs',
    default: 'h-sm w-sm',
    lg: 'h-md w-md'
  };

  return (
    <Skeleton 
      className={cn("rounded-full", sizeClasses[size], className)} 
    />
  );
}

// ============================================================================
// Loading Skeleton Wrapper
// ============================================================================

interface LoadingSkeletonProps {
  isLoading: boolean;
  children: React.ReactNode;
  skeleton: React.ReactNode;
  className?: string;
}

function LoadingSkeleton({ 
  isLoading, 
  children, 
  skeleton, 
  className 
}: LoadingSkeletonProps) {
  return (
    <div className={className}>
      {isLoading ? skeleton : children}
    </div>
  );
}

export { 
  Skeleton,
  SkeletonText,
  SkeletonCard,
  SkeletonTable,
  SkeletonList,
  SkeletonForm,
  SkeletonGrid,
  SkeletonChart,
  SkeletonNavigation,
  SkeletonPage,
  SkeletonAvatar,
  LoadingSkeleton
};