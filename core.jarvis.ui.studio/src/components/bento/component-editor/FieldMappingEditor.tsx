/**
 * FieldMappingEditor - Map fields to UI controls
 * 
 * Provides an interface for mapping ECS component fields to UI controls
 * with validation rules and data source configuration.
 */

import React, { useState, useCallback, useMemo } from 'react';
import {
  ArrowRight,
  Settings,
  Type,
  Hash,
  CheckSquare,
  Calendar,
  FileText,
  Code,
  Palette,
  Upload,
  ChevronDown,
  AlertCircle,
  CheckCircle,
  Plus,
  X
} from 'lucide-react';

import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible';
import type { FieldMapping, UIControlType, FieldValidation } from './WriteConfigModal';

// ============================================================================
// Types
// ============================================================================

export interface FieldMappingEditorProps {
  /** ECS component being mapped */
  ecsComponent: ECSComponent;
  /** UI component type */
  componentType: string;
  /** Current field mappings */
  mappings: FieldMapping[];
  /** Called when mappings are updated */
  onChange?: (mappings: FieldMapping[]) => void;
  /** Whether the editor is read-only */
  readOnly?: boolean;
  /** Additional CSS classes */
  className?: string;
}

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

interface ComponentDataSource {
  source: 'prop' | 'state' | 'computed';
  options: DataSourceOption[];
}

interface DataSourceOption {
  path: string;
  label: string;
  type: string;
  description?: string;
}

// ============================================================================
// UI Control Configurations
// ============================================================================

interface UIControlConfig {
  type: UIControlType;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  description: string;
  supportedTypes: string[];
  defaultProps?: Record<string, unknown>;
}

const UI_CONTROLS: Record<UIControlType, UIControlConfig> = {
  text: {
    type: 'text',
    label: 'Text Input',
    icon: Type,
    description: 'Single-line text input field',
    supportedTypes: ['string'],
    defaultProps: { placeholder: 'Enter text...' }
  },
  number: {
    type: 'number',
    label: 'Number Input',
    icon: Hash,
    description: 'Numeric input with validation',
    supportedTypes: ['number'],
    defaultProps: { min: 0, step: 1 }
  },
  select: {
    type: 'select',
    label: 'Dropdown Select',
    icon: ChevronDown,
    description: 'Single selection dropdown',
    supportedTypes: ['string', 'number'],
    defaultProps: { placeholder: 'Select option...' }
  },
  multiselect: {
    type: 'multiselect',
    label: 'Multi-Select',
    icon: CheckSquare,
    description: 'Multiple selection dropdown',
    supportedTypes: ['array'],
    defaultProps: { placeholder: 'Select options...' }
  },
  checkbox: {
    type: 'checkbox',
    label: 'Checkbox',
    icon: CheckSquare,
    description: 'Boolean checkbox input',
    supportedTypes: ['boolean']
  },
  switch: {
    type: 'switch',
    label: 'Toggle Switch',
    icon: Settings,
    description: 'Boolean toggle switch',
    supportedTypes: ['boolean']
  },
  date: {
    type: 'date',
    label: 'Date Picker',
    icon: Calendar,
    description: 'Date selection input',
    supportedTypes: ['date', 'string']
  },
  datetime: {
    type: 'datetime',
    label: 'DateTime Picker',
    icon: Calendar,
    description: 'Date and time selection',
    supportedTypes: ['date', 'string']
  },
  textarea: {
    type: 'textarea',
    label: 'Text Area',
    icon: FileText,
    description: 'Multi-line text input',
    supportedTypes: ['string']
  },
  json: {
    type: 'json',
    label: 'JSON Editor',
    icon: Code,
    description: 'JSON object editor',
    supportedTypes: ['json', 'array']
  },
  code: {
    type: 'code',
    label: 'Code Editor',
    icon: Code,
    description: 'Syntax-highlighted code editor',
    supportedTypes: ['string']
  },
  color: {
    type: 'color',
    label: 'Color Picker',
    icon: Palette,
    description: 'Color selection input',
    supportedTypes: ['string']
  },
  file: {
    type: 'file',
    label: 'File Upload',
    icon: Upload,
    description: 'File upload control',
    supportedTypes: ['string', 'array']
  }
};

// ============================================================================
// Component Data Sources
// ============================================================================

