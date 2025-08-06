/**
 * Pagination Component
 * 
 * A comprehensive pagination component with page navigation, page size selection,
 * and accessibility features. Designed to work with UIStudio pagination state.
 */

import React from 'react';
import { Button } from './button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './select';
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';
import { cn } from '../../lib/utils';

// ============================================================================
// Types and Interfaces
// ============================================================================

export interface PaginationProps {
  /** Current page number (1-based) */
  currentPage: number;
  
  /** Total number of pages */
  totalPages: number;
  
  /** Items per page */
  pageSize: number;
  
  /** Total number of items */
  totalItems: number;
  
  /** Available page size options */
  pageSizeOptions?: number[];
  
  /** Whether there is a next page */
  hasNextPage?: boolean;
  
  /** Whether there is a previous page */
  hasPreviousPage?: boolean;
  
  /** Callback when page changes */
  onPageChange: (page: number) => void;
  
  /** Callback when page size changes */
  onPageSizeChange?: (pageSize: number) => void;
  
  /** Show page size selector */
  showPageSizeSelector?: boolean;
  
  /** Show page info text */
  showPageInfo?: boolean;
  
  /** Maximum number of page buttons to show */
  maxPageButtons?: number;
  
  /** Custom CSS classes */
  className?: string;
  
  /** Loading state */
  loading?: boolean;
  
  /** Disabled state */
  disabled?: boolean;
  
  /** Size variant */
  size?: 'sm' | 'md' | 'lg';
}

// ============================================================================
// Pagination Component
// ============================================================================

