# UI Studio Workflow Testing Guide

This document provides comprehensive testing procedures for the UI Studio end-to-end workflow from discovery to page creation.

## 🎯 Complete Workflow Overview

The UI Studio workflow consists of the following major steps:

1. **Template Discovery** - Browse and select templates
2. **Page Creation** - Create pages from templates or blank
3. **Layout Design** - Use the Bento Grid system for layout
4. **Component Addition** - Drag & drop components from palette
5. **Component Configuration** - Configure properties and styling
6. **Data Binding** - Connect components to ECS data sources
7. **Field Mapping** - Map ECS fields to component properties
8. **Testing & Preview** - Test functionality and responsive design
9. **Publishing** - Deploy pages to different environments

## 🧪 Testing Scenarios

### Scenario 1: Template-Based Page Creation

**Objective**: Create a page using a pre-built template

**Steps**:
1. Navigate to Templates page (`/templates`)
2. Browse template categories (Dashboard, Forms, Landing, etc.)
3. Select "Analytics Dashboard" template
4. Click "Use This Template" 
5. Fill in page details:
   - Page Name: "My Analytics Dashboard"
   - Route: "/my-dashboard"
   - Description: "Custom analytics dashboard"
6. Click "Create Page"
7. Verify page is created and loaded in edit mode

**Expected Results**:
- Template browser loads with categories
- Template preview shows correctly
- Page creation form accepts input
- New page loads with template components
- Edit mode is automatically enabled
- Page appears in navigation

### Scenario 2: Blank Page Creation with Component Addition

**Objective**: Create a blank page and add components manually

**Steps**:
1. Navigate to Templates page
2. Click "Start from Scratch" 
3. Create blank page:
   - Page Name: "Custom Page"
   - Route: "/custom"
4. Enter edit mode (should be automatic)
5. Open component palette from toolbar
6. Drag "Metric Card" component to grid
7. Drag "Chart" component to grid
8. Resize and position components

**Expected Results**:
- Blank page is created successfully
- Edit mode toolbar appears
- Component palette shows categorized components
- Drag and drop works smoothly
- Visual feedback during drag operations
- Components snap to grid positions
- Resize handles work correctly

### Scenario 3: Component Configuration and Styling

**Objective**: Configure component properties and styling

**Steps**:
1. Select a metric card component
2. Open component properties panel
3. Modify basic properties:
   - Title: "Revenue"
   - Value: "125,000"
   - Change: "+12.5%"
4. Adjust styling:
   - Padding: 24px
   - Border radius: 12px
   - Background color: Light blue
5. Save changes

**Expected Results**:
- Component selection works
- Properties panel opens with correct component data
- Property changes reflect immediately
- Styling changes apply visually
- Changes are marked as unsaved
- Save functionality works

### Scenario 4: ECS Data Binding

**Objective**: Connect components to ECS data sources

**Steps**:
1. Select a component
2. Open Component Binding Interface
3. Browse ECS components
4. Select "UserComponent" 
5. Configure field mappings:
   - Map `firstName` to component `title`
   - Map `email` to component `subtitle`
   - Map `isActive` to component `status`
6. Test the binding
7. Save binding configuration

**Expected Results**:
- ECS component browser loads
- Component selection works
- Field mapping interface appears
- Auto-mapping suggestions work
- Test binding returns sample data
- Binding saves successfully

### Scenario 5: Responsive Design Testing

**Objective**: Test responsive behavior across device sizes

**Steps**:
1. Use device selector in toolbar
2. Switch between Desktop, Tablet, Mobile views
3. Verify component layouts adapt
4. Check component visibility and sizing
5. Test component interactions on each device

**Expected Results**:
- Device selector works smoothly
- Grid adapts to different column counts
- Components resize appropriately
- Navigation remains functional
- Touch interactions work on mobile

### Scenario 6: Save and Publish Workflow

**Objective**: Save changes and publish page

**Steps**:
1. Make several changes to the page
2. Verify "Unsaved" indicator appears
3. Click Save button
4. Verify changes are saved
5. Open publish dialog
6. Configure publish settings:
   - Environment: Development
   - Make public: No
   - Optimize assets: Yes
7. Publish page
8. Verify publish success