const getComponentDataSources = (componentType: string): Record<string, ComponentDataSource> => {
  const commonProps: ComponentDataSource = {
    source: 'prop',
    options: [
      { path: 'className', label: 'CSS Classes', type: 'string' },
      { path: 'style', label: 'Inline Styles', type: 'object' },
      { path: 'disabled', label: 'Disabled State', type: 'boolean' },
      { path: 'loading', label: 'Loading State', type: 'boolean' }
    ]
  };

  const commonState: ComponentDataSource = {
    source: 'state',
    options: [
      { path: 'isVisible', label: 'Visibility', type: 'boolean' },
      { path: 'isSelected', label: 'Selection State', type: 'boolean' },
      { path: 'isModified', label: 'Modified State', type: 'boolean' },
      { path: 'validationErrors', label: 'Validation Errors', type: 'array' }
    ]
  };

  const commonComputed: ComponentDataSource = {
    source: 'computed',
    options: [
      { path: 'getCurrentUser()', label: 'Current User', type: 'object' },
      { path: 'getCurrentTime()', label: 'Current Time', type: 'date' },
      { path: 'getFormData()', label: 'Form Data', type: 'object' },
      { path: 'getValidationState()', label: 'Validation State', type: 'boolean' }
    ]
  };

  // Component-specific data sources
  const specificSources: Record<string, Partial<Record<string, ComponentDataSource>>> = {
    'metric-card': {
      prop: {
        source: 'prop',
        options: [
          ...commonProps.options,
          { path: 'title', label: 'Card Title', type: 'string' },
          { path: 'value', label: 'Metric Value', type: 'number' },
          { path: 'previousValue', label: 'Previous Value', type: 'number' },
          { path: 'trend', label: 'Trend Direction', type: 'string' },
          { path: 'format', label: 'Value Format', type: 'string' }
        ]
      }
    },
    'user-list': {
      prop: {
        source: 'prop',
        options: [
          ...commonProps.options,
          { path: 'users', label: 'User Data', type: 'array' },
          { path: 'selectedUsers', label: 'Selected Users', type: 'array' },
          { path: 'showAvatar', label: 'Show Avatars', type: 'boolean' },
          { path: 'maxUsers', label: 'Max Users', type: 'number' }
        ]
      }
    },
    'contact-form': {
      prop: {
        source: 'prop',
        options: [
          ...commonProps.options,
          { path: 'formData', label: 'Form Data', type: 'object' },
          { path: 'errors', label: 'Form Errors', type: 'object' },
          { path: 'isSubmitting', label: 'Submitting State', type: 'boolean' }
        ]
      },
      state: {
        source: 'state',
        options: [
          ...commonState.options,
          { path: 'firstName', label: 'First Name', type: 'string' },
          { path: 'lastName', label: 'Last Name', type: 'string' },
          { path: 'email', label: 'Email Address', type: 'string' },
          { path: 'message', label: 'Message', type: 'string' }
        ]
      }
    }
  };

  return {
    prop: specificSources[componentType]?.prop || commonProps,
    state: specificSources[componentType]?.state || commonState,
    computed: specificSources[componentType]?.computed || commonComputed
  };
};

// ============================================================================
// Field Validation Editor
// ============================================================================

interface ValidationEditorProps {
  field: ECSField;
  validation: FieldValidation;
  onChange: (validation: FieldValidation) => void;
  readOnly?: boolean;
}

