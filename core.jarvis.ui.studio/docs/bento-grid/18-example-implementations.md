# Example Implementations with shadcn/ui and Tailwind CSS

## Overview

This document provides comprehensive examples of Bento Grid implementations using **shadcn/ui components** and **Tailwind CSS**. Each example demonstrates best practices for responsive design, accessibility, and component composition.

## Complete Import Structure

```typescript
// shadcn/ui components
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Progress } from "@/components/ui/progress";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { DataTable } from "@/components/ui/data-table";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";

// Form handling
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";

// Icons and utilities
import { Plus, Edit, Trash2, MoreHorizontal, Calendar, User, Mail, Phone } from "lucide-react";
import { cn } from "@/lib/utils";
```

## Example 1: Customer Management Dashboard

### Complete Implementation with shadcn/ui Data Table

```typescript
interface Customer {
  id: string;
  name: string;
  email: string;
  phone: string;
  status: 'active' | 'inactive' | 'pending';
  lastContact: Date;
  totalOrders: number;
  avatar?: string;
}

const CustomerManagementDashboard: React.FC = () => {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [selectedCustomer, setSelectedCustomer] = useState<Customer | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);

  // shadcn data table columns configuration
  const columns: ColumnDef<Customer>[] = [
    {
      accessorKey: "avatar",
      header: "",
      cell: ({ row }) => (
        <Avatar className="h-8 w-8">
          <AvatarImage src={row.original.avatar} alt={row.original.name} />
          <AvatarFallback className="bg-primary/10 text-primary font-medium">
            {row.original.name.split(' ').map(n => n[0]).join('')}
          </AvatarFallback>
        </Avatar>
      ),
    },
    {
      accessorKey: "name",
      header: "Customer",
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="font-medium">{row.original.name}</span>
          <span className="text-sm text-muted-foreground">{row.original.email}</span>
        </div>
      ),
    },
    {
      accessorKey: "phone",
      header: "Contact",
      cell: ({ row }) => (
        <span className="text-sm">{row.original.phone}</span>
      ),
    },
    {
      accessorKey: "status",
      header: "Status",
      cell: ({ row }) => (
        <Badge 
          variant={
            row.original.status === 'active' ? 'default' :
            row.original.status === 'inactive' ? 'destructive' : 'secondary'
          }
          className="capitalize"
        >
          {row.original.status}
        </Badge>
      ),
    },
    {
      accessorKey: "totalOrders",
      header: "Orders",
      cell: ({ row }) => (
        <span className="font-medium">{row.original.totalOrders}</span>
      ),
    },
    {
      accessorKey: "lastContact",
      header: "Last Contact",
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {format(row.original.lastContact, 'MMM dd, yyyy')}
        </span>
      ),
    },
    {
      id: "actions",
      cell: ({ row }) => (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" className="h-8 w-8 p-0">
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => handleEditCustomer(row.original)}>
              <Edit className="mr-2 h-4 w-4" />
              Edit
            </DropdownMenuItem>
            <DropdownMenuItem 
              onClick={() => handleDeleteCustomer(row.original.id)}
              className="text-destructive"
            >
              <Trash2 className="mr-2 h-4 w-4" />
              Delete
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      ),
    },
  ];

  return (
    <div className="container mx-auto py-6 space-y-6">
      {/* Header Section */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Customer Management</h1>
          <p className="text-muted-foreground">
            Manage your customer database and relationships
          </p>
        </div>
        <Button onClick={() => setIsDialogOpen(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Add Customer
        </Button>
      </div>

      {/* Stats Cards using Bento Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Customers</CardTitle>
            <User className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">1,234</div>
            <p className="text-xs text-muted-foreground">
              +20.1% from last month
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Active Customers</CardTitle>
            <Badge variant="default" className="h-4 px-1 text-xs">Active</Badge>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">987</div>
            <p className="text-xs text-muted-foreground">
              80% of total customers
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">New This Month</CardTitle>
            <Calendar className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">45</div>
            <p className="text-xs text-muted-foreground">
              +15% from last month
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Avg Order Value</CardTitle>
            <span className="text-xs text-muted-foreground">$</span>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">$249</div>
            <p className="text-xs text-muted-foreground">
              +5.2% from last month
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Data Table */}
      <Card>
        <CardHeader>
          <CardTitle>All Customers</CardTitle>
          <CardDescription>
            A comprehensive list of all customers in your database
          </CardDescription>
        </CardHeader>
        <CardContent>
          <DataTable 
            columns={columns} 
            data={customers}
            searchKey="name"
            searchPlaceholder="Search customers..."
          />
        </CardContent>
      </Card>

      {/* Customer Form Dialog */}
      <CustomerFormDialog 
        customer={selectedCustomer}
        open={isDialogOpen}
        onOpenChange={setIsDialogOpen}
        onSave={handleSaveCustomer}
      />
    </div>
  );
};
```

