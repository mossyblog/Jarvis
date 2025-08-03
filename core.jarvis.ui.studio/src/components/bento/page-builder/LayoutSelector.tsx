/**
 * LayoutSelector - Layout template selector
 * 
 * Provides a visual interface for selecting and configuring page layouts
 * from available templates with preview thumbnails and descriptions.
 */

import React, { useState, useMemo, useCallback } from 'react';
import {
  Plus,
  Search,
  Filter,
  Check,
  Eye,
  BarChart3,
  Settings,
  Star,
  Users,
  Grid3X3
} from 'lucide-react';

import type { BentoLayout } from '@/types/bento';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';

// ============================================================================
// Types
// ============================================================================

export interface LayoutSelectorProps {
  /** Available layout templates */
  layouts?: BentoLayout[];
  /** Currently selected layout ID */
  selectedLayoutId?: string;
  /** Called when a layout is selected */
  onLayoutSelect?: (layoutId: string) => void;
  /** Called when a new layout should be created */
  onCreateLayout?: () => void;
  /** Whether the selector is read-only */
  readOnly?: boolean;
  /** Whether to render in compact mobile mode */
  compact?: boolean;
  /** Additional CSS classes */
  className?: string;
}

interface LayoutTemplate {
  id: string;
  name: string;
  description: string;
  category: 'dashboard' | 'admin' | 'marketing' | 'content' | 'custom';
  preview: string; // SVG or image URL
  isPopular?: boolean;
  isFeatured?: boolean;
  isNew?: boolean;
  gridConfiguration: {
    desktop: { columns: number; rows: number };
    tablet: { columns: number; rows: number };
    mobile: { columns: number; rows: number };
  };
  componentCount: number;
  lastUpdated: string;
}

// ============================================================================
// Mock Layout Templates
// ============================================================================

const LAYOUT_TEMPLATES: LayoutTemplate[] = [
  {
    id: 'standard',
    name: 'Standard Layout',
    description: 'Balanced grid layout suitable for most applications',
    category: 'dashboard',
    preview: 'standard-layout-preview.svg',
    isPopular: true,
    gridConfiguration: {
      desktop: { columns: 12, rows: 8 },
      tablet: { columns: 8, rows: 10 },
      mobile: { columns: 4, rows: 12 }
    },
    componentCount: 6,
    lastUpdated: '2024-01-15'
  },
  {
    id: 'two-column',
    name: 'Two Column',
    description: 'Split layout with main content and sidebar',
    category: 'dashboard',
    preview: 'two-column-preview.svg',
    gridConfiguration: {
      desktop: { columns: 12, rows: 10 },
      tablet: { columns: 8, rows: 12 },
      mobile: { columns: 4, rows: 16 }
    },
    componentCount: 8,
    lastUpdated: '2024-01-12'
  },
  {
    id: 'full-width',
    name: 'Full Width',
    description: 'Wide layout optimized for data visualization',
    category: 'dashboard',
    preview: 'full-width-preview.svg',
    isFeatured: true,
    gridConfiguration: {
      desktop: { columns: 16, rows: 6 },
      tablet: { columns: 12, rows: 8 },
      mobile: { columns: 4, rows: 12 }
    },
    componentCount: 4,
    lastUpdated: '2024-01-20'
  },
  {
    id: 'analytics-dashboard',
    name: 'Analytics Dashboard',
    description: 'Optimized for KPIs, charts, and data visualization',
    category: 'dashboard',
    preview: 'analytics-preview.svg',
    isPopular: true,
    gridConfiguration: {
      desktop: { columns: 12, rows: 10 },
      tablet: { columns: 8, rows: 12 },
      mobile: { columns: 4, rows: 16 }
    },
    componentCount: 12,
    lastUpdated: '2024-01-18'
  },
  {
    id: 'admin-panel',
    name: 'Admin Panel',
    description: 'Administrative interface with navigation and settings',
    category: 'admin',
    preview: 'admin-panel-preview.svg',
    gridConfiguration: {
      desktop: { columns: 12, rows: 12 },
      tablet: { columns: 8, rows: 14 },
      mobile: { columns: 4, rows: 18 }
    },
    componentCount: 10,
    lastUpdated: '2024-01-10'
  },
  {
    id: 'marketing-page',
    name: 'Marketing Page',
    description: 'Landing page layout with hero and feature sections',
    category: 'marketing',
    preview: 'marketing-preview.svg',
    isNew: true,
    gridConfiguration: {
      desktop: { columns: 12, rows: 16 },
      tablet: { columns: 8, rows: 20 },
      mobile: { columns: 4, rows: 24 }
    },
    componentCount: 8,
    lastUpdated: '2024-01-22'
  },
  {
    id: 'user-profile',
    name: 'User Profile',
    description: 'Personal dashboard and profile management',
    category: 'content',
    preview: 'profile-preview.svg',
    gridConfiguration: {
      desktop: { columns: 10, rows: 12 },
      tablet: { columns: 8, rows: 14 },
      mobile: { columns: 4, rows: 18 }
    },
    componentCount: 9,
    lastUpdated: '2024-01-14'
  },
  {
    id: 'content-management',
    name: 'Content Management',
    description: 'Editorial interface for managing content',
    category: 'content',
    preview: 'cms-preview.svg',
    gridConfiguration: {
      desktop: { columns: 14, rows: 10 },
      tablet: { columns: 10, rows: 12 },
      mobile: { columns: 4, rows: 16 }
    },
    componentCount: 7,
    lastUpdated: '2024-01-16'
  }
];

