/**
 * ComponentBindingInterface - Main interface for binding UI components to ECS data
 * 
 * This is the primary interface for connecting UI Studio components to ECS (Entity Component System)
 * data sources. It provides a comprehensive workflow for:
 * - Selecting ECS components to bind to
 * - Mapping component fields to UI properties
 * - Configuring read and write operations
 * - Testing and validating the binding
 * - Deploying the binding configuration
 */

import React, { useState, useCallback, useMemo } from 'react';
import {
  Database,
  Search,
  Check,
  Loader2,
  Play,
  Settings,
  Eye,
  Save,
  Upload,
  FileText,
  TestTube,
  ChevronRight,
  X,
  Lock,
  Link
} from 'lucide-react';

import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Switch } from '@/components/ui/switch';
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
import { Textarea } from '@/components/ui/textarea';
import { toast } from 'sonner';

import { FieldMappingEditor } from './FieldMappingEditor';
import type { GridComponent } from '@/types/bento';

// ============================================================================
// Types
// ============================================================================

export interface ComponentBindingInterfaceProps {
  /** The UI component being configured */
  component: GridComponent;
  /** Whether the interface is open */
  isOpen: boolean;
  /** Called when the interface should close */
  onClose: () => void;
  /** Called when the binding is saved */
  onSave?: (binding: ComponentBinding) => void;
  /** Called when a test is requested */
  onTest?: (binding: ComponentBinding) => Promise<TestResult>;
  /** Whether in read-only mode */
  readOnly?: boolean;
}

export interface ComponentBinding {
  id: string;
  componentId: string;
  componentType: string;
  
  // ECS Configuration
  ecsComponent: string;
  ecsComponentConfig: ECSComponentConfig;
  
  // Field Mappings
  fieldMappings: FieldMapping[];
  
  // Read/Write Configuration
  readConfig: ReadConfig;
  writeConfig?: WriteConfig;
  
  // Deployment
  deploymentConfig: DeploymentConfig;
  
  // Metadata
  name: string;
  description?: string;
  version: string;
  status: 'draft' | 'testing' | 'deployed' | 'error';
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  updatedBy: string;
}

export interface ECSComponentConfig {
  name: string;
  displayName: string;
  description: string;
  category: string;
  version: string;
  documentation?: string;
  fields: ECSField[];
  permissions: {
    read: boolean;
    write: boolean;
    admin: boolean;
  };
}

export interface ECSField {
  name: string;
  type: 'string' | 'number' | 'boolean' | 'date' | 'json' | 'array' | 'object';
  required: boolean;
  description?: string;
  defaultValue?: unknown;
  constraints?: {
    min?: number;
    max?: number;
    pattern?: string;
    enum?: string[];
  };
  metadata?: Record<string, unknown>;
}

export interface FieldMapping {
  ecsField: string;
  source: 'prop' | 'state' | 'computed';
  sourcePath: string;
  uiControl: UIControlType;
  transform?: string;
  validation?: FieldValidation;
}

export interface FieldValidation {
  required?: boolean;
  min?: number;
  max?: number;
  pattern?: string;
  custom?: string;
}

export type UIControlType = 
  | 'text' | 'number' | 'select' | 'multiselect' 
  | 'checkbox' | 'switch' | 'date' | 'datetime' 
  | 'textarea' | 'json' | 'code' | 'color' | 'file';

export interface ReadConfig {
  enabled: boolean;
  query: string;
  polling: {
    enabled: boolean;
    interval: number;
  };
  caching: {
    enabled: boolean;
    ttl: number;
  };
  errorHandling: {
    retries: number;
    fallback?: unknown;
  };
}

export interface WriteConfig {
  enabled: boolean;
  mutations: WriteMutation[];
  validation: {
    enabled: boolean;
    schema?: string;
  };
  confirmation: {
    required: boolean;
    message?: string;
  };
}

export interface WriteMutation {
  trigger: string;
  operation: 'create' | 'update' | 'delete';
  target: string;
  mapping: Record<string, string>;
}

export interface DeploymentConfig {
  environment: 'development' | 'staging' | 'production';
  autoRefresh: boolean;
  errorReporting: boolean;
  analytics: boolean;
  permissions: {
    public: boolean;
    roles: string[];
    users: string[];
  };
}