const ValidationEditor: React.FC<ValidationEditorProps> = ({
  field,
  validation,
  onChange,
  readOnly = false
}) => {
  const handleValidationChange = useCallback((updates: Partial<FieldValidation>) => {
    onChange({ ...validation, ...updates });
  }, [validation, onChange]);

  return (
    <div className="space-y-3 text-sm">
      <div className="flex items-center space-x-2">
        <Switch
          id="required"
          checked={validation.required || false}
          onCheckedChange={(checked) => handleValidationChange({ required: checked })}
          disabled={readOnly || field.required}
        />
        <Label htmlFor="required">Required field</Label>
        {field.required && (
          <Badge variant="secondary" className="text-xs">
            Required by ECS
          </Badge>
        )}
      </div>

      {(field.type === 'number' || field.type === 'string') && (
        <div className="grid grid-cols-2 gap-2">
          <div>
            <Label htmlFor="min" className="text-xs">
              {field.type === 'number' ? 'Min Value' : 'Min Length'}
            </Label>
            <Input
              id="min"
              type="number"
              value={validation.min || ''}
              onChange={(e) => handleValidationChange({ 
                min: parseInt(e.target.value) || undefined 
              })}
              disabled={readOnly}
              className="h-8"
            />
          </div>
          <div>
            <Label htmlFor="max" className="text-xs">
              {field.type === 'number' ? 'Max Value' : 'Max Length'}
            </Label>
            <Input
              id="max"
              type="number"
              value={validation.max || ''}
              onChange={(e) => handleValidationChange({ 
                max: parseInt(e.target.value) || undefined 
              })}
              disabled={readOnly}
              className="h-8"
            />
          </div>
        </div>
      )}

      {field.type === 'string' && (
        <div>
          <Label htmlFor="pattern" className="text-xs">Pattern (RegExp)</Label>
          <Input
            id="pattern"
            value={validation.pattern || ''}
            onChange={(e) => handleValidationChange({ pattern: e.target.value })}
            disabled={readOnly}
            placeholder="^[a-zA-Z0-9]+$"
            className="h-8 font-mono"
          />
        </div>
      )}

      <div>
        <Label htmlFor="custom" className="text-xs">Custom Validation</Label>
        <Input
          id="custom"
          value={validation.custom || ''}
          onChange={(e) => handleValidationChange({ custom: e.target.value })}
          disabled={readOnly}
          placeholder="Custom validation function"
          className="h-8 font-mono"
        />
      </div>
    </div>
  );
};

// ============================================================================
// Field Mapping Row Component
// ============================================================================

interface FieldMappingRowProps {
  field: ECSField;
  mapping: FieldMapping | null;
  dataSources: Record<string, ComponentDataSource>;
  onUpdate: (mapping: FieldMapping) => void;
  onRemove: () => void;
  readOnly?: boolean;
}

