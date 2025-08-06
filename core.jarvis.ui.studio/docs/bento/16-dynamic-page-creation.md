# Dynamic Page Creation with UIStudio API Integration

## Overview

**This guide covers Dynamic Pages - one of three page architecture patterns in the Jarvis UI Studio Bento system.**

Dynamic pages are **user-created pages using the component selector and field picker** that enable the creation of data-driven dashboards without writing code. These pages automatically discover and bind to Jarvis ECS components through the UIStudio API, allowing non-technical users and developers to create sophisticated layouts through visual tools.

### UIStudio API Integration

Dynamic pages are fully integrated with the UIStudio API backend, providing:

- **Page Persistence**: All page configurations saved via `/api/uistudio/pages`
- **Component Discovery**: Automatic ECS component discovery through `/api/uistudio/components`
- **Real-time Updates**: Live synchronization of page changes
- **Permission Management**: Role-based access control via `/api/uistudio/permissions`
- **Template System**: Reusable page templates through `/api/uistudio/templates`

> 📚 **Context**: Before diving into dynamic page creation, review the [Page Types and Architecture guide](./09-page-types-and-architecture.md) to understand when dynamic pages are the right choice compared to fixed pages or hybrid pages.

### What Makes Dynamic Pages Special

- **Zero Code Required**: Built entirely through the UI using drag-and-drop
- **UIStudio API Integration**: Seamless integration with backend ECS components and data
- **ECS Component Discovery**: Automatic discovery of Jarvis ECS components and fields through API
- **Self-Service**: Business users can create and modify pages independently
- **Rapid Prototyping**: Minutes to create vs hours/days for fixed pages
- **Data-Driven**: Automatically adapts to available data sources via API
- **Real-time Persistence**: All changes automatically saved to UIStudio backend

## When to Use Dynamic Pages

### Choose Dynamic Pages When:
- ✅ Business users need self-service capabilities
- ✅ Requirements change frequently  
- ✅ Multiple similar pages with different data sources needed
- ✅ Rapid prototyping is required
- ✅ Non-technical stakeholders own the layout
- ✅ Content-driven applications

### Dynamic vs Other Page Types:

| Aspect | Dynamic Pages | Fixed Pages | Hybrid Pages |
|--------|---------------|-------------|-------------|
| **Development Time** | ⚡ Minutes | 🕒 Hours/Days | ⚖️ Moderate |
| **Flexibility** | 🎨 High | 🔒 Low | ⚖️ Balanced |
| **Performance** | 🐌 Good | 🚀 Excellent | ⚡ Very Good |
| **Type Safety** | ⚠️ Runtime | ✅ Compile-time | ⚖️ Mixed |
| **User Control** | 👥 Full | 🛠️ None | 📐 Guided |

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                Dynamic Page Creation Architecture                 │
│                                                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   Component     │  │   GraphQL       │  │   ECS Component │ │
│  │   Metadata      │  │   Discovery     │  │   Inspector     │ │
│  │   System        │  │   Engine        │  │                 │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
│           │                      │                      │        │
│           ▼                      ▼                      ▼        │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │               Field Discovery & Mapping Engine              │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                              │                                   │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                Page Generation Engine                       │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                              │                                   │
│                              ▼                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   Security      │  │   Permission    │  │   Data Access   │ │
│  │   Validation    │  │   Filtering     │  │   Layer         │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Core Concepts

### 1. Component Metadata System

The component metadata system automatically discovers available Jarvis ECS components and their field structures to enable dynamic binding.

```typescript
interface ComponentMetadata {
  // Component identification
  componentName: string;
  tableName: string;
  
  // Component structure
  fields: ComponentField[];
  relationships: ComponentRelationship[];
  
  // Access control
  permissions: ComponentPermissions;
  
  // Display metadata
  displayName?: string;
  description?: string;
  category?: string;
}

interface ComponentField {
  // Field identification
  name: string;
  type: FieldType;
  
  // Database mapping
  columnName: string;
  isRequired: boolean;
  isIdentifier: boolean;
  
  // Display properties
  displayName?: string;
  description?: string;
  format?: FieldFormat;
  
  // Validation
  constraints?: FieldConstraints;
  
  // Binding capabilities
  bindable: boolean;
  searchable: boolean;
  sortable: boolean;
}

type FieldType = 
  | 'string'
  | 'number' 
  | 'boolean' 
  | 'date' 
  | 'datetime'
  | 'uuid'
  | 'json'
  | 'array';

interface ComponentRelationship {
  // Relationship definition
  type: 'parent' | 'child' | 'reference';
  targetComponent: string;
  foreignKey: string;
  
  // Display properties
  displayName?: string;
  
  // Loading behavior
  eager?: boolean;
  cascadeDelete?: boolean;
}
```

### 2. Component Discovery Engine

The discovery engine scans the Jarvis ECS system to automatically identify available components and their schemas.

```typescript
class ComponentDiscoveryEngine {
  private readonly graphqlService: GraphQLService;
  private readonly metadataCache = new Map<string, ComponentMetadata>();
  
  constructor(graphqlService: GraphQLService) {
    this.graphqlService = graphqlService;
  }
  
  /**
   * Discover all available ECS components
   */
  async discoverComponents(): Promise<ComponentMetadata[]> {
    const query = `
      query DiscoverComponents {
        # Query information schema to get component tables
        __schema {
          types {
            name
            fields {
              name
              type {
                name
                kind
              }
              description
            }
            description
          }
        }
        
        # Get component metadata from Jarvis system
        componentMetadataCollection {
          edges {
            node {
              componentName: component_name
              tableName: table_name
              displayName: display_name
              description
              category
              fieldsMetadata: fields_metadata
              relationshipsMetadata: relationships_metadata
              permissionsMetadata: permissions_metadata
            }
          }
        }
      }
    `;
    
    try {
      const result = await this.graphqlService.executeQuery<DiscoveryResult>(query);
      
      // Combine GraphQL schema information with Jarvis metadata
      const components = this.mergeSchemaWithMetadata(
        result.__schema,
        result.componentMetadataCollection.edges
      );
      
      // Cache results
      components.forEach(component => {
        this.metadataCache.set(component.componentName, component);
      });
      
      return components;
    } catch (error) {
      console.error('Component discovery failed:', error);
      return [];
    }
  }
  
  /**
   * Get metadata for a specific component
   */
  async getComponentMetadata(componentName: string): Promise<ComponentMetadata | null> {
    // Check cache first
    if (this.metadataCache.has(componentName)) {
      return this.metadataCache.get(componentName)!;
    }
    
    // Discover all components if not cached
    const components = await this.discoverComponents();
    return components.find(c => c.componentName === componentName) || null;
  }
  
  /**
   * Discover fields for a specific component
   */
  async discoverComponentFields(componentName: string): Promise<ComponentField[]> {
    const metadata = await this.getComponentMetadata(componentName);
    return metadata?.fields || [];
  }
  
  /**
   * Check if component supports real-time updates
   */
  async supportsRealTime(componentName: string): Promise<boolean> {
    // Check if component has LastUpdated field and supports subscriptions
    const metadata = await this.getComponentMetadata(componentName);
    
    return metadata?.fields.some(f => 
      f.name === 'lastUpdated' || f.name === 'LastUpdated'
    ) || false;
  }
  
  private mergeSchemaWithMetadata(
    schema: GraphQLSchema,
    metadataEdges: MetadataEdge[]
  ): ComponentMetadata[] {
    const components: ComponentMetadata[] = [];
    
    // Process each metadata entry
    metadataEdges.forEach(edge => {
      const node = edge.node;
      
      // Find corresponding GraphQL type
      const graphqlType = schema.types.find(type => 
        type.name === `${node.componentName}Component` ||
        type.name === node.tableName
      );
      
      if (graphqlType) {
        const fields = this.extractFieldsFromType(graphqlType, node.fieldsMetadata);
        const relationships = this.parseRelationships(node.relationshipsMetadata);
        const permissions = this.parsePermissions(node.permissionsMetadata);
        
        components.push({
          componentName: node.componentName,
          tableName: node.tableName,
          displayName: node.displayName,
          description: node.description,
          category: node.category,
          fields,
          relationships,
          permissions
        });
      }
    });
    
    return components;
  }
}
```