### Customer Form with shadcn Form Validation

```typescript
const customerSchema = z.object({
  name: z.string().min(2, "Name must be at least 2 characters"),
  email: z.string().email("Invalid email address"),
  phone: z.string().min(10, "Phone number must be at least 10 digits"),
  status: z.enum(['active', 'inactive', 'pending']),
  notes: z.string().optional(),
});

type CustomerFormData = z.infer<typeof customerSchema>;

const CustomerFormDialog: React.FC<{
  customer?: Customer | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSave: (data: CustomerFormData) => void;
}> = ({ customer, open, onOpenChange, onSave }) => {
  const form = useForm<CustomerFormData>({
    resolver: zodResolver(customerSchema),
    defaultValues: {
      name: customer?.name || "",
      email: customer?.email || "",
      phone: customer?.phone || "",
      status: customer?.status || "pending",
      notes: "",
    },
  });

  const handleSubmit = (data: CustomerFormData) => {
    onSave(data);
    form.reset();
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>
            {customer ? 'Edit Customer' : 'Add New Customer'}
          </DialogTitle>
          <DialogDescription>
            {customer 
              ? 'Update customer information below' 
              : 'Fill out the form to add a new customer to your database'
            }
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Full Name</FormLabel>
                  <FormControl>
                    <Input 
                      placeholder="John Doe" 
                      {...field}
                      className="w-full"
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="email"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Email</FormLabel>
                    <FormControl>
                      <Input 
                        type="email"
                        placeholder="john@example.com" 
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="phone"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Phone</FormLabel>
                    <FormControl>
                      <Input 
                        type="tel"
                        placeholder="+1 (555) 123-4567" 
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <FormField
              control={form.control}
              name="status"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Status</FormLabel>
                  <Select onValueChange={field.onChange} value={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="Select customer status" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectItem value="active">
                        <div className="flex items-center space-x-2">
                          <div className="w-2 h-2 bg-green-500 rounded-full" />
                          <span>Active</span>
                        </div>
                      </SelectItem>
                      <SelectItem value="inactive">
                        <div className="flex items-center space-x-2">
                          <div className="w-2 h-2 bg-red-500 rounded-full" />
                          <span>Inactive</span>
                        </div>
                      </SelectItem>
                      <SelectItem value="pending">
                        <div className="flex items-center space-x-2">
                          <div className="w-2 h-2 bg-yellow-500 rounded-full" />
                          <span>Pending</span>
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
              name="notes"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Notes</FormLabel>
                  <FormControl>
                    <Textarea 
                      placeholder="Additional notes about this customer..."
                      className="min-h-[100px] resize-none"
                      {...field}
                    />
                  </FormControl>
                  <FormDescription>
                    Optional notes or comments about the customer
                  </FormDescription>
                  <FormMessage />
                </FormItem>
              )}
            />

            <Separator className="my-4" />

            <div className="flex justify-end space-x-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
              >
                Cancel
              </Button>
              <Button 
                type="submit"
                disabled={form.formState.isSubmitting}
              >
                {form.formState.isSubmitting ? "Saving..." : "Save Customer"}
              </Button>
            </div>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
};
```

## Example 2: Project Dashboard with Bento Grid Layout

