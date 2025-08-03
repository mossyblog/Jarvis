/**
 * SearchSuggestions Component
 * 
 * A dropdown component for displaying search suggestions, including:
 * - Search history
 * - Popular searches
 * - Real-time suggestions
 * - Keyboard navigation support
 * 
 * @module SearchSuggestions
 */

import React from 'react';
import { cn } from '../../lib/utils';
import { Button } from './button';
import { ScrollArea } from './scroll-area';
import { Separator } from './separator';
import { Badge } from './badge';
import { 
  Clock, 
  TrendingUp, 
  Search, 
  X, 
  ArrowUpRight,
  Star,
  History
} from 'lucide-react';
import type { SearchSuggestion } from '../../hooks/useSearchState';

// ============================================================================
// Component Props
// ============================================================================

/** Props for SearchSuggestions component */
export interface SearchSuggestionsProps {
  /** Array of suggestions to display */
  suggestions: SearchSuggestion[];
  
  /** Whether suggestions are currently visible */
  isVisible: boolean;
  
  /** Current search term */
  searchTerm: string;
  
  /** Currently focused suggestion index */
  focusedIndex?: number;
  
  /** Callback when a suggestion is selected */
  onSelect: (suggestion: SearchSuggestion) => void;
  
  /** Callback to close suggestions */
  onClose: () => void;
  
  /** Callback when suggestion is removed from history */
  onRemove?: (suggestion: SearchSuggestion) => void;
  
  /** Maximum suggestions to display per category */
  maxPerCategory?: number;
  
  /** Show category headers */
  showCategories?: boolean;
  
  /** Show suggestion type icons */
  showIcons?: boolean;
  
  /** Custom CSS classes */
  className?: string;
  
  /** Loading state */
  isLoading?: boolean;
  
  /** Empty state message */
  emptyMessage?: string;
}

/** Props for individual suggestion items */
export interface SuggestionItemProps {
  /** Suggestion data */
  suggestion: SearchSuggestion;
  
  /** Whether this item is focused */
  isFocused: boolean;
  
  /** Current search term for highlighting */
  searchTerm: string;
  
  /** Selection callback */
  onSelect: () => void;
  
  /** Remove callback */
  onRemove?: () => void;
  
  /** Show type icon */
  showIcon: boolean;
}

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Get icon component for suggestion type
 */
function getSuggestionIcon(type: SearchSuggestion['type']) {
  switch (type) {
    case 'history':
      return Clock;
    case 'popular':
      return TrendingUp;
    case 'recent':
      return History;
    case 'suggestion':
    default:
      return Search;
  }
}

/**
 * Get display label for suggestion type
 */
function getSuggestionTypeLabel(type: SearchSuggestion['type']): string {
  switch (type) {
    case 'history':
      return 'Recent Searches';
    case 'popular':
      return 'Popular';
    case 'recent':
      return 'Recent';
    case 'suggestion':
    default:
      return 'Suggestions';
  }
}

/**
 * Group suggestions by type
 */
function groupSuggestionsByType(
  suggestions: SearchSuggestion[],
  maxPerCategory: number
): Record<string, SearchSuggestion[]> {
  const groups: Record<string, SearchSuggestion[]> = {};
  
  for (const suggestion of suggestions) {
    const type = suggestion.type;
    if (!groups[type]) {
      groups[type] = [];
    }
    
    if (groups[type].length < maxPerCategory) {
      groups[type].push(suggestion);
    }
  }
  
  return groups;
}

/**
 * Highlight matching text in suggestion
 */
function highlightSuggestionText(text: string, searchTerm: string): React.ReactNode {
  if (!searchTerm.trim()) {
    return text;
  }
  
  const lowerText = text.toLowerCase();
  const lowerSearch = searchTerm.toLowerCase();
  const index = lowerText.indexOf(lowerSearch);
  
  if (index === -1) {
    return text;
  }
  
  return (
    <>
      {text.slice(0, index)}
      <span className="font-semibold text-foreground bg-primary/10 px-0.5 rounded-sm">
        {text.slice(index, index + searchTerm.length)}
      </span>
      {text.slice(index + searchTerm.length)}
    </>
  );
}

// ============================================================================
// Individual Suggestion Item Component
// ============================================================================

/**
 * Individual suggestion item component
 */