### 3. Field Selection Mechanism

The field selection mechanism allows users to choose which component fields to display and how to bind them to UI components.

```typescript
interface FieldSelector {
  // Component selection
  selectedComponent: string;
  
  // Field selection
  selectedFields: SelectedField[];
  
  // Display configuration
  displayMode: 'table' | 'cards' | 'list' | 'chart';
  groupBy?: string;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
  
  // Filtering
  filters: FieldFilter[];
  
  // Pagination
  pageSize?: number;
  enablePagination?: boolean;
}

interface SelectedField {
  // Field reference
  fieldName: string;
  componentName: string;
  
  // Display properties
  displayName?: string;
  width?: number;
  visible: boolean;
  
  // Formatting
  format?: FieldFormat;
  transform?: string; // Expression for data transformation
  
  // Interaction
  clickable?: boolean;
  sortable?: boolean;
  filterable?: boolean;
  
  // Binding configuration
  bindingType: 'direct' | 'computed' | 'reference';
  bindingExpression?: string;
}

interface FieldFilter {
  fieldName: string;
  operator: FilterOperator;
  value: unknown;
  dataType: FieldType;
}

type FilterOperator = 
  | 'equals'
  | 'notEquals'
  | 'contains'
  | 'startsWith'
  | 'endsWith'
  | 'greaterThan'
  | 'lessThan'
  | 'between'
  | 'in'
  | 'notIn'
  | 'isNull'
  | 'isNotNull';

// Field selector component
const FieldSelectorWidget: React.FC<{
  onSelectionChange: (selection: FieldSelector) => void;
}> = ({ onSelectionChange }) => {
  const [availableComponents, setAvailableComponents] = useState<ComponentMetadata[]>([]);
  const [selection, setSelection] = useState<FieldSelector>({
    selectedComponent: '',
    selectedFields: [],
    displayMode: 'table',
    filters: []
  });
  
  useEffect(() => {
    // Load available components
    const loadComponents = async () => {
      const discovery = new ComponentDiscoveryEngine(graphqlService);
      const components = await discovery.discoverComponents();
      setAvailableComponents(components);
    };
    
    loadComponents();
  }, []);
  
  const handleComponentSelect = async (componentName: string) => {
    const discovery = new ComponentDiscoveryEngine(graphqlService);
    const metadata = await discovery.getComponentMetadata(componentName);
    
    if (metadata) {
      const defaultFields = metadata.fields
        .filter(f => f.bindable)
        .slice(0, 5) // Show first 5 bindable fields by default
        .map(field => ({
          fieldName: field.name,
          componentName: componentName,
          displayName: field.displayName || field.name,
          visible: true,
          bindingType: 'direct' as const,
          sortable: field.sortable,
          filterable: field.searchable
        }));
      
      const newSelection = {
        ...selection,
        selectedComponent: componentName,
        selectedFields: defaultFields
      };
      
      setSelection(newSelection);
      onSelectionChange(newSelection);
    }
  };
  
  return (
    <div className="space-y-6">
      {/* Component Selection */}
      <div>
        <label className="block text-sm font-medium mb-2">
          Select Component
        </label>
        <select
          value={selection.selectedComponent}
          onChange={(e) => handleComponentSelect(e.target.value)}
          className="w-full border rounded px-3 py-2"
        >
          <option value="">Choose a component...</option>
          {availableComponents.map(component => (
            <option key={component.componentName} value={component.componentName}>
              {component.displayName || component.componentName}
            </option>
          ))}
        </select>
      </div>
      
      {/* Field Selection */}
      {selection.selectedComponent && (
        <FieldSelectionPanel
          component={availableComponents.find(c => c.componentName === selection.selectedComponent)!}
          selectedFields={selection.selectedFields}
          onFieldsChange={(fields) => {
            const newSelection = { ...selection, selectedFields: fields };
            setSelection(newSelection);
            onSelectionChange(newSelection);
          }}
        />
      )}
      
      {/* Display Configuration */}
      {selection.selectedFields.length > 0 && (
        <DisplayConfigurationPanel
          selection={selection}
          onSelectionChange={(newSelection) => {
            setSelection(newSelection);
            onSelectionChange(newSelection);
          }}
        />
      )}
    </div>
  );
};
```

## GraphQL Integration

### 1. Dynamic Query Generation

The system generates GraphQL queries based on field selections and component metadata.

