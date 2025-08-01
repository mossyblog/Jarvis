/**
 * WriteConfigModal - ECS component selection for write operations
 * 
 * Provides a modal interface for configuring write operations by mapping
 * component interactions to ECS component updates with field mappings
 * and validation rules.
 */

import React, { useState, useCallback, useMemo } from 'react';
import {
  Database,
  Zap,
  CheckCircle,
  AlertTriangle,
  Plus,
  X,
  Clock
} from 'lucide-react';

import type { GridComponent } from '@/types/bento';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import { Separator } from '@/components/ui/separator';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
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
import { FieldMappingEditor } from './FieldMappingEditor';

// ============================================================================
// Types
// ============================================================================

export interface WriteConfigModalProps {
  /** Component being configured */
  component: GridComponent;
  /** Called when modal is closed */
  onClose: () => void;
  /** Called when configuration is saved */
  onSave: (config: WriteConfiguration) => void;
  /** Whether the modal is read-only */
  readOnly?: boolean;
}

export interface WriteConfiguration {
  ecsComponent: string;
  fieldMappings: FieldMapping[];
  triggers: WriteTrigger[];
  validation?: ValidationConfig;
  optimisticUpdate?: boolean;
}

export interface FieldMapping {
  ecsField: string;
  source: 'prop' | 'state' | 'computed';
  sourcePath: string;
  uiControl: UIControlType;
  transform?: string;
  validation?: FieldValidation;
}

export interface WriteTrigger {
  event: string;
  condition?: string;
  debounce?: number;
  confirmation?: ConfirmationConfig;
}

export interface ValidationConfig {
  enabled: boolean;
  rules: ValidationRule[];
  onError: 'throw' | 'ignore' | 'fallback';
}

export interface FieldValidation {
  required?: boolean;
  min?: number;
  max?: number;
  pattern?: string;
  custom?: string;
}

export interface ConfirmationConfig {
  enabled: boolean;
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
}

export interface ValidationRule {
  field: string;
  type: 'required' | 'type' | 'range' | 'pattern' | 'custom';
  value?: unknown;
  message: string;
}

export type UIControlType = 
  | 'text'
  | 'number'
  | 'select'
  | 'multiselect'
  | 'checkbox'
  | 'switch'
  | 'date'
  | 'datetime'
  | 'textarea'
  | 'json'
  | 'code'
  | 'color'
  | 'file';

// ============================================================================
// Mock ECS Components
// ============================================================================

interface ECSComponent {
  name: string;
  displayName: string;
  description: string;
  category: string;
  fields: ECSField[];
}

interface ECSField {
  name: string;
  type: 'string' | 'number' | 'boolean' | 'date' | 'json' | 'array';
  required: boolean;
  description?: string;
  defaultValue?: unknown;
  constraints?: {
    min?: number;
    max?: number;
    pattern?: string;
    enum?: string[];
  };
}

