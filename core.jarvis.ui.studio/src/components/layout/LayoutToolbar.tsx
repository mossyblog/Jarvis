/**
 * LayoutToolbar - Toolbar that appears when edit mode is active
 * 
 * Provides component search, palette, and layout tools for editing
 * pages using the Bento Grid system. Only visible in edit mode.
 */

import React, { useState, useMemo } from 'react';
import { 
  Grid3X3,
  Save,
  BarChart3,
  Database,
  FileText,
  Image,
  MousePointer2,
  TrendingUp,
  Target,
  Gauge,
  Table,
  Grid3X3 as GridView,
  Type,
  CreditCard,
  Video,
  Circle,
  Sliders,
  Edit,
  Eye,
  Send,
  Download,

  History,
  Settings,
  ChevronDown,
  Play,
  Pause,
  RotateCcw
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { RibbonTabBar, RibbonTabContent, RibbonTabPanel, createRibbonTabs } from '@/components/ui/ribbon-tab-bar';
import { useEditMode } from '@/contexts/EditModeContext';
import { motion, AnimatePresence } from 'framer-motion';
import { ComponentTile } from './ComponentTile';
import { 
  DropdownMenu, 
  DropdownMenuContent, 
  DropdownMenuItem, 
  DropdownMenuSeparator, 
  DropdownMenuTrigger 
} from '@/components/ui/dropdown-menu';
import { 
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { toast } from 'sonner';

// ============================================================================
// Types
// ============================================================================

interface ComponentDefinition {
  id: string;
  name: string;
  category: string;
  description: string;
  icon: React.ReactNode;
  defaultSize: { w: number; h: number };
}

// ============================================================================
// Component Definitions
// ============================================================================

const COMPONENT_DEFINITIONS: ComponentDefinition[] = [
  // Dashboard Components
  {
    id: 'metric-card',
    name: 'Metric Card',
    category: 'Dashboard',
    description: 'Display key metrics with charts',
    icon: <BarChart3 size={16} />,
    defaultSize: { w: 2, h: 2 }
  },
  {
    id: 'chart',
    name: 'Chart',
    category: 'Dashboard',
    description: 'Data visualization component',
    icon: <TrendingUp size={16} />,
    defaultSize: { w: 4, h: 3 }
  },
  {
    id: 'kpi',
    name: 'KPI',
    category: 'Dashboard',
    description: 'Key performance indicator',
    icon: <Target size={16} />,
    defaultSize: { w: 2, h: 1 }
  },
  {
    id: 'gauge',
    name: 'Gauge',
    category: 'Dashboard',
    description: 'Progress gauge visualization',
    icon: <Gauge size={16} />,
    defaultSize: { w: 2, h: 2 }
  },
  
  // Data Components
  {
    id: 'table',
    name: 'Data Table',
    category: 'Data',
    description: 'Tabular data display',
    icon: <Table size={16} />,
    defaultSize: { w: 6, h: 4 }
  },
  {
    id: 'list',
    name: 'List View',
    category: 'Data',
    description: 'Scrollable list of items',
    icon: <FileText size={16} />,
    defaultSize: { w: 3, h: 4 }
  },
  {
    id: 'grid-view',
    name: 'Grid View',
    category: 'Data',
    description: 'Card grid layout',
    icon: <GridView size={16} />,
    defaultSize: { w: 4, h: 3 }
  },
  
  // Content Components
  {
    id: 'text-block',
    name: 'Text Block',
    category: 'Content',
    description: 'Rich text content',
    icon: <FileText size={16} />,
    defaultSize: { w: 3, h: 2 }
  },
  {
    id: 'heading',
    name: 'Heading',
    category: 'Content',
    description: 'Section heading',
    icon: <Type size={16} />,
    defaultSize: { w: 4, h: 1 }
  },
  {
    id: 'card',
    name: 'Card',
    category: 'Content',
    description: 'Content card container',
    icon: <CreditCard size={16} />,
    defaultSize: { w: 2, h: 3 }
  },
  
  // Media Components
  {
    id: 'image',
    name: 'Image',
    category: 'Media',
    description: 'Image display component',
    icon: <Image size={16} />,
    defaultSize: { w: 2, h: 2 }
  },
  {
    id: 'video',
    name: 'Video',
    category: 'Media',
    description: 'Video player component',
    icon: <Video size={16} />,
    defaultSize: { w: 3, h: 2 }
  },
  {
    id: 'gallery',
    name: 'Gallery',
    category: 'Media',
    description: 'Image gallery',
    icon: <Image size={16} />,
    defaultSize: { w: 4, h: 3 }
  },
  
  // Action Components
  {
    id: 'button',
    name: 'Button',
    category: 'Actions',
    description: 'Action button',
    icon: <Circle size={16} />,
    defaultSize: { w: 1, h: 1 }
  },
  {
    id: 'button-group',
    name: 'Button Group',
    category: 'Actions',
    description: 'Group of action buttons',
    icon: <Sliders size={16} />,
    defaultSize: { w: 2, h: 1 }
  },
  {
    id: 'form',
    name: 'Form',
    category: 'Actions',
    description: 'Input form',
    icon: <Edit size={16} />,
    defaultSize: { w: 3, h: 4 }
  }
];

// ============================================================================
// Main Component
// ============================================================================

export const LayoutToolbar: React.FC = () => {
  const { 
    isEditMode,
    showGrid,
    toggleGrid,
    hasUnsavedChanges,
    savePage,
    publishPage,
    currentPage
  } = useEditMode();

  const [activeTab, setActiveTab] = useState('dashboard');

  // Define ribbon tabs
  const ribbonTabs = createRibbonTabs([
    {
      id: 'dashboard',
      label: 'Dashboard',
      icon: <BarChart3 size={16} />,
      color: 'blue'
    },
    {
      id: 'data',
      label: 'Data',
      icon: <Database size={16} />,
      color: 'green'
    },
    {
      id: 'content',
      label: 'Content',
      icon: <FileText size={16} />,
      color: 'purple'
    },
    {
      id: 'media',
      label: 'Media',
      icon: <Image size={16} />,
      color: 'orange'
    },
    {
      id: 'actions',
      label: 'Actions',
      icon: <MousePointer2 size={16} />,
      color: 'red'
    }
  ]);
  const [isPreviewMode, setIsPreviewMode] = useState(false);
  const [showPublishDialog, setShowPublishDialog] = useState(false);
  const [showVersionHistory, setShowVersionHistory] = useState(false);
  const [publishSettings, setPublishSettings] = useState({
    makePublic: false,
    publishNow: true,
    scheduleDate: '',
    notes: ''
  });

  // Get unique categories
  const categories = useMemo(() => {
    const cats = [...new Set(COMPONENT_DEFINITIONS.map(comp => comp.category))];
    return cats.sort();
  }, []);

  // Group components by category
  const componentsByCategory = useMemo(() => {
    const grouped: Record<string, ComponentDefinition[]> = {};
    COMPONENT_DEFINITIONS.forEach(comp => {
      if (!grouped[comp.category]) {
        grouped[comp.category] = [];
      }
      grouped[comp.category].push(comp);
    });
    return grouped;
  }, []);

  // Handle save
  const handleSave = async () => {
    try {
      await savePage();
      toast.success('Page saved successfully');
    } catch (error) {
      console.error('Failed to save:', error);
      toast.error('Failed to save page');
    }
  };
  
  // Handle preview mode toggle
  const handlePreviewToggle = () => {
    setIsPreviewMode(!isPreviewMode);
    toast.info(isPreviewMode ? 'Edit mode enabled' : 'Preview mode enabled');
  };
  
  // Handle publish
  const handlePublish = async () => {
    try {
      const environment = publishSettings.publishNow ? 
        'development' : 
        'staging'; // Default staging for scheduled publishes
      
      await publishPage(environment, {
        makePublic: publishSettings.makePublic,
        notifyUsers: true,
        generateSitemap: true,
        optimizeAssets: true
      });
      
      setShowPublishDialog(false);
    } catch (error) {
      console.error('Failed to publish:', error);
    }
  };
  
  // Handle export
  const handleExport = () => {
    if (!currentPage) return;
    
    const exportData = {
      page: currentPage,
      exportedAt: new Date().toISOString(),
      version: '1.0'
    };
    
    const blob = new Blob([JSON.stringify(exportData, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${currentPage.displayName.replace(/\s+/g, '-')}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    
    toast.success('Page exported successfully');
  };

  return (
    <AnimatePresence>
      {isEditMode && (
        <motion.div
          initial={{ height: 0, opacity: 0 }}
          animate={{ height: 'auto', opacity: 1 }}
          exit={{ height: 0, opacity: 0 }}
          transition={{ duration: 0.3, ease: 'easeInOut' }}
          className="layout-toolbar border-b bg-card/95 backdrop-blur supports-[backdrop-filter]:bg-card/60 overflow-hidden"
        >
          {/* Ribbon Header with Tabs and Controls */}
          <div className="flex items-center justify-between px-4 h-12 border-b">
            {/* Ribbon Tab Bar */}
            <RibbonTabBar
              tabs={ribbonTabs}
              activeTab={activeTab}
              onTabChange={setActiveTab}
              size="md"
              className="h-full"
            />

            {/* Right Controls */}
            <div className="flex items-center gap-2">

              {/* Grid Toggle */}
              <Button
                variant={showGrid ? "secondary" : "ghost"}
                size="sm"
                onClick={toggleGrid}
                className="h-8 px-2"
                title="Toggle grid overlay"
              >
                <Grid3X3 className="h-sm w-sm" />
              </Button>
              
              {/* Preview Toggle */}
              <Button
                variant={isPreviewMode ? "secondary" : "ghost"}
                size="sm"
                onClick={handlePreviewToggle}
                className="h-8 px-2"
                title="Toggle preview mode"
              >
                {isPreviewMode ? <Pause className="h-sm w-sm" /> : <Play className="h-sm w-sm" />}
              </Button>

              <Separator orientation="vertical" className="h-6" />
              
              {/* Tools Dropdown */}
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="sm" className="h-8 px-2">
                    <Settings className="h-sm w-sm" />
                    <ChevronDown className="h-3 w-3 ml-1" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-48">
                  <DropdownMenuItem onClick={handleExport}>
                    <Download className="h-4 w-4 mr-2" />
                    Export Page
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => setShowVersionHistory(true)}>
                    <History className="h-4 w-4 mr-2" />
                    Version History
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem>
                    <RotateCcw className="h-4 w-4 mr-2" />
                    Reset Layout
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>

              <Separator orientation="vertical" className="h-6" />

              {/* Save Action */}
              <div className="flex items-center gap-2">
                {hasUnsavedChanges && (
                  <Badge variant="secondary" className="text-xs animate-pulse">
                    Unsaved
                  </Badge>
                )}
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleSave}
                  disabled={!hasUnsavedChanges}
                  className="h-8 px-3"
                >
                  <Save className="h-4 w-4 mr-1" />
                  Save
                </Button>
                
                {/* Publish Button */}
                <Dialog open={showPublishDialog} onOpenChange={setShowPublishDialog}>
                  <DialogTrigger asChild>
                    <Button
                      variant="default"
                      size="sm"
                      className="h-8 px-3 bg-green-600 hover:bg-green-700"
                      disabled={hasUnsavedChanges}
                    >
                      <Send className="h-4 w-4 mr-1" />
                      Publish
                    </Button>
                  </DialogTrigger>
                  <DialogContent className="sm:max-w-md">
                    <DialogHeader>
                      <DialogTitle>Publish Page</DialogTitle>
                      <DialogDescription>
                        Make your page live and accessible to users.
                      </DialogDescription>
                    </DialogHeader>
                    
                    <div className="space-y-4">
                      <div className="flex items-center space-x-2">
                        <Switch
                          id="make-public"
                          checked={publishSettings.makePublic}
                          onCheckedChange={(checked) => 
                            setPublishSettings(prev => ({ ...prev, makePublic: checked }))
                          }
                        />
                        <Label htmlFor="make-public">Make page public</Label>
                      </div>
                      
                      <div className="flex items-center space-x-2">
                        <Switch
                          id="publish-now"
                          checked={publishSettings.publishNow}
                          onCheckedChange={(checked) => 
                            setPublishSettings(prev => ({ ...prev, publishNow: checked }))
                          }
                        />
                        <Label htmlFor="publish-now">Publish immediately</Label>
                      </div>
                      
                      {!publishSettings.publishNow && (
                        <div className="space-y-2">
                          <Label htmlFor="schedule-date">Schedule for</Label>
                          <Input
                            id="schedule-date"
                            type="datetime-local"
                            value={publishSettings.scheduleDate}
                            onChange={(e) => 
                              setPublishSettings(prev => ({ ...prev, scheduleDate: e.target.value }))
                            }
                          />
                        </div>
                      )}
                      
                      <div className="space-y-2">
                        <Label htmlFor="publish-notes">Release notes (optional)</Label>
                        <Textarea
                          id="publish-notes"
                          placeholder="Describe the changes in this version..."
                          value={publishSettings.notes}
                          onChange={(e) => 
                            setPublishSettings(prev => ({ ...prev, notes: e.target.value }))
                          }
                        />
                      </div>
                    </div>
                    
                    <DialogFooter className="gap-2">
                      <Button 
                        variant="outline" 
                        onClick={() => setShowPublishDialog(false)}
                      >
                        Cancel
                      </Button>
                      <Button 
                        onClick={handlePublish}
                        className="bg-green-600 hover:bg-green-700"
                      >
                        <Send className="h-4 w-4 mr-2" />
                        {publishSettings.publishNow ? 'Publish Now' : 'Schedule'}
                      </Button>
                    </DialogFooter>
                  </DialogContent>
                </Dialog>
              </div>
            </div>
          </div>

          {/* Ribbon Content - Component Tiles or Preview Message */}
          <div className="px-4 py-2">
            <RibbonTabContent activeTab={activeTab} animate={true}>
              {isPreviewMode ? (
                <div className="flex items-center justify-center py-8 text-center">
                  <div className="space-y-2">
                    <div className="flex items-center justify-center gap-2 text-lg font-medium text-muted-foreground">
                      <Eye className="h-5 w-5" />
                      Preview Mode Active
                    </div>
                    <p className="text-sm text-muted-foreground max-w-md">
                      You're viewing the page as users will see it. Click the preview button again to return to editing.
                    </p>
                  </div>
                </div>
              ) : (
                <>
                  {/* Tab Panels */}
                  {categories.map(category => (
                    <RibbonTabPanel
                      key={category.toLowerCase()}
                      tabId={category.toLowerCase()}
                      activeTab={activeTab}
                    >
                      <div className="flex flex-wrap gap-2">
                        {componentsByCategory[category]?.map((component: ComponentDefinition) => (
                          <ComponentTile
                            key={component.id}
                            id={component.id}
                            name={component.name}
                            description={component.description}
                            icon={component.icon}
                            defaultSize={component.defaultSize}
                          />
                        ))}
                      </div>
                    </RibbonTabPanel>
                  ))}
                </>
              )}
            </RibbonTabContent>
          </div>
        </motion.div>
      )}
      
      {/* Version History Dialog */}
      <Dialog open={showVersionHistory} onOpenChange={setShowVersionHistory}>
        <DialogContent className="sm:max-w-2xl max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Version History</DialogTitle>
            <DialogDescription>
              View and restore previous versions of this page.
            </DialogDescription>
          </DialogHeader>
          
          <div className="space-y-4">
            {/* Mock version history */}
            {[
              { id: '1', version: 'v2.1', date: '2 hours ago', author: 'You', changes: 'Added new metric cards, updated layout' },
              { id: '2', version: 'v2.0', date: '1 day ago', author: 'John Doe', changes: 'Major redesign with new components' },
              { id: '3', version: 'v1.9', date: '3 days ago', author: 'You', changes: 'Fixed responsive layout issues' },
              { id: '4', version: 'v1.8', date: '1 week ago', author: 'Jane Smith', changes: 'Initial layout structure' },
            ].map((version, index) => (
              <div key={version.id} className={cn(
                "flex items-center justify-between p-4 border rounded-lg",
                index === 0 && "border-primary bg-primary/5"
              )}>
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <Badge variant={index === 0 ? "default" : "outline"}>
                      {version.version}
                    </Badge>
                    {index === 0 && <Badge variant="secondary">Current</Badge>}
                    <span className="text-sm text-muted-foreground">{version.date}</span>
                  </div>
                  <div className="font-medium">{version.changes}</div>
                  <div className="text-sm text-muted-foreground">By {version.author}</div>
                </div>
                
                {index !== 0 && (
                  <div className="flex gap-2">
                    <Button variant="outline" size="sm">
                      <Eye className="h-4 w-4 mr-1" />
                      Preview
                    </Button>
                    <Button variant="outline" size="sm">
                      <RotateCcw className="h-4 w-4 mr-1" />
                      Restore
                    </Button>
                  </div>
                )}
              </div>
            ))}
          </div>
          
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowVersionHistory(false)}>
              Close
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </AnimatePresence>
  );
};

export default LayoutToolbar;