```typescript
class DynamicQueryBuilder {
  /**
   * Build a GraphQL query based on field selection
   */
  buildQuery(selector: FieldSelector): string {
    const { selectedComponent, selectedFields, filters, sortBy, sortOrder } = selector;
    
    // Build field selection
    const fieldSelection = this.buildFieldSelection(selectedFields);
    
    // Build filter conditions
    const filterConditions = this.buildFilterConditions(filters);
    
    // Build sorting
    const orderBy = sortBy ? this.buildOrderBy(sortBy, sortOrder) : '';
    
    return `
      query Get${selectedComponent}Data(
        ${this.buildVariables(filters)}
      ) {
        ${this.getCollectionName(selectedComponent)}(
          ${filterConditions ? `filter: ${filterConditions}` : ''}
          ${orderBy ? `orderBy: ${orderBy}` : ''}
        ) {
          edges {
            node {
              ${fieldSelection}
            }
          }
          pageInfo {
            hasNextPage
            hasPreviousPage
            startCursor
            endCursor
          }
          totalCount
        }
      }
    `;
  }
  
  /**
   * Build field selection string
   */
  private buildFieldSelection(selectedFields: SelectedField[]): string {
    return selectedFields
      .filter(field => field.visible)
      .map(field => {
        // Handle binding types
        switch (field.bindingType) {
          case 'direct':
            return this.mapFieldToColumn(field.fieldName);
            
          case 'computed':
            // For computed fields, we might need to select dependencies
            return this.buildComputedField(field);
            
          case 'reference':
            // For reference fields, we need to join to related components
            return this.buildReferenceField(field);
            
          default:
            return this.mapFieldToColumn(field.fieldName);
        }
      })
      .join('\n        ');
  }
  
  /**
   * Build filter conditions for GraphQL
   */
  private buildFilterConditions(filters: FieldFilter[]): string {
    if (filters.length === 0) return '';
    
    const conditions = filters.map(filter => {
      const columnName = this.mapFieldToColumn(filter.fieldName);
      
      switch (filter.operator) {
        case 'equals':
          return `${columnName}: { eq: $${filter.fieldName} }`;
        case 'notEquals':
          return `${columnName}: { neq: $${filter.fieldName} }`;
        case 'contains':
          return `${columnName}: { ilike: $${filter.fieldName} }`;
        case 'greaterThan':
          return `${columnName}: { gt: $${filter.fieldName} }`;
        case 'lessThan':
          return `${columnName}: { lt: $${filter.fieldName} }`;
        case 'in':
          return `${columnName}: { in: $${filter.fieldName} }`;
        case 'isNull':
          return `${columnName}: { is: null }`;
        case 'isNotNull':
          return `${columnName}: { is: { null: false } }`;
        default:
          return `${columnName}: { eq: $${filter.fieldName} }`;
      }
    });
    
    return `{ ${conditions.join(', ')} }`;
  }
  
  /**
   * Build variables for GraphQL query
   */
  private buildVariables(filters: FieldFilter[]): string {
    return filters.map(filter => {
      const graphqlType = this.mapFieldTypeToGraphQL(filter.dataType);
      return `$${filter.fieldName}: ${graphqlType}`;
    }).join(', ');
  }
  
  /**
   * Map field names to database column names
   */
  private mapFieldToColumn(fieldName: string): string {
    // Convert camelCase to snake_case for database columns
    return fieldName.replace(/[A-Z]/g, letter => `_${letter.toLowerCase()}`);
  }
  
  /**
   * Get collection name for GraphQL
   */
  private getCollectionName(componentName: string): string {
    return `${componentName.toLowerCase()}_componentCollection`;
  }
  
  /**
   * Map field types to GraphQL types
   */
  private mapFieldTypeToGraphQL(fieldType: FieldType): string {
    switch (fieldType) {
      case 'string': return 'String';
      case 'number': return 'Float';
      case 'boolean': return 'Boolean';
      case 'date': return 'Date';
      case 'datetime': return 'DateTime';
      case 'uuid': return 'UUID';
      case 'array': return '[String]';
      default: return 'String';
    }
  }
}
```

### 2. Real-Time Data Subscriptions

For components that support real-time updates, the system can establish GraphQL subscriptions.

```typescript
class RealTimeDataManager {
  private subscriptions = new Map<string, ZenObservable.Subscription>();
  
  /**
   * Subscribe to real-time updates for a component
   */
  subscribeToComponent(
    selector: FieldSelector,
    onUpdate: (data: unknown[]) => void,
    onError: (error: Error) => void
  ): () => void {
    const subscriptionKey = this.buildSubscriptionKey(selector);
    
    // Build subscription query
    const query = this.buildSubscriptionQuery(selector);
    
    // Establish WebSocket connection
    const subscription = this.graphqlService.subscribe(query, {
      next: (result) => {
        if (result.data) {
          const edges = result.data[this.getCollectionName(selector.selectedComponent)]?.edges || [];
          const data = edges.map((edge: any) => edge.node);
          onUpdate(data);
        }
      },
      error: onError
    });
    
    this.subscriptions.set(subscriptionKey, subscription);
    
    // Return unsubscribe function
    return () => {
      const sub = this.subscriptions.get(subscriptionKey);
      if (sub) {
        sub.unsubscribe();
        this.subscriptions.delete(subscriptionKey);
      }
    };
  }
  
  /**
   * Build subscription query for real-time updates
   */
  private buildSubscriptionQuery(selector: FieldSelector): string {
    const queryBuilder = new DynamicQueryBuilder();
    
    // Convert query to subscription
    const baseQuery = queryBuilder.buildQuery(selector);
    return baseQuery.replace('query Get', 'subscription Watch');
  }
  
  private buildSubscriptionKey(selector: FieldSelector): string {
    return `${selector.selectedComponent}:${JSON.stringify(selector.selectedFields.map(f => f.fieldName))}`;
  }
}
```

## UIStudio API Integration for Dynamic Pages

### 1. Page Management Operations

Dynamic pages are managed through the UIStudio API, providing comprehensive CRUD operations.

