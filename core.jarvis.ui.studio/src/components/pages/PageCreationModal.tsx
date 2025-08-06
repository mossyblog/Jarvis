/**
 * Page Creation Modal Component
 * 
 * Multi-step wizard for creating new pages with template selection,
 * form validation, and REST API integration.
 * 
 * Features:
 * - 3-step wizard: Basic Info, Template Selection, Configuration
 * - Form validation with React Hook Form + Zod
 * - Template preview integration
 * - REST API integration for page creation
 * - Responsive design with accessibility support
 * - Error handling and loading states
 * 
 * @module PageCreationModal
 */

import React, { useState, useCallback, useMemo, useEffect } from 'react';
import { useForm, FormProvider } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';

// UIStudio hooks and services
import { uistudioApiClient } from '../../services/api/uistudioApiClient';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

// Shadcn/ui components
import { Button } from '../ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Input } from '../ui/input';
import { Textarea } from '../ui/textarea';
import { Badge } from '../ui/badge';
import { Label } from '../ui/label';
import { 
  Dialog, 
  DialogContent, 
  DialogDescription, 
  DialogFooter, 
  DialogHeader, 
  DialogTitle 
} from '../ui/dialog';
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '../ui/form';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../ui/select';
import { 
  Tabs, 
  TabsContent, 
  TabsList, 
  TabsTrigger 
} from '../ui/tabs';
import { LoadingSpinner } from '../ui/loading-spinner';
import { Separator } from '../ui/separator';
import { ScrollArea } from '../ui/scroll-area';

// Icons
import { 
  FileText,
  Layout,
  Settings,
  ChevronLeft,
  ChevronRight,
  Check,
  AlertTriangle,
  Sparkles,
  Globe,
  Database,
  Eye,
  Star,
  Users,
  Zap,
  X
} from 'lucide-react';

// UIStudio types
import type {
  UIStudioTemplate,
  UIStudioEntityId,
  CreatePageRequest,
  UIStudioPage
} from '../../types/uistudio';

// ============================================================================
// Validation Schemas
// ============================================================================

const basicInfoSchema = z.object({
  pageName: z.string()
    .min(1, 'Page name is required')
    .max(100, 'Page name must be less than 100 characters')
    .regex(/^[a-zA-Z0-9\s\-_]+$/, 'Page name can only contain letters, numbers, spaces, hyphens, and underscores'),
  
  pageSlug: z.string()
    .min(1, 'Page slug is required')
    .max(100, 'Page slug must be less than 100 characters')
    .regex(/^[a-z0-9\-_]+$/, 'Page slug must be lowercase with hyphens or underscores only'),
  
  pageType: z.enum(['static', 'dynamic'], {
    required_error: 'Please select a page type'
  }),
  
  description: z.string()
    .max(500, 'Description must be less than 500 characters')
    .optional(),
    
  tags: z.string()
    .max(200, 'Tags must be less than 200 characters')
    .optional()
});

const templateSelectionSchema = z.object({
  selectedTemplateId: z.string().optional(),
  templateOptions: z.object({
    applyLayout: z.boolean().default(true),
    applyStyles: z.boolean().default(true),
    applyContent: z.boolean().default(false)
  }).optional()
});

const configurationSchema = z.object({
  metadata: z.record(z.any()).optional(),
  initialSettings: z.object({
    isPublished: z.boolean().optional().default(false),
    enableComments: z.boolean().optional().default(false),
    enableVersioning: z.boolean().optional().default(true)
  }).optional()
});

const pageCreationSchema = z.object({
  basicInfo: basicInfoSchema,
  templateSelection: templateSelectionSchema,
  configuration: configurationSchema
});

type PageCreationFormData = z.infer<typeof pageCreationSchema>;

// ============================================================================
// Component Props Interface
// ============================================================================

export interface PageCreationModalProps {
  /** Whether the modal is open */
  isOpen: boolean;
  
