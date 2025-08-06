/**
 * PropertyMapper - Map GraphQL results to component props
 * 
 * Provides an interface for mapping GraphQL query result fields
 * to component properties with transformation support.
 */

import React, { useState, useCallback, useMemo } from 'react';
import {
  ArrowRight,
  Code,
  Plus,
  X,
  Eye,
  EyeOff,
  AlertCircle,
  CheckCircle,
  Wand2
} from 'lucide-react';

import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';

// ============================================================================
// Types
// ============================================================================

export interface PropertyMapperProps {
  /** GraphQL query to extract fields from */
  query: string;
  /** Component type to get available properties */
  componentType: string;
  /** Current property mappings */
  mappings: PropertyMapping[];
  /** Called when mappings are updated */
  onChange?: (mappings: PropertyMapping[]) => void;
  /** Whether the mapper is read-only */
  readOnly?: boolean;
  /** Additional CSS classes */
  className?: string;
}

export interface PropertyMapping {
  componentProp: string;
  queryPath: string;
  transform?: string;
}

interface PropTypeDefinition {
  name: string;
  type: 'string' | 'number' | 'boolean' | 'object' | 'array';
  required?: boolean;
  description?: string;
  defaultValue?: unknown;
}

interface QueryField {
  path: string;
  type: string;
  description?: string;
  isArray?: boolean;
}

// ============================================================================
// Component Property Definitions
// ============================================================================

const COMPONENT_PROPS: Record<string, PropTypeDefinition[]> = {
  'metric-card': [
    { name: 'title', type: 'string', required: true, description: 'Card title' },
    { name: 'value', type: 'number', required: true, description: 'Primary metric value' },
    { name: 'previousValue', type: 'number', description: 'Previous period value for comparison' },
    { name: 'trend', type: 'string', description: 'Trend indicator (up, down, stable)' },
    { name: 'percentage', type: 'number', description: 'Percentage change' },
    { name: 'format', type: 'string', description: 'Value format (currency, percent, number)' },
    { name: 'color', type: 'string', description: 'Color theme for the card' }
  ],
  'line-chart': [
    { name: 'data', type: 'array', required: true, description: 'Chart data points' },
    { name: 'xField', type: 'string', required: true, description: 'X-axis field name' },
    { name: 'yField', type: 'string', required: true, description: 'Y-axis field name' },
    { name: 'title', type: 'string', description: 'Chart title' },
    { name: 'color', type: 'string', description: 'Line color' },
    { name: 'smooth', type: 'boolean', description: 'Use smooth curves' }
  ],
  'user-list': [
    { name: 'users', type: 'array', required: true, description: 'Array of user objects' },
    { name: 'showAvatar', type: 'boolean', description: 'Display user avatars' },
    { name: 'showStatus', type: 'boolean', description: 'Display online status' },
    { name: 'maxUsers', type: 'number', description: 'Maximum users to display' }
  ],
  'data-table': [
    { name: 'data', type: 'array', required: true, description: 'Table row data' },
    { name: 'columns', type: 'array', required: true, description: 'Column definitions' },
    { name: 'pagination', type: 'object', description: 'Pagination configuration' },
    { name: 'sortable', type: 'boolean', description: 'Enable column sorting' },
    { name: 'filterable', type: 'boolean', description: 'Enable row filtering' }
  ],
  'text-block': [
    { name: 'content', type: 'string', required: true, description: 'Text content' },
    { name: 'variant', type: 'string', description: 'Text variant (h1, h2, p, etc.)' },
    { name: 'align', type: 'string', description: 'Text alignment' }
  ],
  'placeholder': [
    { name: 'text', type: 'string', description: 'Placeholder text' },
    { name: 'icon', type: 'string', description: 'Icon to display' }
  ]
};

// ============================================================================
// GraphQL Field Extraction
// ============================================================================