```typescript
class UIStudioPageService {
  private readonly apiService: ApiService;
  
  constructor(apiService: ApiService) {
    this.apiService = apiService;
  }
  
  /**
   * Create a new dynamic page
   */
  async createDynamicPage(
    pageData: CreatePageRequest
  ): Promise<UIStudioPage[]> {
    // Validate page data
    const validationResult = this.validatePageData(pageData);
    
    if (!validationResult.valid) {
      throw new ValidationError('Invalid page data', validationResult.errors);
    }
    
    // Call UIStudio API
    const response = await this.apiService.post('/api/uistudio/pages', {
      ...pageData,
      pageType: 'dynamic',
      layoutConfig: {
        type: 'bento',
        columns: 12,
        gap: 16,
        ...pageData.layoutConfig
      }
    });
    
    return response.data;
  }
  
  /**
   * Update page configuration
   */
  async updatePageConfig(
    pageEntityId: string,
    updates: Partial<UIStudioPage>
  ): Promise<UIStudioPage> {
    const response = await this.apiService.put(`/api/uistudio/pages/${pageEntityId}`, {
      updates,
      modifiedByEntityId: this.getCurrentUserId(),
      changeSummary: 'Dynamic page configuration updated'
    });
    
    return response.data;
  }
  
  /**
   * Create component bindings for dynamic page
   */
  async createComponentBindings(
    pageEntityId: string,
    bindings: ComponentBinding[]
  ): Promise<ComponentBinding[]> {
    const response = await this.apiService.post(`/api/uistudio/pages/${pageEntityId}/bindings`, {
      bindings: bindings.map(binding => ({
        componentType: binding.componentType,
        gridArea: this.formatGridArea(binding.position),
        fieldMappings: binding.fieldMappings
      })),
      createdByEntityId: this.getCurrentUserId()
    });
    
    return response.data;
  }
  
  /**
   * Update component binding
   */
  async updateComponentBinding(
    bindingEntityId: string,
    updates: Partial<ComponentBinding>
  ): Promise<ComponentBinding> {
    const response = await this.apiService.put(`/api/uistudio/bindings/${bindingEntityId}`, {
      updates: {
        gridArea: updates.position ? this.formatGridArea(updates.position) : undefined,
        fieldMappings: updates.fieldMappings
      },
      modifiedByEntityId: this.getCurrentUserId()
    });
    
    return response.data;
  }
  
  /**
   * Delete component binding
   */
  async deleteComponentBinding(bindingEntityId: string): Promise<void> {
    await this.apiService.delete(`/api/uistudio/bindings/${bindingEntityId}`);
  }
  
  /**
   * Publish dynamic page
   */
  async publishPage(pageEntityId: string): Promise<UIStudioPage> {
    const response = await this.apiService.post(`/api/uistudio/pages/${pageEntityId}/publish`, {
      publishedByEntityId: this.getCurrentUserId()
    });
    
    return response.data;
  }
  
  /**
   * Load page with all bindings
   */
  async loadPageWithBindings(pageEntityId: string): Promise<DynamicPageData> {
    const response = await this.apiService.get(`/api/uistudio/pages/${pageEntityId}`, {
      params: {
        includeLayout: true,
        includeBindings: true,
        includePermissions: true
      }
    });
    
    return this.transformPageData(response.data);
  }
  
  /**
   * Bulk update component bindings
   */
  async bulkUpdateBindings(
    pageEntityId: string,
    operations: BulkBindingOperations
  ): Promise<ComponentBinding[]> {
    const response = await this.apiService.post('/api/uistudio/bindings/bulk', {
      creates: operations.creates?.map(binding => ({
        pageEntityId,
        componentType: binding.componentType,
        gridArea: this.formatGridArea(binding.position),
        fieldMappings: binding.fieldMappings
      })),
      updates: operations.updates?.map(update => ({
        bindingEntityId: update.id,
        updates: {
          gridArea: update.position ? this.formatGridArea(update.position) : undefined,
          fieldMappings: update.fieldMappings
        }
      })),
      deletes: operations.deletes,
      modifiedByEntityId: this.getCurrentUserId()
    });
    
    return response.data;
  }
  
  private formatGridArea(position: ComponentPosition): string {
    return `${position.y} / ${position.x} / ${position.y + position.h} / ${position.x + position.w}`;
  }
  
  private transformPageData(apiData: any): DynamicPageData {
    return {
      page: apiData.page,
      layout: apiData.layout,
      bindings: apiData.componentBindings || [],
      permissions: apiData.permissions || []
    };
  }
  
  private getCurrentUserId(): string {
    // Get current user ID from auth context
    return localStorage.getItem('currentUserId') || '';
  }
  
  private validateComponentData(
    data: Record<string, unknown>,
    metadata: ComponentMetadata
  ): { valid: boolean; errors: string[] } {
    const errors: string[] = [];
    
    // Check required fields
    const requiredFields = metadata.fields.filter(f => f.isRequired);
    for (const field of requiredFields) {
      if (!(field.name in data) || data[field.name] == null) {
        errors.push(`Required field '${field.name}' is missing`);
      }
    }
    
    // Validate field types and constraints
    for (const [fieldName, value] of Object.entries(data)) {
      const fieldMetadata = metadata.fields.find(f => f.name === fieldName);
      if (fieldMetadata) {
        const fieldValidation = this.validateFieldValue(value, fieldMetadata);
        if (!fieldValidation.valid) {
          errors.push(...fieldValidation.errors);
        }
      }
    }
    
    return {
      valid: errors.length === 0,
      errors
    };
  }
  
  private validateFieldValue(
    value: unknown,
    field: ComponentField
  ): { valid: boolean; errors: string[] } {
    const errors: string[] = [];
    
    // Type validation
    if (!this.isValidType(value, field.type)) {
      errors.push(`Field '${field.name}' expected type ${field.type} but got ${typeof value}`);
    }
    
    // Constraint validation
    if (field.constraints) {
      const constraintErrors = this.validateConstraints(value, field.constraints);
      errors.push(...constraintErrors);
    }
    
    return {
      valid: errors.length === 0,
      errors
    };
  }
}
```

### 2. API Route Generation

Dynamic API routes are generated based on available components.

```typescript
interface DynamicRoute {
  path: string;
  method: 'GET' | 'POST' | 'PUT' | 'DELETE';
  handler: string;
  middleware: string[];
  permissions: string[];
}

class DynamicRouteGenerator {
  /**
   * Generate all CRUD routes for a component
   */
  generateComponentRoutes(componentName: string, metadata: ComponentMetadata): DynamicRoute[] {
    const basePath = `/api/components/${componentName.toLowerCase()}`;
    
    const routes: DynamicRoute[] = [
      // List/Search
      {
        path: basePath,
        method: 'GET',
        handler: `${componentName}Handler.list`,
        middleware: ['auth', 'rateLimit'],
        permissions: [`${componentName}:read`]
      },
      
      // Get by ID
      {
        path: `${basePath}/:id`,
        method: 'GET',
        handler: `${componentName}Handler.getById`,
        middleware: ['auth'],
        permissions: [`${componentName}:read`]
      },
      
      // Create
      {
        path: basePath,
        method: 'POST',
        handler: `${componentName}Handler.create`,
        middleware: ['auth', 'validate'],
        permissions: [`${componentName}:create`]
      },
      
      // Update
      {
        path: `${basePath}/:id`,
        method: 'PUT',
        handler: `${componentName}Handler.update`,
        middleware: ['auth', 'validate'],
        permissions: [`${componentName}:update`]
      },
      
      // Delete
      {
        path: `${basePath}/:id`,
        method: 'DELETE',
        handler: `${componentName}Handler.delete`,
        middleware: ['auth'],
        permissions: [`${componentName}:delete`]
      },
      
      // Bulk operations
      {
        path: `${basePath}/bulk`,
        method: 'POST',
        handler: `${componentName}Handler.bulkCreate`,
        middleware: ['auth', 'validate', 'rateLimit'],
        permissions: [`${componentName}:create`]
      }
    ];
    
    // Add relationship routes if component has relationships
    if (metadata.relationships.length > 0) {
      routes.push(...this.generateRelationshipRoutes(componentName, metadata.relationships));
    }
    
    return routes;
  }
  
  /**
   * Generate routes for component relationships
   */
  private generateRelationshipRoutes(
    componentName: string,
    relationships: ComponentRelationship[]
  ): DynamicRoute[] {
    const routes: DynamicRoute[] = [];
    const basePath = `/api/components/${componentName.toLowerCase()}`;
    
    relationships.forEach(relationship => {
      const relationshipPath = `${basePath}/:id/${relationship.type}s`;
      
      routes.push({
        path: relationshipPath,
        method: 'GET',
        handler: `${componentName}Handler.get${relationship.type}s`,
        middleware: ['auth'],
        permissions: [`${componentName}:read`, `${relationship.targetComponent}:read`]
      });
      
      // Add/remove relationship
      routes.push({
        path: `${relationshipPath}/:relationshipId`,
        method: 'POST',
        handler: `${componentName}Handler.add${relationship.type}`,
        middleware: ['auth'],
        permissions: [`${componentName}:update`]
      });
      
      routes.push({
        path: `${relationshipPath}/:relationshipId`,
        method: 'DELETE',
        handler: `${componentName}Handler.remove${relationship.type}`,
        middleware: ['auth'],
        permissions: [`${componentName}:update`]
      });
    });
    
    return routes;
  }
}
```