const SuggestionItem: React.FC<SuggestionItemProps> = ({
  suggestion,
  isFocused,
  searchTerm,
  onSelect,
  onRemove,
  showIcon
}) => {
  const Icon = getSuggestionIcon(suggestion.type);
  
  return (
    <div
      className={cn(
        'flex items-center justify-between px-3 py-2 cursor-pointer transition-colors',
        'hover:bg-muted/50 focus:bg-muted/50 focus:outline-none',
        isFocused && 'bg-muted/70'
      )}
      onClick={onSelect}
      onMouseDown={(e) => e.preventDefault()} // Prevent input blur
      role="option"
      aria-selected={isFocused}
      tabIndex={-1}
    >
      <div className="flex items-center space-x-3 flex-1 min-w-0">
        {showIcon && (
          <Icon 
            className={cn(
              'h-4 w-4 shrink-0',
              suggestion.type === 'popular' ? 'text-orange-500' : 'text-muted-foreground'
            )} 
          />
        )}
        
        <div className="flex-1 min-w-0">
          <div className="text-sm font-medium truncate">
            {highlightSuggestionText(suggestion.text, searchTerm)}
          </div>
          
          {suggestion.metadata?.resultCount !== undefined && (
            <div className="text-xs text-muted-foreground">
              {suggestion.metadata.resultCount} results
            </div>
          )}
        </div>
        
        <div className="flex items-center space-x-2 shrink-0">
          {suggestion.type === 'popular' && (
            <Badge variant="secondary" className="text-xs px-1.5 py-0.5">
              <Star className="h-3 w-3 mr-1" />
              Popular
            </Badge>
          )}
          
          {suggestion.usage > 1 && (
            <span className="text-xs text-muted-foreground">
              {suggestion.usage}x
            </span>
          )}
          
          {onRemove && suggestion.type === 'history' && (
            <Button
              variant="ghost"
              size="sm"
              className="h-6 w-6 p-0 opacity-0 group-hover:opacity-100 hover:bg-destructive hover:text-destructive-foreground"
              onClick={(e) => {
                e.stopPropagation();
                onRemove();
              }}
              aria-label={`Remove "${suggestion.text}" from history`}
            >
              <X className="h-3 w-3" />
            </Button>
          )}
          
          <ArrowUpRight className="h-3 w-3 text-muted-foreground opacity-0 group-hover:opacity-100" />
        </div>
      </div>
    </div>
  );
};

// ============================================================================
// Main SearchSuggestions Component
// ============================================================================

/**
 * SearchSuggestions dropdown component
 */
export const SearchSuggestions: React.FC<SearchSuggestionsProps> = ({
  suggestions,
  isVisible,
  searchTerm,
  focusedIndex = -1,
  onSelect,
  onClose,
  onRemove,
  maxPerCategory = 5,
  showCategories = true,
  showIcons = true,
  className = '',
  isLoading = false,
  emptyMessage = 'No suggestions available'
}) => {
  // Don't render if not visible
  if (!isVisible) {
    return null;
  }
  
  // Group suggestions by type if showing categories
  const groupedSuggestions = showCategories 
    ? groupSuggestionsByType(suggestions, maxPerCategory)
    : { all: suggestions.slice(0, maxPerCategory * 3) };
  
  const hasAnySuggestions = suggestions.length > 0;
  
  return (
    <div
      className={cn(
        'absolute top-full left-0 right-0 z-50 mt-1',
        'bg-popover border border-border rounded-md shadow-lg',
        'max-h-80 overflow-hidden',
        className
      )}
      role="listbox"
      aria-label="Search suggestions"
    >
      <ScrollArea className="max-h-80">
        {isLoading ? (
          // Loading state
          <div className="flex items-center justify-center py-6">
            <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-primary" />
            <span className="ml-2 text-sm text-muted-foreground">Loading suggestions...</span>
          </div>
        ) : !hasAnySuggestions ? (
          // Empty state
          <div className="py-6 px-3 text-center">
            <Search className="h-8 w-8 text-muted-foreground mx-auto mb-2" />
            <p className="text-sm text-muted-foreground">{emptyMessage}</p>
            {searchTerm.trim() && (
              <p className="text-xs text-muted-foreground mt-1">
                Try different keywords or check your spelling
              </p>
            )}
          </div>
        ) : (
          // Suggestions list
          <div className="py-1">
            {Object.entries(groupedSuggestions).map(([type, typeSuggestions], groupIndex) => {
              if (typeSuggestions.length === 0) return null;
              
              const typeLabel = showCategories ? getSuggestionTypeLabel(type as SearchSuggestion['type']) : null;
              
              return (
                <div key={type} className="group">
                  {showCategories && typeLabel && groupIndex > 0 && (
                    <Separator className="my-1" />
                  )}
                  
                  {showCategories && typeLabel && (
                    <div className="px-3 py-2 text-xs font-medium text-muted-foreground uppercase tracking-wider">
                      {typeLabel}
                    </div>
                  )}
                  
                  {typeSuggestions.map((suggestion, index) => {
                    // Calculate global index for focus management
                    let globalIndex = index;
                    if (showCategories) {
                      const previousGroups = Object.entries(groupedSuggestions)
                        .slice(0, groupIndex)
                        .reduce((acc, [, groupSuggestions]) => acc + groupSuggestions.length, 0);
                      globalIndex = previousGroups + index;
                    }
                    
                    return (
                      <SuggestionItem
                        key={suggestion.id}
                        suggestion={suggestion}
                        isFocused={globalIndex === focusedIndex}
                        searchTerm={searchTerm}
                        showIcon={showIcons}
                        onSelect={() => onSelect(suggestion)}
                        onRemove={onRemove ? () => onRemove(suggestion) : undefined}
                      />
                    );
                  })}
                </div>
              );
            })}
            
            {/* Footer with keyboard hints */}
            <Separator className="my-1" />
            <div className="px-3 py-2 text-xs text-muted-foreground flex items-center justify-between">
              <span>Use ↑↓ to navigate</span>
              <span>Press Enter to select</span>
            </div>
          </div>
        )}
      </ScrollArea>
    </div>
  );
};

