# Dynamic Page Creation with Bento Grid

## Overview

The Bento Grid system enables dynamic page creation through a powerful field-driven approach that leverages **shadcn/ui components** and **Tailwind CSS** for consistent, accessible, and beautiful interfaces. This document outlines how to create dynamic pages using our component system.

## Core Concepts

### Field-Based Architecture

Dynamic pages are built by defining field configurations that automatically generate the appropriate shadcn/ui components:

```typescript
interface FieldConfig {
  id: string;
  type: 'text' | 'select' | 'textarea' | 'number' | 'date' | 'boolean' | 'file';
  label: string;
  placeholder?: string;
  required?: boolean;
  validation?: FieldValidation;
  options?: SelectOption[]; // For select fields
  className?: string; // Tailwind classes
}
```

## shadcn/ui Component Selection

### Field Type to Component Mapping

Our system automatically selects the appropriate shadcn/ui component based on field type:

```typescript
const FIELD_COMPONENT_MAP = {
  text: Input,
  email: Input,
  password: Input,
  number: Input,
  textarea: Textarea,
  select: Select,
  checkbox: Checkbox,
  switch: Switch,
  date: DatePicker,
  file: FileUpload,
  radio: RadioGroup,
  combobox: Combobox,
} as const;
```

### Component Import Structure

All components are imported from our shadcn/ui library:

```typescript
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { cn } from "@/lib/utils";
```

## Dynamic Form Generation

### Basic Form Setup with shadcn Form

```typescript
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";

interface DynamicFormProps {
  fields: FieldConfig[];
  onSubmit: (data: any) => void;
  className?: string;
}

const DynamicForm: React.FC<DynamicFormProps> = ({ 
  fields, 
  onSubmit, 
  className 
}) => {
  // Generate dynamic schema based on field configurations
  const schema = generateValidationSchema(fields);
  
  const form = useForm<z.infer<typeof schema>>({
    resolver: zodResolver(schema),
    defaultValues: getDefaultValues(fields),
  });

  return (
    <Card className={cn("w-full max-w-2xl mx-auto", className)}>
      <CardHeader>
        <CardTitle className="text-2xl font-bold">Dynamic Form</CardTitle>
        <CardDescription>
          Fill out the form below with the required information
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
            {fields.map((field) => (
              <DynamicField 
                key={field.id} 
                field={field} 
                form={form} 
              />
            ))}
            
            <Separator className="my-6" />
            
            <div className="flex justify-end space-x-4">
              <Button 
                type="button" 
                variant="outline" 
                onClick={() => form.reset()}
                className="w-24"
              >
                Reset
              </Button>
              <Button 
                type="submit" 
                className="w-24"
                disabled={form.formState.isSubmitting}
              >
                {form.formState.isSubmitting ? "Saving..." : "Submit"}
              </Button>
            </div>
          </form>
        </Form>
      </CardContent>
    </Card>
  );
};
```

### Dynamic Field Renderer

```typescript
const DynamicField: React.FC<{
  field: FieldConfig;
  form: UseFormReturn<any>;
}> = ({ field, form }) => {
  return (
    <FormField
      control={form.control}
      name={field.id}
      render={({ field: formField }) => (
        <FormItem className={cn("space-y-2", field.className)}>
          <FormLabel className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">
            {field.label}
            {field.required && (
              <span className="text-destructive ml-1">*</span>
            )}
          </FormLabel>
          
          <FormControl>
            {renderFieldComponent(field, formField)}
          </FormControl>
          
          {field.description && (
            <FormDescription className="text-sm text-muted-foreground">
              {field.description}
            </FormDescription>
          )}
          
          <FormMessage className="text-sm font-medium text-destructive" />
        </FormItem>
      )}
    />
  );
};
```

### Field Component Rendering