```typescript
interface Project {
  id: string;
  title: string;
  description: string;
  status: 'planning' | 'in-progress' | 'completed' | 'on-hold';
  progress: number;
  dueDate: Date;
  team: TeamMember[];
  priority: 'low' | 'medium' | 'high';
}

const ProjectDashboard: React.FC = () => {
  const [projects, setProjects] = useState<Project[]>([]);

  return (
    <div className="container mx-auto py-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold tracking-tight">Project Dashboard</h1>
        <p className="text-muted-foreground">
          Overview of all active projects and their progress
        </p>
      </div>

      {/* Bento Grid Layout */}
      <div className="grid auto-rows-[300px] grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        
        {/* Quick Stats - spans 2 columns */}
        <Card className="col-span-1 sm:col-span-2 lg:col-span-2">
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <span>Project Overview</span>
              <Badge variant="secondary" className="ml-auto">
                {projects.length} Projects
              </Badge>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <p className="text-sm text-muted-foreground">Completed</p>
                <p className="text-2xl font-bold text-green-600">
                  {projects.filter(p => p.status === 'completed').length}
                </p>
              </div>
              <div className="space-y-2">
                <p className="text-sm text-muted-foreground">In Progress</p>
                <p className="text-2xl font-bold text-blue-600">
                  {projects.filter(p => p.status === 'in-progress').length}
                </p>
              </div>
              <div className="space-y-2">
                <p className="text-sm text-muted-foreground">Planning</p>
                <p className="text-2xl font-bold text-yellow-600">
                  {projects.filter(p => p.status === 'planning').length}
                </p>
              </div>
              <div className="space-y-2">
                <p className="text-sm text-muted-foreground">On Hold</p>
                <p className="text-2xl font-bold text-red-600">
                  {projects.filter(p => p.status === 'on-hold').length}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Active Projects List - spans full height */}
        <Card className="col-span-1 sm:col-span-2 lg:col-span-1 row-span-2">
          <CardHeader>
            <CardTitle>Active Projects</CardTitle>
            <CardDescription>Currently in progress</CardDescription>
          </CardHeader>
          <CardContent>
            <ScrollArea className="h-[400px]">
              <div className="space-y-3">
                {projects
                  .filter(p => p.status === 'in-progress')
                  .map((project) => (
                    <div key={project.id} className="space-y-2 p-3 rounded-lg border">
                      <div className="flex items-center justify-between">
                        <h4 className="font-medium text-sm">{project.title}</h4>
                        <Badge 
                          variant={
                            project.priority === 'high' ? 'destructive' :
                            project.priority === 'medium' ? 'default' : 'secondary'
                          }
                          className="text-xs"
                        >
                          {project.priority}
                        </Badge>
                      </div>
                      <Progress value={project.progress} className="h-2" />
                      <div className="flex items-center justify-between text-xs text-muted-foreground">
                        <span>{project.progress}% complete</span>
                        <span>Due {format(project.dueDate, 'MMM dd')}</span>
                      </div>
                    </div>
                  ))}
              </div>
            </ScrollArea>
          </CardContent>
        </Card>

        {/* Team Members */}
        <Card className="col-span-1">
          <CardHeader>
            <CardTitle className="text-lg">Team Members</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {/* Sample team members */}
              <div className="flex items-center space-x-3">
                <Avatar className="h-8 w-8">
                  <AvatarFallback>JD</AvatarFallback>
                </Avatar>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium">John Doe</p>
                  <p className="text-xs text-muted-foreground">Developer</p>
                </div>
                <Badge variant="outline" className="text-xs">3 Projects</Badge>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Recent Activity */}
        <Card className="col-span-1">
          <CardHeader>
            <CardTitle className="text-lg">Recent Activity</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              <div className="flex items-center space-x-2 text-sm">
                <div className="w-2 h-2 bg-green-500 rounded-full" />
                <span className="text-muted-foreground">Project completed</span>
              </div>
              <div className="flex items-center space-x-2 text-sm">
                <div className="w-2 h-2 bg-blue-500 rounded-full" />
                <span className="text-muted-foreground">New task assigned</span>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Calendar Widget - spans 2 columns */}
        <Card className="col-span-1 sm:col-span-2">
          <CardHeader>
            <CardTitle>Upcoming Deadlines</CardTitle>
            <CardDescription>Projects due in the next 30 days</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {projects
                .filter(p => isWithinNextDays(p.dueDate, 30))
                .map((project) => (
                  <div key={project.id} className="flex items-center justify-between p-2 rounded border">
                    <div>
                      <p className="font-medium text-sm">{project.title}</p>
                      <p className="text-xs text-muted-foreground">
                        Due {format(project.dueDate, 'MMM dd, yyyy')}
                      </p>
                    </div>
                    <Badge variant="outline">
                      {differenceInDays(project.dueDate, new Date())}d
                    </Badge>
                  </div>
                ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};
```