export interface TestResult {
  success: boolean;
  data?: unknown;
  error?: string;
  performance: {
    responseTime: number;
    dataSize: number;
  };
  validation: {
    fieldCount: number;
    errors: string[];
    warnings: string[];
  };
}

// ============================================================================
// Mock Data
// ============================================================================

const MOCK_ECS_COMPONENTS: ECSComponentConfig[] = [
  {
    name: 'UserComponent',
    displayName: 'User Profile',
    description: 'User profile information and settings',
    category: 'Authentication',
    version: '2.1.0',
    documentation: 'Stores user profile data including personal information, preferences, and security settings.',
    fields: [
      {
        name: 'id',
        type: 'string',
        required: true,
        description: 'Unique user identifier'
      },
      {
        name: 'firstName',
        type: 'string',
        required: true,
        description: 'User first name'
      },
      {
        name: 'lastName',
        type: 'string',
        required: true,
        description: 'User last name'
      },
      {
        name: 'email',
        type: 'string',
        required: true,
        description: 'User email address',
        constraints: {
          pattern: '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'
        }
      },
      {
        name: 'avatar',
        type: 'string',
        required: false,
        description: 'User avatar URL'
      },
      {
        name: 'isActive',
        type: 'boolean',
        required: true,
        description: 'Whether the user account is active',
        defaultValue: true
      },
      {
        name: 'createdAt',
        type: 'date',
        required: true,
        description: 'Account creation timestamp'
      },
      {
        name: 'lastLoginAt',
        type: 'date',
        required: false,
        description: 'Last login timestamp'
      },
      {
        name: 'preferences',
        type: 'json',
        required: false,
        description: 'User preferences and settings'
      }
    ],
    permissions: {
      read: true,
      write: true,
      admin: false
    }
  },
  {
    name: 'MetricComponent',
    displayName: 'System Metrics',
    description: 'Real-time system performance metrics',
    category: 'Analytics',
    version: '1.5.2',
    documentation: 'Provides real-time access to system performance metrics including CPU, memory, and network usage.',
    fields: [
      {
        name: 'id',
        type: 'string',
        required: true,
        description: 'Metric identifier'
      },
      {
        name: 'name',
        type: 'string',
        required: true,
        description: 'Metric display name'
      },
      {
        name: 'value',
        type: 'number',
        required: true,
        description: 'Current metric value'
      },
      {
        name: 'previousValue',
        type: 'number',
        required: false,
        description: 'Previous metric value for comparison'
      },
      {
        name: 'unit',
        type: 'string',
        required: false,
        description: 'Metric unit (%, MB, etc.)'
      },
      {
        name: 'trend',
        type: 'string',
        required: false,
        description: 'Trend direction',
        constraints: {
          enum: ['up', 'down', 'stable']
        }
      },
      {
        name: 'threshold',
        type: 'object',
        required: false,
        description: 'Alert thresholds'
      },
      {
        name: 'lastUpdated',
        type: 'date',
        required: true,
        description: 'Last update timestamp'
      }
    ],
    permissions: {
      read: true,
      write: false,
      admin: true
    }
  },
  {
    name: 'TaskComponent',
    displayName: 'Task Management',
    description: 'Task tracking and management system',
    category: 'Productivity',
    version: '3.0.1',
    fields: [
      {
        name: 'id',
        type: 'string',
        required: true,
        description: 'Task identifier'
      },
      {
        name: 'title',
        type: 'string',
        required: true,
        description: 'Task title'
      },
      {
        name: 'description',
        type: 'string',
        required: false,
        description: 'Task description'
      },
      {
        name: 'status',
        type: 'string',
        required: true,
        description: 'Task status',
        constraints: {
          enum: ['pending', 'in_progress', 'completed', 'cancelled']
        },
        defaultValue: 'pending'
      },
      {
        name: 'priority',
        type: 'string',
        required: true,
        description: 'Task priority',
        constraints: {
          enum: ['low', 'medium', 'high', 'urgent']
        },
        defaultValue: 'medium'
      },
      {
        name: 'assigneeId',
        type: 'string',
        required: false,
        description: 'Assigned user ID'
      },
      {
        name: 'dueDate',
        type: 'date',
        required: false,
        description: 'Task due date'
      },
      {
        name: 'tags',
        type: 'array',
        required: false,
        description: 'Task tags'
      },
      {
        name: 'estimatedHours',
        type: 'number',
        required: false,
        description: 'Estimated completion time in hours'
      },
      {
        name: 'actualHours',
        type: 'number',
        required: false,
        description: 'Actual time spent in hours'
      }
    ],
    permissions: {
      read: true,
      write: true,
      admin: true
    }
  }
];

