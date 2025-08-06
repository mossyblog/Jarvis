# Component Editor Interface

## Overview

Each component in edit mode features a sophisticated editing interface with vertical tabs on the right side, providing access to binding properties and preview functionality.

## Visual Design

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Component Edit Mode                                │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌────────────────────────────┐ ┌──┐ ┌─────────────────────────────┐│
│  │                            │ │ B│ │   Binding Properties         ││
│  │                            │ │ i│ │                             ││
│  │      Component Content     │ │ n│ │  Read Query (GraphQL):      ││
│  │                            │ │ d│ │  ┌─────────────────────────┐││
│  │   ┌──────────────────┐     │ │ i│ │  │query GetMetricData {    │││
│  │   │ Revenue          │     │ │ n│ │  │  metrics {              │││
│  │   │ $125,450         │     │ │ g│ │  │    revenue {            │││
│  │   │ ▲ +12.5%         │     │ │  │ │  │      current            │││
│  │   └──────────────────┘     │ │ s│ │  │      previous           │││
│  │                            │ │  │ │  │      trend              │││
│  │                            │ ├──┤ │  │    }                    │││
│  │                            │ │ P│ │  │  }                      │││
│  │                            │ │ r│ │  │}                        │││
│  │                            │ │ e│ │  └─────────────────────────┘││
│  │                            │ │ v│ │                             ││
│  │                            │ │ i│ │  [Map to Properties]        ││
│  │                            │ │ e│ │                             ││
│  │                            │ │ w│ │  Write Actions:             ││
│  │                            │ │  │ │  [Configure Write ▼]        ││
│  └────────────────────────────┘ └──┘ └─────────────────────────────┘│
│                                                                       │
│  ⟳⟲ (rotate) ══ (resize) ◢◣ (drag)                                   │
└─────────────────────────────────────────────────────────────────────┘
```

## Component Editor Architecture

### Editor Wrapper Component

```typescript
interface ComponentEditorProps {
  component: GridComponent;
  isSelected: boolean;
  onUpdate: (updates: Partial<GridComponent>) => void;
  onDelete: () => void;
}

export const ComponentEditor: React.FC<ComponentEditorProps> = ({
  component,
  isSelected,
  onUpdate,
  onDelete
}) => {
  const [activeTab, setActiveTab] = useState<'bindings' | 'preview'>('bindings');
  const [showWriteConfig, setShowWriteConfig] = useState(false);
  
  return (
    <div className={cn(
      "component-editor",
      isSelected && "component-editor--selected"
    )}>
      {/* Component Content */}
      <div className="component-editor__content">
        <ComponentRenderer component={component} />
      </div>
      
      {/* Vertical Tabs */}
      {isSelected && (
        <div className="component-editor__tabs">
          <VerticalTabs
            tabs={[
              { id: 'bindings', label: 'Bindings', icon: Link },
              { id: 'preview', label: 'Preview', icon: Eye }
            ]}
            activeTab={activeTab}
            onTabChange={setActiveTab}
          />
          
          <div className="component-editor__panel">
            {activeTab === 'bindings' && (
              <BindingsPanel
                component={component}
                onUpdate={onUpdate}
                onShowWriteConfig={() => setShowWriteConfig(true)}
              />
            )}
            
            {activeTab === 'preview' && (
              <PreviewPanel component={component} />
            )}
          </div>
        </div>
      )}
      
      {/* Write Configuration Modal */}
      {showWriteConfig && (
        <WriteConfigModal
          component={component}
          onClose={() => setShowWriteConfig(false)}
          onSave={(writeConfig) => {
            onUpdate({ 
              bindings: { 
                ...component.bindings, 
                write: writeConfig 
              } 
            });
            setShowWriteConfig(false);
          }}
        />
      )}
    </div>
  );
};
```

## Binding Properties Panel

### GraphQL Query Editor

```typescript
interface BindingsPanelProps {
  component: GridComponent;
  onUpdate: (updates: Partial<GridComponent>) => void;
  onShowWriteConfig: () => void;
}

