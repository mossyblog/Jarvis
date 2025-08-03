/**
 * TemplateBrowser - Browse and select page templates
 * 
 * Provides a comprehensive interface for browsing, filtering, and selecting
 * page templates from the UIStudio template library. Includes preview,
 * search, categorization, and template details.
 */

import React, { useState, useCallback, useMemo, useEffect } from 'react';
import {
  Search,
  Filter,
  Star,
  Download,
  Eye,
  Calendar,
  User,
  Grid3X3,
  Layers,
  Zap,
  Clock,
  Tag,
  ChevronDown,
  X,
  Play,
  Bookmark,
  TrendingUp,
  Award,
  Image as ImageIcon,
  FileText,
  BarChart3,
  Database,
  Users,
  Settings,
  Globe,
  Smartphone,
  Monitor,
  Tablet
} from 'lucide-react';

import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Separator } from '@/components/ui/separator';
import { 
  Select, 
  SelectContent, 
  SelectItem, 
  SelectTrigger, 
  SelectValue 
} from '@/components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@/components/ui/tabs';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { toast } from 'sonner';

import { uiStudioAPI, type Template } from '@/services/uiStudioApiService';
import { useEditMode } from '@/contexts/EditModeContext';

// ============================================================================
// Types
// ============================================================================

export interface TemplateBrowserProps {
  /** Whether the browser is open */
  isOpen: boolean;
  /** Called when the browser should close */
  onClose: () => void;
  /** Called when a template is selected */
  onSelectTemplate?: (template: Template) => void;
  /** Filter by category */
  initialCategory?: string;
  /** Whether to show featured templates only */
  featuredOnly?: boolean;
}

interface TemplateFilter {
  category: string | null;
  featured: boolean | null;
  sortBy: 'name' | 'downloads' | 'rating' | 'created';
  sortOrder: 'asc' | 'desc';
}

interface CreatePageFromTemplateData {
  templateId: string;
  pageName: string;
  route: string;
  description: string;
}

// ============================================================================
// Template Categories
// ============================================================================

const TEMPLATE_CATEGORIES = [
  { id: 'dashboard', label: 'Dashboard', icon: BarChart3, description: 'Analytics and metrics dashboards' },
  { id: 'forms', label: 'Forms', icon: FileText, description: 'Contact forms, surveys, and data collection' },
  { id: 'landing', label: 'Landing Pages', icon: Globe, description: 'Marketing and promotional pages' },
  { id: 'admin', label: 'Admin Panels', icon: Settings, description: 'Management and administration interfaces' },
  { id: 'ecommerce', label: 'E-commerce', icon: Tag, description: 'Product catalogs and shopping interfaces' },
  { id: 'portfolio', label: 'Portfolio', icon: ImageIcon, description: 'Showcase and gallery layouts' },
  { id: 'blog', label: 'Blog & Content', icon: FileText, description: 'Content management and publishing' },
  { id: 'social', label: 'Social', icon: Users, description: 'Community and social interaction pages' }
];

// ============================================================================
// Template Preview Component
// ============================================================================

interface TemplatePreviewProps {
  template: Template;
  isOpen: boolean;
  onClose: () => void;
  onUseTemplate: (data: CreatePageFromTemplateData) => void;
}