## Security and Permissions

### 1. Component-Level Security

Each discovered component has associated permissions that control access.

```typescript
interface ComponentPermissions {
  // Basic CRUD permissions
  read: PermissionRule[];
  create: PermissionRule[];
  update: PermissionRule[];
  delete: PermissionRule[];
  
  // Field-level permissions
  fieldPermissions: FieldPermission[];
  
  // Row-level security
  rowLevelSecurity?: RowLevelSecurityRule[];
}

interface PermissionRule {
  type: 'role' | 'permission' | 'expression';
  value: string;
  condition?: string;
}

interface FieldPermission {
  fieldName: string;
  read?: PermissionRule[];
  write?: PermissionRule[];
}

interface RowLevelSecurityRule {
  name: string;
  condition: string; // SQL-like expression
  applies_to: ('SELECT' | 'INSERT' | 'UPDATE' | 'DELETE')[];
}

class ComponentSecurityManager {
  /**
   * Check if user can access component
   */
  async canAccessComponent(
    userId: string,
    componentName: string,
    operation: 'read' | 'create' | 'update' | 'delete'
  ): Promise<boolean> {
    const metadata = await this.getComponentMetadata(componentName);
    const permissions = metadata.permissions[operation];
    
    for (const rule of permissions) {
      if (await this.evaluatePermissionRule(userId, rule)) {
        return true;
      }
    }
    
    return false;
  }
  
  /**
   * Filter fields based on user permissions
   */
  async filterFieldsForUser(
    userId: string,
    componentName: string,
    fields: ComponentField[]
  ): Promise<ComponentField[]> {
    const metadata = await this.getComponentMetadata(componentName);
    const fieldPermissions = metadata.permissions.fieldPermissions;
    
    const allowedFields: ComponentField[] = [];
    
    for (const field of fields) {
      const fieldPerm = fieldPermissions.find(fp => fp.fieldName === field.name);
      
      if (!fieldPerm || !fieldPerm.read) {
        // No specific permission required, allow by default
        allowedFields.push(field);
      } else {
        // Check field-level read permission
        for (const rule of fieldPerm.read) {
          if (await this.evaluatePermissionRule(userId, rule)) {
            allowedFields.push(field);
            break;
          }
        }
      }
    }
    
    return allowedFields;
  }
  
  /**
   * Apply row-level security to GraphQL queries
   */
  async applyRowLevelSecurity(
    userId: string,
    componentName: string,
    baseQuery: string
  ): Promise<string> {
    const metadata = await this.getComponentMetadata(componentName);
    const rlsRules = metadata.permissions.rowLevelSecurity || [];
    
    if (rlsRules.length === 0) {
      return baseQuery;
    }
    
    // Build additional filter conditions based on RLS rules
    const rlsConditions: string[] = [];
    
    for (const rule of rlsRules) {
      if (rule.applies_to.includes('SELECT')) {
        const condition = await this.evaluateRLSCondition(userId, rule.condition);
        if (condition) {
          rlsConditions.push(condition);
        }
      }
    }
    
    if (rlsConditions.length > 0) {
      // Inject RLS conditions into the GraphQL query
      return this.injectSecurityConditions(baseQuery, rlsConditions);
    }
    
    return baseQuery;
  }
  
  private async evaluatePermissionRule(
    userId: string,
    rule: PermissionRule
  ): Promise<boolean> {
    switch (rule.type) {
      case 'role':
        return await this.userHasRole(userId, rule.value);
        
      case 'permission':
        return await this.userHasPermission(userId, rule.value);
        
      case 'expression':
        return await this.evaluateExpression(userId, rule.value);
        
      default:
        return false;
    }
  }
}
```

### 2. Dynamic Permission Assignment

Permissions can be dynamically assigned based on component metadata and user context.

```typescript
class DynamicPermissionManager {
  /**
   * Generate permissions for a new component
   */
  async generateComponentPermissions(
    componentName: string,
    metadata: ComponentMetadata
  ): Promise<ComponentPermissions> {
    const baseName = componentName.toLowerCase();
    
    // Default CRUD permissions
    const defaultPermissions: ComponentPermissions = {
      read: [
        { type: 'permission', value: `${baseName}:read` },
        { type: 'role', value: 'viewer' }
      ],
      create: [
        { type: 'permission', value: `${baseName}:create` },
        { type: 'role', value: 'editor' }
      ],
      update: [
        { type: 'permission', value: `${baseName}:update` },
        { type: 'role', value: 'editor' }
      ],
      delete: [
        { type: 'permission', value: `${baseName}:delete` },
        { type: 'role', value: 'admin' }
      ],
      fieldPermissions: []
    };
    
    // Add field-level permissions for sensitive fields
    const sensitiveFields = metadata.fields.filter(f => 
      this.isSensitiveField(f.name) || f.name.includes('password') || f.name.includes('secret')
    );
    
    sensitiveFields.forEach(field => {
      defaultPermissions.fieldPermissions.push({
        fieldName: field.name,
        read: [{ type: 'role', value: 'admin' }],
        write: [{ type: 'role', value: 'admin' }]
      });
    });
    
    // Add ownership-based RLS if component has owner field
    const hasOwnerField = metadata.fields.some(f => 
      f.name === 'ownerEntityId' || f.name === 'createdBy'
    );
    
    if (hasOwnerField) {
      defaultPermissions.rowLevelSecurity = [
        {
          name: 'owner_access',
          condition: 'owner_entity_id = $user_id OR created_by = $user_id',
          applies_to: ['SELECT', 'UPDATE', 'DELETE']
        }
      ];
    }
    
    return defaultPermissions;
  }
  
  private isSensitiveField(fieldName: string): boolean {
    const sensitivePatterns = [
      /password/i,
      /secret/i,
      /token/i,
      /key/i,
      /ssn/i,
      /social/i,
      /credit/i,
      /bank/i
    ];
    
    return sensitivePatterns.some(pattern => pattern.test(fieldName));
  }
}
```