```typescript
const renderFieldComponent = (field: FieldConfig, formField: any) => {
  const baseClasses = "w-full transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2";
  
  switch (field.type) {
    case 'text':
    case 'email':
    case 'password':
    case 'number':
      return (
        <Input
          {...formField}
          type={field.type}
          placeholder={field.placeholder}
          className={cn(baseClasses, field.className)}
          disabled={field.disabled}
        />
      );
      
    case 'textarea':
      return (
        <Textarea
          {...formField}
          placeholder={field.placeholder}
          className={cn(baseClasses, "min-h-[80px] resize-none", field.className)}
          disabled={field.disabled}
        />
      );
      
    case 'select':
      return (
        <Select onValueChange={formField.onChange} value={formField.value}>
          <SelectTrigger className={cn(baseClasses, field.className)}>
            <SelectValue placeholder={field.placeholder || "Select an option"} />
          </SelectTrigger>
          <SelectContent>
            {field.options?.map((option) => (
              <SelectItem key={option.value} value={option.value}>
                <div className="flex items-center space-x-2">
                  {option.icon && <span className="text-muted-foreground">{option.icon}</span>}
                  <span>{option.label}</span>
                </div>
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      );
      
    case 'checkbox':
      return (
        <div className="flex items-center space-x-2">
          <Checkbox
            {...formField}
            checked={formField.value}
            onCheckedChange={formField.onChange}
            className="data-[state=checked]:bg-primary data-[state=checked]:text-primary-foreground"
          />
          <Label className="text-sm font-normal cursor-pointer">
            {field.placeholder}
          </Label>
        </div>
      );
      
    case 'switch':
      return (
        <div className="flex items-center space-x-2">
          <Switch
            {...formField}
            checked={formField.value}
            onCheckedChange={formField.onChange}
          />
          <Label className="text-sm font-normal">
            {field.placeholder}
          </Label>
        </div>
      );
      
    default:
      return (
        <Input
          {...formField}
          placeholder={field.placeholder}
          className={cn(baseClasses, field.className)}
        />
      );
  }
};
```

## Field Selector Interface

### Field Type Selector with shadcn Components

```typescript
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { ScrollArea } from "@/components/ui/scroll-area";

const FieldSelector: React.FC<{
  onFieldAdd: (field: FieldConfig) => void;
}> = ({ onFieldAdd }) => {
  const [selectedType, setSelectedType] = useState<string>('');
  const [isOpen, setIsOpen] = useState(false);

  const fieldTypes = [
    { 
      value: 'text', 
      label: 'Text Input', 
      icon: '📝',
      description: 'Single line text input with validation'
    },
    { 
      value: 'textarea', 
      label: 'Text Area', 
      icon: '📄',
      description: 'Multi-line text input for longer content'
    },
    { 
      value: 'select', 
      label: 'Select Dropdown', 
      icon: '📋',
      description: 'Dropdown selection with multiple options'
    },
    { 
      value: 'checkbox', 
      label: 'Checkbox', 
      icon: '☑️',
      description: 'Boolean checkbox for yes/no values'
    },
    { 
      value: 'date', 
      label: 'Date Picker', 
      icon: '📅',
      description: 'Date selection with calendar interface'
    },
  ];

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        <Button 
          variant="outline" 
          size="sm"
          className="h-9 px-3 border-dashed border-2 hover:border-solid transition-all duration-200"
        >
          <Plus className="w-4 h-4 mr-2" />
          Add Field
        </Button>
      </DialogTrigger>
      
      <DialogContent className="sm:max-w-[600px]">
        <DialogHeader>
          <DialogTitle className="text-xl font-semibold">Add New Field</DialogTitle>
          <DialogDescription>
            Choose a field type to add to your form
          </DialogDescription>
        </DialogHeader>
        
        <ScrollArea className="h-[400px] w-full rounded-md border p-4">
          <div className="grid grid-cols-1 gap-3">
            {fieldTypes.map((type) => (
              <Card 
                key={type.value}
                className={cn(
                  "cursor-pointer transition-all duration-200 hover:shadow-md border-2",
                  selectedType === type.value 
                    ? "border-primary bg-primary/5" 
                    : "border-border hover:border-primary/50"
                )}
                onClick={() => setSelectedType(type.value)}
              >
                <CardContent className="p-4">
                  <div className="flex items-start space-x-3">
                    <span className="text-2xl">{type.icon}</span>
                    <div className="flex-1 space-y-1">
                      <h4 className="font-medium leading-none">{type.label}</h4>
                      <p className="text-sm text-muted-foreground">
                        {type.description}
                      </p>
                    </div>
                    {selectedType === type.value && (
                      <Badge variant="default" className="ml-auto">
                        Selected
                      </Badge>
                    )}
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </ScrollArea>
        
        <div className="flex justify-end space-x-3 pt-4">
          <Button 
            variant="outline" 
            onClick={() => setIsOpen(false)}
          >
            Cancel
          </Button>
          <Button 
            onClick={() => {
              if (selectedType) {
                onFieldAdd(createDefaultField(selectedType));
                setIsOpen(false);
                setSelectedType('');
              }
            }}
            disabled={!selectedType}
          >
            Add Field
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
};
```