  /** Called when modal is closed */
  onClose: () => void;
  
  /** Called when page is successfully created */
  onPageCreated?: (page: UIStudioPage) => void;
  
  /** Current user entity ID for ownership */
  userEntityId: UIStudioEntityId;
  
  /** Optional initial page type */
  initialPageType?: 'static' | 'dynamic';
  
  /** Optional callback for errors */
  onError?: (error: Error) => void;
}

// ============================================================================
// Template Card Component
// ============================================================================

interface TemplateCardProps {
  template: UIStudioTemplate;
  isSelected: boolean;
  onSelect: (templateId: string) => void;
  onPreview: (template: UIStudioTemplate) => void;
}

const TemplateCard: React.FC<TemplateCardProps> = ({
  template,
  isSelected,
  onSelect,
  onPreview
}) => {
  return (
    <Card 
      className={`cursor-pointer transition-all hover:shadow-md ${
        isSelected ? 'ring-2 ring-primary ring-offset-2' : ''
      }`}
      onClick={() => onSelect(template.id)}
    >
      <CardHeader className="pb-2">
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <CardTitle className="text-base flex items-center gap-2">
              <Layout className="h-xs w-xs" />
              {template.templateName}
              {isSelected && (
                <Check className="h-xs w-xs text-primary" />
              )}
            </CardTitle>
            <CardDescription className="mt-1">
              {template.description || 'No description available'}
            </CardDescription>
          </div>
          <div className="flex items-center gap-2">
            <Badge variant="outline">{template.category}</Badge>
            {template.isPublic && (
              <Badge variant="secondary">
                <Globe className="h-2xs w-2xs mr-1" />
                Public
              </Badge>
            )}
          </div>
        </div>
      </CardHeader>
      <CardContent className="pt-0">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4 text-xs text-muted-foreground">
            <span className="flex items-center gap-1">
              <Users className="h-2xs w-2xs" />
              {template.usageCount || 0}
            </span>
            <span className="flex items-center gap-1">
              <Star className="h-2xs w-2xs" />
              Template
            </span>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={(e) => {
              e.stopPropagation();
              onPreview(template);
            }}
          >
            <Eye className="h-2xs w-2xs mr-1" />
            Preview
          </Button>
        </div>
      </CardContent>
    </Card>
  );
};

// ============================================================================
// Main Component
// ============================================================================

