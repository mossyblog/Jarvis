/**
 * Template Gallery Grid Component
 * 
 * Visual template selection with preview cards, filtering, search,
 * and template application functionality.
 * 
 * Features:
 * - Grid layout with template thumbnails
 * - Template metadata display (title, description, rating, usage)
 * - Filtering and search for templates
 * - Preview modal for detailed template view
 * - Apply template functionality
 * - Responsive mobile-first design
 * - Keyboard navigation support
 * - Accessibility compliance
 * 
 * @module TemplateGalleryGrid
 */

import React, { useState, useCallback, useMemo, useRef } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';

// UIStudio hooks and services
import {
  useUIStudioTemplatesByOwner,
  useApplyUIStudioTemplate,
  useUIStudioErrorHandler
} from '../../hooks/useUIStudio';

// Shadcn/ui components
import { Button } from '../ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '../ui/card';
import { Input } from '../ui/input';
import { Badge } from '../ui/badge';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '../ui/dialog';
import { Label } from '../ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Tabs, TabsList, TabsTrigger } from '../ui/tabs';
import { ScrollArea } from '../ui/scroll-area';
import { LoadingSpinner } from '../ui/loading-spinner';
import { Separator } from '../ui/separator';

// Icons for navigation and actions
import { 
  Search,
  Filter,
  Grid3X3,
  List,
  Eye,
  Star,
  Users,
  Calendar,
  Tag,
  X,
  Trophy,
  Sparkles
} from 'lucide-react';

// UIStudio types
import type {
  UIStudioTemplate,
  UIStudioEntityId,
  ApplyTemplateRequest
} from '../../types/uistudio';

// ============================================================================
// Component Props Interface
// ============================================================================

/**
 * Props for the TemplateGalleryGrid component
 */
export interface TemplateGalleryGridProps {
  /** Current user entity ID for data filtering */
  userEntityId: UIStudioEntityId;
  
  /** Optional initial view mode */
  initialView?: 'grid' | 'list';
  
  /** Optional initial filters */
  initialFilters?: Partial<TemplateFilters>;
  
  /** Optional callback when a template is applied */
  onTemplateApply?: (template: UIStudioTemplate, targetPageName: string) => void;
  
  /** Optional callback when modal is closed */
  onClose?: () => void;
  
  /** Optional custom CSS classes */
  className?: string;
  
  /** Whether the gallery is open */
  isOpen?: boolean;
  
  /** Loading override */
  isLoading?: boolean;
  
  /** Error override */
  error?: string | null;
}

// ============================================================================
// State Management Interfaces
// ============================================================================

/**
 * Template filtering configuration
 */
export interface TemplateFilters {
  /** Search query string */
  search: string;
  
  /** Filter by template type */
  templateType: 'page' | 'layout' | 'component' | 'all';
  
  /** Filter by category */
  category: string;
  
  /** Filter by public/private */
  visibility: 'public' | 'private' | 'all';
  
  /** Sort configuration */
  sortBy: 'name' | 'usage' | 'created' | 'updated' | 'rating';
  
  /** Sort direction */
  sortDirection: 'asc' | 'desc';
  
  /** Tags filter */
  tags: string[];
}

/**
 * Template gallery view state
 */
export interface TemplateViewState {
  /** Current view mode */
  mode: 'grid' | 'list';
  
  /** Cards per row for grid view */
  cardsPerRow: number;
  
  /** Show preview images */
  showPreviews: boolean;
}

/**
 * Template preview modal state
 */
export interface TemplatePreviewState {
  /** Whether modal is open */
  isOpen: boolean;
  
  /** Currently previewed template */
  template: UIStudioTemplate | null;
  
  /** Loading state for template application */
  applying: boolean;
  
  /** Error state for template application */
  error: string | null;
}

/**
 * Template application form state
 */
export interface TemplateApplicationForm {
  /** Target page name */
  pageName: string;
  
  /** Target page slug */
  pageSlug: string;
  
  /** Whether to validate form */
  validate: boolean;
}

// ============================================================================
// Mock Template Data (for demonstration)
// ============================================================================