const BindingsPanel: React.FC<BindingsPanelProps> = ({
  component,
  onUpdate,
  onShowWriteConfig
}) => {
  const [query, setQuery] = useState(
    component.bindings?.readQuery || ''
  );
  const [mappings, setMappings] = useState<PropertyMapping[]>(
    component.bindings?.propertyMappings || []
  );
  
  return (
    <div className="bindings-panel">
      <h3>Data Bindings</h3>
      
      {/* Read Query Section */}
      <div className="bindings-panel__section">
        <h4>Read Query (GraphQL)</h4>
        <GraphQLEditor
          value={query}
          onChange={setQuery}
          schema={graphqlSchema}
          height="200px"
        />
        
        <Button
          variant="secondary"
          size="sm"
          onClick={() => validateAndTestQuery(query)}
        >
          Test Query
        </Button>
      </div>
      
      {/* Property Mappings */}
      <div className="bindings-panel__section">
        <h4>Property Mappings</h4>
        <PropertyMapper
          query={query}
          componentProps={getComponentPropTypes(component.componentType)}
          mappings={mappings}
          onChange={setMappings}
        />
      </div>
      
      {/* Write Actions */}
      <div className="bindings-panel__section">
        <h4>Write Actions</h4>
        <Button
          variant="outline"
          onClick={onShowWriteConfig}
          className="w-full"
        >
          Configure Write Actions
          <Settings className="ml-2 h-4 w-4" />
        </Button>
        
        {component.bindings?.write && (
          <WriteActionsSummary
            writeConfig={component.bindings.write}
          />
        )}
      </div>
      
      {/* Apply Changes */}
      <div className="bindings-panel__actions">
        <Button
          variant="primary"
          onClick={() => {
            onUpdate({
              bindings: {
                ...component.bindings,
                readQuery: query,
                propertyMappings: mappings
              }
            });
          }}
        >
          Apply Bindings
        </Button>
      </div>
    </div>
  );
};
```

### Property Mapper Component

```typescript
interface PropertyMapperProps {
  query: string;
  componentProps: PropTypeDefinition[];
  mappings: PropertyMapping[];
  onChange: (mappings: PropertyMapping[]) => void;
}

interface PropertyMapping {
  componentProp: string;
  queryPath: string;
  transform?: string; // Optional JS expression
}