const ECS_COMPONENTS: ECSComponent[] = [
  {
    name: 'UserComponent',
    displayName: 'User Profile',
    description: 'Manages user profile information and preferences',
    category: 'User Management',
    fields: [
      { name: 'firstName', type: 'string', required: true, description: 'User first name' },
      { name: 'lastName', type: 'string', required: true, description: 'User last name' },
      { name: 'email', type: 'string', required: true, description: 'User email address' },
      { name: 'phoneNumber', type: 'string', required: false, description: 'User phone number' },
      { name: 'dateOfBirth', type: 'date', required: false, description: 'User date of birth' },
      { name: 'isActive', type: 'boolean', required: true, description: 'User active status' }
    ]
  },
  {
    name: 'MetricComponent',
    displayName: 'Business Metric',
    description: 'Stores business metrics and KPIs',
    category: 'Analytics',
    fields: [
      { name: 'name', type: 'string', required: true, description: 'Metric name' },
      { name: 'value', type: 'number', required: true, description: 'Metric value' },
      { name: 'target', type: 'number', required: false, description: 'Target value' },
      { name: 'unit', type: 'string', required: false, description: 'Unit of measurement' },
      { name: 'category', type: 'string', required: true, description: 'Metric category' },
      { name: 'timestamp', type: 'date', required: true, description: 'Measurement timestamp' }
    ]
  },
  {
    name: 'SettingsComponent',
    displayName: 'Application Settings',
    description: 'Application configuration and preferences',
    category: 'Configuration',
    fields: [
      { name: 'theme', type: 'string', required: true, description: 'UI theme preference' },
      { name: 'language', type: 'string', required: true, description: 'Language preference' },
      { name: 'notifications', type: 'boolean', required: true, description: 'Enable notifications' },
      { name: 'timezone', type: 'string', required: true, description: 'User timezone' },
      { name: 'autoSave', type: 'boolean', required: false, description: 'Enable auto-save' },
      { name: 'defaultView', type: 'string', required: false, description: 'Default dashboard view' }
    ]
  },
  {
    name: 'OrderComponent',
    displayName: 'Order Management',
    description: 'Manages order information and status',
    category: 'E-commerce',
    fields: [
      { name: 'orderNumber', type: 'string', required: true, description: 'Unique order number' },
      { name: 'customerId', type: 'string', required: true, description: 'Customer identifier' },
      { name: 'status', type: 'string', required: true, description: 'Order status' },
      { name: 'totalAmount', type: 'number', required: true, description: 'Total order amount' },
      { name: 'orderDate', type: 'date', required: true, description: 'Order creation date' },
      { name: 'notes', type: 'string', required: false, description: 'Order notes' }
    ]
  },
  {
    name: 'ContentComponent',
    displayName: 'Content Management',
    description: 'Manages content items and metadata',
    category: 'Content',
    fields: [
      { name: 'title', type: 'string', required: true, description: 'Content title' },
      { name: 'body', type: 'string', required: true, description: 'Content body' },
      { name: 'status', type: 'string', required: true, description: 'Publication status' },
      { name: 'authorId', type: 'string', required: true, description: 'Author identifier' },
      { name: 'publishDate', type: 'date', required: false, description: 'Publication date' },
      { name: 'tags', type: 'array', required: false, description: 'Content tags' }
    ]
  }
];

// ============================================================================
// Component Events Map
// ============================================================================

const COMPONENT_EVENTS: Record<string, string[]> = {
  'metric-card': ['onClick', 'onDoubleClick', 'onValueChange', 'onTargetChange'],
  'user-list': ['onUserClick', 'onUserSelect', 'onStatusChange', 'onBulkAction'],
  'data-table': ['onRowClick', 'onCellEdit', 'onSort', 'onFilter', 'onPageChange'],
  'text-block': ['onTextChange', 'onSave', 'onCancel'],
  'settings-panel': ['onSettingChange', 'onSave', 'onReset'],
  'contact-form': ['onSubmit', 'onFieldChange', 'onValidation'],
  'default': ['onClick', 'onChange', 'onSubmit', 'onFocus', 'onBlur']
};

// ============================================================================
// Main Component
// ============================================================================