// ============================================================================
// ECS Component Browser
// ============================================================================

interface ECSComponentBrowserProps {
  selectedComponent: string | null;
  onSelect: (component: ECSComponentConfig) => void;
  onClose: () => void;
}

const ECSComponentBrowser: React.FC<ECSComponentBrowserProps> = ({
  selectedComponent,
  onSelect,
  onClose
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [sortBy, setSortBy] = useState<'name' | 'category' | 'updated'>('name');

  // Filter and sort components
  const filteredComponents = useMemo(() => {
    const filtered = MOCK_ECS_COMPONENTS.filter(component => {
      const matchesSearch = searchQuery === '' || 
        component.displayName.toLowerCase().includes(searchQuery.toLowerCase()) ||
        component.description.toLowerCase().includes(searchQuery.toLowerCase()) ||
        component.name.toLowerCase().includes(searchQuery.toLowerCase());
      
      const matchesCategory = selectedCategory === null || 
        component.category === selectedCategory;
      
      return matchesSearch && matchesCategory;
    });

    // Sort components
    filtered.sort((a, b) => {
      switch (sortBy) {
        case 'name':
          return a.displayName.localeCompare(b.displayName);
        case 'category':
          return a.category.localeCompare(b.category);
        case 'updated':
          return a.version.localeCompare(b.version);
        default:
          return 0;
      }
    });

    return filtered;
  }, [searchQuery, selectedCategory, sortBy]);

  // Get unique categories
  const categories = useMemo(() => {
    return [...new Set(MOCK_ECS_COMPONENTS.map(c => c.category))].sort();
  }, []);

  return (
    <Dialog open={true} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-4xl max-h-[80vh] overflow-hidden">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Database className="h-sm w-sm" />
            ECS Component Browser
          </DialogTitle>
          <DialogDescription>
            Browse and select an ECS component to bind to your UI component.
          </DialogDescription>
        </DialogHeader>

        {/* Search and Filters */}
        <div className="flex items-center gap-4 py-4 border-b">
          <div className="flex-1 relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-xs w-xs text-muted-foreground" />
            <Input
              placeholder="Search components..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-10"
            />
          </div>
          
          <Select value={selectedCategory || 'all'} onValueChange={(value) => 
            setSelectedCategory(value === 'all' ? null : value)
          }>
            <SelectTrigger className="w-xs8">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Categories</SelectItem>
              {categories.map(category => (
                <SelectItem key={category} value={category}>
                  {category}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          
          <Select value={sortBy} onValueChange={(value) => setSortBy(value as 'name' | 'category' | 'updated')}>
            <SelectTrigger className="w-252">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="name">Name</SelectItem>
              <SelectItem value="category">Category</SelectItem>
              <SelectItem value="updated">Version</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Components Grid */}
        <ScrollArea className="h-412">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 p-4">
            {filteredComponents.map(component => (
              <Card 
                key={component.name}
                className={cn(
                  "cursor-pointer transition-all duration-200 hover:shadow-md",
                  selectedComponent === component.name && "ring-2 ring-primary"
                )}
                onClick={() => onSelect(component)}
              >
                <CardHeader className="pb-3">
                  <div className="flex items-start justify-between">
                    <div className="space-y-1">
                      <CardTitle className="text-base">{component.displayName}</CardTitle>
                      <div className="flex items-center gap-2">
                        <Badge variant="outline" className="text-xs">
                          {component.category}
                        </Badge>
                        <Badge variant="secondary" className="text-xs font-mono">
                          {component.version}
                        </Badge>
                      </div>
                    </div>
                    <div className="flex items-center gap-1">
                      {component.permissions.read && <Eye className="h-2xs w-2xs text-green-600" />}
                      {component.permissions.write && <Database className="h-2xs w-2xs text-blue-600" />}
                      {component.permissions.admin && <Lock className="h-2xs w-2xs text-orange-600" />}
                    </div>
                  </div>
                </CardHeader>
                <CardContent className="pt-0">
                  <p className="text-sm text-muted-foreground mb-3">
                    {component.description}
                  </p>
                  <div className="flex items-center justify-between text-xs text-muted-foreground">
                    <span>{component.fields.length} fields</span>
                    <span className="font-mono">{component.name}</span>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </ScrollArea>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button 
            onClick={() => {
              const component = MOCK_ECS_COMPONENTS.find(c => c.name === selectedComponent);
              if (component) {
                onSelect(component);
                onClose();
              }
            }}
            disabled={!selectedComponent}
          >
            Select Component
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};

// ============================================================================
// Main Component
// ============================================================================

export const ComponentBindingInterface: React.FC<ComponentBindingInterfaceProps> = ({
  component,
  isOpen,
  onClose,
  onSave,
  onTest,
  readOnly = false
}) => {
  const [activeTab, setActiveTab] = useState<'component' | 'mapping' | 'config' | 'test' | 'deploy'>('component');
  const [showECSBrowser, setShowECSBrowser] = useState(false);
  const [selectedECSComponent, setSelectedECSComponent] = useState<ECSComponentConfig | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [testResult, setTestResult] = useState<TestResult | null>(null);
  
  // Binding configuration state
  const [binding, setBinding] = useState<Partial<ComponentBinding>>({
    name: `${component.componentType} Binding`,
    description: `Data binding for ${component.componentType} component`,
    status: 'draft',
    fieldMappings: [],
    readConfig: {
      enabled: true,
      query: '',
      polling: { enabled: false, interval: 30000 },
      caching: { enabled: true, ttl: 300000 },
      errorHandling: { retries: 3 }
    },
    deploymentConfig: {
      environment: 'development',
      autoRefresh: true,
      errorReporting: true,
      analytics: false,
      permissions: {
        public: false,
        roles: [],
        users: []
      }
    }
  });

  // Handle ECS component selection
  const handleECSComponentSelect = useCallback((ecsComponent: ECSComponentConfig) => {
    setSelectedECSComponent(ecsComponent);
    setBinding(prev => ({
      ...prev,
      ecsComponent: ecsComponent.name,
      ecsComponentConfig: ecsComponent,
      readConfig: {
        ...prev.readConfig!,
        query: `query Get${ecsComponent.displayName.replace(/\s+/g, '')} {
  ${ecsComponent.name.toLowerCase()} {
    ${ecsComponent.fields.slice(0, 5).map(f => f.name).join('\n    ')}
  }
}`
      }
    }));
    setActiveTab('mapping');
  }, []);

  // Handle field mappings update
  const handleFieldMappingsUpdate = useCallback((mappings: FieldMapping[]) => {
    setBinding(prev => ({
      ...prev,
      fieldMappings: mappings
    }));
  }, []);

  // Handle test binding
  const handleTestBinding = useCallback(async () => {
    if (!onTest || !binding.ecsComponent) return;
    
    setIsLoading(true);
    try {
      const result = await onTest(binding as ComponentBinding);
      setTestResult(result);
      if (result.success) {
        toast.success('Binding test completed successfully');
      } else {
        toast.error('Binding test failed');
      }
    } catch (error) {
      toast.error('Failed to test binding');
      setTestResult({
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error',
        performance: { responseTime: 0, dataSize: 0 },
        validation: { fieldCount: 0, errors: [], warnings: [] }
      });
    } finally {
      setIsLoading(false);
    }
  }, [binding, onTest]);

  // Handle save binding
  const handleSaveBinding = useCallback(() => {
    if (!onSave || !selectedECSComponent) return;
    
    const finalBinding: ComponentBinding = {
      id: crypto.randomUUID(),
      componentId: component.id,
      componentType: component.componentType,
      ecsComponent: selectedECSComponent.name,
      ecsComponentConfig: selectedECSComponent,
      fieldMappings: binding.fieldMappings || [],
      readConfig: binding.readConfig!,
      writeConfig: binding.writeConfig,
      deploymentConfig: binding.deploymentConfig!,
      name: binding.name!,
      description: binding.description,
      version: '1.0.0',
      status: 'draft',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      createdBy: 'current-user',
      updatedBy: 'current-user'
    };
    
    onSave(finalBinding);
    toast.success('Binding configuration saved');
    onClose();
  }, [binding, selectedECSComponent, component, onSave, onClose]);

  if (!isOpen) return null;

  return (
    <>
      {/* Main Interface */}
      <div className="fixed inset-0 bg-black/20 backdrop-blur-sm z-50">
        <div className="fixed inset-y-0 right-0 w-full max-w-mdxl bg-background border-l border-border overflow-hidden">
          {/* Header */}
          <div className="flex items-center justify-between p-6 border-b border-border">
            <div>
              <h2 className="text-xl font-semibold">Component Data Binding</h2>
              <p className="text-sm text-muted-foreground mt-1">
                Connect {component.componentType} to ECS data sources
              </p>
            </div>
            <div className="flex items-center gap-2">
              {selectedECSComponent && (
                <Badge variant="outline" className="text-sm">
                  {selectedECSComponent.displayName}
                </Badge>
              )}
              <Button variant="ghost" size="sm" onClick={onClose}>
                <X className="h-xs w-xs" />
              </Button>
            </div>
          </div>

          {/* Progress Steps */}
          <div className="flex items-center px-6 py-4 bg-muted/30 border-b border-border">
            {[
              { id: 'component', label: 'ECS Component', icon: Database },
              { id: 'mapping', label: 'Field Mapping', icon: Link },
              { id: 'config', label: 'Configuration', icon: Settings },
              { id: 'test', label: 'Test & Validate', icon: TestTube },
              { id: 'deploy', label: 'Deploy', icon: Upload }
            ].map((step, index) => {
              const Icon = step.icon;
              const isActive = activeTab === step.id;
              const isCompleted = 
                (step.id === 'component' && selectedECSComponent) ||
                (step.id === 'mapping' && binding.fieldMappings && binding.fieldMappings.length > 0) ||
                (step.id === 'config' && binding.readConfig?.query) ||
                (step.id === 'test' && testResult?.success) ||
                (step.id === 'deploy' && binding.status === 'deployed');

              return (
                <React.Fragment key={step.id}>
                  <button
                    onClick={() => setActiveTab(step.id as 'component' | 'mapping' | 'config' | 'test' | 'deploy')}
                    className={cn(
                      "flex items-center gap-2 px-3 py-2 rounded-md text-sm font-medium transition-colors",
                      isActive && "bg-primary text-primary-foreground",
                      !isActive && isCompleted && "bg-green-100 text-green-700",
                      !isActive && !isCompleted && "text-muted-foreground hover:text-foreground"
                    )}
                  >
                    <Icon className="h-xs w-xs" />
                    {step.label}
                    {isCompleted && !isActive && <Check className="h-2xs w-2xs ml-1" />}
                  </button>
                  {index < 4 && (
                    <ChevronRight className="h-xs w-xs text-muted-foreground mx-2" />
                  )}
                </React.Fragment>
              );
            })}
          </div>

          {/* Content */}
          <div className="flex-1 p-6 overflow-y-auto">
            {activeTab === 'component' && (
              <div className="space-y-6">
                <div className="text-center py-12">
                  <Database className="h-3xl w-3xl mx-auto text-muted-foreground mb-4" />
                  <h3 className="text-lg font-medium mb-2">Select ECS Component</h3>
                  <p className="text-muted-foreground mb-6 max-w-md mx-auto">
                    Choose an ECS component to bind to your UI component. This will provide the data structure and fields available for mapping.
                  </p>
                  <Button onClick={() => setShowECSBrowser(true)}>
                    <Search className="h-xs w-xs mr-2" />
                    Browse ECS Components
                  </Button>
                </div>
              </div>
            )}

            {activeTab === 'mapping' && selectedECSComponent && (
              <div className="space-y-6">
                <FieldMappingEditor
                  ecsComponent={selectedECSComponent}
                  componentType={component.componentType}
                  mappings={binding.fieldMappings || []}
                  onChange={handleFieldMappingsUpdate}
                  readOnly={readOnly}
                />
              </div>
            )}

            {activeTab === 'config' && (
              <div className="space-y-6">
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <FileText className="h-sm w-sm" />
                      Read Configuration
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="space-y-2">
                      <Label>GraphQL Query</Label>
                      <Textarea
                        value={binding.readConfig?.query || ''}
                        onChange={(e) => setBinding(prev => ({
                          ...prev,
                          readConfig: { ...prev.readConfig!, query: e.target.value }
                        }))}
                        className="font-mono text-sm h-232"
                        placeholder="Enter GraphQL query..."
                      />
                    </div>
                    
                    <div className="grid grid-cols-2 gap-4">
                      <div className="space-y-2">
                        <div className="flex items-center space-x-2">
                          <Switch
                            checked={binding.readConfig?.polling.enabled || false}
                            onCheckedChange={(checked) => setBinding(prev => ({
                              ...prev,
                              readConfig: {
                                ...prev.readConfig!,
                                polling: { ...prev.readConfig!.polling, enabled: checked }
                              }
                            }))}
                          />
                          <Label>Enable Polling</Label>
                        </div>
                        {binding.readConfig?.polling.enabled && (
                          <Input
                            type="number"
                            value={binding.readConfig.polling.interval / 1000}
                            onChange={(e) => setBinding(prev => ({
                              ...prev,
                              readConfig: {
                                ...prev.readConfig!,
                                polling: { 
                                  ...prev.readConfig!.polling, 
                                  interval: Number(e.target.value) * 1000 
                                }
                              }
                            }))}
                            placeholder="Interval (seconds)"
                          />
                        )}
                      </div>
                      
                      <div className="space-y-2">
                        <div className="flex items-center space-x-2">
                          <Switch
                            checked={binding.readConfig?.caching.enabled || false}
                            onCheckedChange={(checked) => setBinding(prev => ({
                              ...prev,
                              readConfig: {
                                ...prev.readConfig!,
                                caching: { ...prev.readConfig!.caching, enabled: checked }
                              }
                            }))}
                          />
                          <Label>Enable Caching</Label>
                        </div>
                        {binding.readConfig?.caching.enabled && (
                          <Input
                            type="number"
                            value={binding.readConfig.caching.ttl / 1000}
                            onChange={(e) => setBinding(prev => ({
                              ...prev,
                              readConfig: {
                                ...prev.readConfig!,
                                caching: { 
                                  ...prev.readConfig!.caching, 
                                  ttl: Number(e.target.value) * 1000 
                                }
                              }
                            }))}
                            placeholder="TTL (seconds)"
                          />
                        )}
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </div>
            )}

            {activeTab === 'test' && (
              <div className="space-y-6">
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <TestTube className="h-sm w-sm" />
                      Test Binding
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="flex items-center gap-4">
                      <Button 
                        onClick={handleTestBinding}
                        disabled={isLoading || !selectedECSComponent}
                      >
                        {isLoading ? (
                          <Loader2 className="h-xs w-xs mr-2 animate-spin" />
                        ) : (
                          <Play className="h-xs w-xs mr-2" />
                        )}
                        Run Test
                      </Button>
                      
                      {testResult && (
                        <Badge variant={testResult.success ? "default" : "destructive"}>
                          {testResult.success ? "Passed" : "Failed"}
                        </Badge>
                      )}
                    </div>
                    
                    {testResult && (
                      <div className="space-y-4">
                        <div className="grid grid-cols-3 gap-4 text-sm">
                          <div>
                            <span className="text-muted-foreground">Response Time:</span>
                            <div className="font-medium">{testResult.performance.responseTime}ms</div>
                          </div>
                          <div>
                            <span className="text-muted-foreground">Data Size:</span>
                            <div className="font-medium">{testResult.performance.dataSize} bytes</div>
                          </div>
                          <div>
                            <span className="text-muted-foreground">Fields:</span>
                            <div className="font-medium">{testResult.validation.fieldCount}</div>
                          </div>
                        </div>
                        
                        {testResult.validation.errors.length > 0 && (
                          <div className="p-3 bg-destructive/10 border border-destructive/20 rounded">
                            <div className="text-sm font-medium text-destructive mb-2">Validation Errors:</div>
                            <ul className="text-sm text-destructive space-y-1">
                              {testResult.validation.errors.map((error, index) => (
                                <li key={index}>• {error}</li>
                              ))}
                            </ul>
                          </div>
                        )}
                        
                        {testResult.data != null && (
                          <div className="space-y-2">
                            <Label>Sample Data:</Label>
                            <pre className="text-xs p-3 bg-muted rounded font-mono overflow-auto max-h-mdxl">
                              {JSON.stringify(testResult.data as Record<string, unknown>, null, 2)}
                            </pre>
                          </div>
                        )}
                      </div>
                    )}
                  </CardContent>
                </Card>
              </div>
            )}

            {activeTab === 'deploy' && (
              <div className="space-y-6">
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <Upload className="h-sm w-sm" />
                      Deployment Configuration
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="grid grid-cols-2 gap-4">
                      <div className="space-y-2">
                        <Label>Environment</Label>
                        <Select
                          value={binding.deploymentConfig?.environment || 'development'}
                          onValueChange={(value) => setBinding(prev => ({
                            ...prev,
                            deploymentConfig: {
                              ...prev.deploymentConfig!,
                              environment: value as 'development' | 'staging' | 'production'
                            }
                          }))}
                        >
                          <SelectTrigger>
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="development">Development</SelectItem>
                            <SelectItem value="staging">Staging</SelectItem>
                            <SelectItem value="production">Production</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                      
                      <div className="space-y-2">
                        <Label>Binding Name</Label>
                        <Input
                          value={binding.name || ''}
                          onChange={(e) => setBinding(prev => ({
                            ...prev,
                            name: e.target.value
                          }))}
                          placeholder="Enter binding name..."
                        />
                      </div>
                    </div>
                    
                    <div className="space-y-2">
                      <Label>Description</Label>
                      <Textarea
                        value={binding.description || ''}
                        onChange={(e) => setBinding(prev => ({
                          ...prev,
                          description: e.target.value
                        }))}
                        placeholder="Describe this binding configuration..."
                      />
                    </div>
                    
                    <div className="space-y-3">
                      <Label>Options</Label>
                      <div className="space-y-2">
                        <div className="flex items-center space-x-2">
                          <Switch
                            checked={binding.deploymentConfig?.autoRefresh || false}
                            onCheckedChange={(checked) => setBinding(prev => ({
                              ...prev,
                              deploymentConfig: {
                                ...prev.deploymentConfig!,
                                autoRefresh: checked
                              }
                            }))}
                          />
                          <Label>Auto-refresh data</Label>
                        </div>
                        
                        <div className="flex items-center space-x-2">
                          <Switch
                            checked={binding.deploymentConfig?.errorReporting || false}
                            onCheckedChange={(checked) => setBinding(prev => ({
                              ...prev,
                              deploymentConfig: {
                                ...prev.deploymentConfig!,
                                errorReporting: checked
                              }
                            }))}
                          />
                          <Label>Enable error reporting</Label>
                        </div>
                        
                        <div className="flex items-center space-x-2">
                          <Switch
                            checked={binding.deploymentConfig?.analytics || false}
                            onCheckedChange={(checked) => setBinding(prev => ({
                              ...prev,
                              deploymentConfig: {
                                ...prev.deploymentConfig!,
                                analytics: checked
                              }
                            }))}
                          />
                          <Label>Enable analytics</Label>
                        </div>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </div>
            )}
          </div>

          {/* Footer */}
          <div className="flex items-center justify-between p-6 border-t border-border">
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              {binding.status && (
                <Badge variant="outline">{binding.status}</Badge>
              )}
              <span>Component: {component.componentType}</span>
            </div>
            
            <div className="flex items-center gap-2">
              <Button variant="outline" onClick={onClose}>
                Cancel
              </Button>
              <Button 
                onClick={handleSaveBinding}
                disabled={!selectedECSComponent || readOnly}
              >
                <Save className="h-xs w-xs mr-2" />
                Save Binding
              </Button>
            </div>
          </div>
        </div>
      </div>

      {/* ECS Component Browser */}
      {showECSBrowser && (
        <ECSComponentBrowser
          selectedComponent={selectedECSComponent?.name || null}
          onSelect={handleECSComponentSelect}
          onClose={() => setShowECSBrowser(false)}
        />
      )}
    </>
  );
};

ComponentBindingInterface.displayName = 'ComponentBindingInterface';