## Example Implementations

### 1. User Management Dashboard

Here's an example of creating a dynamic user management dashboard:

```typescript
// Dynamic page configuration
const userManagementPageConfig = {
  pageId: 'user-management',
  displayName: 'User Management',
  route: '/admin/users',
  
  // Security configuration
  bindings: {
    security: {
      requiredRoles: ['admin', 'user_manager'],
      requiredPermissions: ['user:read']
    }
  },
  
  // Dynamic component configuration
  components: [
    {
      id: 'user-list',
      type: 'DynamicDataTable',
      position: { x: 0, y: 0, w: 12, h: 8 },
      
      // Dynamic binding configuration
      bindings: {
        dataSource: 'jarvis-ecs',
        componentSelector: {
          selectedComponent: 'Account',
          selectedFields: [
            {
              fieldName: 'email',
              displayName: 'Email Address',
              visible: true,
              sortable: true,
              filterable: true,
              bindingType: 'direct'
            },
            {
              fieldName: 'isActive',
              displayName: 'Active',
              visible: true,
              sortable: true,
              filterable: true,
              bindingType: 'direct',
              format: 'boolean'
            },
            {
              fieldName: 'createdAt',
              displayName: 'Created',
              visible: true,
              sortable: true,
              bindingType: 'direct',
              format: 'datetime'
            },
            {
              fieldName: 'lastLogin',
              displayName: 'Last Login',
              visible: true,
              sortable: true,
              bindingType: 'computed',
              bindingExpression: 'authSessionCollection.lastActivity'
            }
          ],
          displayMode: 'table',
          filters: [
            {
              fieldName: 'isActive',
              operator: 'equals',
              value: true,
              dataType: 'boolean'
            }
          ]
        }
      },
      
      // Event bindings for interactions
      eventBindings: [
        {
          event: 'onRowClick',
          action: 'openUserDetails',
          parameters: {
            userId: '${row.ownerEntityId}'
          }
        },
        {
          event: 'onCreateClick',
          action: 'openCreateUserModal'
        }
      ]
    },
    
    {
      id: 'user-stats',
      type: 'MetricCard',
      position: { x: 0, y: 8, w: 3, h: 2 },
      
      bindings: {
        dataSource: 'jarvis-ecs',
        componentSelector: {
          selectedComponent: 'Account',
          selectedFields: [
            {
              fieldName: 'totalUsers',
              displayName: 'Total Users',
              bindingType: 'computed',
              bindingExpression: 'COUNT(*)'
            }
          ]
        }
      }
    }
  ]
};
```

### 2. Dynamic Report Builder

Example of a component that lets users build reports dynamically:

```typescript
const ReportBuilderComponent: React.FC = () => {
  const [availableComponents, setAvailableComponents] = useState<ComponentMetadata[]>([]);
  const [reportConfig, setReportConfig] = useState<ReportConfiguration | null>(null);
  const [reportData, setReportData] = useState<unknown[]>([]);
  const [loading, setLoading] = useState(false);
  
  const discovery = useMemo(() => new ComponentDiscoveryEngine(graphqlService), []);
  const queryBuilder = useMemo(() => new DynamicQueryBuilder(), []);
  
  useEffect(() => {
    // Load available components
    discovery.discoverComponents().then(setAvailableComponents);
  }, [discovery]);
  
  const generateReport = async () => {
    if (!reportConfig) return;
    
    setLoading(true);
    try {
      // Build GraphQL query from report configuration
      const query = queryBuilder.buildQuery(reportConfig.fieldSelector);
      
      // Apply security filters
      const securityManager = new ComponentSecurityManager();
      const secureQuery = await securityManager.applyRowLevelSecurity(
        getCurrentUserId(),
        reportConfig.fieldSelector.selectedComponent,
        query
      );
      
      // Execute query
      const result = await graphqlService.executeQuery(secureQuery);
      const edges = result[queryBuilder.getCollectionName(reportConfig.fieldSelector.selectedComponent)]?.edges || [];
      const data = edges.map((edge: any) => edge.node);
      
      setReportData(data);
    } catch (error) {
      console.error('Failed to generate report:', error);
    } finally {
      setLoading(false);
    }
  };
  
  return (
    <div className="space-y-6">
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-xl font-semibold mb-4">Build Your Report</h2>
        
        <FieldSelectorWidget
          onSelectionChange={(selection) => {
            setReportConfig({
              name: `Custom Report - ${selection.selectedComponent}`,
              fieldSelector: selection,
              createdAt: new Date().toISOString(),
              createdBy: getCurrentUserId()
            });
          }}
        />
        
        {reportConfig && (
          <div className="mt-4">
            <button
              onClick={generateReport}
              disabled={loading}
              className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:opacity-50"
            >
              {loading ? 'Generating...' : 'Generate Report'}
            </button>
          </div>
        )}
      </div>
      
      {reportData.length > 0 && (
        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="text-lg font-semibold mb-4">Report Results</h3>
          
          <DynamicDataTable
            data={reportData}
            columns={reportConfig!.fieldSelector.selectedFields.map(field => ({
              key: field.fieldName,
              title: field.displayName,
              sortable: field.sortable,
              filterable: field.filterable,
              format: field.format
            }))}
            pagination={true}
            exportable={true}
          />
        </div>
      )}
    </div>
  );
};
```

### 3. Component Relationship Explorer

Example showing how to explore and visualize component relationships:

```typescript
const RelationshipExplorerComponent: React.FC<{
  rootComponent: string;
  rootId: string;
}> = ({ rootComponent, rootId }) => {
  const [relationshipGraph, setRelationshipGraph] = useState<RelationshipNode | null>(null);
  const [selectedNode, setSelectedNode] = useState<string | null>(null);
  
  useEffect(() => {
    const buildRelationshipGraph = async () => {
      const discovery = new ComponentDiscoveryEngine(graphqlService);
      const metadata = await discovery.getComponentMetadata(rootComponent);
      
      if (metadata) {
        const graph = await this.exploreRelationships(metadata, rootId);
        setRelationshipGraph(graph);
      }
    };
    
    buildRelationshipGraph();
  }, [rootComponent, rootId]);
  
  const exploreRelationships = async (
    metadata: ComponentMetadata,
    entityId: string,
    visited = new Set<string>()
  ): Promise<RelationshipNode> => {
    if (visited.has(entityId)) {
      return { id: entityId, component: metadata.componentName, children: [] };
    }
    
    visited.add(entityId);
    
    const node: RelationshipNode = {
      id: entityId,
      component: metadata.componentName,
      children: []
    };
    
    // Explore each relationship
    for (const relationship of metadata.relationships) {
      try {
        const relatedData = await this.fetchRelatedData(
          metadata.componentName,
          entityId,
          relationship
        );
        
        for (const relatedItem of relatedData) {
          const relatedMetadata = await discovery.getComponentMetadata(relationship.targetComponent);
          if (relatedMetadata) {
            const childNode = await this.exploreRelationships(
              relatedMetadata,
              relatedItem.id,
              new Set(visited) // Create new set to prevent infinite loops
            );
            node.children.push(childNode);
          }
        }
      } catch (error) {
        console.warn(`Failed to explore relationship ${relationship.targetComponent}:`, error);
      }
    }
    
    return node;
  };
  
  return (
    <div className="space-y-6">
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-xl font-semibold mb-4">
          Relationship Explorer: {rootComponent}
        </h2>
        
        {relationshipGraph && (
          <RelationshipVisualization
            graph={relationshipGraph}
            onNodeSelect={setSelectedNode}
            selectedNode={selectedNode}
          />
        )}
      </div>
      
      {selectedNode && (
        <div className="bg-white rounded-lg shadow p-6">
          <ComponentDetailsPanel
            componentId={selectedNode}
            onClose={() => setSelectedNode(null)}
          />
        </div>
      )}
    </div>
  );
};
```

## Performance Considerations

### 1. Query Optimization

- **Field Selection**: Only query fields that are actually needed
- **Pagination**: Implement cursor-based pagination for large datasets
- **Caching**: Cache component metadata and frequently accessed data
- **Lazy Loading**: Load related data only when needed

### 2. Real-Time Updates

- **Selective Subscriptions**: Only subscribe to updates for visible components
- **Debouncing**: Debounce rapid updates to prevent UI thrashing
- **Connection Management**: Efficiently manage WebSocket connections

### 3. Security Performance

- **Permission Caching**: Cache permission checks to reduce database load
- **Bulk Validation**: Validate permissions in batches when possible
- **Query Optimization**: Optimize RLS queries to use indexes effectively

## Upgrading Dynamic Pages to Hybrid Pages

As dynamic pages mature and require more structure, they can be upgraded to hybrid pages by adding fixed components while preserving dynamic functionality.

### Migration Example: Dashboard with Fixed Navigation

```typescript
// Original dynamic page configuration
const dynamicDashboardConfig = {
  pageId: 'sales-dashboard',
  components: [
    // All components are dynamic
    { type: 'MetricCard', bindings: { dataSource: 'salesData' } },
    { type: 'ChartWidget', bindings: { dataSource: 'revenueChart' } },
    { type: 'DataTable', bindings: { dataSource: 'transactions' } }
  ]
};

// Upgraded to hybrid page
const hybridDashboardTemplate = {
  templateId: 'sales-dashboard-template',
  fixedComponents: [
    // Fixed navigation and header
    { type: 'DashboardHeader', position: 'header' },
    { type: 'Navigation', position: 'sidebar' }
  ],
  dynamicSlots: {
    // Keep dynamic area for metrics
    metricsSlot: {
      allowedComponents: ['MetricCard', 'KPIWidget'],
      maxComponents: 6,
      gridConstraints: { minHeight: 2, maxHeight: 4 }
    },
    // Keep dynamic area for charts
    chartsSlot: {
      allowedComponents: ['ChartWidget', 'GraphComponent'],
      maxComponents: 4,
      gridConstraints: { minHeight: 4 }
    }
  }
};
```

### Hybrid Page Implementation

```typescript
const SalesDashboardHybrid: React.FC<{ config: HybridConfig }> = ({ config }) => {
  return (
    <div className="sales-dashboard-hybrid">
      {/* Fixed header - always consistent */}
      <DashboardHeader 
        title="Sales Dashboard"
        actions={[
          { label: 'Export', action: 'export' },
          { label: 'Refresh', action: 'refresh' }
        ]}
      />
      
      <div className="dashboard-layout">
        {/* Fixed navigation - developer controlled */}
        <aside className="dashboard-sidebar">
          <Navigation 
            items={[
              { label: 'Overview', path: '/dashboard' },
              { label: 'Sales', path: '/dashboard/sales' },
              { label: 'Reports', path: '/dashboard/reports' }
            ]}
          />
        </aside>
        
        {/* Dynamic content area - user configurable */}
        <main className="dashboard-content">
          {/* Metrics slot - constrained dynamic area */}
          <section className="metrics-section">
            <h2>Key Metrics</h2>
            <BentoGrid
              grid={config.metricsSlot}
              constraints={template.slots.metricsSlot}
              isEditing={editMode}
              onUpdate={handleMetricsUpdate}
            />
          </section>
          
          {/* Charts slot - another constrained dynamic area */}
          <section className="charts-section">
            <h2>Analytics</h2>
            <BentoGrid
              grid={config.chartsSlot}
              constraints={template.slots.chartsSlot}
              isEditing={editMode}
              onUpdate={handleChartsUpdate}
            />
          </section>
        </main>
      </div>
      
      {/* Fixed footer */}
      <footer className="dashboard-footer">
        <p>© 2024 Your Company - Sales Dashboard</p>
      </footer>
    </div>
  );
};
```

## Integration with Fixed and Hybrid Pages

### Application Architecture with Mixed Page Types

