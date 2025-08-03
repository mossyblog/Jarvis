/**
 * Templates Page - Browse and manage page templates
 * 
 * Main page for browsing, previewing, and selecting templates from the
 * UIStudio template library. Includes featured templates, categories,
 * and search functionality.
 */

import React, { useState, useCallback } from 'react';
import {
  Layout,
  Search,
  Star,
  Grid3X3,
  Zap,
  Download,
  TrendingUp,
  Clock,
  Plus,
  Filter,
  BarChart3,
  FileText,
  Globe,
  Settings,
  Tag,
  ImageIcon,
  Users,
  Sparkles
} from 'lucide-react';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';

import { TemplateBrowser } from '@/components/templates/TemplateBrowser';
import { useEditMode } from '@/contexts/EditModeContext';

// ============================================================================
// Featured Categories
// ============================================================================

const FEATURED_CATEGORIES = [
  {
    id: 'dashboard',
    name: 'Dashboard Templates',
    description: 'Analytics and metrics dashboards',
    icon: BarChart3,
    count: 12,
    color: 'blue'
  },
  {
    id: 'forms',
    name: 'Form Templates',
    description: 'Contact forms and data collection',
    icon: FileText,
    count: 8,
    color: 'green'
  },
  {
    id: 'landing',
    name: 'Landing Pages',
    description: 'Marketing and promotional pages',
    icon: Globe,
    count: 15,
    color: 'purple'
  },
  {
    id: 'admin',
    name: 'Admin Panels',
    description: 'Management interfaces',
    icon: Settings,
    count: 6,
    color: 'orange'
  }
];

// ============================================================================
// Quick Actions
// ============================================================================

const QUICK_ACTIONS = [
  {
    id: 'blank',
    title: 'Start from Scratch',
    description: 'Create a blank page and build from the ground up',
    icon: Plus,
    action: 'create-blank'
  },
  {
    id: 'featured',
    title: 'Browse Featured',
    description: 'Explore our most popular and highest-rated templates',
    icon: Star,
    action: 'browse-featured'
  },
  {
    id: 'recent',
    title: 'Recent Templates',
    description: 'Check out the latest additions to our template library',
    icon: Clock,
    action: 'browse-recent'
  }
];

// ============================================================================
// Main Templates Component
// ============================================================================