## Example 3: E-commerce Product Catalog

```typescript
interface Product {
  id: string;
  name: string;
  description: string;
  price: number;
  originalPrice?: number;
  image: string;
  category: string;
  rating: number;
  reviews: number;
  inStock: boolean;
  tags: string[];
}

const ProductCatalog: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [filters, setFilters] = useState({
    category: '',
    priceRange: [0, 1000],
    inStock: false,
  });

  return (
    <div className="container mx-auto py-6">
      <div className="flex flex-col lg:flex-row gap-6">
        
        {/* Filters Sidebar */}
        <Card className="lg:w-80 h-fit">
          <CardHeader>
            <CardTitle>Filters</CardTitle>
            <CardDescription>Refine your search</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            
            {/* Category Filter */}
            <div className="space-y-2">
              <Label>Category</Label>
              <Select 
                value={filters.category} 
                onValueChange={(value) => setFilters(prev => ({ ...prev, category: value }))}
              >
                <SelectTrigger>
                  <SelectValue placeholder="All Categories" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">All Categories</SelectItem>
                  <SelectItem value="electronics">Electronics</SelectItem>
                  <SelectItem value="clothing">Clothing</SelectItem>
                  <SelectItem value="books">Books</SelectItem>
                  <SelectItem value="home">Home & Garden</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Price Range */}
            <div className="space-y-2">
              <Label>Price Range</Label>
              <div className="grid grid-cols-2 gap-2">
                <Input 
                  type="number" 
                  placeholder="Min" 
                  value={filters.priceRange[0]}
                  onChange={(e) => setFilters(prev => ({
                    ...prev,
                    priceRange: [Number(e.target.value), prev.priceRange[1]]
                  }))}
                />
                <Input 
                  type="number" 
                  placeholder="Max" 
                  value={filters.priceRange[1]}
                  onChange={(e) => setFilters(prev => ({
                    ...prev,
                    priceRange: [prev.priceRange[0], Number(e.target.value)]
                  }))}
                />
              </div>
            </div>

            {/* In Stock Filter */}
            <div className="flex items-center space-x-2">
              <input 
                type="checkbox" 
                id="inStock"
                checked={filters.inStock}
                onChange={(e) => setFilters(prev => ({ ...prev, inStock: e.target.checked }))}
                className="w-4 h-4"
              />
              <Label htmlFor="inStock">In Stock Only</Label>
            </div>

            <Button 
              variant="outline" 
              className="w-full"
              onClick={() => setFilters({ category: '', priceRange: [0, 1000], inStock: false })}
            >
              Clear Filters
            </Button>
          </CardContent>
        </Card>

        {/* Products Grid */}
        <div className="flex-1">
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
            {products.map((product) => (
              <Card key={product.id} className="group hover:shadow-lg transition-shadow duration-200">
                <div className="relative overflow-hidden rounded-t-lg">
                  <img 
                    src={product.image} 
                    alt={product.name}
                    className="w-full h-48 object-cover group-hover:scale-105 transition-transform duration-200"
                  />
                  {product.originalPrice && (
                    <Badge className="absolute top-2 left-2 bg-red-500">
                      Sale
                    </Badge>
                  )}
                  {!product.inStock && (
                    <div className="absolute inset-0 bg-black/50 flex items-center justify-center">
                      <Badge variant="destructive">Out of Stock</Badge>
                    </div>
                  )}
                </div>

                <CardContent className="p-4">
                  <div className="space-y-2">
                    <h3 className="font-semibold text-lg line-clamp-1">{product.name}</h3>
                    <p className="text-sm text-muted-foreground line-clamp-2">
                      {product.description}
                    </p>
                    
                    {/* Rating */}
                    <div className="flex items-center space-x-1">
                      <div className="flex text-yellow-400">
                        {[...Array(5)].map((_, i) => (
                          <span key={i} className={i < product.rating ? 'text-yellow-400' : 'text-gray-300'}>
                            ★
                          </span>
                        ))}
                      </div>
                      <span className="text-sm text-muted-foreground">
                        ({product.reviews})
                      </span>
                    </div>

                    {/* Price */}
                    <div className="flex items-center space-x-2">
                      <span className="text-lg font-bold">${product.price}</span>
                      {product.originalPrice && (
                        <span className="text-sm text-muted-foreground line-through">
                          ${product.originalPrice}
                        </span>
                      )}
                    </div>

                    {/* Tags */}
                    <div className="flex flex-wrap gap-1">
                      {product.tags.slice(0, 2).map((tag) => (
                        <Badge key={tag} variant="outline" className="text-xs">
                          {tag}
                        </Badge>
                      ))}
                    </div>
                  </div>
                </CardContent>

                <div className="p-4 pt-0">
                  <Button 
                    className="w-full" 
                    disabled={!product.inStock}
                    variant={product.inStock ? "default" : "secondary"}
                  >
                    {product.inStock ? "Add to Cart" : "Notify When Available"}
                  </Button>
                </div>
              </Card>
            ))}
          </div>

          {/* Load More */}
          <div className="flex justify-center mt-8">
            <Button variant="outline" size="lg">
              Load More Products
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
};
```