**Expected Results**:
- Unsaved changes indicator works
- Save operation completes successfully
- Publish dialog opens with options
- Publish process completes
- Success notification appears
- Published URL is provided

## 🔧 Integration Testing

### API Integration Tests

**Template API**:
- [ ] `getTemplates()` returns template list
- [ ] `getTemplate(id)` returns specific template
- [ ] `createPageFromTemplate()` creates page correctly

**Page API**:
- [ ] `savePage()` persists page data
- [ ] `loadPage()` retrieves page data
- [ ] `publishPage()` deploys to environment

**Binding API**:
- [ ] `saveBinding()` stores component bindings
- [ ] `testBinding()` validates data connections

### Data Flow Tests

**Page Creation Flow**:
```
Template Selection → Page Creation → Edit Mode → Component Addition → Configuration → Data Binding → Save → Publish
```

**Component Lifecycle**:
```
Palette → Drag → Drop → Configure → Bind Data → Test → Save
```

**Data Binding Flow**:
```
Select Component → Choose ECS Component → Map Fields → Test Connection → Deploy
```

## 🐛 Error Handling Tests

### Network Errors
- [ ] Template loading failure
- [ ] Save operation failure  
- [ ] Publish operation failure
- [ ] Binding test failure

### Validation Errors
- [ ] Invalid page name
- [ ] Duplicate route
- [ ] Required field mapping missing
- [ ] Invalid component configuration

### User Experience Errors
- [ ] Drag operation outside valid area
- [ ] Component collision detection
- [ ] Unsaved changes warning
- [ ] Permission denied scenarios

## 📱 Device and Browser Testing

### Device Types
- [ ] Desktop (1920x1080)
- [ ] Tablet (768x1024) 
- [ ] Mobile (375x667)

### Browser Compatibility
- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)

### Touch Interactions
- [ ] Touch drag and drop
- [ ] Pinch to zoom
- [ ] Scroll behavior
- [ ] Touch component selection

## ⚡ Performance Testing

### Load Times
- [ ] Template browser opens < 2s
- [ ] Page creation completes < 3s
- [ ] Component drag response < 100ms
- [ ] Save operation completes < 5s

### Memory Usage
- [ ] No memory leaks during long sessions
- [ ] Component cleanup on page navigation
- [ ] Event listener cleanup

### Bundle Size
- [ ] Initial bundle size reasonable
- [ ] Code splitting working
- [ ] Lazy loading components

## 🎨 Visual Testing

### Layout Consistency
- [ ] Grid alignment correct
- [ ] Component spacing consistent
- [ ] Typography consistent
- [ ] Color scheme consistent

### Animation Quality
- [ ] Smooth drag animations
- [ ] Component transitions
- [ ] Loading states
- [ ] Hover effects

### Accessibility
- [ ] Keyboard navigation works
- [ ] Screen reader compatibility
- [ ] Color contrast sufficient
- [ ] Focus indicators visible

## 📊 Testing Checklist

### High Priority ✅
- [x] Drag & drop from palette to grid canvas
- [x] Device selector with responsive preview modes  
- [x] Layout toolbar with edit controls (save, preview, publish)
- [x] Component properties panel for selected grid components
- [x] Component Binding Interface for ECS data connection
- [x] Field Mapping Editor to map component props to ECS fields
- [x] Save/publish functionality with UIStudio API integration
- [x] Template fetching from UIStudio API endpoints

### Medium Priority 🔄
- [x] Build verification ensuring no compilation errors
- [ ] Complete workflow testing from discovery to page creation
- [ ] Responsive behavior verification across all device sizes

### Low Priority ⏳
- [ ] API integration testing with real backend endpoints
- [ ] All wireframe elements implementation verification

## 🚀 Next Steps

After completing these tests:

1. **Bug Fixes**: Address any issues found during testing
2. **Performance Optimization**: Optimize any slow operations
3. **Documentation**: Update user documentation
4. **Training**: Create user training materials
5. **Deployment**: Prepare for production deployment

## 📝 Test Results Template

For each test scenario, record:

- **Test Date**: 
- **Tester**: 
- **Environment**: 
- **Result**: Pass/Fail
- **Issues Found**: 
- **Screenshots**: 
- **Notes**: 

---

**Note**: This testing guide should be executed by the development team and stakeholders before considering the UI Studio feature complete.