export const Pagination: React.FC<PaginationProps> = ({
  currentPage,
  totalPages,
  pageSize,
  totalItems,
  pageSizeOptions = [12, 24, 48, 96],
  hasNextPage = currentPage < totalPages,
  hasPreviousPage = currentPage > 1,
  onPageChange,
  onPageSizeChange,
  showPageSizeSelector = true,
  showPageInfo = true,
  maxPageButtons = 7,
  className,
  loading = false,
  disabled = false,
  size = 'md'
}) => {
  // Calculate page range to display
  const getPageRange = (): number[] => {
    if (totalPages <= maxPageButtons) {
      return Array.from({ length: totalPages }, (_, i) => i + 1);
    }

    const half = Math.floor(maxPageButtons / 2);
    let start = Math.max(1, currentPage - half);
    const end = Math.min(totalPages, start + maxPageButtons - 1);

    if (end - start + 1 < maxPageButtons) {
      start = Math.max(1, end - maxPageButtons + 1);
    }

    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  };

  const pageRange = getPageRange();

  // Calculate current items range
  const startItem = Math.min((currentPage - 1) * pageSize + 1, totalItems);
  const endItem = Math.min(currentPage * pageSize, totalItems);

  // Size variants
  const sizeVariants = {
    sm: {
      button: 'h-md px-2 text-xs',
      select: 'h-md text-xs',
      text: 'text-xs'
    },
    md: {
      button: 'h-lg px-3 text-sm',
      select: 'h-lg text-sm',
      text: 'text-sm'
    },
    lg: {
      button: 'h-xl px-4 text-base',
      select: 'h-xl text-base',
      text: 'text-base'
    }
  };

  const variant = sizeVariants[size];

  return (
    <nav
      className={cn(
        'flex flex-col space-y-3 sm:flex-row sm:items-center sm:justify-between sm:space-y-0',
        className
      )}
      role="navigation"
      aria-label="Pagination"
    >
      {/* Page Info */}
      {showPageInfo && (
        <div className={cn('text-muted-foreground', variant.text)}>
          {totalItems > 0 ? (
            <>
              Showing <span className="font-medium text-foreground">{startItem}</span> to{' '}
              <span className="font-medium text-foreground">{endItem}</span> of{' '}
              <span className="font-medium text-foreground">{totalItems}</span> results
            </>
          ) : (
            'No results found'
          )}
        </div>
      )}

      {/* Pagination Controls */}
      <div className="flex flex-col space-y-3 sm:flex-row sm:items-center sm:space-x-6 sm:space-y-0">
        {/* Page Size Selector */}
        {showPageSizeSelector && onPageSizeChange && (
          <div className="flex items-center space-x-2">
            <span className={cn('text-muted-foreground whitespace-nowrap', variant.text)}>
              Per page:
            </span>
            <Select
              value={pageSize.toString()}
              onValueChange={(value) => onPageSizeChange(parseInt(value, 10))}
              disabled={disabled || loading}
            >
              <SelectTrigger 
                className={cn('w-3xl', variant.select)}
                aria-label="Items per page"
              >
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {pageSizeOptions.map((option) => (
                  <SelectItem key={option} value={option.toString()}>
                    {option}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        )}

        {/* Page Navigation */}
        {totalPages > 1 && (
          <div className="flex items-center space-x-1">
            {/* First Page Button */}
            <Button
              variant="outline"
              className={cn(variant.button)}
              onClick={() => onPageChange(1)}
              disabled={!hasPreviousPage || disabled || loading}
              aria-label="Go to first page"
            >
              <ChevronsLeft className="h-xs w-xs" />
            </Button>

            {/* Previous Page Button */}
            <Button
              variant="outline"
              className={cn(variant.button)}
              onClick={() => onPageChange(currentPage - 1)}
              disabled={!hasPreviousPage || disabled || loading}
              aria-label="Go to previous page"
            >
              <ChevronLeft className="h-xs w-xs" />
            </Button>

            {/* Page Number Buttons */}
            <div className="flex items-center space-x-1">
              {/* Show first page if not in range */}
              {pageRange[0] > 1 && (
                <>
                  <Button
                    variant={1 === currentPage ? 'default' : 'outline'}
                    className={cn(variant.button)}
                    onClick={() => onPageChange(1)}
                    disabled={disabled || loading}
                    aria-label="Go to page 1"
                    aria-current={1 === currentPage ? 'page' : undefined}
                  >
                    1
                  </Button>
                  {pageRange[0] > 2 && (
                    <span className={cn('text-muted-foreground', variant.text)}>...</span>
                  )}
                </>
              )}

              {/* Page range buttons */}
              {pageRange.map((page) => (
                <Button
                  key={page}
                  variant={page === currentPage ? 'default' : 'outline'}
                  className={cn(variant.button)}
                  onClick={() => onPageChange(page)}
                  disabled={disabled || loading}
                  aria-label={`Go to page ${page}`}
                  aria-current={page === currentPage ? 'page' : undefined}
                >
                  {page}
                </Button>
              ))}

              {/* Show last page if not in range */}
              {pageRange[pageRange.length - 1] < totalPages && (
                <>
                  {pageRange[pageRange.length - 1] < totalPages - 1 && (
                    <span className={cn('text-muted-foreground', variant.text)}>...</span>
                  )}
                  <Button
                    variant={totalPages === currentPage ? 'default' : 'outline'}
                    className={cn(variant.button)}
                    onClick={() => onPageChange(totalPages)}
                    disabled={disabled || loading}
                    aria-label={`Go to page ${totalPages}`}
                    aria-current={totalPages === currentPage ? 'page' : undefined}
                  >
                    {totalPages}
                  </Button>
                </>
              )}
            </div>

            {/* Next Page Button */}
            <Button
              variant="outline"
              className={cn(variant.button)}
              onClick={() => onPageChange(currentPage + 1)}
              disabled={!hasNextPage || disabled || loading}
              aria-label="Go to next page"
            >
              <ChevronRight className="h-xs w-xs" />
            </Button>

            {/* Last Page Button */}
            <Button
              variant="outline"
              className={cn(variant.button)}
              onClick={() => onPageChange(totalPages)}
              disabled={!hasNextPage || disabled || loading}
              aria-label="Go to last page"
            >
              <ChevronsRight className="h-xs w-xs" />
            </Button>
          </div>
        )}
      </div>
    </nav>
  );
};

// ============================================================================
// Export
// ============================================================================

export default Pagination;