## shadcn/ui Component Selection Guide

### Field Type to Component Mapping

| Field Type | shadcn Component | Use Case | Tailwind Classes |
|------------|------------------|----------|------------------|
| Text Input | `Input` | Single line text | `w-full focus-visible:ring-2` |
| Email | `Input type="email"` | Email addresses | `w-full invalid:border-destructive` |
| Password | `Input type="password"` | Sensitive text | `w-full font-mono` |
| Number | `Input type="number"` | Numeric input | `w-full text-right` |
| Textarea | `Textarea` | Multi-line text | `min-h-[80px] resize-none` |
| Select | `Select` | Single choice | `w-full` |
| Multi-select | `Combobox` | Multiple choices | `w-full` |
| Checkbox | `Checkbox` | Boolean values | `data-[state=checked]:bg-primary` |
| Switch | `Switch` | Toggle states | `data-[state=checked]:bg-primary` |
| Radio | `RadioGroup` | Single choice | `grid grid-cols-2 gap-4` |
| Date | `DatePicker` | Date selection | `w-full` |
| File Upload | Custom + `Input` | File selection | `cursor-pointer` |

### Responsive Design with Tailwind

```typescript
const responsiveClasses = {
  // Grid layouts
  bento: "grid auto-rows-[200px] grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4",
  cards: "grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6",
  
  // Form layouts
  form: "space-y-4 sm:space-y-6",
  formGrid: "grid grid-cols-1 sm:grid-cols-2 gap-4",
  
  // Button layouts
  actions: "flex flex-col sm:flex-row gap-2 sm:gap-4",
  
  // Text sizing
  heading: "text-2xl sm:text-3xl lg:text-4xl font-bold",
  subheading: "text-lg sm:text-xl lg:text-2xl",
  body: "text-sm sm:text-base",
};
```

### Theme Integration

```typescript
// Custom CSS variables for theming
const themeVariables = {
  light: {
    '--background': '0 0% 100%',
    '--foreground': '222.2 84% 4.9%',
    '--primary': '222.2 47.4% 11.2%',
    '--primary-foreground': '210 40% 98%',
  },
  dark: {
    '--background': '222.2 84% 4.9%',
    '--foreground': '210 40% 98%',
    '--primary': '210 40% 98%',
    '--primary-foreground': '222.2 84% 4.9%',
  }
};

// Usage in components
const ThemedCard = ({ children, className }) => (
  <Card className={cn(
    "bg-background text-foreground border-border",
    "hover:bg-accent hover:text-accent-foreground transition-colors",
    className
  )}>
    {children}
  </Card>
);
```

### Performance Optimization Patterns

```typescript
// Memoized component with Tailwind classes
const MemoizedProductCard = memo(({ product }) => (
  <Card className="group hover:shadow-lg transition-all duration-200 hover:scale-[1.02]">
    <CardContent className="p-0">
      <div className="relative overflow-hidden rounded-t-lg">
        <img 
          src={product.image}
          alt={product.name}
          className="w-full h-48 object-cover group-hover:scale-105 transition-transform duration-300"
          loading="lazy"
        />
      </div>
      <div className="p-4 space-y-2">
        <h3 className="font-semibold line-clamp-2">{product.name}</h3>
        <p className="text-muted-foreground text-sm line-clamp-3">
          {product.description}
        </p>
      </div>
    </CardContent>
  </Card>
));
```

### Accessibility Best Practices

