/**
 * SearchHighlight Component
 * 
 * A component for rendering text with search term highlighting.
 * Used in conjunction with the useSearchState hook to display
 * search results with highlighted matching text.
 * 
 * @module SearchHighlight
 */

import React from 'react';
import { cn } from '../../lib/utils';
import type { TextHighlight } from '../../hooks/useSearchState';

// ============================================================================
// Component Props
// ============================================================================

/** Props for SearchHighlight component */
export interface SearchHighlightProps {
  /** The text to highlight */
  text: string;
  
  /** The search term to highlight */
  searchTerm?: string;
  
  /** Pre-computed highlights (from useSearchState) */
  highlights?: TextHighlight[];
  
  /** Case sensitive highlighting */
  caseSensitive?: boolean;
  
  /** CSS class for highlighted text */
  highlightClassName?: string;
  
  /** CSS class for the container */
  className?: string;
  
  /** HTML element type for the container */
  as?: keyof React.JSX.IntrinsicElements;
  
  /** Maximum length before truncation */
  maxLength?: number;
  
  /** Show ellipsis when truncated */
  showEllipsis?: boolean;
}

// ============================================================================
// Highlighting Utility Functions
// ============================================================================

/**
 * Create highlights from text and search term
 */
function createTextHighlights(text: string, searchTerm: string, caseSensitive = false): TextHighlight[] {
  if (!searchTerm.trim() || !text) {
    return [{ text, isHighlighted: false, startIndex: 0, endIndex: text.length }];
  }

  const searchText = caseSensitive ? text : text.toLowerCase();
  const searchQuery = caseSensitive ? searchTerm : searchTerm.toLowerCase();
  
  const highlights: TextHighlight[] = [];
  let currentIndex = 0;
  let matchIndex = searchText.indexOf(searchQuery, currentIndex);

  while (matchIndex !== -1) {
    // Add non-highlighted text before the match
    if (matchIndex > currentIndex) {
      highlights.push({
        text: text.slice(currentIndex, matchIndex),
        isHighlighted: false,
        startIndex: currentIndex,
        endIndex: matchIndex
      });
    }

    // Add highlighted match
    highlights.push({
      text: text.slice(matchIndex, matchIndex + searchQuery.length),
      isHighlighted: true,
      startIndex: matchIndex,
      endIndex: matchIndex + searchQuery.length
    });

    currentIndex = matchIndex + searchQuery.length;
    matchIndex = searchText.indexOf(searchQuery, currentIndex);
  }

  // Add remaining non-highlighted text
  if (currentIndex < text.length) {
    highlights.push({
      text: text.slice(currentIndex),
      isHighlighted: false,
      startIndex: currentIndex,
      endIndex: text.length
    });
  }

  return highlights;
}

/**
 * Truncate text while preserving word boundaries
 */
function truncateText(text: string, maxLength: number, showEllipsis = true): string {
  if (text.length <= maxLength) {
    return text;
  }

  // Find the last space before the max length
  let truncateIndex = maxLength;
  while (truncateIndex > 0 && text[truncateIndex] !== ' ') {
    truncateIndex--;
  }

  // If no space found, just truncate at maxLength
  if (truncateIndex === 0) {
    truncateIndex = maxLength;
  }

  const truncated = text.slice(0, truncateIndex).trim();
  return showEllipsis ? `${truncated}...` : truncated;
}

// ============================================================================
// Main Component
// ============================================================================

/**
 * SearchHighlight component for rendering highlighted search results
 */
export const SearchHighlight: React.FC<SearchHighlightProps> = ({
  text,
  searchTerm,
  highlights: providedHighlights,
  caseSensitive = false,
  highlightClassName = 'bg-yellow-xs00 dark:bg-yellow-lg00/50 font-medium text-foreground rounded-sm px-0.5',
  className = '',
  as = 'span',
  maxLength,
  showEllipsis = true
}) => {
  // ============================================================================
  // Process Text and Highlights
  // ============================================================================

  // Handle text truncation
  const processedText = React.useMemo(() => {
    if (maxLength && text.length > maxLength) {
      return truncateText(text, maxLength, showEllipsis);
    }
    return text;
  }, [text, maxLength, showEllipsis]);

  // Get or create highlights
  const highlights = React.useMemo(() => {
    if (providedHighlights) {
      return providedHighlights;
    }
    
    if (searchTerm) {
      return createTextHighlights(processedText, searchTerm, caseSensitive);
    }
    
    return [{ 
      text: processedText, 
      isHighlighted: false, 
      startIndex: 0, 
      endIndex: processedText.length 
    }];
  }, [providedHighlights, searchTerm, processedText, caseSensitive]);

  // ============================================================================
  // Render Highlights
  // ============================================================================

  const Component = as as React.ElementType;
  
  return (
    <Component 
      className={cn('inline', className)}
      role="text"
      aria-label={`Text with search highlights: ${processedText}`}
    >
      {highlights.map((highlight, index) => {
        if (highlight.isHighlighted) {
          return (
            <mark
              key={`${highlight.startIndex}-${index}`}
              className={cn(highlightClassName)}
              role="mark"
              aria-label={`Highlighted text: ${highlight.text}`}
            >
              {highlight.text}
            </mark>
          );
        }
        
        return (
          <span 
            key={`${highlight.startIndex}-${index}`}
            className="inline"
          >
            {highlight.text}
          </span>
        );
      })}
    </Component>
  );
};