// ============================================================================
// Layout Preview Component
// ============================================================================

const LayoutPreview: React.FC<{ template: LayoutTemplate }> = ({ template }) => {
  // Generate a simple grid preview based on the template configuration
  const { columns, rows } = template.gridConfiguration.desktop;
  const cells = Array.from({ length: Math.min(columns * rows, 24) }, (_, i) => i);
  
  return (
    <div className="layout-preview bg-muted/20 rounded-md p-2 aspect-[4/3] overflow-hidden">
      <div 
        className="grid gap-1 h-full"
        style={{
          gridTemplateColumns: `repeat(${Math.min(columns, 6)}, 1fr)`,
          gridTemplateRows: `repeat(${Math.min(rows, 4)}, 1fr)`
        }}
      >
        {cells.slice(0, 24).map((i) => (
          <div
            key={i}
            className={cn(
              'bg-primary/20 rounded-sm',
              // Simulate different component sizes
              i % 7 === 0 && 'col-span-2',
              i % 11 === 0 && 'row-span-2'
            )}
          />
        ))}
      </div>
    </div>
  );
};

// ============================================================================
// Layout Card Component
// ============================================================================

interface LayoutCardProps {
  template: LayoutTemplate;
  isSelected: boolean;
  onSelect: (layoutId: string) => void;
  onPreview?: (layoutId: string) => void;
  readOnly?: boolean;
}