export const WriteConfigModal: React.FC<WriteConfigModalProps> = ({
  component,
  onClose,
  onSave,
  readOnly = false
}) => {
  const [selectedECS, setSelectedECS] = useState<string>('');
  const [fieldMappings, setFieldMappings] = useState<FieldMapping[]>([]);
  const [triggers, setTriggers] = useState<WriteTrigger[]>([]);
  const [validation, setValidation] = useState<ValidationConfig>({
    enabled: true,
    rules: [],
    onError: 'throw'
  });
  const [optimisticUpdate, setOptimisticUpdate] = useState(false);
  const [activeTab, setActiveTab] = useState('ecs-selection');

  // Get selected ECS component details
  const selectedECSComponent = useMemo(() => {
    return ECS_COMPONENTS.find(ecs => ecs.name === selectedECS);
  }, [selectedECS]);

  // Get available events for component type
  const availableEvents = useMemo(() => {
    return COMPONENT_EVENTS[component.componentType] || COMPONENT_EVENTS.default;
  }, [component.componentType]);

  // Handle ECS component selection
  const handleECSSelection = useCallback((ecsName: string) => {
    setSelectedECS(ecsName);
    setFieldMappings([]); // Reset mappings when ECS changes
    setActiveTab('field-mapping');
  }, []);

  // Handle field mappings update
  const handleFieldMappingsUpdate = useCallback((mappings: FieldMapping[]) => {
    setFieldMappings(mappings);
  }, []);

  // Handle trigger addition
  const handleAddTrigger = useCallback(() => {
    const newTrigger: WriteTrigger = {
      event: availableEvents[0] || 'onClick',
      debounce: 300
    };
    setTriggers(prev => [...prev, newTrigger]);
  }, [availableEvents]);

  // Handle trigger update
  const handleUpdateTrigger = useCallback((index: number, updates: Partial<WriteTrigger>) => {
    setTriggers(prev => prev.map((trigger, i) => 
      i === index ? { ...trigger, ...updates } : trigger
    ));
  }, []);

  // Handle trigger removal
  const handleRemoveTrigger = useCallback((index: number) => {
    setTriggers(prev => prev.filter((_, i) => i !== index));
  }, []);

  // Handle save
  const handleSave = useCallback(() => {
    const config: WriteConfiguration = {
      ecsComponent: selectedECS,
      fieldMappings,
      triggers,
      validation: validation.enabled ? validation : undefined,
      optimisticUpdate
    };
    
    onSave(config);
  }, [selectedECS, fieldMappings, triggers, validation, optimisticUpdate, onSave]);

  // Validate configuration
  const isConfigValid = useMemo(() => {
    return selectedECS && fieldMappings.length > 0 && triggers.length > 0;
  }, [selectedECS, fieldMappings, triggers]);

  return (
    <Dialog open onOpenChange={onClose}>
      <DialogContent className="max-w-4xl max-h-[90vh] overflow-hidden">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Zap className="h-5 w-5" />
            Configure Write Actions
          </DialogTitle>
          <DialogDescription>
            Map component interactions to ECS component updates for data persistence.
          </DialogDescription>
        </DialogHeader>

        <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1 overflow-hidden">
          <TabsList className="grid w-full grid-cols-4">
            <TabsTrigger value="ecs-selection">ECS Component</TabsTrigger>
            <TabsTrigger value="field-mapping" disabled={!selectedECS}>Field Mapping</TabsTrigger>
            <TabsTrigger value="triggers" disabled={!selectedECS}>Triggers</TabsTrigger>
            <TabsTrigger value="validation" disabled={!selectedECS}>Validation</TabsTrigger>
          </TabsList>

          <div className="flex-1 overflow-auto mt-4">
            {/* ECS Component Selection */}
            <TabsContent value="ecs-selection" className="mt-0">
              <div className="space-y-4">
                <div>
                  <Label className="text-base font-medium">Select Target ECS Component</Label>
                  <p className="text-sm text-muted-foreground mt-1">
                    Choose the ECS component that will store the data from this UI component.
                  </p>
                </div>

                <div className="grid gap-3">
                  {ECS_COMPONENTS.map((ecs) => (
                    <Card
                      key={ecs.name}
                      className={cn(
                        'cursor-pointer transition-all hover:shadow-md',
                        selectedECS === ecs.name && 'ring-2 ring-primary ring-offset-2'
                      )}
                      onClick={() => handleECSSelection(ecs.name)}
                    >
                      <CardHeader className="pb-2">
                        <div className="flex items-start justify-between">
                          <div className="flex-1">
                            <CardTitle className="text-base flex items-center gap-2">
                              <Database className="h-4 w-4" />
                              {ecs.displayName}
                              {selectedECS === ecs.name && (
                                <CheckCircle className="h-4 w-4 text-primary" />
                              )}
                            </CardTitle>
                            <p className="text-sm text-muted-foreground mt-1">
                              {ecs.description}
                            </p>
                          </div>
                          <Badge variant="outline">{ecs.category}</Badge>
                        </div>
                      </CardHeader>
                      <CardContent className="pt-0">
                        <div className="flex items-center gap-4 text-xs text-muted-foreground">
                          <span>{ecs.fields.length} fields</span>
                          <span>{ecs.fields.filter(f => f.required).length} required</span>
                          <span className="font-mono">{ecs.name}</span>
                        </div>
                      </CardContent>
                    </Card>
                  ))}
                </div>
              </div>
            </TabsContent>

            {/* Field Mapping */}
            <TabsContent value="field-mapping" className="mt-0">
              {selectedECSComponent && (
                <FieldMappingEditor
                  ecsComponent={selectedECSComponent}
                  componentType={component.componentType}
                  mappings={fieldMappings}
                  onChange={handleFieldMappingsUpdate}
                  readOnly={readOnly}
                />
              )}
            </TabsContent>

            {/* Triggers */}
            <TabsContent value="triggers" className="mt-0">
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <div>
                    <Label className="text-base font-medium">Write Triggers</Label>
                    <p className="text-sm text-muted-foreground mt-1">
                      Configure when write operations should be executed.
                    </p>
                  </div>
                  <Button onClick={handleAddTrigger} disabled={readOnly}>
                    <Plus className="h-4 w-4 mr-1" />
                    Add Trigger
                  </Button>
                </div>

                {triggers.length === 0 ? (
                  <Card className="border-dashed">
                    <CardContent className="flex items-center justify-center py-8">
                      <div className="text-center">
                        <Clock className="h-8 w-8 mx-auto mb-2 text-muted-foreground opacity-50" />
                        <p className="text-sm text-muted-foreground">No triggers configured</p>
                        <p className="text-xs text-muted-foreground">Add triggers to define when writes occur</p>
                      </div>
                    </CardContent>
                  </Card>
                ) : (
                  <div className="space-y-3">
                    {triggers.map((trigger, index) => (
                      <Card key={index}>
                        <CardContent className="p-4">
                          <div className="space-y-3">
                            <div className="flex items-center justify-between">
                              <Label className="text-sm font-medium">Trigger {index + 1}</Label>
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => handleRemoveTrigger(index)}
                                disabled={readOnly}
                              >
                                <X className="h-3 w-3" />
                              </Button>
                            </div>

                            <div className="grid grid-cols-2 gap-3">
                              <div>
                                <Label htmlFor={`event-${index}`}>Event</Label>
                                <Select
                                  value={trigger.event}
                                  onValueChange={(value) => handleUpdateTrigger(index, { event: value })}
                                  disabled={readOnly}
                                >
                                  <SelectTrigger>
                                    <SelectValue />
                                  </SelectTrigger>
                                  <SelectContent>
                                    {availableEvents.map((event) => (
                                      <SelectItem key={event} value={event}>
                                        {event}
                                      </SelectItem>
                                    ))}
                                  </SelectContent>
                                </Select>
                              </div>

                              <div>
                                <Label htmlFor={`debounce-${index}`}>Debounce (ms)</Label>
                                <Input
                                  id={`debounce-${index}`}
                                  type="number"
                                  value={trigger.debounce || 0}
                                  onChange={(e) => handleUpdateTrigger(index, { 
                                    debounce: parseInt(e.target.value) || 0 
                                  })}
                                  disabled={readOnly}
                                />
                              </div>
                            </div>

                            <div>
                              <Label htmlFor={`condition-${index}`}>Condition (optional)</Label>
                              <Input
                                id={`condition-${index}`}
                                placeholder="e.g., value > 0"
                                value={trigger.condition || ''}
                                onChange={(e) => handleUpdateTrigger(index, { condition: e.target.value })}
                                disabled={readOnly}
                              />
                            </div>

                            <div className="space-y-2">
                              <div className="flex items-center space-x-2">
                                <Switch
                                  id={`confirmation-${index}`}
                                  checked={trigger.confirmation?.enabled || false}
                                  onCheckedChange={(checked) => handleUpdateTrigger(index, {
                                    confirmation: checked ? {
                                      enabled: true,
                                      title: 'Confirm Action',
                                      message: 'Are you sure you want to proceed?'
                                    } : undefined
                                  })}
                                  disabled={readOnly}
                                />
                                <Label htmlFor={`confirmation-${index}`}>Require confirmation</Label>
                              </div>

                              {trigger.confirmation?.enabled && (
                                <div className="ml-6 space-y-2">
                                  <Input
                                    placeholder="Confirmation title"
                                    value={trigger.confirmation.title}
                                    onChange={(e) => handleUpdateTrigger(index, {
                                      confirmation: { ...trigger.confirmation!, title: e.target.value }
                                    })}
                                    disabled={readOnly}
                                  />
                                  <Textarea
                                    placeholder="Confirmation message"
                                    value={trigger.confirmation.message}
                                    onChange={(e) => handleUpdateTrigger(index, {
                                      confirmation: { ...trigger.confirmation!, message: e.target.value }
                                    })}
                                    disabled={readOnly}
                                    rows={2}
                                  />
                                </div>
                              )}
                            </div>
                          </div>
                        </CardContent>
                      </Card>
                    ))}
                  </div>
                )}
              </div>
            </TabsContent>

            {/* Validation */}
            <TabsContent value="validation" className="mt-0">
              <div className="space-y-4">
                <div className="flex items-center space-x-2">
                  <Switch
                    id="enable-validation"
                    checked={validation.enabled}
                    onCheckedChange={(checked) => setValidation(prev => ({ ...prev, enabled: checked }))}
                    disabled={readOnly}
                  />
                  <Label htmlFor="enable-validation" className="text-base font-medium">
                    Enable Validation
                  </Label>
                </div>

                {validation.enabled && (
                  <>
                    <div>
                      <Label>Error Handling</Label>
                      <Select
                        value={validation.onError}
                        onValueChange={(value) => setValidation(prev => ({ 
                          ...prev, 
                          onError: value as ValidationConfig['onError'] 
                        }))}
                        disabled={readOnly}
                      >
                        <SelectTrigger>
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="throw">Throw Error</SelectItem>
                          <SelectItem value="ignore">Ignore Error</SelectItem>
                          <SelectItem value="fallback">Use Fallback</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>

                    <Separator />

                    <div className="space-y-2">
                      <div className="flex items-center space-x-2">
                        <Switch
                          id="optimistic-update"
                          checked={optimisticUpdate}
                          onCheckedChange={setOptimisticUpdate}
                          disabled={readOnly}
                        />
                        <Label htmlFor="optimistic-update">Optimistic Updates</Label>
                      </div>
                      <p className="text-xs text-muted-foreground ml-6">
                        Update UI immediately, then sync with server
                      </p>
                    </div>
                  </>
                )}
              </div>
            </TabsContent>
          </div>
        </Tabs>

        <DialogFooter className="border-t pt-4">
          <div className="flex items-center justify-between w-full">
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              {!isConfigValid && (
                <>
                  <AlertTriangle className="h-4 w-4 text-orange-500" />
                  <span>Complete all required steps to save</span>
                </>
              )}
            </div>
            
            <div className="flex gap-2">
              <Button variant="outline" onClick={onClose}>
                Cancel
              </Button>
              <Button
                onClick={handleSave}
                disabled={!isConfigValid || readOnly}
              >
                <CheckCircle className="h-4 w-4 mr-1" />
                Save Configuration
              </Button>
            </div>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};

WriteConfigModal.displayName = 'WriteConfigModal';