## Tailwind CSS Best Practices

### Responsive Design Patterns

```typescript
// Mobile-first responsive classes
const responsiveClasses = {
  container: "w-full max-w-sm sm:max-w-md md:max-w-lg lg:max-w-2xl xl:max-w-4xl mx-auto px-4 sm:px-6 lg:px-8",
  grid: "grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-6",
  form: "space-y-4 sm:space-y-6",
  button: "w-full sm:w-auto px-4 py-2 sm:px-6 sm:py-3",
};
```

### Theme Integration with CSS Variables

```css
/* Define in your global CSS */
:root {
  --background: 0 0% 100%;
  --foreground: 222.2 84% 4.9%;
  --card: 0 0% 100%;
  --card-foreground: 222.2 84% 4.9%;
  --popover: 0 0% 100%;
  --popover-foreground: 222.2 84% 4.9%;
  --primary: 222.2 47.4% 11.2%;
  --primary-foreground: 210 40% 98%;
  --secondary: 210 40% 96%;
  --secondary-foreground: 222.2 84% 4.9%;
  --muted: 210 40% 96%;
  --muted-foreground: 215.4 16.3% 46.9%;
  --accent: 210 40% 96%;
  --accent-foreground: 222.2 84% 4.9%;
  --destructive: 0 84.2% 60.2%;
  --destructive-foreground: 210 40% 98%;
  --border: 214.3 31.8% 91.4%;
  --input: 214.3 31.8% 91.4%;
  --ring: 222.2 84% 4.9%;
  --radius: 0.5rem;
}
```

### Conditional Styling with cn()

```typescript
import { cn } from "@/lib/utils";

const DynamicComponent = ({ variant, size, className }) => {
  return (
    <div 
      className={cn(
        // Base styles
        "flex items-center justify-center rounded-md font-medium transition-colors",
        
        // Variant styles
        {
          "bg-primary text-primary-foreground hover:bg-primary/90": variant === "default",
          "bg-destructive text-destructive-foreground hover:bg-destructive/90": variant === "destructive",
          "border border-input bg-background hover:bg-accent hover:text-accent-foreground": variant === "outline",
        },
        
        // Size styles
        {
          "h-10 px-4 py-2": size === "default",
          "h-9 rounded-md px-3": size === "sm",
          "h-11 rounded-md px-8": size === "lg",
        },
        
        // Custom className
        className
      )}
    >
      Content
    </div>
  );
};
```

## Accessibility Considerations with shadcn

### Built-in Accessibility Features

shadcn/ui components come with accessibility features out of the box:

```typescript
// Form accessibility
<FormField
  control={form.control}
  name="email"
  render={({ field }) => (
    <FormItem>
      <FormLabel>Email Address</FormLabel>
      <FormControl>
        <Input 
          {...field}
          type="email"
          placeholder="Enter your email"
          aria-describedby="email-description email-error"
        />
      </FormControl>
      <FormDescription id="email-description">
        We'll never share your email with anyone else.
      </FormDescription>
      <FormMessage id="email-error" />
    </FormItem>
  )}
/>

// Focus management
<Dialog>
  <DialogTrigger asChild>
    <Button>Open Dialog</Button>
  </DialogTrigger>
  <DialogContent>
    <DialogHeader>
      <DialogTitle>Accessible Dialog</DialogTitle>
      <DialogDescription>
        This dialog follows ARIA patterns for screen readers
      </DialogDescription>
    </DialogHeader>
    {/* Content automatically focuses and traps focus */}
  </DialogContent>
</Dialog>
```