// ============================================================================
// Specialized Highlight Components
// ============================================================================

/** Props for SearchTitle component */
export interface SearchTitleProps extends Omit<SearchHighlightProps, 'as' | 'highlightClassName'> {
  /** Size variant for the title */
  size?: 'sm' | 'md' | 'lg' | 'xl';
}

/**
 * SearchTitle component for highlighted titles
 */
export const SearchTitle: React.FC<SearchTitleProps> = ({
  size = 'md',
  className = '',
  ...props
}) => {
  const sizeClasses = {
    sm: 'text-sm font-medium',
    md: 'text-base font-semibold', 
    lg: 'text-lg font-bold',
    xl: 'text-xl font-bold'
  };

  return (
    <SearchHighlight
      {...props}
      as="h3"
      className={cn(sizeClasses[size], className)}
      highlightClassName="bg-primary/20 dark:bg-primary/30 text-primary font-bold rounded-sm px-1"
    />
  );
};

/** Props for SearchDescription component */
export interface SearchDescriptionProps extends Omit<SearchHighlightProps, 'as' | 'highlightClassName'> {
  /** Number of lines before truncation */
  lines?: number;
}

/**
 * SearchDescription component for highlighted descriptions
 */
export const SearchDescription: React.FC<SearchDescriptionProps> = ({
  lines = 2,
  className = '',
  ...props
}) => {
  const lineClampClass = lines > 0 ? `line-clamp-${Math.min(lines, 6)}` : '';

  return (
    <SearchHighlight
      {...props}
      as="p"
      className={cn(
        'text-sm text-muted-foreground leading-relaxed',
        lineClampClass,
        className
      )}
      highlightClassName="bg-muted text-foreground font-medium rounded-sm px-0.5"
    />
  );
};

/** Props for SearchBadge component */
export interface SearchBadgeProps extends Omit<SearchHighlightProps, 'as' | 'highlightClassName'> {
  /** Badge variant */
  variant?: 'default' | 'secondary' | 'success' | 'warning' | 'destructive';
}

/**
 * SearchBadge component for highlighted badges/tags
 */
export const SearchBadge: React.FC<SearchBadgeProps> = ({
  variant = 'default',
  className = '',
  ...props
}) => {
  const variantClasses = {
    default: 'bg-primary text-primary-foreground',
    secondary: 'bg-secondary text-secondary-foreground',
    success: 'bg-green-500 text-white',
    warning: 'bg-yellow-sm00 text-white',
    destructive: 'bg-destructive text-destructive-foreground'
  };

  return (
    <SearchHighlight
      {...props}
      as="span"
      className={cn(
        'inline-flex items-center px-2 py-1 text-xs font-medium rounded-full',
        variantClasses[variant],
        className
      )}
      highlightClassName="bg-white/20 text-inherit font-bold rounded-sm px-1"
    />
  );
};

// ============================================================================
// Search Result List Component
// ============================================================================

/** Props for SearchResultList component */
export interface SearchResultListProps<T> {
  /** Search results to display */
  results: Array<{
    item: T;
    score: number;
    highlights: Record<string, TextHighlight[]>;
    matchedFields: string[];
  }>;
  
  /** Render function for each result item */
  renderItem: (props: {
    item: T;
    highlights: Record<string, TextHighlight[]>;
    matchedFields: string[];
    score: number;
    index: number;
  }) => React.ReactNode;
  
  /** No results message */
  noResultsMessage?: string;
  
  /** Loading state */
  isLoading?: boolean;
  
  /** Container class name */
  className?: string;
  
  /** Item class name */
  itemClassName?: string;
}

/**
 * SearchResultList component for rendering search results
 */
export const SearchResultList = <T,>({
  results,
  renderItem,
  noResultsMessage = 'No results found',
  isLoading = false,
  className = '',
  itemClassName = ''
}: SearchResultListProps<T>): React.ReactElement => {
  if (isLoading) {
    return (
      <div className={cn('flex justify-center py-8', className)}>
        <div className="animate-spin rounded-full h-md w-md border-b-2 border-primary" />
        <span className="ml-2 text-sm text-muted-foreground">Searching...</span>
      </div>
    );
  }

  if (results.length === 0) {
    return (
      <div className={cn('text-center py-8 text-muted-foreground', className)}>
        <p className="text-sm">{noResultsMessage}</p>
      </div>
    );
  }

  return (
    <div 
      className={cn('space-y-2', className)}
      role="list"
      aria-label={`Search results: ${results.length} items found`}
    >
      {results.map((result, index) => (
        <div 
          key={index}
          className={cn('border rounded-lg p-3 hover:bg-muted/50 transition-colors', itemClassName)}
          role="listitem"
        >
          {renderItem({
            item: result.item,
            highlights: result.highlights,
            matchedFields: result.matchedFields,
            score: result.score,
            index
          })}
        </div>
      ))}
    </div>
  );
};

// ============================================================================
// Export Default
// ============================================================================

export default SearchHighlight;