// ============================================================================
// Search Input with Suggestions Component
// ============================================================================

/** Props for SearchInputWithSuggestions */
export interface SearchInputWithSuggestionsProps {
  /** Input value */
  value: string;
  
  /** Input change handler */
  onChange: (value: string) => void;
  
  /** Search suggestions */
  suggestions: SearchSuggestion[];
  
  /** Show suggestions */
  showSuggestions: boolean;
  
  /** Set show suggestions */
  setShowSuggestions: (show: boolean) => void;
  
  /** Select suggestion handler */
  onSelectSuggestion: (suggestion: SearchSuggestion) => void;
  
  /** Remove suggestion handler */
  onRemoveSuggestion?: (suggestion: SearchSuggestion) => void;
  
  /** Input placeholder */
  placeholder?: string;
  
  /** Loading state */
  isLoading?: boolean;
  
  /** Custom input className */
  inputClassName?: string;
  
  /** Custom suggestions className */
  suggestionsClassName?: string;
}

/**
 * Combined search input with suggestions dropdown
 */
export const SearchInputWithSuggestions: React.FC<SearchInputWithSuggestionsProps> = ({
  value,
  onChange,
  suggestions,
  showSuggestions,
  setShowSuggestions,
  onSelectSuggestion,
  onRemoveSuggestion,
  placeholder = 'Search...',
  isLoading = false,
  inputClassName = '',
  suggestionsClassName = ''
}) => {
  const [focusedIndex, setFocusedIndex] = React.useState(-1);
  const inputRef = React.useRef<HTMLInputElement>(null);
  
  // Handle keyboard navigation
  const handleKeyDown = React.useCallback((e: React.KeyboardEvent) => {
    if (!showSuggestions) return;
    
    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault();
        setFocusedIndex(prev => 
          prev < suggestions.length - 1 ? prev + 1 : 0
        );
        break;
        
      case 'ArrowUp':
        e.preventDefault();
        setFocusedIndex(prev => 
          prev > 0 ? prev - 1 : suggestions.length - 1
        );
        break;
        
      case 'Enter':
        e.preventDefault();
        if (focusedIndex >= 0 && suggestions[focusedIndex]) {
          onSelectSuggestion(suggestions[focusedIndex]);
          setShowSuggestions(false);
        }
        break;
        
      case 'Escape':
        e.preventDefault();
        setShowSuggestions(false);
        setFocusedIndex(-1);
        inputRef.current?.blur();
        break;
    }
  }, [showSuggestions, suggestions, focusedIndex, onSelectSuggestion, setShowSuggestions]);
  
  // Reset focused index when suggestions change
  React.useEffect(() => {
    setFocusedIndex(-1);
  }, [suggestions]);
  
  return (
    <div className="relative">
      <div className="relative">
        <input
          ref={inputRef}
          type="text"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          onKeyDown={handleKeyDown}
          onFocus={() => value.trim() && setShowSuggestions(true)}
          onBlur={() => {
            // Delay hiding suggestions to allow click on suggestion
            setTimeout(() => setShowSuggestions(false), 150);
          }}
          placeholder={placeholder}
          className={cn(
            'w-full px-3 py-2 border border-input bg-background rounded-md',
            'focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent',
            'placeholder:text-muted-foreground',
            inputClassName
          )}
          autoComplete="off"
          role="combobox"
          aria-expanded={showSuggestions}
          aria-haspopup="listbox"
          aria-autocomplete="list"
        />
        
        {isLoading && (
          <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-primary" />
          </div>
        )}
      </div>
      
      <SearchSuggestions
        suggestions={suggestions}
        isVisible={showSuggestions}
        searchTerm={value}
        focusedIndex={focusedIndex}
        onSelect={(suggestion) => {
          onSelectSuggestion(suggestion);
          setShowSuggestions(false);
        }}
        onClose={() => setShowSuggestions(false)}
        onRemove={onRemoveSuggestion}
        className={suggestionsClassName}
        isLoading={isLoading}
      />
    </div>
  );
};

// ============================================================================
// Export Default
// ============================================================================

export default SearchSuggestions;