const TemplatePreview: React.FC<TemplatePreviewProps> = ({
  template,
  isOpen,
  onClose,
  onUseTemplate
}) => {
  const [activeTab, setActiveTab] = useState('overview');
  const [createData, setCreateData] = useState<CreatePageFromTemplateData>({
    templateId: template.id,
    pageName: template.name,
    route: `/${template.name.toLowerCase().replace(/\s+/g, '-')}`,
    description: template.description
  });

  const handleUseTemplate = useCallback(() => {
    if (!createData.pageName.trim()) {
      toast.error('Please enter a page name');
      return;
    }
    if (!createData.route.trim()) {
      toast.error('Please enter a route');
      return;
    }
    
    onUseTemplate(createData);
    onClose();
  }, [createData, onUseTemplate, onClose]);

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-4xl max-h-[90vh] overflow-hidden">
        <DialogHeader>
          <div className="flex items-start justify-between">
            <div>
              <DialogTitle className="text-xl">{template.name}</DialogTitle>
              <DialogDescription className="mt-1">
                {template.description}
              </DialogDescription>
            </div>
            <div className="flex items-center gap-2">
              <Badge variant="outline" className="text-xs">
                {template.category}
              </Badge>
              {template.metadata.featured && (
                <Badge className="text-xs bg-yellow-100 text-yellow-800">
                  <Star className="h-3 w-3 mr-1" />
                  Featured
                </Badge>
              )}
            </div>
          </div>
        </DialogHeader>

        <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1">
          <TabsList className="grid w-full grid-cols-3">
            <TabsTrigger value="overview">Overview</TabsTrigger>
            <TabsTrigger value="preview">Preview</TabsTrigger>
            <TabsTrigger value="create">Create Page</TabsTrigger>
          </TabsList>

          <div className="mt-4 h-96">
            <TabsContent value="overview" className="h-full">
              <ScrollArea className="h-full">
                <div className="space-y-6 pr-4">
                  {/* Template Stats */}
                  <div className="grid grid-cols-4 gap-4">
                    <div className="text-center">
                      <div className="text-2xl font-bold text-blue-600">{template.metadata.downloads}</div>
                      <div className="text-xs text-muted-foreground">Downloads</div>
                    </div>
                    <div className="text-center">
                      <div className="text-2xl font-bold text-green-600">
                        {template.metadata.rating.toFixed(1)}
                      </div>
                      <div className="text-xs text-muted-foreground">Rating</div>
                    </div>
                    <div className="text-center">
                      <div className="text-2xl font-bold text-purple-600">{template.components.length}</div>
                      <div className="text-xs text-muted-foreground">Components</div>
                    </div>
                    <div className="text-center">
                      <div className="text-2xl font-bold text-orange-600">{template.bindings.length}</div>
                      <div className="text-xs text-muted-foreground">Bindings</div>
                    </div>
                  </div>

                  <Separator />

                  {/* Template Details */}
                  <div className="space-y-4">
                    <div>
                      <h4 className="font-medium mb-2">Template Information</h4>
                      <div className="grid grid-cols-2 gap-4 text-sm">
                        <div>
                          <span className="text-muted-foreground">Created by:</span>
                          <div className="font-medium">{template.metadata.createdBy}</div>
                        </div>
                        <div>
                          <span className="text-muted-foreground">Created:</span>
                          <div className="font-medium">
                            {new Date(template.metadata.createdAt).toLocaleDateString()}
                          </div>
                        </div>
                      </div>
                    </div>

                    <div>
                      <h4 className="font-medium mb-2">Tags</h4>
                      <div className="flex flex-wrap gap-2">
                        {template.tags.map(tag => (
                          <Badge key={tag} variant="secondary" className="text-xs">
                            {tag}
                          </Badge>
                        ))}
                      </div>
                    </div>

                    <div>
                      <h4 className="font-medium mb-2">Page Configuration</h4>
                      <div className="space-y-2 text-sm">
                        <div className="flex justify-between">
                          <span className="text-muted-foreground">Route:</span>
                          <span className="font-mono">{template.page.route}</span>
                        </div>
                        <div className="flex justify-between">
                          <span className="text-muted-foreground">Layout:</span>
                          <span>{template.page.layoutId}</span>
                        </div>
                        <div className="flex justify-between">
                          <span className="text-muted-foreground">Public:</span>
                          <span>{template.page.bindings.security.isPublic ? 'Yes' : 'No'}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </ScrollArea>
            </TabsContent>

            <TabsContent value="preview" className="h-full">
              <div className="h-full bg-muted/30 rounded-lg flex items-center justify-center">
                <div className="text-center space-y-4">
                  <div className="w-16 h-16 bg-muted rounded-lg flex items-center justify-center mx-auto">
                    <Eye className="h-8 w-8 text-muted-foreground" />
                  </div>
                  <div>
                    <h4 className="font-medium">Template Preview</h4>
                    <p className="text-sm text-muted-foreground">
                      Preview functionality will be available soon
                    </p>
                  </div>
                  <Button variant="outline" size="sm">
                    <Play className="h-4 w-4 mr-2" />
                    Live Preview
                  </Button>
                </div>
              </div>
            </TabsContent>

            <TabsContent value="create" className="h-full">
              <ScrollArea className="h-full">
                <div className="space-y-4 pr-4">
                  <div className="space-y-2">
                    <Label htmlFor="pageName">Page Name</Label>
                    <Input
                      id="pageName"
                      value={createData.pageName}
                      onChange={(e) => setCreateData(prev => ({ ...prev, pageName: e.target.value }))}
                      placeholder="Enter page name..."
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="route">Route</Label>
                    <Input
                      id="route"
                      value={createData.route}
                      onChange={(e) => setCreateData(prev => ({ ...prev, route: e.target.value }))}
                      placeholder="/page-route"
                    />
                    <p className="text-xs text-muted-foreground">
                      This will be the URL path for your page
                    </p>
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="description">Description (optional)</Label>
                    <Textarea
                      id="description"
                      value={createData.description}
                      onChange={(e) => setCreateData(prev => ({ ...prev, description: e.target.value }))}
                      placeholder="Describe your page..."
                      rows={3}
                    />
                  </div>

                  <div className="p-4 bg-blue-50 rounded-lg">
                    <h4 className="font-medium text-blue-900 mb-2">What happens next?</h4>
                    <ul className="text-sm text-blue-800 space-y-1">
                      <li>• A new page will be created from this template</li>
                      <li>• All components and layout will be copied</li>
                      <li>• You can customize everything after creation</li>
                      <li>• The page will be saved as a draft initially</li>
                    </ul>
                  </div>
                </div>
              </ScrollArea>
            </TabsContent>
          </div>
        </Tabs>

        <DialogFooter className="gap-2">
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          {activeTab === 'create' ? (
            <Button onClick={handleUseTemplate}>
              <Zap className="h-4 w-4 mr-2" />
              Create Page
            </Button>
          ) : (
            <Button onClick={() => setActiveTab('create')}>
              Use This Template
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};

// ============================================================================
// Main Template Browser Component
// ============================================================================

export const TemplateBrowser: React.FC<TemplateBrowserProps> = ({
  isOpen,
  onClose,
  onSelectTemplate,
  initialCategory,
  featuredOnly = false
}) => {
  const { loadTemplate } = useEditMode();
  
  const [templates, setTemplates] = useState<Template[]>([]);
  const [loading, setLoading] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedTemplate, setSelectedTemplate] = useState<Template | null>(null);
  const [showPreview, setShowPreview] = useState(false);
  
  const [filter, setFilter] = useState<TemplateFilter>({
    category: initialCategory || null,
    featured: featuredOnly ? true : null,
    sortBy: 'downloads',
    sortOrder: 'desc'
  });

  // Load templates
  const loadTemplates = useCallback(async () => {
    if (!isOpen) return;
    
    setLoading(true);
    try {
      const response = await uiStudioAPI.getTemplates(
        filter.category || undefined,
        filter.featured || undefined
      );
      
      if (response.success && response.data) {
        setTemplates(response.data);
      }
    } catch (error) {
      console.error('Failed to load templates:', error);
      toast.error('Failed to load templates');
    } finally {
      setLoading(false);
    }
  }, [isOpen, filter.category, filter.featured]);

  useEffect(() => {
    loadTemplates();
  }, [loadTemplates]);

  // Filter and sort templates
  const filteredTemplates = useMemo(() => {
    const filtered = templates.filter(template => {
      const matchesSearch = searchQuery === '' || 
        template.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        template.description.toLowerCase().includes(searchQuery.toLowerCase()) ||
        template.tags.some(tag => tag.toLowerCase().includes(searchQuery.toLowerCase()));
      
      return matchesSearch;
    });

    // Sort templates
    filtered.sort((a, b) => {
      let aValue: any;
      let bValue: any;
      
      switch (filter.sortBy) {
        case 'name':
          aValue = a.name.toLowerCase();
          bValue = b.name.toLowerCase();
          break;
        case 'downloads':
          aValue = a.metadata.downloads;
          bValue = b.metadata.downloads;
          break;
        case 'rating':
          aValue = a.metadata.rating;
          bValue = b.metadata.rating;
          break;
        case 'created':
          aValue = new Date(a.metadata.createdAt).getTime();
          bValue = new Date(b.metadata.createdAt).getTime();
          break;
        default:
          return 0;
      }
      
      const comparison = aValue < bValue ? -1 : aValue > bValue ? 1 : 0;
      return filter.sortOrder === 'asc' ? comparison : -comparison;
    });

    return filtered;
  }, [templates, searchQuery, filter.sortBy, filter.sortOrder]);

  // Handle template selection
  const handleSelectTemplate = useCallback((template: Template) => {
    setSelectedTemplate(template);
    setShowPreview(true);
  }, []);

  // Handle creating page from template
  const handleCreateFromTemplate = useCallback(async (data: CreatePageFromTemplateData) => {
    try {
      await loadTemplate(data.templateId, data.pageName, data.route);
      onClose();
    } catch (error) {
      console.error('Failed to create page from template:', error);
    }
  }, [loadTemplate, onClose]);

  if (!isOpen) return null;

  return (
    <>
      {/* Main Browser */}
      <div className="fixed inset-0 bg-black/20 backdrop-blur-sm z-40">
        <div className="fixed inset-4 bg-background border border-border rounded-lg shadow-xl overflow-hidden">
          {/* Header */}
          <div className="flex items-center justify-between p-6 border-b border-border">
            <div>
              <h2 className="text-xl font-semibold">Template Library</h2>
              <p className="text-sm text-muted-foreground mt-1">
                Choose from {templates.length} professionally designed templates
              </p>
            </div>
            <Button variant="ghost" size="sm" onClick={onClose}>
              <X className="h-4 w-4" />
            </Button>
          </div>

          {/* Filters and Search */}
          <div className="flex items-center gap-4 p-6 border-b border-border">
            <div className="flex-1 relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search templates..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-10"
              />
            </div>
            
            <Select 
              value={filter.category || 'all'} 
              onValueChange={(value) => setFilter(prev => ({ 
                ...prev, 
                category: value === 'all' ? null : value 
              }))}
            >
              <SelectTrigger className="w-48">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Categories</SelectItem>
                {TEMPLATE_CATEGORIES.map(category => (
                  <SelectItem key={category.id} value={category.id}>
                    {category.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            
            <Select 
              value={filter.sortBy} 
              onValueChange={(value) => setFilter(prev => ({ 
                ...prev, 
                sortBy: value as TemplateFilter['sortBy']
              }))}
            >
              <SelectTrigger className="w-32">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="downloads">Downloads</SelectItem>
                <SelectItem value="rating">Rating</SelectItem>
                <SelectItem value="name">Name</SelectItem>
                <SelectItem value="created">Created</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Templates Grid */}
          <ScrollArea className="flex-1 p-6">
            {loading ? (
              <div className="flex items-center justify-center h-64">
                <div className="text-center space-y-2">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto"></div>
                  <p className="text-sm text-muted-foreground">Loading templates...</p>
                </div>
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                {filteredTemplates.map(template => (
                  <Card 
                    key={template.id}
                    className="cursor-pointer transition-all duration-200 hover:shadow-lg hover:scale-105"
                    onClick={() => handleSelectTemplate(template)}
                  >
                    <CardHeader className="pb-3">
                      <div className="aspect-video bg-gradient-to-br from-blue-100 to-purple-100 rounded-lg mb-3 flex items-center justify-center">
                        {template.thumbnail ? (
                          <img 
                            src={template.thumbnail} 
                            alt={template.name}
                            className="w-full h-full object-cover rounded-lg"
                          />
                        ) : (
                          <div className="text-center">
                            <Grid3X3 className="h-8 w-8 text-muted-foreground mx-auto mb-2" />
                            <span className="text-xs text-muted-foreground">Preview</span>
                          </div>
                        )}
                      </div>
                      
                      <div className="space-y-2">
                        <div className="flex items-start justify-between">
                          <CardTitle className="text-base leading-tight">{template.name}</CardTitle>
                          {template.metadata.featured && (
                            <Star className="h-4 w-4 text-yellow-500 fill-current flex-shrink-0" />
                          )}
                        </div>
                        
                        <div className="flex items-center gap-2">
                          <Badge variant="outline" className="text-xs">
                            {template.category}
                          </Badge>
                          <div className="flex items-center gap-1 text-xs text-muted-foreground">
                            <TrendingUp className="h-3 w-3" />
                            {template.metadata.rating.toFixed(1)}
                          </div>
                        </div>
                      </div>
                    </CardHeader>
                    
                    <CardContent className="pt-0">
                      <p className="text-sm text-muted-foreground mb-3 line-clamp-2">
                        {template.description}
                      </p>
                      
                      <div className="flex items-center justify-between text-xs text-muted-foreground">
                        <div className="flex items-center gap-1">
                          <Download className="h-3 w-3" />
                          {template.metadata.downloads}
                        </div>
                        <div className="flex items-center gap-1">
                          <Layers className="h-3 w-3" />
                          {template.components.length} components
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}
          </ScrollArea>
        </div>
      </div>

      {/* Template Preview Modal */}
      {selectedTemplate && (
        <TemplatePreview
          template={selectedTemplate}
          isOpen={showPreview}
          onClose={() => {
            setShowPreview(false);
            setSelectedTemplate(null);
          }}
          onUseTemplate={handleCreateFromTemplate}
        />
      )}
    </>
  );
};

TemplateBrowser.displayName = 'TemplateBrowser';