/**
 * BindingsPanel - GraphQL query editor and property mapping
 * 
 * Provides an interface for configuring component data bindings,
 * including GraphQL queries, property mappings, and data transformations.
 */

import React, { useState, useCallback, useMemo } from 'react';
import {
  Database,
  Play,
  Code,
  RefreshCw,
  Settings,
  AlertCircle,
  CheckCircle,
  Link,
  Zap,
  Plus,
  X
} from 'lucide-react';

import type { GridComponent } from '@/types/bento';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
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
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { PropertyMapper } from './PropertyMapper';

// ============================================================================
// Types
// ============================================================================

export interface BindingsPanelProps {
  /** Component being edited */
  component: GridComponent;
  /** Called when component is updated */
  onUpdate?: (updates: Partial<GridComponent>) => void;
  /** Called to show write configuration modal */
  onShowWriteConfig?: () => void;
  /** Called to test bindings */
  onTestBindings?: () => Promise<void>;
  /** Whether bindings are currently being tested */
  isTestingBindings?: boolean;
  /** Whether the panel is read-only */
  readOnly?: boolean;
  /** Additional CSS classes */
  className?: string;
}

interface QueryValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
  estimatedFields: string[];
}

interface PropertyMapping {
  componentProp: string;
  queryPath: string;
  transform?: string;
}

// ============================================================================
// Sample GraphQL Queries
// ============================================================================

const SAMPLE_QUERIES = {
  'metric-card': `query GetMetricData($period: String!) {
  metrics(period: $period) {
    revenue {
      current
      previous
      trend
      percentage
    }
  }
}`,
  'line-chart': `query GetChartData($startDate: String!, $endDate: String!) {
  analytics(startDate: $startDate, endDate: $endDate) {
    timeSeries {
      date
      value
      category
    }
  }
}`,
  'user-list': `query GetUsers($limit: Int, $offset: Int) {
  users(limit: $limit, offset: $offset) {
    id
    name
    email
    avatar
    status
    lastActive
  }
}`,
  'data-table': `query GetTableData($filter: TableFilter, $sort: SortInput) {
  tableData(filter: $filter, sort: $sort) {
    rows {
      id
      ...tableFields
    }
    pagination {
      total
      hasNext
      hasPrevious
    }
  }
}`
};

// ============================================================================
// GraphQL Query Editor Component
// ============================================================================

interface GraphQLEditorProps {
  value: string;
  onChange: (value: string) => void;
  onValidate?: (result: QueryValidationResult) => void;
  placeholder?: string;
  readOnly?: boolean;
  className?: string;
}