const extractFieldsFromQuery = (query: string): QueryField[] => {
  const fields: QueryField[] = [];
  
  // Simple field extraction - in a real implementation, this would use a proper GraphQL parser
  const lines = query.split('\n');
  const currentPath: string[] = [];
  
  lines.forEach(line => {
    const trimmed = line.trim();
    
    // Skip comments and empty lines
    if (trimmed.startsWith('#') || !trimmed) return;
    
    // Handle opening braces
    if (trimmed.includes('{')) {
      const beforeBrace = trimmed.split('{')[0].trim();
      if (beforeBrace && !beforeBrace.startsWith('query') && !beforeBrace.startsWith('mutation')) {
        // Remove parameters from field name
        const fieldName = beforeBrace.split('(')[0].trim();
        if (fieldName) {
          currentPath.push(fieldName);
        }
      }
    }
    
    // Handle closing braces
    if (trimmed.includes('}')) {
      if (currentPath.length > 0) {
        currentPath.pop();
      }
    }
    
    // Handle field definitions
    if (trimmed && !trimmed.includes('{') && !trimmed.includes('}') && !trimmed.startsWith('query')) {
      const fieldName = trimmed.split('(')[0].trim();
      if (fieldName && currentPath.length > 0) {
        const fullPath = [...currentPath, fieldName].join('.');
        fields.push({
          path: fullPath,
          type: 'unknown',
          isArray: false
        });
      }
    }
  });
  
  return fields;
};

// ============================================================================
// Transform Editor Dialog
// ============================================================================

interface TransformEditorProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  mapping: PropertyMapping;
  onSave: (transform: string) => void;
}

const TransformEditor: React.FC<TransformEditorProps> = ({
  isOpen,
  onOpenChange,
  mapping,
  onSave
}) => {
  const [transform, setTransform] = useState(mapping.transform || '');

  const handleSave = useCallback(() => {
    onSave(transform);
    onOpenChange(false);
  }, [transform, onSave, onOpenChange]);

  const commonTransforms = [
    { label: 'Format Currency', code: `Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value)` },
    { label: 'Format Percentage', code: `(value * 100).toFixed(1) + '%'` },
    { label: 'Uppercase', code: `String(value).toUpperCase()` },
    { label: 'Date Format', code: `new Date(value).toLocaleDateString()` },
    { label: 'Truncate Text', code: `String(value).length > 50 ? String(value).substring(0, 50) + '...' : String(value)` },
    { label: 'Array Length', code: `Array.isArray(value) ? value.length : 0` }
  ];

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Transform Editor</DialogTitle>
          <DialogDescription>
            Create a JavaScript expression to transform the value from <code>{mapping.queryPath}</code> before 
            passing it to the <code>{mapping.componentProp}</code> property.
          </DialogDescription>
        </DialogHeader>
        
        <div className="space-y-4">
          {/* Common Transforms */}
          <div>
            <Label className="text-sm font-medium">Common Transforms</Label>
            <div className="grid grid-cols-2 gap-2 mt-2">
              {commonTransforms.map((transform, index) => (
                <Button
                  key={index}
                  variant="outline"
                  size="sm"
                  onClick={() => setTransform(transform.code)}
                  className="justify-start h-auto p-2"
                >
                  <div className="text-left">
                    <div className="font-medium text-xs">{transform.label}</div>
                  </div>
                </Button>
              ))}
            </div>
          </div>
          
          {/* Transform Code */}
          <div>
            <Label htmlFor="transform" className="text-sm font-medium">
              Transform Expression
            </Label>
            <Textarea
              id="transform"
              value={transform}
              onChange={(e) => setTransform(e.target.value)}
              placeholder="// JavaScript expression (value is available as 'value' variable)&#10;return value;"
              className="font-mono text-sm mt-1"
              rows={6}
            />
            <p className="text-xs text-muted-foreground mt-1">
              Use <code>value</code> to reference the field value. Return the transformed result.
            </p>
          </div>
          
          {/* Preview */}
          {transform && (
            <div>
              <Label className="text-sm font-medium">Preview</Label>
              <div className="p-2 bg-muted rounded-md font-mono text-sm">
                <div className="text-muted-foreground">// Input: {mapping.queryPath}</div>
                <div className="text-muted-foreground">// Output: {mapping.componentProp}</div>
                <div className="mt-1">{transform}</div>
              </div>
            </div>
          )}
        </div>
        
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleSave}>
            Save Transform
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};

// ============================================================================
// Mapping Row Component
// ============================================================================

interface MappingRowProps {
  mapping: PropertyMapping;
  propDef: PropTypeDefinition;
  queryFields: QueryField[];
  onUpdate: (updates: Partial<PropertyMapping>) => void;
  onRemove: () => void;
  readOnly?: boolean;
}