const MOCK_TEMPLATES: UIStudioTemplate[] = [
  {
    id: 'template-1',
    ownerEntityId: 'user-1',
    lastUpdated: new Date().toISOString(),
    templateName: 'Modern Dashboard',
    description: 'A clean, modern dashboard template with analytics cards and charts. Perfect for business applications.',
    templateType: 'page',
    category: 'Dashboard',
    templateData: {},
    defaultValues: {},
    isPublic: true,
    usageCount: 245,
    tags: '["dashboard", "analytics", "modern", "business"]',
    previewImage: 'https://images.unsplash.com/photo-1551288049-bebda4e38f71?w=400&h=300&fit=crop',
    createdByEntityId: 'user-1',
    updatedByEntityId: 'user-1'
  },
  {
    id: 'template-2',
    ownerEntityId: 'user-1',
    lastUpdated: new Date().toISOString(),
    templateName: 'E-commerce Product Grid',
    description: 'Responsive product grid layout with filtering and pagination. Ideal for online stores.',
    templateType: 'page',
    category: 'E-commerce',
    templateData: {},
    defaultValues: {},
    isPublic: true,
    usageCount: 189,
    tags: '["ecommerce", "products", "grid", "shopping"]',
    previewImage: 'https://images.unsplash.com/photo-1556742049-0cfed4f6a45d?w=400&h=300&fit=crop',
    createdByEntityId: 'user-2',
    updatedByEntityId: 'user-2'
  },
  {
    id: 'template-3',
    ownerEntityId: 'user-1',
    lastUpdated: new Date().toISOString(),
    templateName: 'Blog Layout',
    description: 'Clean blog layout with sidebar, featured posts, and comment sections.',
    templateType: 'page',
    category: 'Blog',
    templateData: {},
    defaultValues: {},
    isPublic: true,
    usageCount: 156,
    tags: '["blog", "content", "sidebar", "posts"]',
    previewImage: 'https://images.unsplash.com/photo-1486312338219-ce68d2c6f44d?w=400&h=300&fit=crop',
    createdByEntityId: 'user-3',
    updatedByEntityId: 'user-3'
  },
  {
    id: 'template-4',
    ownerEntityId: 'user-1',
    lastUpdated: new Date().toISOString(),
    templateName: 'Data Table Component',
    description: 'Advanced data table with sorting, filtering, pagination, and bulk actions.',
    templateType: 'component',
    category: 'Data Display',
    templateData: {},
    defaultValues: {},
    isPublic: true,
    usageCount: 892,
    tags: '["table", "data", "sorting", "filtering"]',
    previewImage: 'https://images.unsplash.com/photo-1460925895917-afdab827c52f?w=400&h=300&fit=crop',
    createdByEntityId: 'user-4',
    updatedByEntityId: 'user-4'
  },
  {
    id: 'template-5',
    ownerEntityId: 'user-1',
    lastUpdated: new Date().toISOString(),
    templateName: 'Landing Page Hero',
    description: 'Conversion-optimized landing page with hero section, features, and CTA.',
    templateType: 'page',
    category: 'Marketing',
    templateData: {},
    defaultValues: {},
    isPublic: true,
    usageCount: 634,
    tags: '["landing", "hero", "marketing", "conversion"]',
    previewImage: 'https://images.unsplash.com/photo-1557804506-669a67965ba0?w=400&h=300&fit=crop',
    createdByEntityId: 'user-5',
    updatedByEntityId: 'user-5'
  },
  {
    id: 'template-6',
    ownerEntityId: 'user-1',
    lastUpdated: new Date().toISOString(),
    templateName: 'Admin Panel Layout',
    description: 'Comprehensive admin panel layout with navigation, user management, and settings.',
    templateType: 'layout',
    category: 'Admin',
    templateData: {},
    defaultValues: {},
    isPublic: false,
    usageCount: 78,
    tags: '["admin", "panel", "navigation", "management"]',
    previewImage: 'https://images.unsplash.com/photo-1551650975-87deedd944c3?w=400&h=300&fit=crop',
    createdByEntityId: 'user-1',
    updatedByEntityId: 'user-1'
  }
];

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Calculate template rating based on usage and recency
 */