export const Templates: React.FC = () => {
  const { createPage } = useEditMode();
  
  const [showBrowser, setShowBrowser] = useState(false);
  const [browserConfig, setBrowserConfig] = useState<{
    category?: string;
    featured?: boolean;
  }>({});
  
  const [searchQuery, setSearchQuery] = useState('');
  const [activeTab, setActiveTab] = useState('browse');

  // Handle quick actions
  const handleQuickAction = useCallback(async (actionId: string) => {
    switch (actionId) {
      case 'create-blank':
        try {
          await createPage({
            displayName: 'New Page',
            route: `/page-${Date.now()}`,
          });
        } catch (error) {
          console.error('Failed to create blank page:', error);
        }
        break;
        
      case 'browse-featured':
        setBrowserConfig({ featured: true });
        setShowBrowser(true);
        break;
        
      case 'browse-recent':
        setBrowserConfig({});
        setShowBrowser(true);
        break;
        
      default:
        console.warn('Unknown quick action:', actionId);
    }
  }, [createPage]);

  // Handle category selection
  const handleCategorySelect = useCallback((categoryId: string) => {
    setBrowserConfig({ category: categoryId });
    setShowBrowser(true);
  }, []);

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <div className="border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="container mx-auto px-6 py-8">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold tracking-tight">Templates</h1>
              <p className="text-muted-foreground mt-2">
                Get started quickly with professionally designed page templates
              </p>
            </div>
            
            <div className="flex items-center gap-3">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-xs w-xs text-muted-foreground" />
                <Input
                  placeholder="Search templates..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="pl-10 w-5xl"
                />
              </div>
              <Button onClick={() => setShowBrowser(true)}>
                <Grid3X3 className="h-xs w-xs mr-2" />
                Browse All Templates
              </Button>
            </div>
          </div>
        </div>
      </div>

      <div className="container mx-auto px-6 py-8">
        <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-8">
          <TabsList className="grid w-full max-w-md grid-cols-3">
            <TabsTrigger value="browse">Browse</TabsTrigger>
            <TabsTrigger value="featured">Featured</TabsTrigger>
            <TabsTrigger value="recent">Recent</TabsTrigger>
          </TabsList>

          <TabsContent value="browse" className="space-y-8">
            {/* Quick Actions */}
            <section>
              <h2 className="text-xl font-semibold mb-4">Quick Start</h2>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                {QUICK_ACTIONS.map(action => {
                  const Icon = action.icon;
                  return (
                    <Card 
                      key={action.id}
                      className="cursor-pointer transition-all duration-200 hover:shadow-md hover:scale-105"
                      onClick={() => handleQuickAction(action.action)}
                    >
                      <CardHeader className="pb-3">
                        <div className="flex items-center gap-3">
                          <div className="w-xl h-xl rounded-lg bg-primary/10 flex items-center justify-center">
                            <Icon className="h-sm w-sm text-primary" />
                          </div>
                          <CardTitle className="text-base">{action.title}</CardTitle>
                        </div>
                      </CardHeader>
                      <CardContent className="pt-0">
                        <p className="text-sm text-muted-foreground">
                          {action.description}
                        </p>
                      </CardContent>
                    </Card>
                  );
                })}
              </div>
            </section>

            {/* Template Categories */}
            <section>
              <h2 className="text-xl font-semibold mb-4">Template Categories</h2>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                {FEATURED_CATEGORIES.map(category => {
                  const Icon = category.icon;
                  const colorClasses = {
                    blue: 'from-blue-500 to-blue-600 text-blue-50',
                    green: 'from-green-500 to-green-600 text-green-50',
                    purple: 'from-purple-500 to-purple-600 text-purple-50',
                    orange: 'from-orange-500 to-orange-600 text-orange-50'
                  };
                  
                  return (
                    <Card 
                      key={category.id}
                      className="cursor-pointer transition-all duration-200 hover:shadow-lg hover:scale-105 overflow-hidden"
                      onClick={() => handleCategorySelect(category.id)}
                    >
                      <CardHeader className={`bg-gradient-to-br ${colorClasses[category.color as keyof typeof colorClasses]} pb-4`}>
                        <div className="flex items-center justify-between">
                          <Icon className="h-lg w-lg" />
                          <Badge className="bg-white/20 text-white border-white/30">
                            {category.count}
                          </Badge>
                        </div>
                        <CardTitle className="text-base mt-2">{category.name}</CardTitle>
                      </CardHeader>
                      <CardContent className="pt-4">
                        <p className="text-sm text-muted-foreground">
                          {category.description}
                        </p>
                      </CardContent>
                    </Card>
                  );
                })}
              </div>
            </section>

            {/* Popular Templates Preview */}
            <section>
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-xl font-semibold">Popular Templates</h2>
                <Button variant="outline" onClick={() => setShowBrowser(true)}>
                  View All
                </Button>
              </div>
              
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {[
                  {
                    name: 'Analytics Dashboard',
                    description: 'Comprehensive dashboard with charts and metrics',
                    category: 'Dashboard',
                    downloads: 1240,
                    rating: 4.8,
                    featured: true
                  },
                  {
                    name: 'Contact Form',
                    description: 'Responsive contact form with validation',
                    category: 'Forms',
                    downloads: 890,
                    rating: 4.6,
                    featured: false
                  },
                  {
                    name: 'Product Landing',
                    description: 'Modern product showcase page',
                    category: 'Landing',
                    downloads: 2100,
                    rating: 4.9,
                    featured: true
                  }
                ].map((template, index) => (
                  <Card key={index} className="cursor-pointer transition-all duration-200 hover:shadow-md">
                    <CardHeader className="pb-3">
                      <div className="aspect-video bg-gradient-to-br from-slate-100 to-slate-200 rounded-lg mb-3 flex items-center justify-center">
                        <div className="text-center">
                          <Layout className="h-lg w-lg text-muted-foreground mx-auto mb-2" />
                          <span className="text-xs text-muted-foreground">Preview</span>
                        </div>
                      </div>
                      
                      <div className="space-y-2">
                        <div className="flex items-start justify-between">
                          <CardTitle className="text-base leading-tight">{template.name}</CardTitle>
                          {template.featured && (
                            <Star className="h-xs w-xs text-yellow-500 fill-current flex-shrink-0" />
                          )}
                        </div>
                        
                        <div className="flex items-center gap-2">
                          <Badge variant="outline" className="text-xs">
                            {template.category}
                          </Badge>
                          <div className="flex items-center gap-1 text-xs text-muted-foreground">
                            <TrendingUp className="h-xs w-xs" />
                            {template.rating}
                          </div>
                        </div>
                      </div>
                    </CardHeader>
                    
                    <CardContent className="pt-0">
                      <p className="text-sm text-muted-foreground mb-3">
                        {template.description}
                      </p>
                      
                      <div className="flex items-center justify-between text-xs text-muted-foreground">
                        <div className="flex items-center gap-1">
                          <Download className="h-xs w-xs" />
                          {template.downloads}
                        </div>
                        <Button size="sm" variant="ghost" className="h-sm px-2">
                          <Zap className="h-xs w-xs mr-1" />
                          Use Template
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </section>
          </TabsContent>

          <TabsContent value="featured" className="space-y-8">
            <div className="text-center py-12">
              <Sparkles className="h-3xl w-3xl mx-auto text-yellow-500 mb-4" />
              <h3 className="text-xl font-semibold mb-2">Featured Templates</h3>
              <p className="text-muted-foreground mb-6 max-w-md mx-auto">
                Discover our handpicked selection of the most popular and well-designed templates.
              </p>
              <Button onClick={() => {
                setBrowserConfig({ featured: true });
                setShowBrowser(true);
              }}>
                <Star className="h-xs w-xs mr-2" />
                Browse Featured Templates
              </Button>
            </div>
          </TabsContent>

          <TabsContent value="recent" className="space-y-8">
            <div className="text-center py-12">
              <Clock className="h-3xl w-3xl mx-auto text-blue-500 mb-4" />
              <h3 className="text-xl font-semibold mb-2">Recently Added</h3>
              <p className="text-muted-foreground mb-6 max-w-md mx-auto">
                Check out the latest templates added to our library with the newest design trends.
              </p>
              <Button onClick={() => setShowBrowser(true)}>
                <Clock className="h-xs w-xs mr-2" />
                View Recent Templates
              </Button>
            </div>
          </TabsContent>
        </Tabs>
      </div>

      {/* Template Browser Modal */}
      <TemplateBrowser
        isOpen={showBrowser}
        onClose={() => setShowBrowser(false)}
        initialCategory={browserConfig.category}
        featuredOnly={browserConfig.featured}
      />
    </div>
  );
};

export default Templates;