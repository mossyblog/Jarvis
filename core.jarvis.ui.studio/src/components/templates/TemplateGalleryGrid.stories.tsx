/**
 * Template Gallery Grid Storybook Stories
 * 
 * Visual documentation and testing for the Template Gallery Grid component
 * 
 * @module TemplateGalleryGridStories
 */

import type { Meta, StoryObj } from '@storybook/react';
import { action } from '@storybook/addon-actions';
import { TemplateGalleryGrid } from './TemplateGalleryGrid';

const meta: Meta<typeof TemplateGalleryGrid> = {
  title: 'Templates/TemplateGalleryGrid',
  component: TemplateGalleryGrid,
  parameters: {
    layout: 'fullscreen',
    docs: {
      description: {
        component: `
The Template Gallery Grid component provides a visual template selection interface with:

- Grid and list view modes
- Advanced filtering and search capabilities  
- Template preview modal with detailed information
- Template application functionality
- Responsive mobile-first design
- Keyboard navigation support
- Accessibility compliance

Features include template metadata display (rating, usage count, tags), 
visual thumbnails, and smooth user interactions for template discovery and application.
        `,
      },
    },
  },
  tags: ['autodocs'],
  argTypes: {
    userEntityId: {
      control: 'text',
      description: 'Current user entity ID for data filtering',
    },
    initialView: {
      control: { type: 'radio' },
      options: ['grid', 'list'],
      description: 'Initial view mode for the gallery',
    },
    isOpen: {
      control: 'boolean',
      description: 'Whether the gallery is open/visible',
    },
    isLoading: {
      control: 'boolean',
      description: 'Loading state override',
    },
    error: {
      control: 'text',
      description: 'Error message override',
    },
    className: {
      control: 'text',
      description: 'Additional CSS classes',
    },
  },
};

export default meta;
type Story = StoryObj<typeof TemplateGalleryGrid>;

// Default story showing the gallery in normal state
export const Default: Story = {
  args: {
    userEntityId: 'user-1',
    isOpen: true,
    onTemplateApply: action('template-applied'),
    onClose: action('gallery-closed'),
  },
};

// Grid view story
export const GridView: Story = {
  args: {
    userEntityId: 'user-1',
    initialView: 'grid',
    isOpen: true,
    onTemplateApply: action('template-applied'),
    onClose: action('gallery-closed'),
  },
};

// List view story
export const ListView: Story = {
  args: {
    userEntityId: 'user-1',
    initialView: 'list',
    isOpen: true,
    onTemplateApply: action('template-applied'),
    onClose: action('gallery-closed'),
  },
};

// Loading state story
export const Loading: Story = {
  args: {
    userEntityId: 'user-1',
    isOpen: true,
    isLoading: true,
    onTemplateApply: action('template-applied'),
    onClose: action('gallery-closed'),
  },
};

// Error state story
export const Error: Story = {
  args: {
    userEntityId: 'user-1',
    isOpen: true,
    error: 'Failed to load templates. Please check your connection and try again.',
    onTemplateApply: action('template-applied'),
    onClose: action('gallery-closed'),
  },
};

// Closed/Hidden state story
export const Closed: Story = {
  args: {
    userEntityId: 'user-1',
    isOpen: false,
    onTemplateApply: action('template-applied'),
    onClose: action('gallery-closed'),
  },
};

// With pre-applied filters story
export const WithFilters: Story = {
  args: {
    userEntityId: 'user-1',
    isOpen: true,
    initialFilters: {
      search: 'dashboard',
      templateType: 'page',
      category: 'Dashboard',
      visibility: 'public',
      sortBy: 'usage',
      sortDirection: 'desc',
      tags: ['analytics'],
    },
    onTemplateApply: action('template-applied'),
    onClose: action('gallery-closed'),
  },
};

// Mobile responsive story (smaller viewport)
export const Mobile: Story = {
  args: {
    userEntityId: 'user-1',
    isOpen: true,
    onTemplateApply: action('template-applied'),
    onClose: action('gallery-closed'),
  },
  parameters: {
    viewport: {
      defaultViewport: 'mobile1',
    },
  },
};

// Tablet responsive story
export const Tablet: Story = {
  args: {
    userEntityId: 'user-1',
    isOpen: true,
    onTemplateApply: action('template-applied'),
    onClose: action('gallery-closed'),
  },
  parameters: {
    viewport: {
      defaultViewport: 'tablet',
    },
  },
};

// Dark mode story
export const DarkMode: Story = {
  args: {
    userEntityId: 'user-1',
    isOpen: true,
    onTemplateApply: action('template-applied'),
    onClose: action('gallery-closed'),
  },
  parameters: {
    backgrounds: {
      default: 'dark',
    },
  },
};