```typescript
// Accessible form with proper ARIA labels
const AccessibleForm = () => (
  <Form>
    <FormField
      name="email"
      render={({ field, fieldState }) => (
        <FormItem>
          <FormLabel htmlFor="email">
            Email Address
            <span className="text-destructive ml-1" aria-label="required">*</span>
          </FormLabel>
          <FormControl>
            <Input
              {...field}
              id="email"
              type="email"
              aria-describedby="email-description email-error"
              aria-invalid={fieldState.error ? "true" : "false"}
              className={cn(
                "w-full",
                fieldState.error && "border-destructive focus-visible:ring-destructive"
              )}
            />
          </FormControl>
          <FormDescription id="email-description">
            We'll never share your email with anyone else.
          </FormDescription>
          <FormMessage id="email-error" />
        </FormItem>
      )}
    />
  </Form>
);
```

## Advanced shadcn/ui Patterns

### Complex Form Layouts with shadcn

```typescript
// Multi-step form with shadcn components
const MultiStepForm = () => {
  const [currentStep, setCurrentStep] = useState(0);
  const steps = ['Personal Info', 'Preferences', 'Review'];

  return (
    <Card className="w-full max-w-4xl mx-auto">
      <CardHeader>
        <CardTitle>Multi-Step Registration</CardTitle>
        <CardDescription>
          Complete all steps to create your account
        </CardDescription>
        
        {/* Progress indicator */}
        <div className="mt-4">
          <Progress value={(currentStep + 1) / steps.length * 100} className="h-2" />
          <div className="flex justify-between mt-2">
            {steps.map((step, index) => (
              <Badge 
                key={step}
                variant={index <= currentStep ? "default" : "outline"}
                className="text-xs"
              >
                {index + 1}. {step}
              </Badge>
            ))}
          </div>
        </div>
      </CardHeader>
      
      <CardContent>
        <Tabs value={currentStep.toString()} className="w-full">
          <TabsContent value="0" className="space-y-4">
            <PersonalInfoStep />
          </TabsContent>
          <TabsContent value="1" className="space-y-4">
            <PreferencesStep />
          </TabsContent>
          <TabsContent value="2" className="space-y-4">
            <ReviewStep />
          </TabsContent>
        </Tabs>
        
        <div className="flex justify-between mt-6">
          <Button 
            variant="outline" 
            onClick={() => setCurrentStep(prev => Math.max(0, prev - 1))}
            disabled={currentStep === 0}
          >
            Previous
          </Button>
          <Button 
            onClick={() => setCurrentStep(prev => Math.min(steps.length - 1, prev + 1))}
            disabled={currentStep === steps.length - 1}
          >
            Next
          </Button>
        </div>
      </CardContent>
    </Card>
  );
};
```

### Data Visualization with shadcn Charts

```typescript
// Dashboard with charts and metrics
const AnalyticsDashboard = () => {
  return (
    <div className="grid auto-rows-[300px] grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      
      {/* KPI Cards */}
      <Card className="col-span-1">
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Total Revenue</CardTitle>
          <svg className="h-4 w-4 text-muted-foreground" /* icon */ />
        </CardHeader>
        <CardContent>
          <div className="text-2xl font-bold">$45,231.89</div>
          <p className="text-xs text-muted-foreground">
            <span className="text-green-600">+20.1%</span> from last month
          </p>
          <Progress value={75} className="mt-3 h-2" />
        </CardContent>
      </Card>

      {/* Chart Card - spans 2 columns */}
      <Card className="col-span-1 sm:col-span-2">
        <CardHeader>
          <CardTitle>Sales Overview</CardTitle>
          <CardDescription>Monthly sales data for the last 6 months</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="h-[200px] w-full">
            {/* Chart component would go here */}
            <div className="flex items-center justify-center h-full border-2 border-dashed border-muted">
              <p className="text-muted-foreground">Chart Placeholder</p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Activity Feed */}
      <Card className="col-span-1 row-span-2">
        <CardHeader>
          <CardTitle>Recent Activity</CardTitle>
        </CardHeader>
        <CardContent>
          <ScrollArea className="h-[400px]">
            <div className="space-y-4">
              {[...Array(10)].map((_, i) => (
                <div key={i} className="flex items-center space-x-3">
                  <Avatar className="h-8 w-8">
                    <AvatarFallback>U{i}</AvatarFallback>
                  </Avatar>
                  <div className="flex-1 space-y-1">
                    <p className="text-sm font-medium">User action {i + 1}</p>
                    <p className="text-xs text-muted-foreground">
                      {i + 1} minutes ago
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </ScrollArea>
        </CardContent>
      </Card>
    </div>
  );
};
```

