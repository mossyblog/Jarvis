/**
 * Page Creation Modal Stories
 * 
 * Storybook stories demonstrating the PageCreationModal component
 * with various configurations and states.
 */

import type { Meta, StoryObj } from '@storybook/react';
import { action } from '@storybook/addon-actions';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { PageCreationModal } from './PageCreationModal';
import { Button } from '../ui/button';
import { useState } from 'react';

// Create a QueryClient for stories
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      staleTime: Infinity,
    },
  },
});

const meta: Meta<typeof PageCreationModal> = {
  title: 'Pages/PageCreationModal',
  component: PageCreationModal,
  parameters: {
    layout: 'centered',
    docs: {
      description: {
        component: `
The PageCreationModal is a comprehensive multi-step wizard for creating new pages.

## Features

- **3-Step Wizard**: Basic Info, Template Selection, Configuration
- **Form Validation**: Real-time validation with React Hook Form + Zod
- **Template Integration**: Browse and preview available templates
- **REST API Integration**: Full CRUD operations with error handling
- **Responsive Design**: Works on all screen sizes
- **Accessibility**: Full keyboard navigation and screen reader support

## Usage

The modal guides users through three sequential steps:

1. **Basic Information**: Page name, URL slug, type, description, and tags
2. **Template Selection**: Choose from available templates or start from scratch
3. **Configuration**: Advanced settings and review

## API Integration

Uses the UIStudio API client for:
- Fetching templates
- Creating pages
- Applying templates
- Error handling and retries
        `,
      },
    },
  },
  decorators: [
    (Story) => (
      <QueryClientProvider client={queryClient}>
        <div className="min-h-screen bg-background">
          <Story />
        </div>
      </QueryClientProvider>
    ),
  ],
  argTypes: {
    isOpen: {
      control: 'boolean',
      description: 'Whether the modal is open',
    },
    userEntityId: {
      control: 'text',
      description: 'Current user entity ID for ownership',
    },
    initialPageType: {
      control: { type: 'select' },
      options: ['static', 'dynamic'],
      description: 'Initial page type selection',
    },
    onClose: {
      action: 'closed',
      description: 'Called when modal is closed',
    },
    onPageCreated: {
      action: 'pageCreated',
      description: 'Called when page is successfully created',
    },
    onError: {
      action: 'error',
      description: 'Called when an error occurs',
    },
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof PageCreationModal>;

// ============================================================================
// Interactive Wrapper Component
// ============================================================================

interface InteractiveWrapperProps {
  initialPageType?: 'static' | 'dynamic';
  userEntityId: string;
}

const InteractiveWrapper: React.FC<InteractiveWrapperProps> = ({ 
  initialPageType = 'static',
  userEntityId 
}) => {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <div className="flex flex-col items-center justify-center min-h-[400px] gap-4">
      <Button onClick={() => setIsOpen(true)}>
        Create New Page
      </Button>
      
      <PageCreationModal
        isOpen={isOpen}
        onClose={() => setIsOpen(false)}
        onPageCreated={action('pageCreated')}
        onError={action('error')}
        userEntityId={userEntityId}
        initialPageType={initialPageType}
      />
    </div>
  );
};

// ============================================================================
// Story Definitions
// ============================================================================

/**
 * Default state of the PageCreationModal.
 * Shows the modal trigger and demonstrates the complete workflow.
 */
export const Default: Story = {
  render: () => (
    <InteractiveWrapper userEntityId="user-123" />
  ),
};

/**
 * Modal configured for static page creation.
 * Demonstrates the static page workflow with template selection.
 */
export const StaticPage: Story = {
  render: () => (
    <InteractiveWrapper 
      userEntityId="user-123" 
      initialPageType="static"
    />
  ),
  parameters: {
    docs: {
      description: {
        story: 'Configured for creating static pages with fixed content and layout.',
      },
    },
  },
};

/**
 * Modal configured for dynamic page creation.
 * Demonstrates the dynamic page workflow with data-driven features.
 */
export const DynamicPage: Story = {
  render: () => (
    <InteractiveWrapper 
      userEntityId="user-123" 
      initialPageType="dynamic"
    />
  ),
  parameters: {
    docs: {
      description: {
        story: 'Configured for creating dynamic pages with data-driven content.',
      },
    },
  },
};

/**
 * Modal opened directly without trigger.
 * Useful for testing the modal interface directly.
 */
export const OpenModal: Story = {
  args: {
    isOpen: true,
    userEntityId: 'user-123',
    initialPageType: 'static',
    onClose: action('onClose'),
    onPageCreated: action('onPageCreated'),
    onError: action('onError'),
  },
  parameters: {
    docs: {
      description: {
        story: 'Modal shown in open state for direct interface testing.',
      },
    },
  },
};

/**
 * Modal opened at Step 1 (Template Selection).
 * Demonstrates the template selection interface.
 */
export const TemplateSelection: Story = {
  args: {
    isOpen: true,
    userEntityId: 'user-123',
    initialPageType: 'static',
    onClose: action('onClose'),
    onPageCreated: action('onPageCreated'),
    onError: action('onError'),
  },
  parameters: {
    docs: {
      description: {
        story: 'Focus on the template selection step with preview functionality.',
      },
    },
  },
};

/**
 * Modal with different user context.
 * Shows how the modal adapts to different users and their templates.
 */
export const DifferentUser: Story = {
  render: () => (
    <InteractiveWrapper userEntityId="admin-456" />
  ),
  parameters: {
    docs: {
      description: {
        story: 'Modal with a different user context, showing user-specific templates.',
      },
    },
  },
};

/**
 * Demonstrates the complete workflow from start to finish.
 * Shows all steps and the progression through the wizard.
 */
export const CompleteWorkflow: Story = {
  render: () => {
    const [isOpen, setIsOpen] = useState(false);
    const [step, setStep] = useState(0);

    return (
      <div className="flex flex-col items-center justify-center min-h-[500px] gap-6">
        <div className="text-center">
          <h3 className="text-lg font-semibold mb-2">Page Creation Workflow</h3>
          <p className="text-sm text-muted-foreground mb-4">
            This demonstrates the complete 3-step process for creating a new page.
          </p>
          
          <div className="flex items-center justify-center gap-2 mb-4">
            {['Basic Info', 'Template', 'Configuration'].map((stepName, index) => (
              <div key={stepName} className="flex items-center">
                <div className={`w-lg h-lg rounded-full border-2 flex items-center justify-center text-xs ${
                  index <= step ? 'border-primary bg-primary text-primary-foreground' : 'border-muted-foreground'
                }`}>
                  {index + 1}
                </div>
                {index < 2 && <div className="w-lg h-px bg-muted-foreground mx-2" />}
              </div>
            ))}
          </div>
        </div>

        <Button onClick={() => setIsOpen(true)}>
          Start Page Creation
        </Button>
        
        <PageCreationModal
          isOpen={isOpen}
          onClose={() => {
            setIsOpen(false);
            setStep(0);
          }}
          onPageCreated={(page) => {
            action('pageCreated')(page);
            setIsOpen(false);
            setStep(0);
          }}
          onError={action('error')}
          userEntityId="user-123"
          initialPageType="static"
        />
      </div>
    );
  },
  parameters: {
    docs: {
      description: {
        story: 'Complete workflow demonstration showing all steps and progression.',
      },
    },
  },
};

/**
 * Mobile responsive version of the modal.
 * Shows how the modal adapts to smaller screen sizes.
 */
export const MobileView: Story = {
  args: {
    isOpen: true,
    userEntityId: 'user-123',
    initialPageType: 'static',
    onClose: action('onClose'),
    onPageCreated: action('onPageCreated'),
    onError: action('onError'),
  },
  parameters: {
    viewport: {
      defaultViewport: 'mobile1',
    },
    docs: {
      description: {
        story: 'Mobile-optimized view showing responsive design adaptations.',
      },
    },
  },
};

/**
 * Tablet responsive version of the modal.
 * Shows the modal on medium-sized screens.
 */
export const TabletView: Story = {
  args: {
    isOpen: true,
    userEntityId: 'user-123',
    initialPageType: 'static',
    onClose: action('onClose'),
    onPageCreated: action('onPageCreated'),
    onError: action('onError'),
  },
  parameters: {
    viewport: {
      defaultViewport: 'tablet',
    },
    docs: {
      description: {
        story: 'Tablet-optimized view showing responsive design for medium screens.',
      },
    },
  },
};

/**
 * Demonstrates error handling scenarios.
 * Shows how the modal handles API errors and validation failures.
 */
export const ErrorHandling: Story = {
  render: () => {
    const [isOpen, setIsOpen] = useState(false);

    return (
      <div className="flex flex-col items-center justify-center min-h-[400px] gap-4">
        <div className="text-center">
          <h3 className="text-lg font-semibold mb-2">Error Handling</h3>
          <p className="text-sm text-muted-foreground mb-4">
            This story demonstrates various error scenarios and validation.
          </p>
        </div>

        <Button onClick={() => setIsOpen(true)}>
          Test Error Scenarios
        </Button>
        
        <PageCreationModal
          isOpen={isOpen}
          onClose={() => setIsOpen(false)}
          onPageCreated={action('pageCreated')}
          onError={(error) => {
            console.error('Error in story:', error);
            action('error')(error);
          }}
          userEntityId="user-123"
          initialPageType="static"
        />
      </div>
    );
  },
  parameters: {
    docs: {
      description: {
        story: 'Demonstrates error handling, validation failures, and network errors.',
      },
    },
  },
};

/**
 * Accessibility testing version.
 * Focused on keyboard navigation and screen reader compatibility.
 */
export const AccessibilityTest: Story = {
  args: {
    isOpen: true,
    userEntityId: 'user-123',
    initialPageType: 'static',
    onClose: action('onClose'),
    onPageCreated: action('onPageCreated'),
    onError: action('onError'),
  },
  parameters: {
    docs: {
      description: {
        story: 'Accessibility-focused version for testing keyboard navigation and screen readers.',
      },
    },
    a11y: {
      config: {
        rules: [
          {
            id: 'color-contrast',
            enabled: true,
          },
          {
            id: 'keyboard-navigation',
            enabled: true,
          },
        ],
      },
    },
  },
};