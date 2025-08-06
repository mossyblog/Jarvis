# Example Implementations: CRM Form Builder

This document provides a comprehensive, real-world example of implementing a Customer Relationship Management (CRM) form using the Bento Grid System's dynamic page creation capabilities.

## Table of Contents

1. [Overview](#overview)
2. [Component Definitions](#component-definitions)
3. [Form Builder Implementation](#form-builder-implementation)
4. [Data Flow and Integration](#data-flow-and-integration)
5. [Complete Code Examples](#complete-code-examples)
6. [API Integration](#api-integration)
7. [Real-time Updates](#real-time-updates)
8. [Testing Examples](#testing-examples)

## Overview

This example demonstrates how to build a dynamic CRM form that allows users to:

- Drag and drop components from different data sources
- Select specific fields from each component
- Configure field relationships and validation
- Save mixed form data from multiple components
- Handle real-time updates via GraphQL subscriptions

### Components Used

- **AccountComponent**: Basic customer information (email, name, phone)
- **ContactComponent**: Address and communication preferences
- **ActivityComponent**: Notes, interactions, and tracking data

## Component Definitions

### Core Component Interfaces

```typescript
// core.jarvis.ui.studio/src/types/crm-components.ts

export interface AccountComponent extends IComponent {
  componentType: 'account';
  fields: {
    email: string;
    firstName: string;
    lastName: string;
    phone: string;
    company: string;
    status: 'active' | 'inactive' | 'prospect';
    createdAt: Date;
    lastContactDate?: Date;
  };
  validation: {
    email: { required: true; pattern: 'email' };
    firstName: { required: true; minLength: 2 };
    lastName: { required: true; minLength: 2 };
    phone: { required: false; pattern: 'phone' };
    company: { required: false };
  };
  relationships: {
    contacts: string[]; // ContactComponent IDs
    activities: string[]; // ActivityComponent IDs
  };
}

export interface ContactComponent extends IComponent {
  componentType: 'contact';
  fields: {
    type: 'primary' | 'billing' | 'shipping' | 'other';
    street: string;
    city: string;
    state: string;
    zipCode: string;
    country: string;
    preferredContactMethod: 'email' | 'phone' | 'mail';
    timezone: string;
    language: string;
  };
  validation: {
    street: { required: true };
    city: { required: true };
    state: { required: true };
    zipCode: { required: true; pattern: 'zipcode' };
    country: { required: true };
  };
  relationships: {
    account: string; // AccountComponent ID
  };
}

export interface ActivityComponent extends IComponent {
  componentType: 'activity';
  fields: {
    type: 'call' | 'email' | 'meeting' | 'note' | 'task';
    subject: string;
    description: string;
    priority: 'low' | 'medium' | 'high' | 'urgent';
    status: 'pending' | 'completed' | 'cancelled';
    dueDate?: Date;
    completedDate?: Date;
    assignedTo: string;
    tags: string[];
  };
  validation: {
    subject: { required: true; minLength: 5 };
    description: { required: false };
    priority: { required: true };
    status: { required: true };
    assignedTo: { required: true };
  };
  relationships: {
    account: string; // AccountComponent ID
    relatedActivities: string[]; // Other ActivityComponent IDs
  };
}

// Combined form data interface
export interface CRMFormData {
  account: Partial<AccountComponent['fields']>;
  contacts: Partial<ContactComponent['fields']>[];
  activities: Partial<ActivityComponent['fields']>[];
  metadata: {
    formId: string;
    version: number;
    lastModified: Date;
    createdBy: string;
  };
}
```

### Component Registration

```typescript
// core.jarvis.ui.studio/src/components/crm/registry.ts

import { ComponentDefinition } from '../../types/component-registry';

export const crmComponentRegistry: ComponentDefinition[] = [
  {
    id: 'crm-account',
    name: 'Account Information',
    category: 'CRM',
    description: 'Customer account details and basic information',
    icon: 'User',
    component: AccountComponent,
    defaultProps: {
      showFields: ['email', 'firstName', 'lastName', 'company'],
      layout: 'card',
      size: { width: 4, height: 3 }
    },
    configSchema: {
      showFields: {
        type: 'multiselect',
        options: ['email', 'firstName', 'lastName', 'phone', 'company', 'status'],
        default: ['email', 'firstName', 'lastName', 'company']
      },
      layout: {
        type: 'select',
        options: ['card', 'form', 'inline'],
        default: 'card'
      },
      allowEdit: {
        type: 'boolean',
        default: true
      }
    }
  },
  {
    id: 'crm-contact',
    name: 'Contact Details',
    category: 'CRM',
    description: 'Address and contact preferences',
    icon: 'MapPin',
    component: ContactComponent,
    defaultProps: {
      showFields: ['street', 'city', 'state', 'zipCode'],
      contactType: 'primary',
      size: { width: 3, height: 4 }
    },
    configSchema: {
      showFields: {
        type: 'multiselect',
        options: ['street', 'city', 'state', 'zipCode', 'country', 'preferredContactMethod'],
        default: ['street', 'city', 'state', 'zipCode']
      },
      contactType: {
        type: 'select',
        options: ['primary', 'billing', 'shipping', 'other'],
        default: 'primary'
      }
    }
  },
  {
    id: 'crm-activity',
    name: 'Activity Tracker',
    category: 'CRM',
    description: 'Notes, tasks, and interaction history',
    icon: 'Activity',
    component: ActivityComponent,
    defaultProps: {
      showFields: ['subject', 'type', 'priority', 'status'],
      maxItems: 5,
      size: { width: 6, height: 4 }
    },
    configSchema: {
      showFields: {
        type: 'multiselect',
        options: ['type', 'subject', 'description', 'priority', 'status', 'dueDate', 'assignedTo'],
        default: ['subject', 'type', 'priority', 'status']
      },
      maxItems: {
        type: 'number',
        min: 1,
        max: 20,
        default: 5
      },
      allowCreate: {
        type: 'boolean',
        default: true
      }
    }
  }
];
```

## Form Builder Implementation

### CRM Form Builder Component

```tsx
// core.jarvis.ui.studio/src/components/crm/CRMFormBuilder.tsx

import React, { useState, useCallback, useEffect } from 'react';
import { DndProvider } from 'react-dnd';
import { HTML5Backend } from 'react-dnd-html5-backend';
import { BentoGrid } from '../bento/BentoGrid';
import { ComponentPalette } from '../bento/ComponentPalette';
import { FieldSelector } from './FieldSelector';
import { RelationshipMapper } from './RelationshipMapper';
import { FormPreview } from './FormPreview';
import { crmComponentRegistry } from './registry';
import { useCRMFormData } from '../../hooks/useCRMFormData';
import { useGraphQLSubscription } from '../../hooks/useGraphQLSubscription';
import type { CRMFormData, GridLayout } from '../../types';

interface CRMFormBuilderProps {
  formId?: string;
  onSave?: (formData: CRMFormData) => Promise<void>;
  onPreview?: (formData: CRMFormData) => void;
  readOnly?: boolean;
}

export const CRMFormBuilder: React.FC<CRMFormBuilderProps> = ({
  formId,
  onSave,
  onPreview,
  readOnly = false
}) => {
  const [layout, setLayout] = useState<GridLayout | null>(null);
  const [selectedComponent, setSelectedComponent] = useState<string | null>(null);
  const [fieldSelections, setFieldSelections] = useState<Record<string, string[]>>({});
  const [relationships, setRelationships] = useState<Record<string, any>>({});
  const [isPreviewMode, setIsPreviewMode] = useState(false);

  // Custom hook for CRM data management
  const {
    formData,
    loading,
    error,
    updateFormData,
    saveFormData,
    loadFormData
  } = useCRMFormData(formId);

  // Real-time updates subscription
  const { data: realtimeUpdates } = useGraphQLSubscription(
    FORM_UPDATES_SUBSCRIPTION,
    { formId },
    { skip: !formId }
  );

  // Load existing form if formId provided
  useEffect(() => {
    if (formId) {
      loadFormData(formId);
    }
  }, [formId, loadFormData]);

  // Handle real-time updates
  useEffect(() => {
    if (realtimeUpdates?.formUpdated) {
      const { layout: updatedLayout, fieldSelections: updatedFields } = realtimeUpdates.formUpdated;
      setLayout(updatedLayout);
      setFieldSelections(updatedFields);
    }
  }, [realtimeUpdates]);

  const handleComponentDrop = useCallback((componentId: string, position: { x: number; y: number; width: number; height: number }) => {
    if (readOnly) return;

    const newComponent = {
      id: `${componentId}-${Date.now()}`,
      type: componentId,
      position,
      config: crmComponentRegistry.find(c => c.id === componentId)?.defaultProps || {}
    };

    setLayout(prev => ({
      ...prev,
      components: [...(prev?.components || []), newComponent]
    }));

    // Auto-open field selector for new component
    setSelectedComponent(newComponent.id);
  }, [readOnly]);

  const handleFieldSelection = useCallback((componentId: string, fields: string[]) => {
    setFieldSelections(prev => ({
      ...prev,
      [componentId]: fields
    }));

    // Update component config
    setLayout(prev => {
      if (!prev) return prev;
      
      return {
        ...prev,
        components: prev.components.map(comp => 
          comp.id === componentId 
            ? { ...comp, config: { ...comp.config, showFields: fields } }
            : comp
        )
      };
    });
  }, []);

  const handleRelationshipChange = useCallback((relationships: Record<string, any>) => {
    setRelationships(relationships);
    
    // Update form data with relationship mappings
    updateFormData(prev => ({
      ...prev,
      metadata: {
        ...prev.metadata,
        relationships
      }
    }));
  }, [updateFormData]);

  const handleSave = useCallback(async () => {
    if (!layout || readOnly) return;

    const formDataToSave: CRMFormData = {
      ...formData,
      metadata: {
        ...formData.metadata,
        formId: formId || `crm-form-${Date.now()}`,
        version: (formData.metadata?.version || 0) + 1,
        lastModified: new Date()
      }
    };

    try {
      await saveFormData(formDataToSave, layout, fieldSelections, relationships);
      if (onSave) {
        await onSave(formDataToSave);
      }
    } catch (error) {
      console.error('Failed to save CRM form:', error);
    }
  }, [layout, formData, fieldSelections, relationships, formId, readOnly, saveFormData, onSave]);

  const handlePreview = useCallback(() => {
    if (!layout) return;

    const previewData: CRMFormData = {
      ...formData,
      metadata: {
        ...formData.metadata,
        relationships
      }
    };

    setIsPreviewMode(true);
    if (onPreview) {
      onPreview(previewData);
    }
  }, [formData, relationships, layout, onPreview]);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        <span className="ml-3 text-gray-600">Loading CRM form...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-md p-4">
        <h3 className="text-red-800 font-medium">Error loading form</h3>
        <p className="text-red-600 text-sm mt-1">{error.message}</p>
      </div>
    );
  }

  return (
    <DndProvider backend={HTML5Backend}>
      <div className="crm-form-builder h-full flex flex-col">
        {/* Header */}
        <div className="bg-white border-b border-gray-200 px-6 py-4">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-xl font-semibold text-gray-900">CRM Form Builder</h1>
              <p className="text-sm text-gray-500 mt-1">
                Drag components to build your customer form
              </p>
            </div>
            <div className="flex items-center space-x-3">
              <button
                onClick={handlePreview}
                disabled={!layout?.components?.length}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
              >
                Preview
              </button>
              {!readOnly && (
                <button
                  onClick={handleSave}
                  disabled={!layout?.components?.length}
                  className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50"
                >
                  Save Form
                </button>
              )}
            </div>
          </div>
        </div>

        {/* Main Content */}
        <div className="flex-1 flex overflow-hidden">
          {/* Component Palette */}
          {!readOnly && (
            <div className="w-80 bg-gray-50 border-r border-gray-200 overflow-y-auto">
              <ComponentPalette
                components={crmComponentRegistry}
                onComponentSelect={setSelectedComponent}
                selectedComponent={selectedComponent}
              />
              
              {/* Field Selector */}
              {selectedComponent && (
                <FieldSelector
                  componentId={selectedComponent}
                  componentType={layout?.components?.find(c => c.id === selectedComponent)?.type}
                  selectedFields={fieldSelections[selectedComponent] || []}
                  onFieldChange={(fields) => handleFieldSelection(selectedComponent, fields)}
                />
              )}
              
              {/* Relationship Mapper */}
              {layout?.components && layout.components.length > 1 && (
                <RelationshipMapper
                  components={layout.components}
                  relationships={relationships}
                  onChange={handleRelationshipChange}
                />
              )}
            </div>
          )}

          {/* Grid Area */}
          <div className="flex-1 relative">
            {isPreviewMode ? (
              <FormPreview
                formData={formData}
                layout={layout}
                fieldSelections={fieldSelections}
                relationships={relationships}
                onClose={() => setIsPreviewMode(false)}
              />
            ) : (
              <BentoGrid
                layout={layout}
                onLayoutChange={setLayout}
                onComponentDrop={handleComponentDrop}
                onComponentSelect={setSelectedComponent}
                selectedComponent={selectedComponent}
                readOnly={readOnly}
                gridSize={{ width: 12, height: 24 }}
                cellSize={{ width: 120, height: 80 }}
              />
            )}
          </div>
        </div>
      </div>
    </DndProvider>
  );
};
```

### Field Selector Component

```tsx
// core.jarvis.ui.studio/src/components/crm/FieldSelector.tsx

import React, { useMemo } from 'react';
import { CheckIcon } from '@heroicons/react/24/outline';
import { crmComponentRegistry } from './registry';

interface FieldSelectorProps {
  componentId: string;
  componentType?: string;
  selectedFields: string[];
  onFieldChange: (fields: string[]) => void;
}

export const FieldSelector: React.FC<FieldSelectorProps> = ({
  componentId,
  componentType,
  selectedFields,
  onFieldChange
}) => {
  const componentDef = useMemo(() => 
    crmComponentRegistry.find(c => c.id === componentType), 
    [componentType]
  );

  const availableFields = useMemo(() => {
    if (!componentDef?.configSchema?.showFields) return [];
    
    return componentDef.configSchema.showFields.options.map(field => ({
      id: field,
      label: field.charAt(0).toUpperCase() + field.slice(1).replace(/([A-Z])/g, ' $1'),
      description: getFieldDescription(componentType, field),
      required: isFieldRequired(componentType, field)
    }));
  }, [componentDef, componentType]);

  const handleFieldToggle = (fieldId: string) => {
    const newFields = selectedFields.includes(fieldId)
      ? selectedFields.filter(f => f !== fieldId)
      : [...selectedFields, fieldId];
    
    onFieldChange(newFields);
  };

  if (!componentDef) {
    return (
      <div className="p-4 text-sm text-gray-500">
        Select a component to configure fields
      </div>
    );
  }

  return (
    <div className="border-t border-gray-200 bg-white">
      <div className="p-4">
        <h3 className="text-sm font-medium text-gray-900 mb-3">
          Configure Fields
        </h3>
        <p className="text-xs text-gray-500 mb-4">
          Select which fields to show in the {componentDef.name} component
        </p>

        <div className="space-y-2">
          {availableFields.map((field) => (
            <label
              key={field.id}
              className="flex items-start space-x-3 cursor-pointer hover:bg-gray-50 p-2 rounded"
            >
              <div className="flex-shrink-0 mt-0.5">
                <input
                  type="checkbox"
                  checked={selectedFields.includes(field.id)}
                  onChange={() => handleFieldToggle(field.id)}
                  className="sr-only"
                />
                <div className={`
                  w-4 h-4 rounded border-2 flex items-center justify-center
                  ${selectedFields.includes(field.id)
                    ? 'bg-blue-600 border-blue-600 text-white'
                    : 'border-gray-300 bg-white'
                  }
                `}>
                  {selectedFields.includes(field.id) && (
                    <CheckIcon className="w-3 h-3" />
                  )}
                </div>
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center space-x-2">
                  <span className="text-sm font-medium text-gray-900">
                    {field.label}
                  </span>
                  {field.required && (
                    <span className="text-xs text-red-500 font-medium">
                      Required
                    </span>
                  )}
                </div>
                {field.description && (
                  <p className="text-xs text-gray-500 mt-1">
                    {field.description}
                  </p>
                )}
              </div>
            </label>
          ))}
        </div>
      </div>
    </div>
  );
};

// Helper functions
function getFieldDescription(componentType: string, field: string): string {
  const descriptions: Record<string, Record<string, string>> = {
    'crm-account': {
      email: 'Primary email address for the customer',
      firstName: 'Customer\'s first name',
      lastName: 'Customer\'s last name',
      phone: 'Primary phone number',
      company: 'Company or organization name',
      status: 'Current status of the customer account'
    },
    'crm-contact': {
      street: 'Street address',
      city: 'City name',
      state: 'State or province',
      zipCode: 'Postal or ZIP code',
      country: 'Country name',
      preferredContactMethod: 'How the customer prefers to be contacted'
    },
    'crm-activity': {
      type: 'Type of activity or interaction',
      subject: 'Brief description of the activity',
      description: 'Detailed notes about the activity',
      priority: 'Priority level for this activity',
      status: 'Current status of the activity',
      dueDate: 'When the activity should be completed',
      assignedTo: 'Person responsible for this activity'
    }
  };

  return descriptions[componentType]?.[field] || '';
}

function isFieldRequired(componentType: string, field: string): boolean {
  const requiredFields: Record<string, string[]> = {
    'crm-account': ['email', 'firstName', 'lastName'],
    'crm-contact': ['street', 'city', 'state', 'zipCode', 'country'],
    'crm-activity': ['subject', 'priority', 'status', 'assignedTo']
  };

  return requiredFields[componentType]?.includes(field) || false;
}
```

## Data Flow and Integration

### Custom Hook for CRM Data Management

```typescript
// core.jarvis.ui.studio/src/hooks/useCRMFormData.ts

import { useState, useCallback, useRef } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useGraphQLMutation } from './useGraphQLMutation';
import type { CRMFormData, GridLayout } from '../types';

const SAVE_CRM_FORM = `
  mutation SaveCRMForm($input: CRMFormInput!) {
    saveCRMForm(input: $input) {
      id
      formData
      layout
      fieldSelections
      relationships
      version
      lastModified
    }
  }
`;

const LOAD_CRM_FORM = `
  query LoadCRMForm($formId: String!) {
    crmForm(id: $formId) {
      id
      formData
      layout
      fieldSelections
      relationships
      version
      lastModified
    }
  }
`;

export function useCRMFormData(formId?: string) {
  const [formData, setFormData] = useState<CRMFormData>({
    account: {},
    contacts: [],
    activities: [],
    metadata: {
      formId: formId || '',
      version: 1,
      lastModified: new Date(),
      createdBy: '' // Will be set from auth context
    }
  });

  const queryClient = useQueryClient();
  const saveTimeoutRef = useRef<NodeJS.Timeout>();

  // Load existing form data
  const { data: loadedForm, isLoading: loading, error } = useQuery({
    queryKey: ['crm-form', formId],
    queryFn: async () => {
      if (!formId) return null;
      
      const response = await fetch('/api/graphql', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          query: LOAD_CRM_FORM,
          variables: { formId }
        })
      });
      
      const result = await response.json();
      return result.data?.crmForm;
    },
    enabled: !!formId
  });

  // Save form mutation
  const [saveMutation] = useGraphQLMutation(SAVE_CRM_FORM);

  // Update form data
  const updateFormData = useCallback((updater: (prev: CRMFormData) => CRMFormData) => {
    setFormData(prev => {
      const updated = updater(prev);
      
      // Auto-save after 2 seconds of inactivity
      if (saveTimeoutRef.current) {
        clearTimeout(saveTimeoutRef.current);
      }
      
      saveTimeoutRef.current = setTimeout(() => {
        // Auto-save logic here
        console.log('Auto-saving form data...');
      }, 2000);
      
      return updated;
    });
  }, []);

  // Save form data
  const saveFormData = useCallback(async (
    data: CRMFormData,
    layout: GridLayout,
    fieldSelections: Record<string, string[]>,
    relationships: Record<string, any>
  ) => {
    try {
      const result = await saveMutation({
        input: {
          formId: data.metadata.formId,
          formData: data,
          layout,
          fieldSelections,
          relationships
        }
      });

      // Update local state with server response
      if (result.data?.saveCRMForm) {
        setFormData(result.data.saveCRMForm.formData);
        
        // Invalidate and refetch related queries
        queryClient.invalidateQueries({ queryKey: ['crm-form', data.metadata.formId] });
      }

      return result;
    } catch (error) {
      console.error('Failed to save CRM form:', error);
      throw error;
    }
  }, [saveMutation, queryClient]);

  // Load form data
  const loadFormData = useCallback(async (formId: string) => {
    if (loadedForm) {
      setFormData(loadedForm.formData);
      return loadedForm;
    }
    return null;
  }, [loadedForm]);

  return {
    formData,
    loading,
    error,
    updateFormData,
    saveFormData,
    loadFormData
  };
}
```

### GraphQL Integration

```typescript
// core.jarvis.ui.studio/src/hooks/useGraphQLSubscription.ts

import { useEffect, useState } from 'react';
import { createClient } from 'graphql-ws';

const FORM_UPDATES_SUBSCRIPTION = `
  subscription FormUpdates($formId: String!) {
    formUpdated(formId: $formId) {
      formId
      layout
      fieldSelections
      relationships
      lastModified
      updatedBy
    }
  }
`;

export function useGraphQLSubscription(
  subscription: string,
  variables: Record<string, any>,
  options: { skip?: boolean } = {}
) {
  const [data, setData] = useState<any>(null);
  const [error, setError] = useState<Error | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (options.skip) return;

    const client = createClient({
      url: process.env.REACT_APP_GRAPHQL_WS_URL || 'ws://localhost:4000/graphql',
      connectionParams: {
        authorization: `Bearer ${localStorage.getItem('auth_token')}`
      }
    });

    setLoading(true);
    setError(null);

    const unsubscribe = client.subscribe(
      {
        query: subscription,
        variables
      },
      {
        next: (result) => {
          setData(result.data);
          setLoading(false);
        },
        error: (err) => {
          setError(err);
          setLoading(false);
        },
        complete: () => {
          setLoading(false);
        }
      }
    );

    return () => {
      unsubscribe();
      client.dispose();
    };
  }, [subscription, variables, options.skip]);

  return { data, error, loading };
}
```

## API Integration

### Backend GraphQL Schema

```graphql
# CRM Form Schema
type CRMForm {
  id: ID!
  formData: CRMFormData!
  layout: GridLayout!
  fieldSelections: JSON!
  relationships: JSON!
  version: Int!
  lastModified: DateTime!
  createdBy: String!
  createdAt: DateTime!
}

type CRMFormData {
  account: AccountData!
  contacts: [ContactData!]!
  activities: [ActivityData!]!
  metadata: FormMetadata!
}

type AccountData {
  email: String
  firstName: String
  lastName: String
  phone: String
  company: String
  status: AccountStatus
  createdAt: DateTime
  lastContactDate: DateTime
}

type ContactData {
  type: ContactType!
  street: String!
  city: String!
  state: String!
  zipCode: String!
  country: String!
  preferredContactMethod: ContactMethod!
  timezone: String
  language: String
}

type ActivityData {
  type: ActivityType!
  subject: String!
  description: String
  priority: Priority!
  status: ActivityStatus!
  dueDate: DateTime
  completedDate: DateTime
  assignedTo: String!
  tags: [String!]!
}

enum AccountStatus {
  ACTIVE
  INACTIVE
  PROSPECT
}

enum ContactType {
  PRIMARY
  BILLING
  SHIPPING
  OTHER
}

enum ContactMethod {
  EMAIL
  PHONE
  MAIL
}

enum ActivityType {
  CALL
  EMAIL
  MEETING
  NOTE
  TASK
}

enum Priority {
  LOW
  MEDIUM
  HIGH
  URGENT
}

enum ActivityStatus {
  PENDING
  COMPLETED
  CANCELLED
}

input CRMFormInput {
  formId: String!
  formData: CRMFormDataInput!
  layout: GridLayoutInput!
  fieldSelections: JSON!
  relationships: JSON!
}

input CRMFormDataInput {
  account: AccountDataInput!
  contacts: [ContactDataInput!]!
  activities: [ActivityDataInput!]!
  metadata: FormMetadataInput!
}

# Mutations
type Mutation {
  saveCRMForm(input: CRMFormInput!): CRMForm!
  deleteCRMForm(formId: String!): Boolean!
  duplicateCRMForm(formId: String!, newName: String!): CRMForm!
}

# Queries
type Query {
  crmForm(id: String!): CRMForm
  crmForms(limit: Int, offset: Int): [CRMForm!]!
  searchCRMForms(query: String!): [CRMForm!]!
}

# Subscriptions
type Subscription {
  formUpdated(formId: String!): FormUpdateEvent!
}

type FormUpdateEvent {
  formId: String!
  layout: GridLayout!
  fieldSelections: JSON!
  relationships: JSON!
  lastModified: DateTime!
  updatedBy: String!
}
```

### API Service Implementation

```typescript
// core.jarvis.ui.studio/src/services/crmFormService.ts

import { GraphQLClient } from 'graphql-request';
import type { CRMFormData, GridLayout } from '../types';

class CRMFormService {
  private client: GraphQLClient;

  constructor() {
    this.client = new GraphQLClient(
      process.env.REACT_APP_GRAPHQL_URL || '/api/graphql',
      {
        headers: {
          authorization: () => `Bearer ${localStorage.getItem('auth_token')}`
        }
      }
    );
  }

  async saveCRMForm(input: {
    formId: string;
    formData: CRMFormData;
    layout: GridLayout;
    fieldSelections: Record<string, string[]>;
    relationships: Record<string, any>;
  }) {
    const mutation = `
      mutation SaveCRMForm($input: CRMFormInput!) {
        saveCRMForm(input: $input) {
          id
          formData {
            account {
              email
              firstName
              lastName
              phone
              company
              status
            }
            contacts {
              type
              street
              city
              state
              zipCode
              country
              preferredContactMethod
            }
            activities {
              type
              subject
              description
              priority
              status
              dueDate
              assignedTo
              tags
            }
            metadata {
              formId
              version
              lastModified
              createdBy
            }
          }
          layout
          fieldSelections
          relationships
          version
          lastModified
        }
      }
    `;

    return this.client.request(mutation, { input });
  }

  async loadCRMForm(formId: string) {
    const query = `
      query LoadCRMForm($formId: String!) {
        crmForm(id: $formId) {
          id
          formData {
            account {
              email
              firstName
              lastName
              phone
              company
              status
              createdAt
              lastContactDate
            }
            contacts {
              type
              street
              city
              state
              zipCode
              country
              preferredContactMethod
              timezone
              language
            }
            activities {
              type
              subject
              description
              priority
              status
              dueDate
              completedDate
              assignedTo
              tags
            }
            metadata {
              formId
              version
              lastModified
              createdBy
            }
          }
          layout
          fieldSelections
          relationships
          version
          lastModified
          createdBy
          createdAt
        }
      }
    `;

    return this.client.request(query, { formId });
  }

  async submitCRMData(formData: CRMFormData) {
    const mutation = `
      mutation SubmitCRMData($data: CRMFormDataInput!) {
        submitCRMData(data: $data) {
          accountId
          contactIds
          activityIds
          success
          errors {
            field
            message
          }
        }
      }
    `;

    return this.client.request(mutation, { data: formData });
  }

  async validateCRMData(formData: CRMFormData) {
    const mutation = `
      mutation ValidateCRMData($data: CRMFormDataInput!) {
        validateCRMData(data: $data) {
          valid
          errors {
            component
            field
            message
            severity
          }
        }
      }
    `;

    return this.client.request(mutation, { data: formData });
  }

  // Real-time collaboration
  subscribeToFormUpdates(formId: string, callback: (update: any) => void) {
    const subscription = `
      subscription FormUpdates($formId: String!) {
        formUpdated(formId: $formId) {
          formId
          layout
          fieldSelections
          relationships
          lastModified
          updatedBy
        }
      }
    `;

    // Implementation would use WebSocket or Server-Sent Events
    // This is a simplified example
    const eventSource = new EventSource(`/api/forms/${formId}/subscribe`);
    
    eventSource.onmessage = (event) => {
      const update = JSON.parse(event.data);
      callback(update);
    };

    return () => eventSource.close();
  }
}

export const crmFormService = new CRMFormService();
```

## Real-time Updates

### Form Collaboration Component

```tsx
// core.jarvis.ui.studio/src/components/crm/FormCollaboration.tsx

import React, { useEffect, useState } from 'react';
import { useAuth } from '../../contexts/AuthContext';
import { crmFormService } from '../../services/crmFormService';

interface CollaboratorInfo {
  id: string;
  name: string;
  avatar?: string;
  lastActivity: Date;
  isActive: boolean;
}

interface FormCollaborationProps {
  formId: string;
  onUpdate: (update: any) => void;
}

export const FormCollaboration: React.FC<FormCollaborationProps> = ({
  formId,
  onUpdate
}) => {
  const { user } = useAuth();
  const [collaborators, setCollaborators] = useState<CollaboratorInfo[]>([]);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    let unsubscribe: (() => void) | undefined;

    const setupCollaboration = async () => {
      try {
        // Subscribe to form updates
        unsubscribe = crmFormService.subscribeToFormUpdates(formId, (update) => {
          // Filter out own updates to prevent loops
          if (update.updatedBy !== user?.id) {
            onUpdate(update);
            
            // Show notification about the update
            showUpdateNotification(update);
          }
        });

        setIsConnected(true);
      } catch (error) {
        console.error('Failed to setup collaboration:', error);
        setIsConnected(false);
      }
    };

    setupCollaboration();

    return () => {
      if (unsubscribe) {
        unsubscribe();
      }
    };
  }, [formId, user?.id, onUpdate]);

  const showUpdateNotification = (update: any) => {
    // Show a toast notification about the update
    const message = `Form updated by ${update.updatedBy}`;
    
    // You could use a toast library here
    console.log(message, update);
  };

  return (
    <div className="form-collaboration">
      {/* Connection Status */}
      <div className="flex items-center space-x-2 text-sm">
        <div className={`w-2 h-2 rounded-full ${isConnected ? 'bg-green-500' : 'bg-red-500'}`} />
        <span className="text-gray-600">
          {isConnected ? 'Connected' : 'Disconnected'}
        </span>
      </div>

      {/* Active Collaborators */}
      {collaborators.length > 0 && (
        <div className="mt-2">
          <p className="text-xs text-gray-500 mb-2">Active collaborators:</p>
          <div className="flex space-x-2">
            {collaborators.map((collaborator) => (
              <div
                key={collaborator.id}
                className="flex items-center space-x-1 bg-blue-50 rounded-full px-2 py-1"
                title={`${collaborator.name} - Last active: ${collaborator.lastActivity.toLocaleTimeString()}`}
              >
                {collaborator.avatar ? (
                  <img
                    src={collaborator.avatar}
                    alt={collaborator.name}
                    className="w-4 h-4 rounded-full"
                  />
                ) : (
                  <div className="w-4 h-4 rounded-full bg-blue-200 flex items-center justify-center">
                    <span className="text-xs text-blue-800">
                      {collaborator.name.charAt(0)}
                    </span>
                  </div>
                )}
                <span className="text-xs text-blue-800">{collaborator.name}</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
```

## Testing Examples

### Unit Tests

```typescript
// core.jarvis.ui.studio/src/components/crm/__tests__/CRMFormBuilder.test.tsx

import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { DndProvider } from 'react-dnd';
import { HTML5Backend } from 'react-dnd-html5-backend';
import { CRMFormBuilder } from '../CRMFormBuilder';
import { AuthProvider } from '../../../contexts/AuthContext';

// Mock services
jest.mock('../../../services/crmFormService');

const TestWrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false }
    }
  });

  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <DndProvider backend={HTML5Backend}>
          {children}
        </DndProvider>
      </AuthProvider>
    </QueryClientProvider>
  );
};

describe('CRMFormBuilder', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders the form builder interface', () => {
    render(
      <TestWrapper>
        <CRMFormBuilder />
      </TestWrapper>
    );

    expect(screen.getByText('CRM Form Builder')).toBeInTheDocument();
    expect(screen.getByText('Drag components to build your customer form')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Preview' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Save Form' })).toBeDisabled();
  });

  it('enables save and preview buttons when components are added', async () => {
    render(
      <TestWrapper>
        <CRMFormBuilder />
      </TestWrapper>
    );

    // Simulate dragging a component
    const accountComponent = screen.getByText('Account Information');
    fireEvent.dragStart(accountComponent);
    
    // Simulate dropping on grid (this would be more complex in real implementation)
    // For simplicity, we'll directly trigger the drop handler
    const gridArea = screen.getByTestId('bento-grid');
    fireEvent.drop(gridArea);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Preview' })).not.toBeDisabled();
      expect(screen.getByRole('button', { name: 'Save Form' })).not.toBeDisabled();
    });
  });

  it('opens field selector when component is selected', async () => {
    render(
      <TestWrapper>
        <CRMFormBuilder />
      </TestWrapper>
    );

    // Add a component and select it
    // (Implementation details would depend on your specific component structure)
    
    await waitFor(() => {
      expect(screen.getByText('Configure Fields')).toBeInTheDocument();
      expect(screen.getByText('Select which fields to show')).toBeInTheDocument();
    });
  });

  it('saves form data correctly', async () => {
    const mockOnSave = jest.fn();
    
    render(
      <TestWrapper>
        <CRMFormBuilder onSave={mockOnSave} />
      </TestWrapper>
    );

    // Add components and configure them
    // Then click save
    const saveButton = screen.getByRole('button', { name: 'Save Form' });
    fireEvent.click(saveButton);

    await waitFor(() => {
      expect(mockOnSave).toHaveBeenCalledWith(
        expect.objectContaining({
          account: expect.any(Object),
          contacts: expect.any(Array),
          activities: expect.any(Array),
          metadata: expect.objectContaining({
            formId: expect.any(String),
            version: expect.any(Number)
          })
        })
      );
    });
  });
});
```

### Integration Tests

```typescript
// core.jarvis.ui.studio/src/components/crm/__tests__/CRMFormBuilder.integration.test.tsx

import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { CRMFormBuilder } from '../CRMFormBuilder';
import { TestWrapper } from '../../../test/TestWrapper';

// Mock GraphQL responses
const mockGraphQLResponse = {
  crmForm: {
    id: 'test-form-1',
    formData: {
      account: { email: 'test@example.com', firstName: 'John', lastName: 'Doe' },
      contacts: [],
      activities: [],
      metadata: { formId: 'test-form-1', version: 1, lastModified: new Date() }
    },
    layout: {
      components: [
        {
          id: 'account-1',
          type: 'crm-account',
          position: { x: 0, y: 0, width: 4, height: 3 },
          config: { showFields: ['email', 'firstName', 'lastName'] }
        }
      ]
    },
    fieldSelections: {
      'account-1': ['email', 'firstName', 'lastName']
    },
    relationships: {}
  }
};

describe('CRMFormBuilder Integration', () => {
  beforeEach(() => {
    // Mock fetch responses
    global.fetch = jest.fn((url, options) => {
      if (url.includes('/api/graphql')) {
        const body = JSON.parse(options?.body as string);
        
        if (body.query.includes('LoadCRMForm')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve({ data: mockGraphQLResponse })
          });
        }
        
        if (body.query.includes('SaveCRMForm')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve({
              data: {
                saveCRMForm: {
                  ...mockGraphQLResponse.crmForm,
                  version: mockGraphQLResponse.crmForm.formData.metadata.version + 1
                }
              }
            })
          });
        }
      }
      
      return Promise.resolve({
        ok: false,
        json: () => Promise.resolve({ error: 'Not found' })
      });
    }) as jest.Mock;
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  it('loads existing form data correctly', async () => {
    render(
      <TestWrapper>
        <CRMFormBuilder formId="test-form-1" />
      </TestWrapper>
    );

    // Wait for loading to complete
    await waitFor(() => {
      expect(screen.queryByText('Loading CRM form...')).not.toBeInTheDocument();
    });

    // Verify the form loaded correctly
    expect(screen.getByDisplayValue('test@example.com')).toBeInTheDocument();
    expect(screen.getByDisplayValue('John')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Doe')).toBeInTheDocument();
  });

  it('handles form submission end-to-end', async () => {
    const user = userEvent.setup();
    const mockOnSave = jest.fn();

    render(
      <TestWrapper>
        <CRMFormBuilder onSave={mockOnSave} />
      </TestWrapper>
    );

    // Add an account component
    const accountComponent = screen.getByText('Account Information');
    await user.click(accountComponent);
    
    // Drag to grid (simplified for test)
    const grid = screen.getByTestId('bento-grid');
    fireEvent.drop(grid, {
      dataTransfer: {
        getData: () => 'crm-account'
      }
    });

    // Fill in form data
    const emailInput = screen.getByPlaceholderText('Email');
    await user.type(emailInput, 'new@example.com');

    const firstNameInput = screen.getByPlaceholderText('First Name');
    await user.type(firstNameInput, 'Jane');

    // Save the form
    const saveButton = screen.getByRole('button', { name: 'Save Form' });
    await user.click(saveButton);

    // Verify the save was called
    await waitFor(() => {
      expect(mockOnSave).toHaveBeenCalledWith(
        expect.objectContaining({
          account: expect.objectContaining({
            email: 'new@example.com',
            firstName: 'Jane'
          })
        })
      );
    });
  });

  it('handles real-time collaboration updates', async () => {
    // Mock WebSocket or EventSource for real-time updates
    const mockEventSource = {
      addEventListener: jest.fn(),
      removeEventListener: jest.fn(),
      close: jest.fn()
    };

    global.EventSource = jest.fn(() => mockEventSource) as any;

    render(
      <TestWrapper>
        <CRMFormBuilder formId="test-form-1" />
      </TestWrapper>
    );

    // Simulate receiving a real-time update
    const updateEvent = new MessageEvent('message', {
      data: JSON.stringify({
        formId: 'test-form-1',
        layout: { components: [] },
        fieldSelections: {},
        updatedBy: 'other-user'
      })
    });

    // Trigger the update
    const messageHandler = mockEventSource.addEventListener.mock.calls
      .find(call => call[0] === 'message')?.[1];
    
    if (messageHandler) {
      messageHandler(updateEvent);
    }

    // Verify the form updated
    await waitFor(() => {
      // Check that the form reflects the real-time update
      expect(screen.getByText(/updated by other-user/i)).toBeInTheDocument();
    });
  });
});
```

## Summary

This comprehensive example demonstrates:

1. **Complete Component Architecture**: Fully typed TypeScript interfaces for CRM components with validation and relationships
2. **Dynamic Form Building**: Drag-and-drop interface for creating forms with field selection and configuration
3. **Real-time Collaboration**: WebSocket integration for live updates and multi-user editing
4. **GraphQL Integration**: Complete schema definition and service implementation
5. **Data Persistence**: Form saving, loading, and versioning
6. **Testing Strategy**: Both unit and integration tests covering the full user journey

The implementation showcases how the Bento Grid System enables rapid creation of complex, data-driven forms while maintaining type safety, real-time collaboration, and a excellent user experience.

Key benefits demonstrated:

- **Developer Productivity**: Reusable components with clear interfaces
- **User Empowerment**: No-code form building with powerful customization
- **Real-time Collaboration**: Multiple users can work on forms simultaneously
- **Type Safety**: Full TypeScript coverage prevents runtime errors
- **Scalability**: Component-based architecture supports growth
- **Testability**: Comprehensive test coverage ensures reliability

This example serves as a blueprint for implementing similar systems across different domains and use cases.