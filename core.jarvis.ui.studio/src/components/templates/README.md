# Template Components

This directory contains template-related components for the UIStudio interface.

## Components

### TemplateGalleryGrid

A comprehensive template gallery component that provides visual template selection with advanced filtering, search, and preview capabilities.

#### Features

- **Visual Layout**: Grid and list view modes with responsive design
- **Template Discovery**: Advanced search and filtering across multiple dimensions
- **Template Preview**: Full-screen preview modal with detailed information
- **Template Application**: Integrated workflow for applying templates to create new pages
- **Mobile Support**: Mobile-first responsive design with touch optimization
- **Accessibility**: Full keyboard navigation and screen reader support

#### Usage

```tsx
import { TemplateGalleryGrid } from './components/templates';

function MyComponent() {
  return (
    <TemplateGalleryGrid
      userEntityId="current-user-id"
      isOpen={true}
      onTemplateApply={(template, pageName) => {
        console.log('Template applied:', template.templateName);
      }}
      onClose={() => {
        console.log('Gallery closed');
      }}
    />
  );
}
```

#### Props

- `userEntityId` (required): Current user entity ID for data filtering
- `isOpen`: Whether the gallery is visible
- `initialView`: Initial view mode ('grid' | 'list')
- `initialFilters`: Pre-applied filter configuration
- `onTemplateApply`: Callback when a template is applied
- `onClose`: Callback when the gallery is closed
- `className`: Additional CSS classes
- `isLoading`: Loading state override
- `error`: Error message override

#### Integration

The TemplateGalleryGrid is integrated into the UIStudioInterface component as a modal dialog. Users can access it through the "Browse Templates" button in the main interface.

#### Mock Data

The component includes 6 mock templates representing different categories:
- Modern Dashboard (Business/Analytics)
- E-commerce Product Grid (E-commerce)
- Blog Layout (Content/Blog)
- Data Table Component (Data Display)
- Landing Page Hero (Marketing)
- Admin Panel Layout (Administration)

#### Storybook

View the component in Storybook at: `Templates/TemplateGalleryGrid`

Stories include:
- Default gallery view
- Grid vs List view comparison
- Loading and error states
- Responsive behavior
- Dark mode support

#### Development

To extend the template gallery:

1. **Add New Templates**: Modify the `MOCK_TEMPLATES` array in `TemplateGalleryGrid.tsx`
2. **Add Filters**: Extend the `TemplateFilters` interface and filtering logic
3. **Customize Layout**: Modify the responsive grid classes and card layouts
4. **Add Features**: Follow the established patterns for state management and event handling

#### Dependencies

- React 19+
- TypeScript
- @radix-ui components (Dialog, Select, etc.)
- Lucide React icons
- Tailwind CSS for styling
- Existing UIStudio hooks and services