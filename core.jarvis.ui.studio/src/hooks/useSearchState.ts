/**
 * useSearchState Hook
 * 
 * A comprehensive search state management hook with:
 * - Debounced search functionality (300ms)
 * - Search history tracking
 * - Search suggestions
 * - Result highlighting
 * - Integration with filtering systems
 * 
 * This implements the search state requirement from TASK.md:
 * "Create search state with debouncing"
 * 
 * @module UseSearchState
 */

import { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import { useSearchDebounce, type UseSearchDebounceOptions } from './useDebounce';

// ============================================================================
// Types and Interfaces
// ============================================================================

/** Search suggestion item */
export interface SearchSuggestion {
  /** Unique identifier for the suggestion */
  id: string;
  
  /** The suggestion text */
  text: string;
  
  /** Type of suggestion (history, popular, etc.) */
  type: 'history' | 'popular' | 'suggestion' | 'recent';
  
  /** Number of times this suggestion was used */
  usage: number;
  
  /** Last time this suggestion was used */
  lastUsed: Date;
  
  /** Category for grouping suggestions */
  category?: string;
  
  /** Additional metadata */
  metadata?: Record<string, any>;
}

/** Search history entry */
export interface SearchHistoryEntry {
  /** Unique identifier */
  id: string;
  
  /** Search term */
  term: string;
  
  /** When the search was performed */
  timestamp: Date;
  
  /** Number of results returned */
  resultCount?: number;
  
  /** How long the search took (ms) */
  duration?: number;
  
  /** User who performed the search */
  userId?: string;
}

/** Highlighted text segment */
export interface TextHighlight {
  /** The text content */
  text: string;
  
  /** Whether this segment should be highlighted */
  isHighlighted: boolean;
  
  /** Start index in original text */
  startIndex: number;
  
  /** End index in original text */
  endIndex: number;
}

/** Search filter configuration */
export interface SearchFilters {
  /** Fields to search in */
  fields?: string[];
  
  /** Case sensitive search */
  caseSensitive?: boolean;
  
  /** Use fuzzy matching */
  fuzzy?: boolean;
  
  /** Fuzzy matching threshold (0-1) */
  fuzzyThreshold?: number;
  
  /** Include partial matches */
  partialMatch?: boolean;
  
  /** Boost certain fields */
  fieldBoosts?: Record<string, number>;
}

/** Search options */
export interface SearchOptions extends UseSearchDebounceOptions {
  /** Enable search history tracking */
  enableHistory?: boolean;
  
  /** Maximum history entries to keep */
  maxHistorySize?: number;
  
  /** Enable search suggestions */
  enableSuggestions?: boolean;
  
  /** Maximum suggestions to show */
  maxSuggestions?: number;
  
  /** Enable result highlighting */
  enableHighlighting?: boolean;
  
  /** Search filters configuration */
  filters?: SearchFilters;
  
  /** Storage key for persisting history */
  storageKey?: string;
  
  /** Popular search terms to suggest */
  popularTerms?: string[];
}

/** Search result with highlighting */
export interface SearchResultItem<T = any> {
  /** The original item */
  item: T;
  
  /** Search relevance score (0-1) */
  score: number;
  
  /** Highlighted fields */
  highlights: Record<string, TextHighlight[]>;
  
  /** Fields that matched the search */
  matchedFields: string[];
  
  /** Additional metadata about the match */
  metadata?: Record<string, any>;
}

/** Search state result */
export interface UseSearchStateResult<T = any> {
  // Search term management
  searchTerm: string;
  debouncedSearchTerm: string;
  setSearchTerm: (term: string) => void;
  clearSearch: () => void;
  
  // Search state
  isSearching: boolean;
  hasSearched: boolean;
  canSearch: boolean;
  
  // Search history
  searchHistory: SearchHistoryEntry[];
  addToHistory: (term: string, resultCount?: number, duration?: number) => void;
  clearHistory: () => void;
  removeFromHistory: (id: string) => void;
  
  // Search suggestions
  suggestions: SearchSuggestion[];
  showSuggestions: boolean;
  setShowSuggestions: (show: boolean) => void;
  selectSuggestion: (suggestion: SearchSuggestion) => void;
  
  // Search results and highlighting
  highlightText: (text: string, searchTerm?: string) => TextHighlight[];
  searchItems: <TItem>(items: TItem[], searchTerm?: string) => SearchResultItem<TItem>[];
  
  // Search actions
  performSearch: () => void;
  cancelSearch: () => void;
  
  // Configuration
  updateFilters: (filters: Partial<SearchFilters>) => void;
  filters: SearchFilters;
}

// ============================================================================
// Local Storage Utilities
// ============================================================================

const DEFAULT_STORAGE_KEY = 'jarvis-ui-studio-search-history';

function getStoredSearchHistory(storageKey: string): SearchHistoryEntry[] {
  try {
    const stored = localStorage.getItem(storageKey);
    if (stored) {
      const parsed = JSON.parse(stored);
      return parsed.map((entry: any) => ({
        ...entry,
        timestamp: new Date(entry.timestamp),
        lastUsed: entry.lastUsed ? new Date(entry.lastUsed) : undefined
      }));
    }
  } catch (error) {
    console.warn('Failed to parse stored search history:', error);
  }
  return [];
}

function setStoredSearchHistory(storageKey: string, history: SearchHistoryEntry[]): void {
  try {
    localStorage.setItem(storageKey, JSON.stringify(history));
  } catch (error) {
    console.warn('Failed to store search history:', error);
  }
}

// ============================================================================
// Text Highlighting Utilities
// ============================================================================

/**
 * Create highlighted text segments
 */
function createHighlights(text: string, searchTerm: string, caseSensitive = false): TextHighlight[] {
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

// ============================================================================
// Search Scoring and Filtering
// ============================================================================

/**
 * Calculate search relevance score
 */
function calculateRelevanceScore(
  item: any,
  searchTerm: string,
  fields: string[],
  fieldBoosts: Record<string, number> = {},
  caseSensitive = false
): number {
  if (!searchTerm.trim()) return 1;

  let totalScore = 0;
  let fieldCount = 0;
  const searchQuery = caseSensitive ? searchTerm : searchTerm.toLowerCase();

  for (const field of fields) {
    const value = getNestedValue(item, field);
    if (value && typeof value === 'string') {
      const fieldText = caseSensitive ? value : value.toLowerCase();
      const boost = fieldBoosts[field] || 1;
      
      // Exact match gets highest score
      if (fieldText === searchQuery) {
        totalScore += 1.0 * boost;
      }
      // Starts with gets high score
      else if (fieldText.startsWith(searchQuery)) {
        totalScore += 0.8 * boost;
      }
      // Contains gets medium score
      else if (fieldText.includes(searchQuery)) {
        totalScore += 0.6 * boost;
      }
      // Word boundary match gets good score
      else if (new RegExp(`\\b${escapeRegex(searchQuery)}`, 'i').test(fieldText)) {
        totalScore += 0.7 * boost;
      }
      
      fieldCount++;
    }
  }

  return fieldCount > 0 ? totalScore / fieldCount : 0;
}

/**
 * Get nested object value by path
 */
function getNestedValue(obj: any, path: string): any {
  return path.split('.').reduce((current, key) => current?.[key], obj);
}

/**
 * Escape regex special characters
 */
function escapeRegex(string: string): string {
  return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

// ============================================================================
// Main Hook Implementation
// ============================================================================

/**
 * Comprehensive search state management hook
 */
export function useSearchState<T = any>(
  initialTerm: string = '',
  options: SearchOptions = {}
): UseSearchStateResult<T> {
  const {
    enableHistory = true,
    maxHistorySize = 50,
    enableSuggestions = true,
    maxSuggestions = 10,
    enableHighlighting = true,
    storageKey = DEFAULT_STORAGE_KEY,
    popularTerms = [],
    filters: initialFilters = {},
    ...debounceOptions
  } = options;

  // ============================================================================
  // State Management
  // ============================================================================

  const [searchHistory, setSearchHistory] = useState<SearchHistoryEntry[]>(() => {
    return enableHistory ? getStoredSearchHistory(storageKey) : [];
  });

  const [showSuggestions, setShowSuggestions] = useState(false);
  const [hasSearched, setHasSearched] = useState(false);
  const [filters, setFilters] = useState<SearchFilters>({
    fields: ['name', 'title', 'description'],
    caseSensitive: false,
    fuzzy: false,
    fuzzyThreshold: 0.8,
    partialMatch: true,
    fieldBoosts: { name: 2, title: 1.5, description: 1 },
    ...initialFilters
  });

  // Use search debounce for the search term
  const {
    searchTerm,
    debouncedSearchTerm,
    isSearchPending,
    canSearch,
    setSearchTerm: setSearchTermDebounced,
    clearSearch: clearSearchDebounced,
    triggerSearch,
    cancelSearch
  } = useSearchDebounce(initialTerm, {
    delay: 300,
    minLength: 1,
    ...debounceOptions,
    onSearch: (term) => {
      setHasSearched(true);
      if (enableHistory && term.trim()) {
        addToHistory(term);
      }
      debounceOptions.onSearch?.(term);
    }
  });

  // ============================================================================
  // Search History Management
  // ============================================================================

  const addToHistory = useCallback((term: string, resultCount?: number, duration?: number) => {
    if (!enableHistory || !term.trim()) return;

    const newEntry: SearchHistoryEntry = {
      id: `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      term: term.trim(),
      timestamp: new Date(),
      resultCount,
      duration
    };

    setSearchHistory(prev => {
      // Remove existing entry with same term
      const filtered = prev.filter(entry => entry.term !== newEntry.term);
      
      // Add new entry at the beginning
      const updated = [newEntry, ...filtered];
      
      // Limit to maxHistorySize
      const limited = updated.slice(0, maxHistorySize);
      
      // Persist to storage
      setStoredSearchHistory(storageKey, limited);
      
      return limited;
    });
  }, [enableHistory, maxHistorySize, storageKey]);

  const clearHistory = useCallback(() => {
    setSearchHistory([]);
    setStoredSearchHistory(storageKey, []);
  }, [storageKey]);

  const removeFromHistory = useCallback((id: string) => {
    setSearchHistory(prev => {
      const updated = prev.filter(entry => entry.id !== id);
      setStoredSearchHistory(storageKey, updated);
      return updated;
    });
  }, [storageKey]);

  // ============================================================================
  // Search Suggestions
  // ============================================================================

  const suggestions = useMemo(() => {
    if (!enableSuggestions || !searchTerm.trim()) {
      return [];
    }

    const term = searchTerm.toLowerCase();
    const suggestionSet = new Set<string>();
    const suggestions: SearchSuggestion[] = [];

    // Add matching history entries
    searchHistory
      .filter(entry => entry.term.toLowerCase().includes(term) && entry.term !== searchTerm)
      .slice(0, 5)
      .forEach(entry => {
        if (!suggestionSet.has(entry.term)) {
          suggestionSet.add(entry.term);
          suggestions.push({
            id: `history-${entry.id}`,
            text: entry.term,
            type: 'history',
            usage: 1,
            lastUsed: entry.timestamp,
            metadata: { resultCount: entry.resultCount }
          });
        }
      });

    // Add popular terms
    popularTerms
      .filter(popular => popular.toLowerCase().includes(term) && popular !== searchTerm)
      .slice(0, 3)
      .forEach(popular => {
        if (!suggestionSet.has(popular)) {
          suggestionSet.add(popular);
          suggestions.push({
            id: `popular-${popular}`,
            text: popular,
            type: 'popular',
            usage: 999,
            lastUsed: new Date()
          });
        }
      });

    // Sort by relevance and usage
    return suggestions
      .sort((a, b) => {
        // Prioritize exact prefix matches
        const aStartsWith = a.text.toLowerCase().startsWith(term);
        const bStartsWith = b.text.toLowerCase().startsWith(term);
        
        if (aStartsWith && !bStartsWith) return -1;
        if (!aStartsWith && bStartsWith) return 1;
        
        // Then by type (popular first, then history)
        if (a.type === 'popular' && b.type !== 'popular') return -1;
        if (a.type !== 'popular' && b.type === 'popular') return 1;
        
        // Finally by usage
        return b.usage - a.usage;
      })
      .slice(0, maxSuggestions);
  }, [enableSuggestions, searchTerm, searchHistory, popularTerms, maxSuggestions]);

  const selectSuggestion = useCallback((suggestion: SearchSuggestion) => {
    setSearchTermDebounced(suggestion.text);
    setShowSuggestions(false);
  }, [setSearchTermDebounced]);

  // ============================================================================
  // Search and Highlighting Functions
  // ============================================================================

  const highlightText = useCallback((text: string, searchTermOverride?: string): TextHighlight[] => {
    if (!enableHighlighting) {
      return [{ text, isHighlighted: false, startIndex: 0, endIndex: text.length }];
    }
    
    const term = searchTermOverride || debouncedSearchTerm;
    return createHighlights(text, term, filters.caseSensitive);
  }, [enableHighlighting, debouncedSearchTerm, filters.caseSensitive]);

  const searchItems = useCallback(<TItem,>(
    items: TItem[],
    searchTermOverride?: string
  ): SearchResultItem<TItem>[] => {
    const term = searchTermOverride || debouncedSearchTerm;
    
    if (!term.trim()) {
      return items.map(item => ({
        item,
        score: 1,
        highlights: {},
        matchedFields: []
      }));
    }

    const searchFields = filters.fields || ['name', 'title', 'description'];
    
    return items
      .map(item => {
        const score = calculateRelevanceScore(
          item,
          term,
          searchFields,
          filters.fieldBoosts,
          filters.caseSensitive
        );

        if (score === 0 && !filters.partialMatch) {
          return null;
        }

        const highlights: Record<string, TextHighlight[]> = {};
        const matchedFields: string[] = [];

        // Generate highlights for matching fields
        for (const field of searchFields) {
          const value = getNestedValue(item, field);
          if (value && typeof value === 'string') {
            const fieldHighlights = highlightText(value, term);
            const hasHighlight = fieldHighlights.some(h => h.isHighlighted);
            
            if (hasHighlight) {
              highlights[field] = fieldHighlights;
              matchedFields.push(field);
            }
          }
        }

        return {
          item,
          score,
          highlights,
          matchedFields
        };
      })
      .filter((result): result is SearchResultItem<TItem> => result !== null)
      .sort((a, b) => b.score - a.score);
  }, [debouncedSearchTerm, filters, highlightText]);

  // ============================================================================
  // Search Actions
  // ============================================================================

  const setSearchTerm = useCallback((term: string) => {
    setSearchTermDebounced(term);
    if (term.trim() && enableSuggestions) {
      setShowSuggestions(true);
    } else {
      setShowSuggestions(false);
    }
  }, [setSearchTermDebounced, enableSuggestions]);

  const clearSearch = useCallback(() => {
    clearSearchDebounced();
    setShowSuggestions(false);
    setHasSearched(false);
  }, [clearSearchDebounced]);

  const performSearch = useCallback(() => {
    triggerSearch();
    setShowSuggestions(false);
  }, [triggerSearch]);

  const updateFilters = useCallback((newFilters: Partial<SearchFilters>) => {
    setFilters(prev => ({ ...prev, ...newFilters }));
  }, []);

  // ============================================================================
  // Return Search State
  // ============================================================================

  return {
    // Search term management
    searchTerm,
    debouncedSearchTerm,
    setSearchTerm,
    clearSearch,
    
    // Search state
    isSearching: isSearchPending,
    hasSearched,
    canSearch,
    
    // Search history
    searchHistory,
    addToHistory,
    clearHistory,
    removeFromHistory,
    
    // Search suggestions
    suggestions,
    showSuggestions,
    setShowSuggestions,
    selectSuggestion,
    
    // Search results and highlighting
    highlightText,
    searchItems,
    
    // Search actions
    performSearch,
    cancelSearch,
    
    // Configuration
    updateFilters,
    filters
  };
}

// ============================================================================
// Export Default
// ============================================================================

export default useSearchState;