const FieldMappingRow: React.FC<FieldMappingRowProps> = ({
  field,
  mapping,
  dataSources,
  onUpdate,
  onRemove,
  readOnly = false
}) => {
  const [isExpanded, setIsExpanded] = useState(false);

  // Get compatible UI controls for this field type
  const compatibleControls = useMemo(() => {
    return Object.values(UI_CONTROLS).filter(control =>
      control.supportedTypes.includes(field.type)
    );
  }, [field.type]);

  // Get default UI control for field type
  const defaultControl = useMemo(() => {
    const typeMap: Record<string, UIControlType> = {
      string: 'text',
      number: 'number',
      boolean: 'switch',
      date: 'date',
      json: 'json',
      array: 'multiselect'
    };
    return typeMap[field.type] || 'text';
  }, [field.type]);

  // Handle mapping updates
  const handleMappingUpdate = useCallback((updates: Partial<FieldMapping>) => {
    const currentMapping = mapping || {
      ecsField: field.name,
      source: 'prop' as const,
      sourcePath: '',
      uiControl: defaultControl
    };
    onUpdate({ ...currentMapping, ...updates });
  }, [mapping, field.name, defaultControl, onUpdate]);

  // Handle validation updates
  const handleValidationUpdate = useCallback((validation: FieldValidation) => {
    handleMappingUpdate({ validation });
  }, [handleMappingUpdate]);

  if (!mapping) {
    // Unmapped field
    return (
      <Card className="border-dashed">
        <CardContent className="p-4">
          <div className="flex items-center justify-between">
            <div className="flex-1">
              <div className="flex items-center gap-2">
                <span className="font-medium text-sm">{field.name}</span>
                <Badge variant="outline" className="text-xs">
                  {field.type}
                </Badge>
                {field.required && (
                  <Badge variant="destructive" className="text-xs">
                    Required
                  </Badge>
                )}
              </div>
              {field.description && (
                <p className="text-xs text-muted-foreground mt-1">
                  {field.description}
                </p>
              )}
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => handleMappingUpdate({})}
              disabled={readOnly}
            >
              <Plus className="h-3 w-3 mr-1" />
              Map Field
            </Button>
          </div>
        </CardContent>
      </Card>
    );
  }

  // Mapped field
  return (
    <Card>
      <CardContent className="p-4">
        <div className="space-y-4">
          {/* Field Header */}
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <span className="font-medium text-sm">{field.name}</span>
              <Badge variant="outline" className="text-xs">
                {field.type}
              </Badge>
              {field.required && (
                <Badge variant="destructive" className="text-xs">
                  Required
                </Badge>
              )}
              <CheckCircle className="h-4 w-4 text-green-600" />
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={onRemove}
              disabled={readOnly}
            >
              <X className="h-3 w-3" />
            </Button>
          </div>

          {/* Mapping Configuration */}
          <div className="grid grid-cols-12 gap-3 items-center">
            {/* Data Source */}
            <div className="col-span-3">
              <Label className="text-xs">Source</Label>
              <Select
                value={mapping.source}
                onValueChange={(value) => handleMappingUpdate({ 
                  source: value as FieldMapping['source'],
                  sourcePath: '' // Reset path when source changes
                })}
                disabled={readOnly}
              >
                <SelectTrigger className="h-8">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="prop">Component Prop</SelectItem>
                  <SelectItem value="state">Component State</SelectItem>
                  <SelectItem value="computed">Computed Value</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Arrow */}
            <div className="col-span-1 flex justify-center">
              <ArrowRight className="h-4 w-4 text-muted-foreground" />
            </div>

            {/* Source Path */}
            <div className="col-span-4">
              <Label className="text-xs">Path</Label>
              <Select
                value={mapping.sourcePath}
                onValueChange={(value) => handleMappingUpdate({ sourcePath: value })}
                disabled={readOnly}
              >
                <SelectTrigger className="h-8">
                  <SelectValue placeholder="Select path..." />
                </SelectTrigger>
                <SelectContent>
                  {dataSources[mapping.source]?.options.map((option) => (
                    <SelectItem key={option.path} value={option.path}>
                      <div className="flex items-center gap-2">
                        <span className="font-mono text-xs">{option.path}</span>
                        <Badge variant="outline" className="text-xs">
                          {option.type}
                        </Badge>
                      </div>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* UI Control */}
            <div className="col-span-4">
              <Label className="text-xs">UI Control</Label>
              <Select
                value={mapping.uiControl}
                onValueChange={(value) => handleMappingUpdate({ 
                  uiControl: value as UIControlType 
                })}
                disabled={readOnly}
              >
                <SelectTrigger className="h-8">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {compatibleControls.map((control) => {
                    const IconComponent = control.icon;
                    return (
                      <SelectItem key={control.type} value={control.type}>
                        <div className="flex items-center gap-2">
                          <IconComponent className="h-3 w-3" />
                          <span>{control.label}</span>
                        </div>
                      </SelectItem>
                    );
                  })}
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Additional Configuration */}
          <Collapsible open={isExpanded} onOpenChange={setIsExpanded}>
            <CollapsibleTrigger asChild>
              <Button variant="ghost" size="sm" className="w-full justify-between">
                <span className="flex items-center gap-2">
                  <Settings className="h-3 w-3" />
                  Advanced Configuration
                </span>
                <ChevronDown className={cn(
                  'h-3 w-3 transition-transform',
                  isExpanded && 'transform rotate-180'
                )} />
              </Button>
            </CollapsibleTrigger>
            <CollapsibleContent className="space-y-3 pt-3 border-t">
              {/* Transform */}
              <div>
                <Label htmlFor="transform" className="text-xs">
                  Transform Expression (optional)
                </Label>
                <Input
                  id="transform"
                  value={mapping.transform || ''}
                  onChange={(e) => handleMappingUpdate({ transform: e.target.value })}
                  disabled={readOnly}
                  placeholder="e.g., value.toUpperCase()"
                  className="h-8 font-mono"
                />
              </div>

              {/* Validation */}
              <div>
                <Label className="text-xs font-medium">Field Validation</Label>
                <div className="mt-2 p-3 border rounded-md">
                  <ValidationEditor
                    field={field}
                    validation={mapping.validation || {}}
                    onChange={handleValidationUpdate}
                    readOnly={readOnly}
                  />
                </div>
              </div>
            </CollapsibleContent>
          </Collapsible>
        </div>
      </CardContent>
    </Card>
  );
};

// ============================================================================
// Main Component
// ============================================================================