### Advanced Table with shadcn DataTable

```typescript
// Enhanced data table with sorting, filtering, and pagination
const AdvancedDataTable = <TData, TValue>({
  columns,
  data,
}: {
  columns: ColumnDef<TData, TValue>[];
  data: TData[];
}) => {
  const [sorting, setSorting] = useState<SortingState>([]);
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([]);
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({});
  const [rowSelection, setRowSelection] = useState({});

  const table = useReactTable({
    data,
    columns,
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    onColumnVisibilityChange: setColumnVisibility,
    onRowSelectionChange: setRowSelection,
    state: {
      sorting,
      columnFilters,
      columnVisibility,
      rowSelection,
    },
  });

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle>Data Management</CardTitle>
            <CardDescription>
              {table.getFilteredRowModel().rows.length} of {data.length} row(s) shown
            </CardDescription>
          </div>
          
          {/* Table controls */}
          <div className="flex items-center space-x-2">
            <Input
              placeholder="Filter..."
              value={(table.getColumn("name")?.getFilterValue() as string) ?? ""}
              onChange={(event) =>
                table.getColumn("name")?.setFilterValue(event.target.value)
              }
              className="max-w-sm"
            />
            
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="outline" className="ml-auto">
                  Columns
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {table
                  .getAllColumns()
                  .filter((column) => column.getCanHide())
                  .map((column) => {
                    return (
                      <DropdownMenuItem
                        key={column.id}
                        className="capitalize"
                        onClick={() => column.toggleVisibility()}
                      >
                        <Checkbox
                          checked={column.getIsVisible()}
                          className="mr-2"
                        />
                        {column.id}
                      </DropdownMenuItem>
                    );
                  })}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>
      </CardHeader>
      
      <CardContent>
        <div className="rounded-md border">
          <Table>
            <TableHeader>
              {table.getHeaderGroups().map((headerGroup) => (
                <TableRow key={headerGroup.id}>
                  {headerGroup.headers.map((header) => {
                    return (
                      <TableHead key={header.id}>
                        {header.isPlaceholder
                          ? null
                          : flexRender(
                              header.column.columnDef.header,
                              header.getContext()
                            )}
                      </TableHead>
                    );
                  })}
                </TableRow>
              ))}
            </TableHeader>
            <TableBody>
              {table.getRowModel().rows?.length ? (
                table.getRowModel().rows.map((row) => (
                  <TableRow
                    key={row.id}
                    data-state={row.getIsSelected() && "selected"}
                    className="hover:bg-muted/50 transition-colors"
                  >
                    {row.getVisibleCells().map((cell) => (
                      <TableCell key={cell.id}>
                        {flexRender(
                          cell.column.columnDef.cell,
                          cell.getContext()
                        )}
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell
                    colSpan={columns.length}
                    className="h-24 text-center"
                  >
                    No results.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
        
        {/* Pagination */}
        <div className="flex items-center justify-between space-x-2 py-4">
          <div className="flex-1 text-sm text-muted-foreground">
            {table.getFilteredSelectedRowModel().rows.length} of{" "}
            {table.getFilteredRowModel().rows.length} row(s) selected.
          </div>
          <div className="space-x-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => table.previousPage()}
              disabled={!table.getCanPreviousPage()}
            >
              Previous
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => table.nextPage()}
              disabled={!table.getCanNextPage()}
            >
              Next
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
};
```

### Mobile-Optimized Components