const PropertyMapper: React.FC<PropertyMapperProps> = ({
  query,
  componentProps,
  mappings,
  onChange
}) => {
  const queryFields = useMemo(() => 
    extractFieldsFromQuery(query), [query]
  );
  
  return (
    <div className="property-mapper">
      {componentProps.map(prop => {
        const mapping = mappings.find(m => 
          m.componentProp === prop.name
        );
        
        return (
          <div key={prop.name} className="property-mapper__row">
            <div className="property-mapper__prop">
              <Label>{prop.name}</Label>
              <Badge variant="outline">{prop.type}</Badge>
            </div>
            
            <ArrowRight className="h-4 w-4" />
            
            <div className="property-mapper__source">
              <Select
                value={mapping?.queryPath || ''}
                onValueChange={(path) => {
                  updateMapping(prop.name, path);
                }}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select field" />
                </SelectTrigger>
                <SelectContent>
                  {queryFields.map(field => (
                    <SelectItem key={field.path} value={field.path}>
                      {field.path} ({field.type})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            
            {mapping && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => openTransformEditor(prop.name)}
              >
                <Code className="h-4 w-4" />
              </Button>
            )}
          </div>
        );
      })}
    </div>
  );
};
```

## Write Configuration Modal

### ECS Component Selection

```typescript
interface WriteConfigModalProps {
  component: GridComponent;
  onClose: () => void;
  onSave: (config: WriteConfiguration) => void;
}

const WriteConfigModal: React.FC<WriteConfigModalProps> = ({
  component,
  onClose,
  onSave
}) => {
  const [selectedECS, setSelectedECS] = useState<string>('');
  const [fieldMappings, setFieldMappings] = useState<FieldMapping[]>([]);
  const [triggers, setTriggers] = useState<WriteTrigger[]>([]);
  
  return (
    <Dialog open onOpenChange={onClose}>
      <DialogContent className="max-w-3xl">
        <DialogHeader>
          <DialogTitle>Configure Write Actions</DialogTitle>
          <DialogDescription>
            Map component interactions to ECS component updates
          </DialogDescription>
        </DialogHeader>
        
        <div className="space-y-6">
          {/* ECS Component Selection */}
          <div>
            <Label>Target ECS Component</Label>
            <Select
              value={selectedECS}
              onValueChange={setSelectedECS}
            >
              <SelectTrigger>
                <SelectValue placeholder="Select ECS component" />
              </SelectTrigger>
              <SelectContent>
                {ecsComponents.map(ecs => (
                  <SelectItem key={ecs.name} value={ecs.name}>
                    <div>
                      <div>{ecs.displayName}</div>
                      <div className="text-sm text-muted-foreground">
                        {ecs.description}
                      </div>
                    </div>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          
          {/* Field Mappings */}
          {selectedECS && (
            <div>
              <Label>Field Mappings</Label>
              <FieldMappingEditor
                ecsComponent={selectedECS}
                componentType={component.componentType}
                mappings={fieldMappings}
                onChange={setFieldMappings}
              />
            </div>
          )}
          
          {/* Write Triggers */}
          <div>
            <Label>Write Triggers</Label>
            <WriteTriggerEditor
              triggers={triggers}
              onChange={setTriggers}
              componentEvents={getComponentEvents(component.componentType)}
            />
          </div>
          
          {/* Validation Rules */}
          <div>
            <Label>Validation</Label>
            <ValidationRulesEditor
              ecsComponent={selectedECS}
              fieldMappings={fieldMappings}
            />
          </div>
        </div>
        
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button
            onClick={() => {
              onSave({
                ecsComponent: selectedECS,
                fieldMappings,
                triggers,
                validation: getValidationRules(selectedECS, fieldMappings)
              });
            }}
          >
            Save Configuration
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};
```

### Field Mapping Editor

```typescript
interface FieldMappingEditorProps {
  ecsComponent: string;
  componentType: string;
  mappings: FieldMapping[];
  onChange: (mappings: FieldMapping[]) => void;
}

interface FieldMapping {
  ecsField: string;
  source: 'prop' | 'state' | 'computed';
  sourcePath: string;
  uiControl: UIControlType;
  validation?: FieldValidation;
}

const FieldMappingEditor: React.FC<FieldMappingEditorProps> = ({
  ecsComponent,
  componentType,
  mappings,
  onChange
}) => {
  const ecsFields = getECSFields(ecsComponent);
  const componentData = getComponentDataSources(componentType);
  
  return (
    <div className="field-mapping-editor">
      {ecsFields.map(field => {
        const mapping = mappings.find(m => m.ecsField === field.name);
        
        return (
          <div key={field.name} className="field-mapping">
            <div className="field-info">
              <Label>{field.name}</Label>
              <Badge>{field.type}</Badge>
              {field.required && <Badge variant="destructive">Required</Badge>}
            </div>
            
            <div className="mapping-controls">
              {/* Source Selection */}
              <Select
                value={mapping?.source || ''}
                onValueChange={(source) => 
                  updateFieldMapping(field.name, 'source', source)
                }
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select source" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="prop">Component Prop</SelectItem>
                  <SelectItem value="state">Component State</SelectItem>
                  <SelectItem value="computed">Computed Value</SelectItem>
                </SelectContent>
              </Select>
              
              {/* Source Path */}
              {mapping?.source && (
                <Select
                  value={mapping.sourcePath || ''}
                  onValueChange={(path) => 
                    updateFieldMapping(field.name, 'sourcePath', path)
                  }
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Select field" />
                  </SelectTrigger>
                  <SelectContent>
                    {getSourceOptions(mapping.source, componentData).map(opt => (
                      <SelectItem key={opt.path} value={opt.path}>
                        {opt.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
              
              {/* UI Control */}
              <Select
                value={mapping?.uiControl || getDefaultControl(field.type)}
                onValueChange={(control) => 
                  updateFieldMapping(field.name, 'uiControl', control)
                }
              >
                <SelectTrigger>
                  <SelectValue placeholder="UI Control" />
                </SelectTrigger>
                <SelectContent>
                  {getUIControlsForType(field.type).map(control => (
                    <SelectItem key={control.type} value={control.type}>
                      <div className="flex items-center gap-2">
                        {control.icon}
                        {control.label}
                      </div>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        );
      })}
    </div>
  );
};
```

## Preview Panel

```typescript
interface PreviewPanelProps {
  component: GridComponent;
}

const PreviewPanel: React.FC<PreviewPanelProps> = ({ component }) => {
  const [previewMode, setPreviewMode] = useState<'desktop' | 'tablet' | 'mobile'>('desktop');
  const [sampleData, setSampleData] = useState(null);
  
  // Fetch sample data based on bindings
  useEffect(() => {
    if (component.bindings?.readQuery) {
      fetchSampleData(component.bindings.readQuery)
        .then(setSampleData);
    }
  }, [component.bindings?.readQuery]);
  
  return (
    <div className="preview-panel">
      <div className="preview-panel__controls">
        <SegmentedControl
          value={previewMode}
          onValueChange={setPreviewMode}
          options={[
            { value: 'desktop', icon: Monitor },
            { value: 'tablet', icon: Tablet },
            { value: 'mobile', icon: Smartphone }
          ]}
        />
        
        <Button
          variant="ghost"
          size="sm"
          onClick={() => refreshSampleData()}
        >
          <RefreshCw className="h-4 w-4" />
        </Button>
      </div>
      
      <div className="preview-panel__viewport">
        <div className={cn(
          "preview-frame",
          `preview-frame--${previewMode}`
        )}>
          <ComponentRenderer
            component={{
              ...component,
              props: {
                ...component.props,
                ...mapDataToProps(sampleData, component.bindings?.propertyMappings)
              }
            }}
            preview={true}
          />
        </div>
      </div>
      
      <div className="preview-panel__data">
        <Collapsible>
          <CollapsibleTrigger className="text-sm font-medium">
            Sample Data
            <ChevronDown className="h-4 w-4 ml-1" />
          </CollapsibleTrigger>
          <CollapsibleContent>
            <pre className="text-xs">
              {JSON.stringify(sampleData, null, 2)}
            </pre>
          </CollapsibleContent>
        </Collapsible>
      </div>
    </div>
  );
};
```

## Data Models

### Binding Configuration

```typescript
interface ComponentBindings {
  // Read configuration
  readQuery?: string; // GraphQL query
  propertyMappings?: PropertyMapping[];
  
  // Write configuration
  write?: WriteConfiguration;
  
  // Real-time subscriptions
  subscriptions?: SubscriptionConfig[];
  
  // Data refresh
  refreshInterval?: number;
  refreshTriggers?: RefreshTrigger[];
}

interface WriteConfiguration {
  ecsComponent: string;
  fieldMappings: FieldMapping[];
  triggers: WriteTrigger[];
  validation?: ValidationConfig;
  optimisticUpdate?: boolean;
}

interface WriteTrigger {
  event: string; // Component event
  condition?: string; // Optional condition
  debounce?: number; // Milliseconds
  confirmation?: ConfirmationConfig;
}

interface FieldMapping {
  ecsField: string;
  source: 'prop' | 'state' | 'computed';
  sourcePath: string;
  uiControl: UIControlType;
  transform?: string; // JS expression
  validation?: FieldValidation;
}

type UIControlType = 
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
```

### UI Control Configuration

```typescript
interface UIControlConfig {
  type: UIControlType;
  label: string;
  icon: React.ComponentType;
  defaultProps?: Record<string, any>;
  validation?: ValidationRule[];
  transformer?: (value: any) => any;
}

const uiControls: Record<UIControlType, UIControlConfig> = {
  text: {
    type: 'text',
    label: 'Text Input',
    icon: Type,
    defaultProps: { placeholder: 'Enter text...' }
  },
  number: {
    type: 'number',
    label: 'Number Input',
    icon: Hash,
    defaultProps: { min: 0, step: 1 },
    transformer: (value) => Number(value)
  },
  select: {
    type: 'select',
    label: 'Dropdown',
    icon: ChevronDown,
    defaultProps: { placeholder: 'Select option...' }
  },
  date: {
    type: 'date',
    label: 'Date Picker',
    icon: Calendar,
    transformer: (value) => new Date(value).toISOString()
  },
  json: {
    type: 'json',
    label: 'JSON Editor',
    icon: Code,
    validation: [{ type: 'json', message: 'Invalid JSON' }],
    transformer: (value) => JSON.parse(value)
  }
  // ... more controls
};
```

## Integration Example

### Complete Component with Bindings

```typescript
// Component with full binding configuration
const MetricCardWithBindings: GridComponent = {
  id: 'metric-revenue',
  componentType: 'MetricCard',
  position: { x: 0, y: 0, w: 3, h: 2 },
  props: {
    title: 'Revenue',
    format: 'currency'
  },
  bindings: {
    // GraphQL read query
    readQuery: `
      query GetRevenue($period: String!) {
        metrics(period: $period) {
          revenue {
            current
            previous
            trend
            percentage
          }
        }
      }
    `,
    
    // Map query results to component props
    propertyMappings: [
      {
        componentProp: 'value',
        queryPath: 'metrics.revenue.current'
      },
      {
        componentProp: 'previousValue',
        queryPath: 'metrics.revenue.previous'
      },
      {
        componentProp: 'trend',
        queryPath: 'metrics.revenue.trend',
        transform: 'value === "up" ? "▲" : "▼"'
      }
    ],
    
    // Write configuration
    write: {
      ecsComponent: 'RevenueTargetComponent',
      fieldMappings: [
        {
          ecsField: 'targetValue',
          source: 'prop',
          sourcePath: 'value',
          uiControl: 'number',
          validation: {
            min: 0,
            max: 1000000,
            required: true
          }
        },
        {
          ecsField: 'period',
          source: 'computed',
          sourcePath: 'getCurrentPeriod()',
          uiControl: 'select'
        }
      ],
      triggers: [
        {
          event: 'onDoubleClick',
          confirmation: {
            title: 'Update Revenue Target',
            message: 'Set new revenue target?'
          }
        }
      ]
    },
    
    // Auto-refresh every minute
    refreshInterval: 60000
  }
};
```

## Best Practices

### 1. GraphQL Query Design
- Keep queries focused and specific
- Use fragments for reusable query parts
- Implement proper error handling
- Cache queries appropriately

### 2. Property Mapping
- Map only required properties
- Use transforms sparingly
- Validate data types match
- Handle null/undefined gracefully

### 3. Write Operations
- Always validate before writing
- Implement optimistic updates
- Show confirmation for destructive actions
- Log all write operations

### 4. Performance
- Debounce write triggers
- Batch multiple field updates
- Use efficient GraphQL queries
- Implement proper caching

## Next Steps

1. Review [Component API](./09-component-api.md) for component integration
2. Check [Data Models](./06-data-models.md) for type definitions
3. See [Storage API](./11-storage-api.md) for ECS integration
4. Explore [Security Model](./12-security-model.md) for access control