export const PageCreationModal: React.FC<PageCreationModalProps> = ({
  isOpen,
  onClose,
  onPageCreated,
  userEntityId,
  initialPageType = 'static',
  onError
}) => {
  const [currentStep, setCurrentStep] = useState(0);
  const [previewTemplate, setPreviewTemplate] = useState<UIStudioTemplate | null>(null);
  const queryClient = useQueryClient();

  // Form setup
  const form = useForm({
    resolver: zodResolver(pageCreationSchema),
    defaultValues: {
      basicInfo: {
        pageName: '',
        pageSlug: '',
        pageType: initialPageType,
        description: '',
        tags: ''
      },
      templateSelection: {
        selectedTemplateId: undefined,
        templateOptions: {
          applyLayout: true,
          applyStyles: true,
          applyContent: false
        }
      },
      configuration: {
        metadata: {},
        initialSettings: {
          isPublished: false,
          enableComments: false,
          enableVersioning: true
        }
      }
    }
  });

  // Watch form values for validation
  const watchedBasicInfo = form.watch('basicInfo');
  const watchedTemplateSelection = form.watch('templateSelection');

  // Generate slug from page name
  const generateSlug = useCallback((name: string) => {
    return name
      .toLowerCase()
      .replace(/[^a-z0-9\s\-_]/g, '')
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-')
      .trim();
  }, []);

  // Auto-generate slug when page name changes
  useEffect(() => {
    if (watchedBasicInfo.pageName && !form.formState.dirtyFields.basicInfo?.pageSlug) {
      const slug = generateSlug(watchedBasicInfo.pageName);
      form.setValue('basicInfo.pageSlug', slug);
    }
  }, [watchedBasicInfo.pageName, form, generateSlug]);

  // Fetch templates
  const { 
    data: templates = [], 
    isLoading: templatesLoading,
    error: templatesError 
  } = useQuery({
    queryKey: ['uistudio', 'templates', 'by-owner', userEntityId],
    queryFn: () => uistudioApiClient.getTemplatesByOwner(userEntityId),
    enabled: isOpen && currentStep >= 1
  });

  // Create page mutation
  const createPageMutation = useMutation({
    mutationFn: async (data: PageCreationFormData) => {
      const request: CreatePageRequest = {
        pageName: data.basicInfo.pageName,
        pageSlug: data.basicInfo.pageSlug,
        pageType: data.basicInfo.pageType as 'static' | 'dynamic',
        description: data.basicInfo.description,
        createdByEntityId: userEntityId,
        tags: Array.isArray(data.basicInfo.tags) ? data.basicInfo.tags.join(', ') : data.basicInfo.tags,
        metadata: data.configuration.metadata
      };

      const response = await uistudioApiClient.createPage(request);
      return response[0]; // API returns array, get first result
    },
    onSuccess: (createdPage) => {
      queryClient.invalidateQueries({ queryKey: ['uistudio', 'pages'] });
      onPageCreated?.(createdPage);
      handleClose();
    },
    onError: (error) => {
      console.error('Failed to create page:', error);
      onError?.(error as Error);
    }
  });

  // Apply template mutation
  const applyTemplateMutation = useMutation({
    mutationFn: async ({ templateId, pageData }: { 
      templateId: string, 
      pageData: PageCreationFormData 
    }) => {
      // First create the page
      const pageRequest: CreatePageRequest = {
        pageName: pageData.basicInfo.pageName,
        pageSlug: pageData.basicInfo.pageSlug,
        pageType: pageData.basicInfo.pageType,
        description: pageData.basicInfo.description,
        createdByEntityId: userEntityId,
        tags: pageData.basicInfo.tags,
        metadata: pageData.configuration.metadata
      };

      const pageResponse = await uistudioApiClient.createPage(pageRequest);
      const createdPage = pageResponse[0];

      // Then apply the template
      const applyRequest = {
        pageName: pageData.basicInfo.pageName,
        pageSlug: pageData.basicInfo.pageSlug,
        createdByEntityId: userEntityId
      };

      await uistudioApiClient.applyTemplate(templateId, applyRequest);
      
      return createdPage;
    },
    onSuccess: (createdPage) => {
      queryClient.invalidateQueries({ queryKey: ['uistudio', 'pages'] });
      queryClient.invalidateQueries({ queryKey: ['uistudio', 'templates'] });
      onPageCreated?.(createdPage);
      handleClose();
    },
    onError: (error) => {
      console.error('Failed to apply template:', error);
      onError?.(error as Error);
    }
  });

  // Handle close
  const handleClose = useCallback(() => {
    form.reset();
    setCurrentStep(0);
    setPreviewTemplate(null);
    onClose();
  }, [form, onClose]);

  // Handle next step
  const handleNext = useCallback(async () => {
    let isValid = false;

    switch (currentStep) {
      case 0:
        isValid = await form.trigger('basicInfo');
        break;
      case 1:
        isValid = await form.trigger('templateSelection');
        break;
      case 2:
        isValid = await form.trigger('configuration');
        break;
      default:
        isValid = true;
    }

    if (isValid) {
      setCurrentStep(prev => Math.min(prev + 1, 2));
    }
  }, [currentStep, form]);

  // Handle previous step
  const handlePrevious = useCallback(() => {
    setCurrentStep(prev => Math.max(prev - 1, 0));
  }, []);

  // Form data type is defined by the schema above

  // Handle form submission
  const handleSubmit = useCallback(async (data: PageCreationFormData) => {
    const hasTemplate = data.templateSelection.selectedTemplateId;

    if (hasTemplate) {
      await applyTemplateMutation.mutateAsync({
        templateId: data.templateSelection.selectedTemplateId!,
        pageData: data
      });
    } else {
      await createPageMutation.mutateAsync(data);
    }
  }, [createPageMutation, applyTemplateMutation]);

  // Handle template selection
  const handleTemplateSelect = useCallback((templateId: string) => {
    form.setValue('templateSelection.selectedTemplateId', templateId);
  }, [form]);

  // Handle template preview
  const handleTemplatePreview = useCallback((template: UIStudioTemplate) => {
    setPreviewTemplate(template);
  }, []);

  // Step validation
  const isStepValid = useMemo(() => {
    switch (currentStep) {
      case 0:
        return watchedBasicInfo.pageName && 
               watchedBasicInfo.pageSlug && 
               watchedBasicInfo.pageType;
      case 1:
        return true; // Template selection is optional
      case 2:
        return true; // Configuration is optional
      default:
        return false;
    }
  }, [currentStep, watchedBasicInfo]);

  const isLoading = createPageMutation.isPending || applyTemplateMutation.isPending;
  const hasError = createPageMutation.error || applyTemplateMutation.error;

  // Step definitions
  const steps = [
    {
      id: 'basic-info',
      title: 'Basic Information',
      description: 'Page name, URL, and basic settings',
      icon: FileText,
      completed: currentStep > 0 && isStepValid
    },
    {
      id: 'template',
      title: 'Template Selection',
      description: 'Choose a template or start from scratch',
      icon: Layout,
      completed: currentStep > 1
    },
    {
      id: 'configuration',
      title: 'Configuration',
      description: 'Advanced settings and metadata',
      icon: Settings,
      completed: currentStep > 2
    }
  ];

  return (
    <>
      <Dialog open={isOpen} onOpenChange={handleClose}>
        <DialogContent className="max-w-4xl max-h-[90vh] overflow-hidden">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Sparkles className="h-sm w-sm" />
              Create New Page
            </DialogTitle>
            <DialogDescription>
              Follow the steps to create a new page with optional template selection.
            </DialogDescription>
          </DialogHeader>

          {/* Step Indicator */}
          <div className="flex items-center justify-between px-6 py-4 border-b">
            {steps.map((step, index) => (
              <div key={step.id} className="flex items-center">
                <div className={`flex items-center justify-center w-lg h-lg rounded-full border-2 ${
                  index === currentStep 
                    ? 'border-primary bg-primary text-primary-foreground'
                    : index < currentStep 
                      ? 'border-primary bg-primary text-primary-foreground'
                      : 'border-muted-foreground bg-background text-muted-foreground'
                }`}>
                  {index < currentStep ? (
                    <Check className="h-xs w-xs" />
                  ) : (
                    <step.icon className="h-xs w-xs" />
                  )}
                </div>
                <div className="ml-3 hidden sm:block">
                  <p className={`text-sm font-medium ${
                    index <= currentStep ? 'text-foreground' : 'text-muted-foreground'
                  }`}>
                    {step.title}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {step.description}
                  </p>
                </div>
                {index < steps.length - 1 && (
                  <div className={`hidden sm:block w-2xl h-px mx-4 ${
                    index < currentStep ? 'bg-primary' : 'bg-muted-foreground'
                  }`} />
                )}
              </div>
            ))}
          </div>

          <FormProvider {...form}>
            <form onSubmit={form.handleSubmit(handleSubmit)} className="flex-1 overflow-hidden">
              <div className="p-6 overflow-auto max-h-[50vh]">
                {/* Step 0: Basic Information */}
                {currentStep === 0 && (
                  <div className="space-y-6">
                    <div className="grid grid-cols-2 gap-4">
                      <FormField
                        control={form.control}
                        name="basicInfo.pageName"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Page Name *</FormLabel>
                            <FormControl>
                              <Input placeholder="My Awesome Page" {...field} />
                            </FormControl>
                            <FormDescription>
                              The display name for your page
                            </FormDescription>
                            <FormMessage />
                          </FormItem>
                        )}
                      />

                      <FormField
                        control={form.control}
                        name="basicInfo.pageSlug"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Page URL *</FormLabel>
                            <FormControl>
                              <Input placeholder="my-awesome-page" {...field} />
                            </FormControl>
                            <FormDescription>
                              URL-friendly identifier (auto-generated)
                            </FormDescription>
                            <FormMessage />
                          </FormItem>
                        )}
                      />
                    </div>

                    <FormField
                      control={form.control}
                      name="basicInfo.pageType"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Page Type *</FormLabel>
                          <Select onValueChange={field.onChange} defaultValue={field.value}>
                            <FormControl>
                              <SelectTrigger>
                                <SelectValue placeholder="Select page type" />
                              </SelectTrigger>
                            </FormControl>
                            <SelectContent>
                              <SelectItem value="static">
                                <div className="flex items-center gap-2">
                                  <FileText className="h-xs w-xs" />
                                  <div>
                                    <div className="font-medium">Static Page</div>
                                    <div className="text-xs text-muted-foreground">
                                      Fixed content and layout
                                    </div>
                                  </div>
                                </div>
                              </SelectItem>
                              <SelectItem value="dynamic">
                                <div className="flex items-center gap-2">
                                  <Database className="h-xs w-xs" />
                                  <div>
                                    <div className="font-medium">Dynamic Page</div>
                                    <div className="text-xs text-muted-foreground">
                                      Data-driven content
                                    </div>
                                  </div>
                                </div>
                              </SelectItem>
                            </SelectContent>
                          </Select>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="basicInfo.description"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Description</FormLabel>
                          <FormControl>
                            <Textarea 
                              placeholder="Brief description of your page..."
                              className="resize-none"
                              rows={3}
                              {...field}
                            />
                          </FormControl>
                          <FormDescription>
                            Optional description for documentation
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="basicInfo.tags"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Tags</FormLabel>
                          <FormControl>
                            <Input 
                              placeholder="dashboard, analytics, reports"
                              {...field}
                            />
                          </FormControl>
                          <FormDescription>
                            Comma-separated tags for organization
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>
                )}

                {/* Step 1: Template Selection */}
                {currentStep === 1 && (
                  <div className="space-y-6">
                    <div>
                      <h3 className="text-lg font-medium mb-2">Choose a Template</h3>
                      <p className="text-sm text-muted-foreground mb-4">
                        Select a template to get started quickly, or continue without a template.
                      </p>
                    </div>

                    {templatesLoading ? (
                      <div className="flex items-center justify-center py-8">
                        <LoadingSpinner size="lg" />
                        <span className="ml-2">Loading templates...</span>
                      </div>
                    ) : templatesError ? (
                      <Card className="border-destructive">
                        <CardContent className="flex items-center justify-center py-8">
                          <div className="text-center">
                            <AlertTriangle className="h-lg w-lg mx-auto mb-2 text-destructive" />
                            <p className="text-sm text-destructive">Failed to load templates</p>
                            <p className="text-xs text-muted-foreground">You can continue without a template</p>
                          </div>
                        </CardContent>
                      </Card>
                    ) : (
                      <div className="space-y-4">
                        {/* No Template Option */}
                        <Card 
                          className={`cursor-pointer transition-all hover:shadow-md ${
                            !watchedTemplateSelection.selectedTemplateId ? 'ring-2 ring-primary ring-offset-2' : ''
                          }`}
                          onClick={() => handleTemplateSelect('')}
                        >
                          <CardContent className="flex items-center p-4">
                            <div className="flex-1">
                              <div className="flex items-center gap-2">
                                <Zap className="h-sm w-sm" />
                                <span className="font-medium">Start from Scratch</span>
                                {!watchedTemplateSelection.selectedTemplateId && (
                                  <Check className="h-xs w-xs text-primary" />
                                )}
                              </div>
                              <p className="text-sm text-muted-foreground mt-1">
                                Create a blank page with no pre-defined layout or content
                              </p>
                            </div>
                          </CardContent>
                        </Card>

                        {/* Template Grid */}
                        {templates.length > 0 ? (
                          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            {templates.map((template) => (
                              <TemplateCard
                                key={template.id}
                                template={template}
                                isSelected={watchedTemplateSelection.selectedTemplateId === template.id}
                                onSelect={handleTemplateSelect}
                                onPreview={handleTemplatePreview}
                              />
                            ))}
                          </div>
                        ) : (
                          <Card className="border-dashed">
                            <CardContent className="flex items-center justify-center py-8">
                              <div className="text-center">
                                <Layout className="h-lg w-lg mx-auto mb-2 text-muted-foreground opacity-50" />
                                <p className="text-sm text-muted-foreground">No templates available</p>
                                <p className="text-xs text-muted-foreground">You can continue without a template</p>
                              </div>
                            </CardContent>
                          </Card>
                        )}

                        {/* Template Options */}
                        {watchedTemplateSelection.selectedTemplateId && (
                          <Card>
                            <CardHeader>
                              <CardTitle className="text-base">Template Options</CardTitle>
                              <CardDescription>
                                Choose what to apply from the selected template
                              </CardDescription>
                            </CardHeader>
                            <CardContent className="space-y-3">
                              <FormField
                                control={form.control}
                                name="templateSelection.templateOptions.applyLayout"
                                render={({ field }) => (
                                  <FormItem className="flex items-center space-x-2">
                                    <FormControl>
                                      <input
                                        type="checkbox"
                                        checked={field.value}
                                        onChange={field.onChange}
                                        className="rounded"
                                      />
                                    </FormControl>
                                    <div>
                                      <FormLabel>Apply Layout Structure</FormLabel>
                                      <FormDescription>
                                        Copy the grid layout and component positioning
                                      </FormDescription>
                                    </div>
                                  </FormItem>
                                )}
                              />
                              
                              <FormField
                                control={form.control}
                                name="templateSelection.templateOptions.applyStyles"
                                render={({ field }) => (
                                  <FormItem className="flex items-center space-x-2">
                                    <FormControl>
                                      <input
                                        type="checkbox"
                                        checked={field.value}
                                        onChange={field.onChange}
                                        className="rounded"
                                      />
                                    </FormControl>
                                    <div>
                                      <FormLabel>Apply Styling</FormLabel>
                                      <FormDescription>
                                        Copy colors, fonts, and visual styling
                                      </FormDescription>
                                    </div>
                                  </FormItem>
                                )}
                              />
                              
                              <FormField
                                control={form.control}
                                name="templateSelection.templateOptions.applyContent"
                                render={({ field }) => (
                                  <FormItem className="flex items-center space-x-2">
                                    <FormControl>
                                      <input
                                        type="checkbox"
                                        checked={field.value}
                                        onChange={field.onChange}
                                        className="rounded"
                                      />
                                    </FormControl>
                                    <div>
                                      <FormLabel>Apply Sample Content</FormLabel>
                                      <FormDescription>
                                        Include placeholder text and sample data
                                      </FormDescription>
                                    </div>
                                  </FormItem>
                                )}
                              />
                            </CardContent>
                          </Card>
                        )}
                      </div>
                    )}
                  </div>
                )}

                {/* Step 2: Configuration */}
                {currentStep === 2 && (
                  <div className="space-y-6">
                    <div>
                      <h3 className="text-lg font-medium mb-2">Page Configuration</h3>
                      <p className="text-sm text-muted-foreground mb-4">
                        Configure advanced settings and initial page state.
                      </p>
                    </div>

                    <Card>
                      <CardHeader>
                        <CardTitle className="text-base">Initial Settings</CardTitle>
                        <CardDescription>
                          Set the initial state of your page
                        </CardDescription>
                      </CardHeader>
                      <CardContent className="space-y-4">
                        <FormField
                          control={form.control}
                          name="configuration.initialSettings.isPublished"
                          render={({ field }) => (
                            <FormItem className="flex items-center justify-between">
                              <div>
                                <FormLabel>Publish Immediately</FormLabel>
                                <FormDescription>
                                  Make the page visible to users right away
                                </FormDescription>
                              </div>
                              <FormControl>
                                <input
                                  type="checkbox"
                                  checked={field.value}
                                  onChange={field.onChange}
                                  className="rounded"
                                />
                              </FormControl>
                            </FormItem>
                          )}
                        />

                        <Separator />

                        <FormField
                          control={form.control}
                          name="configuration.initialSettings.enableVersioning"
                          render={({ field }) => (
                            <FormItem className="flex items-center justify-between">
                              <div>
                                <FormLabel>Enable Version Control</FormLabel>
                                <FormDescription>
                                  Track changes and maintain version history
                                </FormDescription>
                              </div>
                              <FormControl>
                                <input
                                  type="checkbox"
                                  checked={field.value}
                                  onChange={field.onChange}
                                  className="rounded"
                                />
                              </FormControl>
                            </FormItem>
                          )}
                        />

                        <Separator />

                        <FormField
                          control={form.control}
                          name="configuration.initialSettings.enableComments"
                          render={({ field }) => (
                            <FormItem className="flex items-center justify-between">
                              <div>
                                <FormLabel>Enable Comments</FormLabel>
                                <FormDescription>
                                  Allow team members to leave comments
                                </FormDescription>
                              </div>
                              <FormControl>
                                <input
                                  type="checkbox"
                                  checked={field.value}
                                  onChange={field.onChange}
                                  className="rounded"
                                />
                              </FormControl>
                            </FormItem>
                          )}
                        />
                      </CardContent>
                    </Card>

                    {/* Summary */}
                    <Card>
                      <CardHeader>
                        <CardTitle className="text-base">Review</CardTitle>
                        <CardDescription>
                          Review your page configuration before creating
                        </CardDescription>
                      </CardHeader>
                      <CardContent className="space-y-3">
                        <div className="grid grid-cols-2 gap-4 text-sm">
                          <div>
                            <Label className="font-medium">Page Name:</Label>
                            <p className="text-muted-foreground">{watchedBasicInfo.pageName}</p>
                          </div>
                          <div>
                            <Label className="font-medium">Page Type:</Label>
                            <p className="text-muted-foreground capitalize">{watchedBasicInfo.pageType}</p>
                          </div>
                          <div>
                            <Label className="font-medium">URL:</Label>
                            <p className="text-muted-foreground">/{watchedBasicInfo.pageSlug}</p>
                          </div>
                          <div>
                            <Label className="font-medium">Template:</Label>
                            <p className="text-muted-foreground">
                              {watchedTemplateSelection.selectedTemplateId 
                                ? templates.find(t => t.id === watchedTemplateSelection.selectedTemplateId)?.templateName || 'Unknown'
                                : 'None (Blank Page)'
                              }
                            </p>
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  </div>
                )}

                {/* Error Display */}
                {hasError && (
                  <Card className="border-destructive">
                    <CardContent className="flex items-center p-4">
                      <AlertTriangle className="h-sm w-sm text-destructive mr-2" />
                      <div>
                        <p className="text-sm font-medium text-destructive">
                          Failed to create page
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {(createPageMutation.error || applyTemplateMutation.error)?.message}
                        </p>
                      </div>
                    </CardContent>
                  </Card>
                )}
              </div>

              <DialogFooter className="border-t p-6">
                <div className="flex items-center justify-between w-full">
                  <Button
                    type="button"
                    variant="outline"
                    onClick={handlePrevious}
                    disabled={currentStep === 0 || isLoading}
                  >
                    <ChevronLeft className="h-xs w-xs mr-1" />
                    Previous
                  </Button>

                  <div className="flex gap-2">
                    <Button
                      type="button"
                      variant="outline"
                      onClick={handleClose}
                      disabled={isLoading}
                    >
                      Cancel
                    </Button>

                    {currentStep < steps.length - 1 ? (
                      <Button
                        type="button"
                        onClick={handleNext}
                        disabled={!isStepValid || isLoading}
                      >
                        Next
                        <ChevronRight className="h-xs w-xs ml-1" />
                      </Button>
                    ) : (
                      <Button
                        type="submit"
                        disabled={!isStepValid || isLoading}
                      >
                        {isLoading ? (
                          <>
                            <LoadingSpinner size="sm" className="mr-2" />
                            Creating...
                          </>
                        ) : (
                          <>
                            <Check className="h-xs w-xs mr-1" />
                            Create Page
                          </>
                        )}
                      </Button>
                    )}
                  </div>
                </div>
              </DialogFooter>
            </form>
          </FormProvider>
        </DialogContent>
      </Dialog>

      {/* Template Preview Modal */}
      {previewTemplate && (
        <Dialog open={!!previewTemplate} onOpenChange={() => setPreviewTemplate(null)}>
          <DialogContent className="max-w-2xl">
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2">
                <Eye className="h-sm w-sm" />
                Template Preview
              </DialogTitle>
              <DialogDescription>
                {previewTemplate.templateName}
              </DialogDescription>
            </DialogHeader>

            <div className="space-y-4">
              <div className="aspect-video bg-muted rounded-lg flex items-center justify-center">
                {previewTemplate.previewImage ? (
                  <img 
                    src={previewTemplate.previewImage} 
                    alt={previewTemplate.templateName}
                    className="w-full h-full object-cover rounded-lg"
                  />
                ) : (
                  <div className="text-center">
                    <Layout className="h-2xl w-2xl mx-auto mb-2 text-muted-foreground opacity-50" />
                    <p className="text-sm text-muted-foreground">No preview available</p>
                  </div>
                )}
              </div>

              <div>
                <h4 className="font-medium mb-2">Description</h4>
                <p className="text-sm text-muted-foreground">
                  {previewTemplate.description || 'No description available.'}
                </p>
              </div>

              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <Label className="font-medium">Category:</Label>
                  <p className="text-muted-foreground">{previewTemplate.category || 'Uncategorized'}</p>
                </div>
                <div>
                  <Label className="font-medium">Type:</Label>
                  <p className="text-muted-foreground capitalize">{previewTemplate.templateType}</p>
                </div>
                <div>
                  <Label className="font-medium">Usage:</Label>
                  <p className="text-muted-foreground">{previewTemplate.usageCount || 0} times</p>
                </div>
                <div>
                  <Label className="font-medium">Visibility:</Label>
                  <p className="text-muted-foreground">{previewTemplate.isPublic ? 'Public' : 'Private'}</p>
                </div>
              </div>

              {previewTemplate.tags && (
                <div>
                  <Label className="font-medium">Tags:</Label>
                  <div className="flex flex-wrap gap-1 mt-1">
                    {previewTemplate.tags.split(',').map((tag, index) => (
                      <Badge key={index} variant="outline" className="text-xs">
                        {tag.trim()}
                      </Badge>
                    ))}
                  </div>
                </div>
              )}
            </div>

            <DialogFooter>
              <Button variant="outline" onClick={() => setPreviewTemplate(null)}>
                Close
              </Button>
              <Button onClick={() => {
                handleTemplateSelect(previewTemplate.id);
                setPreviewTemplate(null);
              }}>
                <Check className="h-xs w-xs mr-1" />
                Select Template
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}
    </>
  );
};

PageCreationModal.displayName = 'PageCreationModal';