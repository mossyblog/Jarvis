/**
 * Tests for useSearchState hook
 * 
 * @module UseSearchStateTest
 */

import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useSearchState } from '../useSearchState';

// Mock localStorage
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn(),
};
Object.defineProperty(window, 'localStorage', {
  value: localStorageMock
});

describe('useSearchState', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorageMock.getItem.mockReturnValue(null);
  });

  it('should initialize with empty search term', () => {
    const { result } = renderHook(() => useSearchState());
    
    expect(result.current.searchTerm).toBe('');
    expect(result.current.debouncedSearchTerm).toBe('');
    expect(result.current.canSearch).toBe(false);
    expect(result.current.hasSearched).toBe(false);
  });

  it('should initialize with provided search term', () => {
    const { result } = renderHook(() => useSearchState('test query'));
    
    expect(result.current.searchTerm).toBe('test query');
    expect(result.current.canSearch).toBe(true);
  });

  it('should update search term', () => {
    const { result } = renderHook(() => useSearchState());
    
    act(() => {
      result.current.setSearchTerm('new search');
    });
    
    expect(result.current.searchTerm).toBe('new search');
    expect(result.current.canSearch).toBe(true);
  });

  it('should clear search term', () => {
    const { result } = renderHook(() => useSearchState('initial'));
    
    act(() => {
      result.current.clearSearch();
    });
    
    expect(result.current.searchTerm).toBe('');
    expect(result.current.canSearch).toBe(false);
  });

  it('should highlight text correctly', () => {
    const { result } = renderHook(() => useSearchState('test'));
    
    const highlights = result.current.highlightText('This is a test string');
    
    expect(highlights).toHaveLength(3); // Before, highlight, after
    expect(highlights[0].text).toBe('This is a ');
    expect(highlights[0].isHighlighted).toBe(false);
    expect(highlights[1].text).toBe('test');
    expect(highlights[1].isHighlighted).toBe(true);
    expect(highlights[2].text).toBe(' string');
    expect(highlights[2].isHighlighted).toBe(false);
  });

  it('should search items and return scored results', () => {
    const { result } = renderHook(() => useSearchState('test'));
    
    const items = [
      { name: 'Test Item', description: 'A test description' },
      { name: 'Another Item', description: 'Different content' },
      { name: 'Testing Tool', description: 'For testing purposes' }
    ];
    
    const searchResults = result.current.searchItems(items, 'test');
    
    expect(searchResults).toHaveLength(2); // Only items with 'test'
    expect(searchResults[0].item.name).toBe('Test Item'); // Higher score
    expect(searchResults[0].score).toBeGreaterThan(0);
    expect(searchResults[0].matchedFields).toContain('name');
  });

  it('should manage search history', () => {
    const { result } = renderHook(() => 
      useSearchState('', { enableHistory: true })
    );
    
    act(() => {
      result.current.addToHistory('first search');
    });
    
    expect(result.current.searchHistory).toHaveLength(1);
    expect(result.current.searchHistory[0].term).toBe('first search');
    
    act(() => {
      result.current.addToHistory('second search');
    });
    
    expect(result.current.searchHistory).toHaveLength(2);
    expect(result.current.searchHistory[0].term).toBe('second search'); // Most recent first
  });

  it('should limit search history size', () => {
    const { result } = renderHook(() => 
      useSearchState('', { enableHistory: true, maxHistorySize: 2 })
    );
    
    act(() => {
      result.current.addToHistory('first');
      result.current.addToHistory('second');
      result.current.addToHistory('third');
    });
    
    expect(result.current.searchHistory).toHaveLength(2);
    expect(result.current.searchHistory[0].term).toBe('third');
    expect(result.current.searchHistory[1].term).toBe('second');
  });

  it('should generate suggestions from history and popular terms', () => {
    const { result } = renderHook(() => 
      useSearchState('te', { 
        enableSuggestions: true,
        popularTerms: ['test', 'template', 'team']
      })
    );
    
    act(() => {
      result.current.addToHistory('testing framework');
    });
    
    expect(result.current.suggestions.length).toBeGreaterThan(0);
    const suggestionTexts = result.current.suggestions.map(s => s.text);
    expect(suggestionTexts).toContain('test');
    expect(suggestionTexts).toContain('template');
  });

  it('should update filters', () => {
    const { result } = renderHook(() => useSearchState());
    
    act(() => {
      result.current.updateFilters({ caseSensitive: true });
    });
    
    expect(result.current.filters.caseSensitive).toBe(true);
  });

  it('should handle disabled features', () => {
    const { result } = renderHook(() => 
      useSearchState('', { 
        enableHistory: false,
        enableSuggestions: false,
        enableHighlighting: false
      })
    );
    
    act(() => {
      result.current.addToHistory('test');
    });
    
    expect(result.current.searchHistory).toHaveLength(0);
    expect(result.current.suggestions).toHaveLength(0);
    
    const highlights = result.current.highlightText('test string');
    expect(highlights).toHaveLength(1);
    expect(highlights[0].isHighlighted).toBe(false);
  });
});