const calculateTemplateRating = (template: UIStudioTemplate): number => {
  const usageScore = Math.min((template.usageCount || 0) / 100, 5);
  const recencyScore = (() => {
    const daysSinceUpdate = (Date.now() - new Date(template.lastUpdated).getTime()) / (1000 * 60 * 60 * 24);
    return Math.max(0, 5 - (daysSinceUpdate / 30)); // 5 stars for recent, decreasing over 30 days
  })();
  
  return Math.min(5, (usageScore + recencyScore) / 2);
};

/**
 * Parse tags from JSON string or array
 */
const parseTags = (tags: string | undefined): string[] => {
  if (!tags) return [];
  
  try {
    return typeof tags === 'string' ? JSON.parse(tags) : tags;
  } catch {
    return [];
  }
};

/**
 * Generate slug from template name
 */
const generateSlug = (name: string): string => {
  return name
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
};

/**
 * Get all unique categories from templates
 */
const getUniqueCategories = (templates: UIStudioTemplate[]): string[] => {
  const categories = templates
    .map(template => template.category)
    .filter((category): category is string => Boolean(category));
  
  return Array.from(new Set(categories)).sort();
};

/**
 * Get all unique tags from templates
 */
const getUniqueTags = (templates: UIStudioTemplate[]): string[] => {
  const allTags = templates
    .flatMap(template => parseTags(template.tags));
  
  return Array.from(new Set(allTags)).sort();
};

// ============================================================================
// Component Implementation
// ============================================================================

/**
 * TemplateGalleryGrid - Visual template selection component
 */