const MappingRow: React.FC<MappingRowProps> = ({
  mapping,
  propDef,
  queryFields,
  onUpdate,
  onRemove,
  readOnly = false
}) => {
  const [showTransformEditor, setShowTransformEditor] = useState(false);

  const handleTransformSave = useCallback((transform: string) => {
    onUpdate({ transform: transform || undefined });
  }, [onUpdate]);

  const hasTransform = Boolean(mapping.transform);

  return (
    <>
      <div className="mapping-row grid grid-cols-12 gap-3 items-center p-3 border rounded-lg">
        {/* Component Property */}
        <div className="col-span-4">
          <div className="flex items-center gap-2">
            <div className="flex-1 min-w-0">
              <div className="font-medium text-sm">{propDef.name}</div>
              <div className="text-xs text-muted-foreground">
                <Badge variant="outline" className="text-xs mr-1">
                  {propDef.type}
                </Badge>
                {propDef.required && (
                  <Badge variant="destructive" className="text-xs">
                    Required
                  </Badge>
                )}
              </div>
              {propDef.description && (
                <div className="text-xs text-muted-foreground mt-1">
                  {propDef.description}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Arrow */}
        <div className="col-span-1 flex justify-center">
          <ArrowRight className="h-xs w-xs text-muted-foreground" />
        </div>

        {/* Query Field */}
        <div className="col-span-5">
          <Select
            value={mapping.queryPath}
            onValueChange={(value) => onUpdate({ queryPath: value })}
            disabled={readOnly}
          >
            <SelectTrigger>
              <SelectValue placeholder="Select query field..." />
            </SelectTrigger>
            <SelectContent>
              {queryFields.map((field) => (
                <SelectItem key={field.path} value={field.path}>
                  <div className="flex items-center gap-2">
                    <span className="font-mono text-sm">{field.path}</span>
                    <Badge variant="outline" className="text-xs">
                      {field.type}
                    </Badge>
                    {field.isArray && (
                      <Badge variant="secondary" className="text-xs">
                        Array
                      </Badge>
                    )}
                  </div>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Actions */}
        <div className="col-span-2 flex items-center gap-1">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setShowTransformEditor(true)}
            disabled={readOnly}
            className={cn(hasTransform && 'text-blue-600')}
          >
            <Code className="h-2xs w-2xs" />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={onRemove}
            disabled={readOnly}
          >
            <X className="h-2xs w-2xs" />
          </Button>
        </div>

        {/* Transform Indicator */}
        {hasTransform && (
          <div className="col-span-12 mt-2 pt-2 border-t">
            <div className="flex items-center gap-2 text-xs text-blue-600">
              <Wand2 className="h-2xs w-2xs" />
              <span>Transform applied</span>
              <code className="bg-muted px-1 py-0.5 rounded text-xs max-w-[200px] truncate">
                {mapping.transform}
              </code>
            </div>
          </div>
        )}
      </div>

      {/* Transform Editor */}
      <TransformEditor
        isOpen={showTransformEditor}
        onOpenChange={setShowTransformEditor}
        mapping={mapping}
        onSave={handleTransformSave}
      />
    </>
  );
};

// ============================================================================
// Main Component
// ============================================================================

export const PropertyMapper: React.FC<PropertyMapperProps> = ({
  query,
  componentType,
  mappings,
  onChange,
  readOnly = false,
  className
}) => {
  const [showUnmappedOnly, setShowUnmappedOnly] = useState(false);

  // Get component properties
  const componentProps = useMemo(() => {
    return COMPONENT_PROPS[componentType] || [];
  }, [componentType]);

  // Extract fields from GraphQL query
  const queryFields = useMemo(() => {
    return extractFieldsFromQuery(query);
  }, [query]);

  // Get mapped and unmapped properties
  const { mappedProps, unmappedProps } = useMemo(() => {
    const mapped = new Set(mappings.map(m => m.componentProp));
    return {
      mappedProps: componentProps.filter(prop => mapped.has(prop.name)),
      unmappedProps: componentProps.filter(prop => !mapped.has(prop.name))
    };
  }, [componentProps, mappings]);

  // Properties to display
  const displayProps = useMemo(() => {
    return showUnmappedOnly ? unmappedProps : componentProps;
  }, [showUnmappedOnly, unmappedProps, componentProps]);

  // Add new mapping
  const handleAddMapping = useCallback((propName: string) => {
    const newMapping: PropertyMapping = {
      componentProp: propName,
      queryPath: ''
    };
    onChange?.([...mappings, newMapping]);
  }, [mappings, onChange]);

  // Update mapping
  const handleUpdateMapping = useCallback((index: number, updates: Partial<PropertyMapping>) => {
    const updatedMappings = mappings.map((mapping, i) => 
      i === index ? { ...mapping, ...updates } : mapping
    );
    onChange?.(updatedMappings);
  }, [mappings, onChange]);

  // Remove mapping
  const handleRemoveMapping = useCallback((index: number) => {
    const updatedMappings = mappings.filter((_, i) => i !== index);
    onChange?.(updatedMappings);
  }, [mappings, onChange]);

  // Auto-map fields with matching names
  const handleAutoMap = useCallback(() => {
    const newMappings: PropertyMapping[] = [...mappings];
    
    unmappedProps.forEach(prop => {
      // Find matching field by name
      const matchingField = queryFields.find(field =>
        field.path.toLowerCase().includes(prop.name.toLowerCase()) ||
        prop.name.toLowerCase().includes(field.path.split('.').pop()?.toLowerCase() || '')
      );
      
      if (matchingField) {
        newMappings.push({
          componentProp: prop.name,
          queryPath: matchingField.path
        });
      }
    });
    
    onChange?.(newMappings);
  }, [mappings, unmappedProps, queryFields, onChange]);

  if (queryFields.length === 0) {
    return (
      <div className={cn('property-mapper', className)}>
        <div className="text-center py-6 text-muted-foreground">
          <AlertCircle className="h-lg w-lg mx-auto mb-2 opacity-50" />
          <p className="text-sm">No query fields detected</p>
          <p className="text-xs">Write a valid GraphQL query to map properties</p>
        </div>
      </div>
    );
  }

  return (
    <div className={cn('property-mapper space-y-4', className)}>
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <div className="flex items-center gap-2">
            <h4 className="text-sm font-medium">Property Mappings</h4>
            <Badge variant="secondary" className="text-xs">
              {mappings.length} of {componentProps.length} mapped
            </Badge>
          </div>
          <p className="text-xs text-muted-foreground">
            Map query fields to component properties
          </p>
        </div>
        
        <div className="flex items-center gap-2">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setShowUnmappedOnly(!showUnmappedOnly)}
          >
            {showUnmappedOnly ? (
              <><Eye className="h-2xs w-2xs mr-1" />Show All</>
            ) : (
              <><EyeOff className="h-2xs w-2xs mr-1" />Unmapped Only</>
            )}
          </Button>
          
          {unmappedProps.length > 0 && (
            <Button
              variant="outline"
              size="sm"
              onClick={handleAutoMap}
              disabled={readOnly}
            >
              <Wand2 className="h-2xs w-2xs mr-1" />
              Auto-Map
            </Button>
          )}
        </div>
      </div>

      {/* Mappings List */}
      <div className="space-y-3">
        {displayProps.map((prop) => {
          const mappingIndex = mappings.findIndex(m => m.componentProp === prop.name);
          const mapping = mappingIndex >= 0 ? mappings[mappingIndex] : null;
          
          if (!mapping) {
            // Unmapped property
            return (
              <Card key={prop.name} className="border-dashed">
                <CardContent className="p-3">
                  <div className="flex items-center justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <span className="font-medium text-sm">{prop.name}</span>
                        <Badge variant="outline" className="text-xs">
                          {prop.type}
                        </Badge>
                        {prop.required && (
                          <Badge variant="destructive" className="text-xs">
                            Required
                          </Badge>
                        )}
                      </div>
                      {prop.description && (
                        <p className="text-xs text-muted-foreground mt-1">
                          {prop.description}
                        </p>
                      )}
                    </div>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleAddMapping(prop.name)}
                      disabled={readOnly}
                    >
                      <Plus className="h-2xs w-2xs mr-1" />
                      Map
                    </Button>
                  </div>
                </CardContent>
              </Card>
            );
          }
          
          // Mapped property
          return (
            <MappingRow
              key={prop.name}
              mapping={mapping}
              propDef={prop}
              queryFields={queryFields}
              onUpdate={(updates) => handleUpdateMapping(mappingIndex, updates)}
              onRemove={() => handleRemoveMapping(mappingIndex)}
              readOnly={readOnly}
            />
          );
        })}
      </div>

      {/* Summary */}
      <div className="border-t pt-4">
        <div className="flex items-center justify-between text-sm">
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              <CheckCircle className="h-xs w-xs text-green-600" />
              <span>{mappedProps.length} mapped</span>
            </div>
            <div className="flex items-center gap-2">
              <AlertCircle className="h-xs w-xs text-orange-500" />
              <span>{unmappedProps.length} unmapped</span>
            </div>
          </div>
          
          <div className="text-muted-foreground">
            {queryFields.length} query fields available
          </div>
        </div>
      </div>
    </div>
  );
};

PropertyMapper.displayName = 'PropertyMapper';