### Custom Accessibility Enhancements

```typescript
// Enhanced field with accessibility
const AccessibleField = ({ field, form }) => {
  const fieldId = `field-${field.id}`;
  const descriptionId = `${fieldId}-description`;
  const errorId = `${fieldId}-error`;

  return (
    <FormField
      control={form.control}
      name={field.id}
      render={({ field: formField, fieldState }) => (
        <FormItem>
          <FormLabel 
            htmlFor={fieldId}
            className={cn(
              "text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70",
              fieldState.error && "text-destructive"
            )}
          >
            {field.label}
            {field.required && (
              <span className="text-destructive ml-1" aria-label="required">*</span>
            )}
          </FormLabel>
          
          <FormControl>
            <Input
              {...formField}
              id={fieldId}
              aria-describedby={cn(
                field.description && descriptionId,
                fieldState.error && errorId
              )}
              aria-invalid={fieldState.error ? "true" : "false"}
              className={cn(
                "w-full",
                fieldState.error && "border-destructive focus-visible:ring-destructive"
              )}
            />
          </FormControl>
          
          {field.description && (
            <FormDescription id={descriptionId}>
              {field.description}
            </FormDescription>
          )}
          
          <FormMessage id={errorId} />
        </FormItem>
      )}
    />
  );
};
```

## Performance Optimization

### Lazy Loading Components

```typescript
import { lazy, Suspense } from 'react';
import { Skeleton } from "@/components/ui/skeleton";

const LazyFormField = lazy(() => import('./FormField'));

const OptimizedDynamicForm = ({ fields }) => {
  return (
    <Form>
      {fields.map((field) => (
        <Suspense 
          key={field.id} 
          fallback={
            <div className="space-y-2">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-10 w-full" />
            </div>
          }
        >
          <LazyFormField field={field} />
        </Suspense>
      ))}
    </Form>
  );
};
```

### Memoization for Large Forms

```typescript
import { memo, useMemo } from 'react';

const MemoizedDynamicField = memo<{
  field: FieldConfig;
  form: UseFormReturn<any>;
}>(({ field, form }) => {
  const fieldComponent = useMemo(
    () => renderFieldComponent(field, form.register(field.id)),
    [field.type, field.options, field.validation]
  );

  return (
    <FormField
      control={form.control}
      name={field.id}
      render={({ field: formField }) => (
        <FormItem>
          <FormLabel>{field.label}</FormLabel>
          <FormControl>{fieldComponent}</FormControl>
          <FormMessage />
        </FormItem>
      )}
    />
  );
});
```

## Integration with Bento Grid

### Grid-Aware Form Layout

```typescript
const BentoFormLayout = ({ fields }) => {
  const gridFields = useMemo(() => 
    fields.map(field => ({
      ...field,
      gridArea: calculateGridArea(field.type, field.importance),
      className: cn(
        field.className,
        getBentoGridClasses(field.type)
      )
    })),
    [fields]
  );

  return (
    <div className="bento-grid grid auto-rows-[200px] grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
      {gridFields.map((field) => (
        <Card 
          key={field.id}
          className={cn(
            "transition-all duration-200 hover:shadow-lg",
            field.gridArea
          )}
        >
          <CardContent className="p-4 h-full flex flex-col">
            <DynamicField field={field} form={form} />
          </CardContent>
        </Card>
      ))}
    </div>
  );
};

const getBentoGridClasses = (fieldType: string) => {
  const gridClasses = {
    'textarea': 'col-span-2 row-span-2',
    'select': 'col-span-1 row-span-1',
    'text': 'col-span-1 row-span-1',
    'checkbox': 'col-span-1 row-span-1',
    'date': 'col-span-1 row-span-1',
  };
  
  return gridClasses[fieldType] || 'col-span-1 row-span-1';
};
```

## Quick Reference: shadcn/ui Component Library