export const TemplateGalleryGrid: React.FC<TemplateGalleryGridProps> = ({
  userEntityId,
  initialView = 'grid',
  initialFilters = {},
  onTemplateApply,
  onClose,
  className = '',
  isOpen = true,
  isLoading: loadingOverride,
  error: errorOverride
}) => {
  const navigate = useNavigate();
  const location = useLocation();
  const { handleError } = useUIStudioErrorHandler();
  const containerRef = useRef<HTMLDivElement | null>(null);

  // ============================================================================
  // State Management
  // ============================================================================

  const [filters, setFilters] = useState<TemplateFilters>(() => ({
    search: '',
    templateType: 'all',
    category: '',
    visibility: 'all',
    sortBy: 'usage',
    sortDirection: 'desc',
    tags: [],
    ...initialFilters
  }));

  const [viewState, setViewState] = useState<TemplateViewState>(() => ({
    mode: initialView,
    cardsPerRow: 3,
    showPreviews: true
  }));

  const [previewState, setPreviewState] = useState<TemplatePreviewState>(() => ({
    isOpen: false,
    template: null,
    applying: false,
    error: null
  }));

  const [applicationForm, setApplicationForm] = useState<TemplateApplicationForm>(() => ({
    pageName: '',
    pageSlug: '',
    validate: false
  }));

  // ============================================================================
  // Data Fetching
  // ============================================================================

  // For now, we'll use mock data. In production, this would fetch from the API
  const templatesResult = useUIStudioTemplatesByOwner(userEntityId);
  
  // Use mock templates for demonstration
  const allTemplates = useMemo(() => MOCK_TEMPLATES, []);

  const applyTemplateMutation = useApplyUIStudioTemplate(previewState.template?.id || '');

  // ============================================================================
  // Computed Values
  // ============================================================================

  const isLoading = useMemo(() => {
    return loadingOverride ?? (
      templatesResult.isLoading ||
      previewState.applying
    );
  }, [loadingOverride, templatesResult.isLoading, previewState.applying]);

  const hasError = useMemo(() => {
    return errorOverride ?? (
      templatesResult.error?.message ||
      previewState.error
    );
  }, [errorOverride, templatesResult.error?.message, previewState.error]);

  // Filter and sort templates
  const filteredTemplates = useMemo(() => {
    let templates = [...allTemplates];

    // Apply search filter
    if (filters.search.trim()) {
      const searchTerm = filters.search.toLowerCase();
      templates = templates.filter(template =>
        template.templateName.toLowerCase().includes(searchTerm) ||
        template.description?.toLowerCase().includes(searchTerm) ||
        template.category?.toLowerCase().includes(searchTerm) ||
        parseTags(template.tags).some(tag => tag.toLowerCase().includes(searchTerm))
      );
    }

    // Apply template type filter
    if (filters.templateType !== 'all') {
      templates = templates.filter(template => template.templateType === filters.templateType);
    }

    // Apply category filter
    if (filters.category) {
      templates = templates.filter(template => template.category === filters.category);
    }

    // Apply visibility filter
    if (filters.visibility !== 'all') {
      templates = templates.filter(template => 
        filters.visibility === 'public' ? template.isPublic : !template.isPublic
      );
    }

    // Apply tags filter
    if (filters.tags.length > 0) {
      templates = templates.filter(template => {
        const templateTags = parseTags(template.tags);
        return filters.tags.some(filterTag => templateTags.includes(filterTag));
      });
    }

    // Apply sorting
    templates.sort((a, b) => {
      let aValue: string | number;
      let bValue: string | number;

      switch (filters.sortBy) {
        case 'name':
          aValue = a.templateName.toLowerCase();
          bValue = b.templateName.toLowerCase();
          break;
        case 'usage':
          aValue = a.usageCount || 0;
          bValue = b.usageCount || 0;
          break;
        case 'created':
          aValue = new Date(a.lastUpdated).getTime();
          bValue = new Date(b.lastUpdated).getTime();
          break;
        case 'updated':
          aValue = new Date(a.lastUpdated).getTime();
          bValue = new Date(b.lastUpdated).getTime();
          break;
        case 'rating':
          aValue = calculateTemplateRating(a);
          bValue = calculateTemplateRating(b);
          break;
        default:
          aValue = a.templateName.toLowerCase();
          bValue = b.templateName.toLowerCase();
      }

      if (filters.sortDirection === 'asc') {
        return aValue < bValue ? -1 : aValue > bValue ? 1 : 0;
      } else {
        return aValue > bValue ? -1 : aValue < bValue ? 1 : 0;
      }
    });

    return templates;
  }, [allTemplates, filters]);

  const uniqueCategories = useMemo(() => getUniqueCategories(allTemplates), [allTemplates]);
  const uniqueTags = useMemo(() => getUniqueTags(allTemplates), [allTemplates]);

  // ============================================================================
  // Event Handlers
  // ============================================================================

  const handleFilterChange = useCallback(<K extends keyof TemplateFilters>(
    key: K,
    value: TemplateFilters[K]
  ) => {
    setFilters(prev => ({
      ...prev,
      [key]: value
    }));
  }, []);

  const handleViewChange = useCallback((mode: 'grid' | 'list') => {
    setViewState(prev => ({
      ...prev,
      mode
    }));
  }, []);

  const handleTemplatePreview = useCallback((template: UIStudioTemplate) => {
    setPreviewState(prev => ({
      ...prev,
      isOpen: true,
      template,
      error: null
    }));
    
    // Pre-populate form with template name
    setApplicationForm({
      pageName: `${template.templateName} Page`,
      pageSlug: generateSlug(template.templateName),
      validate: false
    });
  }, []);

  const handleClosePreview = useCallback(() => {
    setPreviewState(prev => ({
      ...prev,
      isOpen: false,
      template: null,
      applying: false,
      error: null
    }));
    
    setApplicationForm({
      pageName: '',
      pageSlug: '',
      validate: false
    });
  }, []);

  const handleApplyTemplate = useCallback(async () => {
    if (!previewState.template) return;

    // Validate form
    setApplicationForm(prev => ({ ...prev, validate: true }));
    
    if (!applicationForm.pageName.trim() || !applicationForm.pageSlug.trim()) {
      return;
    }

    setPreviewState(prev => ({ ...prev, applying: true, error: null }));

    try {
      const request: ApplyTemplateRequest = {
        pageName: applicationForm.pageName.trim(),
        pageSlug: applicationForm.pageSlug.trim(),
        createdByEntityId: userEntityId
      };

      const result = await applyTemplateMutation.mutateAsync(request);
      
      // Call callback if provided
      onTemplateApply?.(previewState.template, applicationForm.pageName);
      
      // Navigate to the new page
      if (result && result.length > 0) {
        navigate(`/studio/page/${result[0].id}`);
      }
      
      // Close preview modal
      handleClosePreview();
      
    } catch (error) {
      const errorResult = handleError(error, 'apply_template');
      setPreviewState(prev => ({
        ...prev,
        applying: false,
        error: errorResult.userMessage
      }));
    }
  }, [previewState.template, applicationForm, userEntityId, applyTemplateMutation, onTemplateApply, navigate, handleClosePreview, handleError]);

  const handleApplicationFormChange = useCallback((field: keyof TemplateApplicationForm, value: string) => {
    setApplicationForm(prev => {
      const newForm = { ...prev, [field]: value };
      
      // Auto-generate slug from page name
      if (field === 'pageName') {
        newForm.pageSlug = generateSlug(value);
      }
      
      return newForm;
    });
  }, []);

  // ============================================================================
  // Render Star Rating
  // ============================================================================

  const renderStarRating = useCallback((rating: number) => {
    const stars = [];
    const fullStars = Math.floor(rating);
    const hasHalfStar = rating % 1 >= 0.5;

    for (let i = 0; i < 5; i++) {
      if (i < fullStars) {
        stars.push(
          <Star key={i} className="h-3 w-3 fill-yellow-400 text-yellow-400" />
        );
      } else if (i === fullStars && hasHalfStar) {
        stars.push(
          <Star key={i} className="h-3 w-3 fill-yellow-200 text-yellow-400" />
        );
      } else {
        stars.push(
          <Star key={i} className="h-3 w-3 text-gray-300" />
        );
      }
    }

    return (
      <div className="flex items-center space-x-0.5">
        {stars}
        <span className="ml-1 text-xs text-muted-foreground">
          ({rating.toFixed(1)})
        </span>
      </div>
    );
  }, []);

  // ============================================================================
  // Render Component
  // ============================================================================

  if (!isOpen) {
    return null;
  }

  if (isLoading) {
    return (
      <div className={`flex items-center justify-center min-h-96 ${className}`}>
        <LoadingSpinner />
        <span className="ml-2 text-lg">Loading templates...</span>
      </div>
    );
  }

  if (hasError) {
    return (
      <div className={`flex flex-col items-center justify-center min-h-96 ${className}`}>
        <div className="text-red-500 text-lg mb-4">Error loading templates</div>
        <div className="text-gray-600 mb-4">{hasError}</div>
        <Button onClick={() => templatesResult.refetch()}>
          Retry
        </Button>
      </div>
    );
  }

  return (
    <div 
      ref={containerRef}
      className={`bg-background ${className}`}
      role="main"
      aria-label="Template gallery"
    >
      {/* Header Section */}
      <div className="mb-6">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h2 className="text-2xl font-bold text-foreground">Template Gallery</h2>
            <p className="text-sm text-muted-foreground">
              Choose from {filteredTemplates.length} professional templates
            </p>
          </div>
          {onClose && (
            <Button variant="ghost" size="sm" onClick={onClose}>
              <X className="h-4 w-4" />
            </Button>
          )}
        </div>

        {/* Filter and Search Section */}
        <div className="space-y-4">
          {/* Search and View Toggle */}
          <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
            <div className="relative flex-1 max-w-sm">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search templates..."
                value={filters.search}
                onChange={(e) => handleFilterChange('search', e.target.value)}
                className="pl-10"
                aria-label="Search templates"
              />
            </div>
            
            <Tabs 
              value={viewState.mode} 
              onValueChange={(value) => handleViewChange(value as 'grid' | 'list')}
              className="shrink-0"
            >
              <TabsList>
                <TabsTrigger value="grid" className="flex items-center gap-2">
                  <Grid3X3 className="h-4 w-4" />
                  Grid
                </TabsTrigger>
                <TabsTrigger value="list" className="flex items-center gap-2">
                  <List className="h-4 w-4" />
                  List
                </TabsTrigger>
              </TabsList>
            </Tabs>
          </div>

          {/* Filters */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
            <div>
              <Label htmlFor="template-type" className="text-sm font-medium">
                Type
              </Label>
              <Select
                value={filters.templateType}
                onValueChange={(value) => handleFilterChange('templateType', value as any)}
              >
                <SelectTrigger>
                  <SelectValue placeholder="All Types" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Types</SelectItem>
                  <SelectItem value="page">Page</SelectItem>
                  <SelectItem value="layout">Layout</SelectItem>
                  <SelectItem value="component">Component</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div>
              <Label htmlFor="category" className="text-sm font-medium">
                Category
              </Label>
              <Select
                value={filters.category}
                onValueChange={(value) => handleFilterChange('category', value)}
              >
                <SelectTrigger>
                  <SelectValue placeholder="All Categories" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">All Categories</SelectItem>
                  {uniqueCategories.map((category) => (
                    <SelectItem key={category} value={category}>
                      {category}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div>
              <Label htmlFor="visibility" className="text-sm font-medium">
                Visibility
              </Label>
              <Select
                value={filters.visibility}
                onValueChange={(value) => handleFilterChange('visibility', value as any)}
              >
                <SelectTrigger>
                  <SelectValue placeholder="All" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All</SelectItem>
                  <SelectItem value="public">Public</SelectItem>
                  <SelectItem value="private">Private</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div>
              <Label htmlFor="sort-by" className="text-sm font-medium">
                Sort By
              </Label>
              <Select
                value={filters.sortBy}
                onValueChange={(value) => handleFilterChange('sortBy', value as any)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="usage">Most Used</SelectItem>
                  <SelectItem value="rating">Highest Rated</SelectItem>
                  <SelectItem value="name">Name</SelectItem>
                  <SelectItem value="updated">Recently Updated</SelectItem>
                  <SelectItem value="created">Recently Created</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div>
              <Label htmlFor="sort-direction" className="text-sm font-medium">
                Order
              </Label>
              <Select
                value={filters.sortDirection}
                onValueChange={(value) => handleFilterChange('sortDirection', value as any)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="desc">Descending</SelectItem>
                  <SelectItem value="asc">Ascending</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Results Summary */}
          <div className="flex items-center justify-between pt-2 border-t border-border">
            <div className="text-sm text-muted-foreground">
              Showing {filteredTemplates.length} template{filteredTemplates.length !== 1 ? 's' : ''}
            </div>
            {filters.search && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => handleFilterChange('search', '')}
              >
                Clear search
              </Button>
            )}
          </div>
        </div>
      </div>

      {/* Templates Grid/List */}
      <ScrollArea className="h-[calc(100vh-24rem)]">
        {filteredTemplates.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <Filter className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-medium mb-2">No templates found</h3>
            <p className="text-sm text-muted-foreground mb-4">
              Try adjusting your filters or search terms
            </p>
            <Button
              variant="outline"
              onClick={() => {
                setFilters({
                  search: '',
                  templateType: 'all',
                  category: '',
                  visibility: 'all',
                  sortBy: 'usage',
                  sortDirection: 'desc',
                  tags: []
                });
              }}
            >
              Clear all filters
            </Button>
          </div>
        ) : (
          <div 
            className={
              viewState.mode === 'grid'
                ? `grid gap-6 pb-6
                   grid-cols-1
                   sm:grid-cols-2
                   lg:grid-cols-3
                   xl:grid-cols-4`
                : 'space-y-4 pb-6'
            }
          >
            {filteredTemplates.map((template) => {
              const rating = calculateTemplateRating(template);
              const tags = parseTags(template.tags);

              if (viewState.mode === 'list') {
                return (
                  <Card 
                    key={template.id}
                    className="cursor-pointer hover:shadow-md transition-all duration-200 hover:scale-[1.01]"
                    onClick={() => handleTemplatePreview(template)}
                  >
                    <div className="flex p-6">
                      {/* Preview Image */}
                      {template.previewImage && (
                        <div className="w-32 h-24 rounded-lg overflow-hidden mr-6 shrink-0">
                          <img
                            src={template.previewImage}
                            alt={`${template.templateName} preview`}
                            className="w-full h-full object-cover"
                          />
                        </div>
                      )}
                      
                      {/* Content */}
                      <div className="flex-1 min-w-0">
                        <div className="flex items-start justify-between mb-2">
                          <div className="min-w-0 flex-1">
                            <h3 className="text-lg font-semibold truncate">{template.templateName}</h3>
                            <p className="text-sm text-muted-foreground line-clamp-2 mt-1">
                              {template.description}
                            </p>
                          </div>
                          
                          <div className="flex items-center gap-2 ml-4 shrink-0">
                            <Badge variant={template.isPublic ? 'default' : 'secondary'}>
                              {template.isPublic ? 'Public' : 'Private'}
                            </Badge>
                            <Badge variant="outline">
                              {template.templateType}
                            </Badge>
                          </div>
                        </div>
                        
                        <div className="flex items-center gap-4 text-sm text-muted-foreground">
                          {renderStarRating(rating)}
                          <div className="flex items-center gap-1">
                            <Users className="h-3 w-3" />
                            {template.usageCount} uses
                          </div>
                          <div className="flex items-center gap-1">
                            <Tag className="h-3 w-3" />
                            {template.category}
                          </div>
                        </div>
                      </div>
                      
                      {/* Action Button */}
                      <div className="ml-4 shrink-0">
                        <Button variant="outline" size="sm">
                          <Eye className="h-4 w-4 mr-2" />
                          Preview
                        </Button>
                      </div>
                    </div>
                  </Card>
                );
              }

              return (
                <Card 
                  key={template.id}
                  className="cursor-pointer hover:shadow-md transition-all duration-200 hover:scale-[1.02] group flex flex-col h-full"
                  onClick={() => handleTemplatePreview(template)}
                >
                  {/* Preview Image */}
                  {template.previewImage && (
                    <div className="aspect-video rounded-t-lg overflow-hidden">
                      <img
                        src={template.previewImage}
                        alt={`${template.templateName} preview`}
                        className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200"
                      />
                    </div>
                  )}
                  
                  <CardHeader className="pb-2">
                    <div className="flex items-start justify-between gap-2">
                      <CardTitle className="text-base font-semibold line-clamp-2">
                        {template.templateName}
                      </CardTitle>
                      <div className="flex items-center gap-1 shrink-0">
                        <Badge variant={template.isPublic ? 'default' : 'secondary'} className="text-xs">
                          {template.isPublic ? 'Public' : 'Private'}
                        </Badge>
                      </div>
                    </div>
                    
                    <div className="flex items-center gap-2">
                      <Badge variant="outline" className="text-xs">
                        {template.templateType}
                      </Badge>
                      {template.category && (
                        <Badge variant="outline" className="text-xs">
                          {template.category}
                        </Badge>
                      )}
                    </div>
                  </CardHeader>
                  
                  <CardContent className="flex-1 pb-2">
                    <CardDescription className="text-sm line-clamp-3 mb-3">
                      {template.description}
                    </CardDescription>
                    
                    <div className="space-y-2">
                      {renderStarRating(rating)}
                      
                      <div className="flex items-center justify-between text-xs text-muted-foreground">
                        <div className="flex items-center gap-1">
                          <Users className="h-3 w-3" />
                          {template.usageCount} uses
                        </div>
                        <div className="flex items-center gap-1">
                          <Calendar className="h-3 w-3" />
                          {new Date(template.lastUpdated).toLocaleDateString()}
                        </div>
                      </div>
                    </div>

                    {/* Tags */}
                    {tags.length > 0 && (
                      <div className="flex flex-wrap gap-1 mt-2">
                        {tags.slice(0, 3).map((tag) => (
                          <Badge key={tag} variant="outline" className="text-xs px-1.5 py-0.5">
                            {tag}
                          </Badge>
                        ))}
                        {tags.length > 3 && (
                          <Badge variant="outline" className="text-xs px-1.5 py-0.5">
                            +{tags.length - 3}
                          </Badge>
                        )}
                      </div>
                    )}
                  </CardContent>
                  
                  <CardFooter className="pt-2">
                    <Button variant="outline" size="sm" className="w-full">
                      <Eye className="h-4 w-4 mr-2" />
                      Preview & Apply
                    </Button>
                  </CardFooter>
                </Card>
              );
            })}
          </div>
        )}
      </ScrollArea>

      {/* Template Preview Modal */}
      <Dialog open={previewState.isOpen} onOpenChange={handleClosePreview}>
        <DialogContent className="max-w-4xl max-h-[90vh] overflow-hidden">
          {previewState.template && (
            <>
              <DialogHeader>
                <DialogTitle className="flex items-center gap-3">
                  <span>{previewState.template.templateName}</span>
                  <div className="flex items-center gap-2">
                    <Badge variant={previewState.template.isPublic ? 'default' : 'secondary'}>
                      {previewState.template.isPublic ? 'Public' : 'Private'}
                    </Badge>
                    <Badge variant="outline">
                      {previewState.template.templateType}
                    </Badge>
                  </div>
                </DialogTitle>
                <DialogDescription>
                  {previewState.template.description}
                </DialogDescription>
              </DialogHeader>

              <div className="space-y-6">
                {/* Template Preview */}
                {previewState.template.previewImage && (
                  <div className="aspect-video rounded-lg overflow-hidden border">
                    <img
                      src={previewState.template.previewImage}
                      alt={`${previewState.template.templateName} preview`}
                      className="w-full h-full object-cover"
                    />
                  </div>
                )}

                {/* Template Info */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <Card>
                    <CardContent className="pt-4">
                      <div className="flex items-center gap-2 text-sm">
                        <Trophy className="h-4 w-4 text-yellow-500" />
                        <span className="font-medium">Rating</span>
                      </div>
                      {renderStarRating(calculateTemplateRating(previewState.template))}
                    </CardContent>
                  </Card>
                  
                  <Card>
                    <CardContent className="pt-4">
                      <div className="flex items-center gap-2 text-sm">
                        <Users className="h-4 w-4 text-blue-500" />
                        <span className="font-medium">Usage</span>
                      </div>
                      <p className="text-lg font-semibold">{previewState.template.usageCount} times</p>
                    </CardContent>
                  </Card>
                  
                  <Card>
                    <CardContent className="pt-4">
                      <div className="flex items-center gap-2 text-sm">
                        <Calendar className="h-4 w-4 text-green-500" />
                        <span className="font-medium">Updated</span>
                      </div>
                      <p className="text-lg font-semibold">
                        {new Date(previewState.template.lastUpdated).toLocaleDateString()}
                      </p>
                    </CardContent>
                  </Card>
                </div>

                {/* Tags */}
                {parseTags(previewState.template.tags).length > 0 && (
                  <div>
                    <h4 className="font-medium mb-2">Tags</h4>
                    <div className="flex flex-wrap gap-2">
                      {parseTags(previewState.template.tags).map((tag) => (
                        <Badge key={tag} variant="outline">
                          {tag}
                        </Badge>
                      ))}
                    </div>
                  </div>
                )}

                {/* Application Form */}
                <Separator />
                
                <div className="space-y-4">
                  <h4 className="font-medium">Apply Template</h4>
                  
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label htmlFor="page-name">Page Name *</Label>
                      <Input
                        id="page-name"
                        value={applicationForm.pageName}
                        onChange={(e) => handleApplicationFormChange('pageName', e.target.value)}
                        placeholder="Enter page name"
                        className={applicationForm.validate && !applicationForm.pageName.trim() ? 'border-red-500' : ''}
                      />
                      {applicationForm.validate && !applicationForm.pageName.trim() && (
                        <p className="text-sm text-red-500">Page name is required</p>
                      )}
                    </div>
                    
                    <div className="space-y-2">
                      <Label htmlFor="page-slug">Page Slug *</Label>
                      <Input
                        id="page-slug"
                        value={applicationForm.pageSlug}
                        onChange={(e) => handleApplicationFormChange('pageSlug', e.target.value)}
                        placeholder="page-slug"
                        className={applicationForm.validate && !applicationForm.pageSlug.trim() ? 'border-red-500' : ''}
                      />
                      {applicationForm.validate && !applicationForm.pageSlug.trim() && (
                        <p className="text-sm text-red-500">Page slug is required</p>
                      )}
                    </div>
                  </div>
                  
                  {previewState.error && (
                    <div className="p-3 bg-red-50 border border-red-200 rounded-md">
                      <p className="text-sm text-red-600">{previewState.error}</p>
                    </div>
                  )}
                </div>
              </div>

              <DialogFooter>
                <Button variant="outline" onClick={handleClosePreview}>
                  Cancel
                </Button>
                <Button 
                  onClick={handleApplyTemplate}
                  disabled={previewState.applying}
                  className="flex items-center gap-2"
                >
                  {previewState.applying ? (
                    <>
                      <LoadingSpinner />
                      Applying...
                    </>
                  ) : (
                    <>
                      <Sparkles className="h-4 w-4" />
                      Apply Template
                    </>
                  )}
                </Button>
              </DialogFooter>
            </>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default TemplateGalleryGrid;