```typescript
// Mobile-first responsive design patterns
const MobileOptimizedDashboard = () => {
  const [isMobile, setIsMobile] = useState(false);

  useEffect(() => {
    const checkDevice = () => setIsMobile(window.innerWidth < 768);
    checkDevice();
    window.addEventListener('resize', checkDevice);
    return () => window.removeEventListener('resize', checkDevice);
  }, []);

  return (
    <div className="container mx-auto p-4 space-y-4">
      {/* Mobile Stack Layout */}
      <div className={cn(
        "grid gap-4",
        isMobile 
          ? "grid-cols-1" 
          : "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
      )}>
        
        {/* Collapsible sections on mobile */}
        <Card className="col-span-full">
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle className="text-lg sm:text-xl">Quick Actions</CardTitle>
              {isMobile && (
                <Button variant="ghost" size="sm">
                  <MoreHorizontal className="h-4 w-4" />
                </Button>
              )}
            </div>
          </CardHeader>
          <CardContent>
            <div className={cn(
              "grid gap-2",
              isMobile ? "grid-cols-2" : "grid-cols-4"
            )}>
              <Button size="sm" className="w-full">
                <Plus className="h-4 w-4 mr-2" />
                Add
              </Button>
              <Button size="sm" variant="outline" className="w-full">
                <Edit className="h-4 w-4 mr-2" />
                Edit
              </Button>
              <Button size="sm" variant="outline" className="w-full">
                Export
              </Button>
              <Button size="sm" variant="outline" className="w-full">
                Settings
              </Button>
            </div>
          </CardContent>
        </Card>

        {/* Touch-friendly components */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base sm:text-lg">Status</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {['Active', 'Pending', 'Completed'].map((status) => (
                <div key={status} className="flex items-center justify-between">
                  <Label className="text-sm">{status}</Label>
                  <Switch 
                    size={isMobile ? "default" : "sm"}
                    className="data-[state=checked]:bg-primary"
                  />
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* Responsive text sizing */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base sm:text-lg lg:text-xl">
              Metrics
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div>
                <p className="text-xs sm:text-sm text-muted-foreground">Total Users</p>
                <p className="text-xl sm:text-2xl lg:text-3xl font-bold">1,234</p>
              </div>
              <Progress value={75} className="h-2 sm:h-3" />
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};
```

## Component Composition Patterns

### Compound Components with shadcn

```typescript
// Creating reusable compound components
const StatCard = ({ children, className, ...props }) => (
  <Card className={cn("transition-all duration-200 hover:shadow-md", className)} {...props}>
    {children}
  </Card>
);

const StatCardHeader = ({ title, icon, trend }) => (
  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
    <CardTitle className="text-sm font-medium">{title}</CardTitle>
    {icon && <div className="h-4 w-4 text-muted-foreground">{icon}</div>}
  </CardHeader>
);

const StatCardContent = ({ value, description, trend, progress }) => (
  <CardContent>
    <div className="text-2xl font-bold">{value}</div>
    {description && (
      <p className="text-xs text-muted-foreground">
        {trend && (
          <span className={cn(
            "font-medium",
            trend.type === 'increase' ? 'text-green-600' : 'text-red-600'
          )}>
            {trend.type === 'increase' ? '+' : ''}{trend.value}
          </span>
        )}
        {description}
      </p>
    )}
    {progress && (
      <Progress value={progress} className="mt-3 h-2" />
    )}
  </CardContent>
);

// Usage
const DashboardStats = () => (
  <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
    <StatCard>
      <StatCardHeader 
        title="Total Revenue" 
        icon={<DollarSign />}
      />
      <StatCardContent 
        value="$45,231.89"
        description=" from last month"
        trend={{ type: 'increase', value: '20.1%' }}
        progress={75}
      />
    </StatCard>
  </div>
);
```

### Error Handling and Loading States

```typescript
// Comprehensive error and loading patterns
const AsyncDataComponent = ({ dataFetcher }) => {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const result = await dataFetcher();
        setData(result);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [dataFetcher]);

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <Skeleton className="h-4 w-[250px]" />
          <Skeleton className="h-4 w-[200px]" />
        </CardHeader>
        <CardContent className="space-y-2">
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-3/4" />
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertCircle className="h-4 w-4" />
        <AlertTitle>Error</AlertTitle>
        <AlertDescription>
          {error}
          <Button 
            variant="outline" 
            size="sm" 
            className="mt-2"
            onClick={() => window.location.reload()}
          >
            Try Again
          </Button>
        </AlertDescription>
      </Alert>
    );
  }

  return (
    <Card>
      <CardContent>
        {/* Render data */}
        <pre className="text-sm">{JSON.stringify(data, null, 2)}</pre>
      </CardContent>
    </Card>
  );
};
```

These examples demonstrate comprehensive implementations using shadcn/ui components and Tailwind CSS, following best practices for responsive design, accessibility, and performance optimization within the Bento Grid system.