### Essential Components for Dynamic Pages

```typescript
// Core UI Components
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Checkbox } from "@/components/ui/checkbox";
import { Switch } from "@/components/ui/switch";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";

// Layout Components
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { ScrollArea } from "@/components/ui/scroll-area";

// Form Components
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";

// Feedback Components
import { Badge } from "@/components/ui/badge";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Progress } from "@/components/ui/progress";
import { Skeleton } from "@/components/ui/skeleton";

// Navigation Components
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";

// Data Display
import { DataTable } from "@/components/ui/data-table";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
```

### Tailwind CSS Utility Classes

```typescript
// Spacing System (based on 4px grid)
const spacing = {
  xs: "space-y-1",     // 4px
  sm: "space-y-2",     // 8px  
  md: "space-y-4",     // 16px
  lg: "space-y-6",     // 24px
  xl: "space-y-8",     // 32px
  "2xl": "space-y-12", // 48px
};

// Responsive Breakpoints
const breakpoints = {
  sm: "640px",   // Small devices
  md: "768px",   // Medium devices  
  lg: "1024px",  // Large devices
  xl: "1280px",  // Extra large devices
  "2xl": "1536px", // 2X large devices
};

// Grid Patterns for Bento Layout
const gridPatterns = {
  auto: "grid auto-rows-[200px]",
  responsive: "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4",
  gap: "gap-4 sm:gap-6",
  spanning: {
    wide: "col-span-2",
    tall: "row-span-2", 
    large: "col-span-2 row-span-2",
  }
};
```

### Component Theming with CSS Variables

```css
/* Custom properties for consistent theming */
:root {
  /* Base colors */
  --background: 0 0% 100%;
  --foreground: 222.2 84% 4.9%;
  
  /* Component colors */
  --card: 0 0% 100%;
  --card-foreground: 222.2 84% 4.9%;
  --popover: 0 0% 100%;
  --popover-foreground: 222.2 84% 4.9%;
  
  /* Interactive colors */
  --primary: 222.2 47.4% 11.2%;
  --primary-foreground: 210 40% 98%;
  --secondary: 210 40% 96%;
  --secondary-foreground: 222.2 84% 4.9%;
  
  /* State colors */
  --destructive: 0 84.2% 60.2%;
  --destructive-foreground: 210 40% 98%;
  --success: 142 76% 36%;
  --success-foreground: 210 40% 98%;
  --warning: 38 92% 50%;
  --warning-foreground: 222.2 84% 4.9%;
  
  /* Neutral colors */
  --muted: 210 40% 96%;
  --muted-foreground: 215.4 16.3% 46.9%;
  --accent: 210 40% 96%;
  --accent-foreground: 222.2 84% 4.9%;
  
  /* Borders and inputs */
  --border: 214.3 31.8% 91.4%;
  --input: 214.3 31.8% 91.4%;
  --ring: 222.2 84% 4.9%;
  
  /* Border radius */
  --radius: 0.5rem;
}

.dark {
  --background: 222.2 84% 4.9%;
  --foreground: 210 40% 98%;
  --card: 222.2 84% 4.9%;
  --card-foreground: 210 40% 98%;
  --popover: 222.2 84% 4.9%;
  --popover-foreground: 210 40% 98%;
  --primary: 210 40% 98%;
  --primary-foreground: 222.2 84% 4.9%;
  --secondary: 217.2 32.6% 17.5%;
  --secondary-foreground: 210 40% 98%;
  --muted: 217.2 32.6% 17.5%;
  --muted-foreground: 215 20.2% 65.1%;
  --accent: 217.2 32.6% 17.5%;
  --accent-foreground: 210 40% 98%;
  --destructive: 0 62.8% 30.6%;
  --destructive-foreground: 210 40% 98%;
  --border: 217.2 32.6% 17.5%;
  --input: 217.2 32.6% 17.5%;
  --ring: 212.7 26.8% 83.9%;
}
```

This dynamic page creation system leverages the full power of shadcn/ui components and Tailwind CSS to create beautiful, accessible, and performant forms that integrate seamlessly with the Bento Grid layout system.