const GraphQLEditor: React.FC<GraphQLEditorProps> = ({
  value,
  onChange,
  onValidate,
  placeholder = "Enter your GraphQL query...",
  readOnly = false,
  className
}) => {
  const [validationResult, setValidationResult] = useState<QueryValidationResult | null>(null);

  // Simple GraphQL validation
  const validateQuery = useCallback((query: string): QueryValidationResult => {
    const errors: string[] = [];
    const warnings: string[] = [];
    const estimatedFields: string[] = [];

    if (!query.trim()) {
      return { isValid: false, errors: ['Query is required'], warnings: [], estimatedFields: [] };
    }

    // Basic syntax validation
    // Check for basic GraphQL structure
    if (!query.includes('query') && !query.includes('mutation') && !query.includes('subscription')) {
      errors.push('Query must start with query, mutation, or subscription');
    }

    // Check for balanced braces
    const openBraces = (query.match(/\{/g) || []).length;
    const closeBraces = (query.match(/\}/g) || []).length;
    if (openBraces !== closeBraces) {
      errors.push('Unbalanced braces in query');
    }

    // Extract field names for property mapping
    const fieldMatches = query.match(/(\w+)\s*\{/g);
    if (fieldMatches) {
      fieldMatches.forEach(match => {
        const field = match.replace(/\s*\{/, '');
        if (field !== 'query' && field !== 'mutation' && field !== 'subscription') {
          estimatedFields.push(field);
        }
      });
    }

    // Check for variables
    if (query.includes('$') && !query.includes('(')) {
      warnings.push('Variables detected but no parameter list found');
    }

    return {
      isValid: errors.length === 0,
      errors,
      warnings,
      estimatedFields
    };
  }, []);

  const handleQueryChange = useCallback((newValue: string) => {
    onChange(newValue);
    
    // Validate query
    const result = validateQuery(newValue);
    setValidationResult(result);
    onValidate?.(result);
  }, [onChange, validateQuery, onValidate]);

  return (
    <div className={cn('graphql-editor', className)}>
      <Textarea
        value={value}
        onChange={(e) => handleQueryChange(e.target.value)}
        placeholder={placeholder}
        disabled={readOnly}
        className="font-mono text-sm min-h-[200px]"
        rows={10}
      />
      
      {/* Validation Results */}
      {validationResult && (
        <div className="mt-2 space-y-1">
          {validationResult.errors.map((error, index) => (
            <div key={index} className="flex items-center gap-2 text-sm text-destructive">
              <AlertCircle className="h-xs w-xs" />
              {error}
            </div>
          ))}
          
          {validationResult.warnings.map((warning, index) => (
            <div key={index} className="flex items-center gap-2 text-sm text-yellow-md00">
              <AlertCircle className="h-xs w-xs" />
              {warning}
            </div>
          ))}
          
          {validationResult.isValid && (
            <div className="flex items-center gap-2 text-sm text-green-600">
              <CheckCircle className="h-xs w-xs" />
              Query syntax is valid
            </div>
          )}
        </div>
      )}
    </div>
  );
};

// ============================================================================
// Main Component
// ============================================================================

export const BindingsPanel: React.FC<BindingsPanelProps> = ({
  component,
  onUpdate,
  onShowWriteConfig,
  onTestBindings,
  isTestingBindings = false,
  readOnly = false,
  className
}) => {
  const [query, setQuery] = useState(component.bindings?.readQuery || '');
  const [mappings, setMappings] = useState<PropertyMapping[]>(
    component.bindings?.propertyMappings || []
  );
  const [showConfirmReset, setShowConfirmReset] = useState(false);
  const [validationResult, setValidationResult] = useState<QueryValidationResult | null>(null);

  // Check if component has write configuration
  const hasWriteConfig = useMemo(() => {
    return component.bindings?.write !== undefined;
  }, [component.bindings?.write]);

  // Handle query changes
  const handleQueryChange = useCallback((newQuery: string) => {
    setQuery(newQuery);
  }, []);

  // Handle query validation
  const handleQueryValidation = useCallback((result: QueryValidationResult) => {
    setValidationResult(result);
  }, []);

  // Apply bindings to component
  const handleApplyBindings = useCallback(() => {
    const updatedBindings = {
      ...component.bindings,
      readQuery: query,
      propertyMappings: mappings
    };
    
    onUpdate?.({
      bindings: updatedBindings
    });
  }, [component.bindings, query, mappings, onUpdate]);

  // Load sample query
  const handleLoadSample = useCallback(() => {
    const sampleQuery = SAMPLE_QUERIES[component.componentType as keyof typeof SAMPLE_QUERIES];
    if (sampleQuery) {
      setQuery(sampleQuery);
    }
  }, [component.componentType]);

  // Reset bindings
  const handleResetBindings = useCallback(() => {
    setQuery('');
    setMappings([]);
    onUpdate?.({
      bindings: {
        ...component.bindings,
        readQuery: undefined,
        propertyMappings: []
      }
    });
    setShowConfirmReset(false);
  }, [component.bindings, onUpdate]);

  // Handle property mappings update
  const handleMappingsUpdate = useCallback((newMappings: PropertyMapping[]) => {
    setMappings(newMappings);
  }, []);

  // Test query
  const handleTestQuery = useCallback(async () => {
    if (onTestBindings) {
      await onTestBindings();
    }
  }, [onTestBindings]);

  return (
    <div className={cn('bindings-panel space-y-6', className)}>
      {/* Header */}
      <div className="space-y-2">
        <h3 className="text-lg font-semibold flex items-center gap-2">
          <Database className="h-sm w-sm" />
          Data Bindings
        </h3>
        <p className="text-sm text-muted-foreground">
          Configure how this component connects to and displays data
        </p>
      </div>

      {/* Read Query Section */}
      <Card>
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between">
            <CardTitle className="text-base flex items-center gap-2">
              <Code className="h-xs w-xs" />
              GraphQL Read Query
            </CardTitle>
            <div className="flex items-center gap-2">
              {SAMPLE_QUERIES[component.componentType as keyof typeof SAMPLE_QUERIES] && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleLoadSample}
                  disabled={readOnly}
                >
                  Load Sample
                </Button>
              )}
              <Button
                variant="outline"
                size="sm"
                onClick={handleTestQuery}
                disabled={!query.trim() || isTestingBindings || readOnly}
              >
                {isTestingBindings ? (
                  <RefreshCw className="h-xs w-xs animate-spin mr-1" />
                ) : (
                  <Play className="h-xs w-xs mr-1" />
                )}
                Test Query
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <GraphQLEditor
            value={query}
            onChange={handleQueryChange}
            onValidate={handleQueryValidation}
            readOnly={readOnly}
          />
          
          {validationResult && validationResult.estimatedFields && validationResult.estimatedFields.length > 0 && (
            <div className="mt-3">
              <Label className="text-sm font-medium">Detected Fields:</Label>
              <div className="flex flex-wrap gap-1 mt-1">
                {validationResult.estimatedFields.map((field, index) => (
                  <Badge key={index} variant="outline" className="text-xs">
                    {field}
                  </Badge>
                ))}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Property Mappings */}
      {query.trim() && validationResult?.isValid && (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-base flex items-center gap-2">
              <Link className="h-xs w-xs" />
              Property Mappings
            </CardTitle>
            <p className="text-sm text-muted-foreground">
              Map GraphQL query results to component properties
            </p>
          </CardHeader>
          <CardContent>
            <PropertyMapper
              query={query}
              componentType={component.componentType}
              mappings={mappings}
              onChange={handleMappingsUpdate}
              readOnly={readOnly}
            />
          </CardContent>
        </Card>
      )}

      {/* Write Actions */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <Zap className="h-xs w-xs" />
            Write Actions
          </CardTitle>
          <p className="text-sm text-muted-foreground">
            Configure how user interactions update data
          </p>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {hasWriteConfig ? (
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <CheckCircle className="h-xs w-xs text-green-600" />
                    <span className="text-sm font-medium">Write configuration active</span>
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={onShowWriteConfig}
                    disabled={readOnly}
                  >
                    <Settings className="h-xs w-xs mr-1" />
                    Configure
                  </Button>
                </div>
                
                {/* Write Configuration Summary */}
                <div className="p-3 bg-muted/50 rounded-md">
                  <div className="text-sm space-y-1">
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Target Component:</span>
                      <span className="font-mono text-xs">
                        {component.bindings?.write?.ecsComponent || 'Not configured'}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Field Mappings:</span>
                      <span>
                        {component.bindings?.write?.fieldMappings?.length || 0} configured
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Write Triggers:</span>
                      <span>
                        {component.bindings?.write?.triggers?.length || 0} configured
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            ) : (
              <div className="text-center py-6">
                <div className="space-y-3">
                  <div className="flex justify-center">
                    <div className="w-2xl h-2xl rounded-full bg-muted flex items-center justify-center">
                      <Zap className="h-sm w-sm text-muted-foreground" />
                    </div>
                  </div>
                  <div className="space-y-1">
                    <p className="text-sm font-medium">No write actions configured</p>
                    <p className="text-xs text-muted-foreground">
                      Set up write operations to save user interactions
                    </p>
                  </div>
                  <Button
                    variant="outline"
                    onClick={onShowWriteConfig}
                    disabled={readOnly}
                  >
                    <Plus className="h-xs w-xs mr-1" />
                    Configure Write Actions
                  </Button>
                </div>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Refresh Configuration */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <RefreshCw className="h-xs w-xs" />
            Auto-Refresh
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label className="text-sm font-medium">Enable Auto-Refresh</Label>
                <p className="text-xs text-muted-foreground">
                  Automatically refresh data at regular intervals
                </p>
              </div>
              <Switch
                checked={component.bindings?.refreshInterval !== undefined}
                onCheckedChange={(checked) => {
                  const updatedBindings = {
                    ...component.bindings,
                    refreshInterval: checked ? 60000 : undefined
                  };
                  onUpdate?.({ bindings: updatedBindings });
                }}
                disabled={readOnly}
              />
            </div>
            
            {component.bindings?.refreshInterval && (
              <div className="space-y-2">
                <Label htmlFor="refreshInterval" className="text-sm">
                  Refresh Interval (seconds)
                </Label>
                <Select
                  value={String(component.bindings.refreshInterval / 1000)}
                  onValueChange={(value) => {
                    const updatedBindings = {
                      ...component.bindings,
                      refreshInterval: parseInt(value) * 1000
                    };
                    onUpdate?.({ bindings: updatedBindings });
                  }}
                  disabled={readOnly}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="30">30 seconds</SelectItem>
                    <SelectItem value="60">1 minute</SelectItem>
                    <SelectItem value="300">5 minutes</SelectItem>
                    <SelectItem value="600">10 minutes</SelectItem>
                    <SelectItem value="1800">30 minutes</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Actions */}
      <div className="flex items-center justify-between pt-4 border-t">
        <Button
          variant="outline"
          onClick={() => setShowConfirmReset(true)}
          disabled={readOnly || (!query.trim() && mappings.length === 0)}
        >
          <X className="h-xs w-xs mr-1" />
          Reset All
        </Button>
        
        <Button
          onClick={handleApplyBindings}
          disabled={readOnly || !validationResult?.isValid}
        >
          <CheckCircle className="h-xs w-xs mr-1" />
          Apply Bindings
        </Button>
      </div>

      {/* Reset Confirmation Dialog */}
      <AlertDialog open={showConfirmReset} onOpenChange={setShowConfirmReset}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Reset All Bindings?</AlertDialogTitle>
            <AlertDialogDescription>
              This will remove all data bindings, property mappings, and write configurations.
              This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleResetBindings}>
              Reset All Bindings
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
};

BindingsPanel.displayName = 'BindingsPanel';