const LayoutCard: React.FC<LayoutCardProps> = ({
  template,
  isSelected,
  onSelect,
  onPreview,
  readOnly = false
}) => {
  const handleSelect = useCallback(() => {
    if (!readOnly) {
      onSelect(template.id);
    }
  }, [template.id, onSelect, readOnly]);

  const handlePreview = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
    onPreview?.(template.id);
  }, [template.id, onPreview]);

  return (
    <Card
      className={cn(
        'layout-card cursor-pointer transition-all hover:shadow-md',
        isSelected && 'ring-2 ring-primary ring-offset-2',
        readOnly && 'cursor-default opacity-75'
      )}
      onClick={handleSelect}
    >
      <CardHeader className="pb-2">
        <div className="flex items-start justify-between">
          <div className="flex-1 min-w-0">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              {template.name}
              {isSelected && <Check className="h-4 w-4 text-primary" />}
            </CardTitle>
            <p className="text-xs text-muted-foreground mt-1 line-clamp-2">
              {template.description}
            </p>
          </div>
          <div className="flex flex-col gap-1 ml-2">
            {template.isPopular && (
              <Badge variant="secondary" className="text-xs">Popular</Badge>
            )}
            {template.isFeatured && (
              <Badge variant="default" className="text-xs">Featured</Badge>
            )}
            {template.isNew && (
              <Badge variant="outline" className="text-xs">New</Badge>
            )}
          </div>
        </div>
      </CardHeader>
      
      <CardContent className="pt-0">
        <LayoutPreview template={template} />
        
        <div className="flex items-center justify-between mt-3 text-xs text-muted-foreground">
          <div className="flex items-center gap-3">
            <span>{template.componentCount} components</span>
            <span>
              {template.gridConfiguration.desktop.columns}×{template.gridConfiguration.desktop.rows}
            </span>
          </div>
          
          {onPreview && (
            <Button
              variant="ghost"
              size="sm"
              onClick={handlePreview}
              className="h-auto p-1"
            >
              <Eye className="h-3 w-3" />
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
};

// ============================================================================
// Main Component
// ============================================================================

export const LayoutSelector: React.FC<LayoutSelectorProps> = ({
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  layouts: _layouts = [], // Will be used when integrating with backend
  selectedLayoutId,
  onLayoutSelect,
  onCreateLayout,
  readOnly = false,
  className
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');

  // Combine provided layouts with templates
  const allTemplates = useMemo(() => {
    // For now, we'll use the mock templates
    // In a real implementation, you'd convert BentoLayout[] to LayoutTemplate[]
    // The layouts prop will be used here when backend integration is complete
    return LAYOUT_TEMPLATES;
  }, []);

  // Filter templates
  const filteredTemplates = useMemo(() => {
    return allTemplates.filter(template => {
      // Category filter
      if (selectedCategory !== 'all' && template.category !== selectedCategory) {
        return false;
      }

      // Search filter
      if (searchQuery) {
        const query = searchQuery.toLowerCase();
        return (
          template.name.toLowerCase().includes(query) ||
          template.description.toLowerCase().includes(query) ||
          template.category.toLowerCase().includes(query)
        );
      }

      return true;
    });
  }, [allTemplates, searchQuery, selectedCategory]);

  // Group templates by category
  const templatesByCategory = useMemo(() => {
    const grouped: Record<string, LayoutTemplate[]> = {};
    
    filteredTemplates.forEach(template => {
      if (!grouped[template.category]) {
        grouped[template.category] = [];
      }
      grouped[template.category].push(template);
    });

    return grouped;
  }, [filteredTemplates]);

  // Get available categories (unused for now)
  // const availableCategories = useMemo(() => {
  //   return [...new Set(allTemplates.map(t => t.category))];
  // }, [allTemplates]);

  const handleTemplateSelect = useCallback((layoutId: string) => {
    onLayoutSelect?.(layoutId);
  }, [onLayoutSelect]);

  const handleSearchChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchQuery(e.target.value);
  }, []);

  const handleCategoryChange = useCallback((category: string) => {
    setSelectedCategory(category);
  }, []);

  const getCategoryIcon = (category: string) => {
    switch (category) {
      case 'dashboard': return BarChart3;
      case 'admin': return Settings;
      case 'marketing': return Star;
      case 'content': return Users;
      default: return Grid3X3;
    }
  };

  return (
    <div className={cn('layout-selector h-full flex flex-col', className)}>
      {/* Header */}
      <div className="layout-selector__header mb-4">
        <div className="flex items-center justify-between mb-3">
          <h3 className="text-lg font-semibold">Layout Templates</h3>
          {onCreateLayout && !readOnly && (
            <Button variant="outline" size="sm" onClick={onCreateLayout}>
              <Plus className="h-4 w-4 mr-1" />
              Custom
            </Button>
          )}
        </div>

        {/* Search */}
        <div className="relative mb-3">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search layouts..."
            value={searchQuery}
            onChange={handleSearchChange}
            className="pl-9"
          />
        </div>

        {/* Category Filter */}
        <Tabs value={selectedCategory} onValueChange={handleCategoryChange}>
          <TabsList className="grid w-full grid-cols-3">
            <TabsTrigger value="all">All</TabsTrigger>
            <TabsTrigger value="dashboard">Dashboard</TabsTrigger>
            <TabsTrigger value="admin">Admin</TabsTrigger>
          </TabsList>
        </Tabs>
      </div>

      {/* Template Grid */}
      <div className="layout-selector__content flex-1 overflow-auto">
        {selectedCategory === 'all' ? (
          // Show all categories
          <div className="space-y-6">
            {Object.entries(templatesByCategory).map(([category, templates]) => {
              const IconComponent = getCategoryIcon(category);
              return (
                <div key={category}>
                  <h4 className="text-sm font-medium mb-3 flex items-center gap-2 text-muted-foreground">
                    <IconComponent className="h-4 w-4" />
                    {category.charAt(0).toUpperCase() + category.slice(1)}
                    <span className="text-xs">({templates.length})</span>
                  </h4>
                  <div className="grid gap-3">
                    {templates.map(template => (
                      <LayoutCard
                        key={template.id}
                        template={template}
                        isSelected={selectedLayoutId === template.id}
                        onSelect={handleTemplateSelect}
                        readOnly={readOnly}
                      />
                    ))}
                  </div>
                </div>
              );
            })}
          </div>
        ) : (
          // Show selected category only
          <div className="grid gap-3">
            {filteredTemplates.map(template => (
              <LayoutCard
                key={template.id}
                template={template}
                isSelected={selectedLayoutId === template.id}
                onSelect={handleTemplateSelect}
                readOnly={readOnly}
              />
            ))}
          </div>
        )}

        {filteredTemplates.length === 0 && (
          <div className="flex flex-col items-center justify-center h-32 text-muted-foreground">
            <Filter className="h-8 w-8 mb-2 opacity-50" />
            <p className="text-sm">No layouts found</p>
            {searchQuery && (
              <p className="text-xs">Try adjusting your search query</p>
            )}
          </div>
        )}
      </div>

      {/* Footer */}
      <div className="layout-selector__footer mt-4 pt-4 border-t">
        <div className="text-xs text-muted-foreground">
          <p>{filteredTemplates.length} layouts available</p>
          {selectedLayoutId && (
            <p className="mt-1">
              Selected: {allTemplates.find(t => t.id === selectedLayoutId)?.name}
            </p>
          )}
        </div>
      </div>
    </div>
  );
};

LayoutSelector.displayName = 'LayoutSelector';