```typescript
// Page routing configuration
const pageRoutes = [
  // Fixed pages for critical flows
  {
    path: '/login',
    component: LoginPage, // Fixed page
    type: 'fixed',
    permissions: ['public']
  },
  {
    path: '/admin',
    component: AdminDashboard, // Fixed page
    type: 'fixed',
    permissions: ['admin']
  },
  
  // Hybrid pages for structured flexibility
  {
    path: '/departments/:dept',
    component: DepartmentTemplate, // Hybrid page
    type: 'hybrid',
    permissions: ['department_user'],
    templateResolver: (params) => `${params.dept}-template`
  },
  
  // Dynamic pages for user creation
  {
    path: '/dashboards/:id',
    component: DynamicPageRenderer, // Dynamic page
    type: 'dynamic',
    permissions: ['dashboard_user'],
    configResolver: (params) => loadDynamicPageConfig(params.id)
  }
];

// Page type resolver
const renderPage = (route: PageRoute, params: RouteParams) => {
  switch (route.type) {
    case 'fixed':
      return <route.component />;
      
    case 'hybrid':
      const template = loadTemplate(route.templateResolver(params));
      const config = loadHybridConfig(params);
      return <route.component template={template} config={config} />;
      
    case 'dynamic':
      const pageConfig = route.configResolver(params);
      return (
        <DynamicPageRenderer 
          config={pageConfig}
          isEditing={userCanEdit(params.id)}
        />
      );
      
    default:
      return <NotFoundPage />;
  }
};
```

### Shared Component Strategy

```typescript
// Components can be used across all page types
interface SharedComponent {
  // For fixed pages - direct import
  fixed: React.ComponentType;
  
  // For dynamic/hybrid - registry entry
  registry: ComponentRegistryEntry;
  
  // Common configuration
  metadata: ComponentMetadata;
}

// Example: MetricCard component
const MetricCardShared: SharedComponent = {
  // Direct import for fixed pages
  fixed: MetricCard,
  
  // Registry entry for dynamic/hybrid pages
  registry: {
    id: 'metric-card',
    name: 'Metric Card',
    component: MetricCard,
    schema: MetricCardSchema,
    category: 'data-display',
    allowedIn: ['dynamic', 'hybrid']
  },
  
  // Shared metadata
  metadata: {
    description: 'Displays a key metric with trend indication',
    props: MetricCardPropsSchema,
    examples: MetricCardExamples
  }
};

// Usage in fixed page
const AnalyticsDashboard: React.FC = () => {
  return (
    <div>
      <MetricCardShared.fixed 
        title="Revenue"
        value={1250000}
        trend="up"
      />
    </div>
  );
};

// Usage in dynamic page (through registry)
const dynamicPageConfig = {
  components: [
    {
      type: 'metric-card', // References registry entry
      bindings: {
        title: 'Revenue',
        dataSource: 'salesData.revenue'
      }
    }
  ]
};
```

## Decision Matrix: When to Choose Dynamic Pages

### ✅ Perfect for Dynamic Pages

**Scenario**: Executive wants a custom KPI dashboard
- **Why Dynamic**: Business user can self-serve, requirements change frequently
- **Implementation**: Use component selector to pick metrics, configure data bindings
- **Outcome**: 15-minute setup vs weeks of development time

**Scenario**: Department needs reporting views
- **Why Dynamic**: Multiple similar layouts, different data per department
- **Implementation**: Template approach with field picker for department-specific metrics
- **Outcome**: Scale to dozens of departments without developer involvement

### ⚠️ Consider Alternatives

**Scenario**: Customer-facing landing page
- **Why Not Dynamic**: Brand consistency, performance, SEO requirements
- **Better Choice**: Fixed page with optimized assets and controlled experience

**Scenario**: Complex workflow with business logic
- **Why Not Dynamic**: Custom interactions, state management, validation rules
- **Better Choice**: Fixed page or hybrid with fixed workflow + dynamic data display

**Scenario**: Multi-tenant app with customization
- **Why Not Pure Dynamic**: Need structure while allowing customization
- **Better Choice**: Hybrid page with template constraints and guided customization

### 🔄 Migration Scenarios

**Dynamic → Hybrid**: When structure is needed
```typescript
// Start: Pure dynamic page
const originalConfig = {
  components: [/* all dynamic */]
};

// Evolve: Add structure with hybrid template
const hybridTemplate = {
  fixedComponents: [Navigation, Header], // Add consistency
  dynamicSlots: { content: originalConfig } // Preserve flexibility
};
```

**Dynamic → Fixed**: When optimizing for performance
```typescript
// Extract successful dynamic configuration
const dynamicConfig = loadSuccessfulDynamicPage();

// Generate fixed component scaffold
const FixedVersion = generateComponentFromConfig(dynamicConfig);

// Add optimizations
const OptimizedPage = memo(FixedVersion);
```

## Best Practices for Dynamic Pages

### 1. Component Design for Dynamic Use

```typescript
// Good: Flexible, data-driven component
interface FlexibleCardProps {
  title: string;
  dataSource: string; // Dynamic binding
  displayMode: 'chart' | 'table' | 'metric';
  filters?: FilterConfig[];
  refreshInterval?: number;
}

// Bad: Hardcoded, inflexible component
interface SpecificCardProps {
  salesData: SalesData; // Specific to one use case
  showQuarterlyBreakdown: boolean; // Too specific
}
```

### 2. Progressive Enhancement Strategy

```typescript
// Start simple: Basic dynamic pages
const simpleConfig = {
  components: [{ type: 'data-table', dataSource: 'users' }]
};

// Add complexity: Rich interactions
const enhancedConfig = {
  components: [{
    type: 'data-table',
    dataSource: 'users',
    features: ['sorting', 'filtering', 'export'],
    actions: ['edit', 'delete', 'bulk-operations']
  }]
};

// Graduate to hybrid: When structure is needed
const hybridVersion = {
  template: 'user-management-template',
  slots: {
    userTable: enhancedConfig.components[0]
  }
};
```

### 3. Performance Optimization

```typescript
// Lazy load dynamic components
const DynamicComponentLoader = ({ componentType, ...props }) => {
  const Component = useMemo(
    () => lazy(() => loadComponent(componentType)),
    [componentType]
  );
  
  return (
    <Suspense fallback={<ComponentSkeleton />}>
      <Component {...props} />
    </Suspense>
  );
};

// Cache configuration and data
const useDynamicPageData = (pageId: string) => {
  return useQuery({
    queryKey: ['dynamic-page', pageId],
    queryFn: () => loadDynamicPageConfig(pageId),
    staleTime: 5 * 60 * 1000, // 5 minutes
    cacheTime: 30 * 60 * 1000 // 30 minutes
  });
};
```

## Next Steps

1. **Architecture**: Review [Page Types and Architecture](./09-page-types-and-architecture.md) for choosing the right page type
2. **Components**: Check [Component Registry](./04-component-registry.md) for available dynamic components
3. **Security**: See [Security Model](./12-security-model.md) for implementing proper access controls
4. **Data**: Explore [Data Models](./06-data-models.md) for ECS integration patterns
5. **Implementation**: Follow [Implementation Plan](./07-implementation-plan.md) for development phases
6. **Hybrid Evolution**: Consider [Hybrid Page Patterns](./17-hybrid-page-patterns.md) for structured flexibility