export const FieldMappingEditor: React.FC<FieldMappingEditorProps> = ({
  ecsComponent,
  componentType,
  mappings,
  onChange,
  readOnly = false,
  className
}) => {
  // Get data sources for component type
  const dataSources = useMemo(() => {
    return getComponentDataSources(componentType);
  }, [componentType]);

  // Get mapped and unmapped fields
  const { mappedFields, unmappedFields } = useMemo(() => {
    const mappedFieldNames = new Set(mappings.map(m => m.ecsField));
    return {
      mappedFields: ecsComponent.fields.filter(field => mappedFieldNames.has(field.name)),
      unmappedFields: ecsComponent.fields.filter(field => !mappedFieldNames.has(field.name))
    };
  }, [ecsComponent.fields, mappings]);

  // Update mapping
  const handleUpdateMapping = useCallback((ecsField: string, mapping: FieldMapping) => {
    const updatedMappings = mappings.map(m => 
      m.ecsField === ecsField ? mapping : m
    );
    
    // Add new mapping if it doesn't exist
    if (!mappings.find(m => m.ecsField === ecsField)) {
      updatedMappings.push(mapping);
    }
    
    onChange?.(updatedMappings);
  }, [mappings, onChange]);

  // Remove mapping
  const handleRemoveMapping = useCallback((ecsField: string) => {
    const updatedMappings = mappings.filter(m => m.ecsField !== ecsField);
    onChange?.(updatedMappings);
  }, [mappings, onChange]);

  // Auto-map fields
  const handleAutoMap = useCallback(() => {
    const newMappings = [...mappings];
    
    unmappedFields.forEach(field => {
      // Simple auto-mapping logic
      const propSource = dataSources.prop.options.find(opt => 
        opt.path.toLowerCase().includes(field.name.toLowerCase()) ||
        field.name.toLowerCase().includes(opt.path.toLowerCase())
      );
      
      if (propSource) {
        const defaultControl = Object.values(UI_CONTROLS).find(control =>
          control.supportedTypes.includes(field.type)
        )?.type || 'text' as UIControlType;
        
        newMappings.push({
          ecsField: field.name,
          source: 'prop',
          sourcePath: propSource.path,
          uiControl: defaultControl
        });
      }
    });
    
    onChange?.(newMappings);
  }, [mappings, unmappedFields, dataSources.prop.options, onChange]);

  const requiredUnmappedFields = unmappedFields.filter(f => f.required);

  return (
    <div className={cn('field-mapping-editor space-y-4', className)}>
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h4 className="text-base font-medium">Field Mappings</h4>
          <p className="text-sm text-muted-foreground">
            Map ECS component fields to UI component data sources
          </p>
        </div>
        
        <div className="flex items-center gap-2">
          {unmappedFields.length > 0 && (
            <Button
              variant="outline"
              size="sm"
              onClick={handleAutoMap}
              disabled={readOnly}
            >
              Auto-Map Fields
            </Button>
          )}
        </div>
      </div>

      {/* Status Summary */}
      <div className="flex items-center gap-4 p-3 bg-muted/20 rounded-md text-sm">
        <div className="flex items-center gap-2">
          <CheckCircle className="h-4 w-4 text-green-600" />
          <span>{mappedFields.length} mapped</span>
        </div>
        <div className="flex items-center gap-2">
          <AlertCircle className="h-4 w-4 text-orange-500" />
          <span>{unmappedFields.length} unmapped</span>
        </div>
        {requiredUnmappedFields.length > 0 && (
          <div className="flex items-center gap-2">
            <AlertCircle className="h-4 w-4 text-red-500" />
            <span>{requiredUnmappedFields.length} required unmapped</span>
          </div>
        )}
      </div>

      {/* Field Mappings */}
      <div className="space-y-3">
        {ecsComponent.fields.map(field => {
          const mapping = mappings.find(m => m.ecsField === field.name);
          
          return (
            <FieldMappingRow
              key={field.name}
              field={field}
              mapping={mapping || null}
              dataSources={dataSources}
              onUpdate={(mapping) => handleUpdateMapping(field.name, mapping)}
              onRemove={() => handleRemoveMapping(field.name)}
              readOnly={readOnly}
            />
          );
        })}
      </div>

      {/* Component Info */}
      <Card className="border-dashed">
        <CardHeader className="pb-2">
          <CardTitle className="text-sm">ECS Component: {ecsComponent.displayName}</CardTitle>
        </CardHeader>
        <CardContent className="text-sm space-y-2">
          <div className="flex justify-between">
            <span className="text-muted-foreground">Total Fields:</span>
            <span>{ecsComponent.fields.length}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Required Fields:</span>
            <span>{ecsComponent.fields.filter(f => f.required).length}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Mapped Fields:</span>
            <span>{mappedFields.length}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Component:</span>
            <span className="font-mono text-xs">{ecsComponent.name}</span>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

FieldMappingEditor.displayName = 'FieldMappingEditor';