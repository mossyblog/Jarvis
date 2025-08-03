/**
 * Recent Pages List Component
 * 
 * Displays user's recently accessed pages with thumbnails, metadata,
 * and quick access actions (edit, duplicate, delete).
 * 
 * Features:
 * - Grid layout with page thumbnails and metadata
 * - Quick actions for edit, duplicate, delete
 * - Search and filtering capabilities
 * - Mobile-first responsive design
 * - Integration with localStorage for tracking access
 * - Loading and error states
 * - Accessibility compliance
 * 
 * @module RecentPagesList
 */

import React, { useState, useCallback, useMemo } from 'react';
import { 
  Search, 
  Clock, 
  Eye, 
  Edit, 
  Copy, 
  Trash2, 
  MoreHorizontal,
  Filter,
  SortDesc,
  Grid3X3,
  List,
  Calendar,
  User,
  Tag,
  Globe,
  Lock,
  FileText,
  X,
  RefreshCw,
  AlertCircle,
  ExternalLink
} from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';

// UI Components
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Skeleton } from '@/components/ui/skeleton';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';

// Utilities and Types
import { useRecentPages, type RecentPageMetadata } from '@/utils/recentPagesManager';
import { cn } from '@/lib/utils';

// ============================================================================
// Types and Interfaces
// ============================================================================

export interface RecentPagesListProps {
  /** Maximum number of pages to display */
  limit?: number;
  /** Whether to show search and filter controls */
  showControls?: boolean;
  /** Default view mode */
  defaultViewMode?: 'grid' | 'list';
  /** Callback when a page is selected for editing */
  onEdit?: (page: RecentPageMetadata) => void;
  /** Callback when a page is duplicated */
  onDuplicate?: (page: RecentPageMetadata) => void;
  /** Callback when a page is deleted */
  onDelete?: (page: RecentPageMetadata) => void;
  /** Callback when a page is previewed */
  onPreview?: (page: RecentPageMetadata) => void;
  /** Additional CSS classes */
  className?: string;
  /** Whether to auto-refresh on storage changes */
  autoRefresh?: boolean;
  /** Compact mode for smaller spaces */
  compact?: boolean;
}

type ViewMode = 'grid' | 'list';
type SortOption = 'recent' | 'name' | 'created' | 'updated' | 'access-count';
type StatusFilter = 'all' | 'draft' | 'published' | 'archived';

interface PageCardProps {
  page: RecentPageMetadata;
  viewMode: ViewMode;
  compact?: boolean;
  onEdit?: (page: RecentPageMetadata) => void;
  onDuplicate?: (page: RecentPageMetadata) => void;
  onDelete?: (page: RecentPageMetadata) => void;
  onPreview?: (page: RecentPageMetadata) => void;
}

// ============================================================================
// Mock Thumbnails for Demo
// ============================================================================

const generateThumbnailUrl = (pageId: string): string => {
  // Generate a consistent placeholder thumbnail based on page ID
  const colors = ['bg-blue-100', 'bg-green-100', 'bg-purple-100', 'bg-orange-100', 'bg-pink-100', 'bg-indigo-100'];
  const colorIndex = pageId.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0) % colors.length;
  return `data:image/svg+xml,${encodeURIComponent(`
    <svg width="320" height="180" xmlns="http://www.w3.org/2000/svg">
      <rect width="100%" height="100%" fill="#f8fafc"/>
      <rect x="20" y="20" width="280" height="40" fill="#e2e8f0" rx="4"/>
      <rect x="20" y="80" width="180" height="20" fill="#cbd5e1" rx="2"/>
      <rect x="20" y="110" width="120" height="20" fill="#cbd5e1" rx="2"/>
      <rect x="220" y="80" width="80" height="60" fill="#94a3b8" rx="4"/>
    </svg>
  `)}`;
};

// ============================================================================
// Page Card Component
// ============================================================================

const PageCard: React.FC<PageCardProps> = ({
  page,
  viewMode,
  compact = false,
  onEdit,
  onDuplicate,
  onDelete,
  onPreview
}) => {
  const handleEdit = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
    onEdit?.(page);
  }, [onEdit, page]);

  const handleDuplicate = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
    onDuplicate?.(page);
  }, [onDuplicate, page]);

  const handleDelete = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
    onDelete?.(page);
  }, [onDelete, page]);

  const handlePreview = useCallback(() => {
    onPreview?.(page);
  }, [onPreview, page]);

  const statusColor = {
    draft: 'bg-orange-100 text-orange-800 border-orange-200',
    published: 'bg-green-100 text-green-800 border-green-200',
    archived: 'bg-gray-100 text-gray-800 border-gray-200'
  }[page.status];

  const thumbnailUrl = page.thumbnailUrl || generateThumbnailUrl(page.id);
  
  if (viewMode === 'list') {
    return (
      <Card className="hover:shadow-md transition-shadow duration-200 cursor-pointer group" onClick={handlePreview}>
        <CardContent className="p-3">
          <div className="flex items-center gap-4">
            {/* Thumbnail */}
            <div className="flex-shrink-0">
              <div className="w-20 h-12 bg-muted rounded overflow-hidden border">
                <img 
                  src={thumbnailUrl}
                  alt={`${page.displayName} thumbnail`}
                  className="w-full h-full object-cover"
                  loading="lazy"
                />
              </div>
            </div>

            {/* Content */}
            <div className="flex-1 min-w-0">
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0 flex-1">
                  <h3 className="font-semibold text-sm md:text-base truncate group-hover:text-primary transition-colors">
                    {page.displayName}
                  </h3>
                  <p className="text-xs md:text-sm text-muted-foreground mt-1">
                    {page.route}
                  </p>
                  {page.description && !compact && (
                    <p className="text-xs text-muted-foreground mt-2 line-clamp-2">
                      {page.description}
                    </p>
                  )}
                </div>

                {/* Metadata */}
                <div className="flex-shrink-0 text-right">
                  <Badge variant="outline" className={cn("text-xs mb-1", statusColor)}>
                    {page.status}
                  </Badge>
                  <div className="text-xs text-muted-foreground space-y-0.5">
                    <div className="flex items-center gap-1 justify-end">
                      <Clock className="h-3 w-3" />
                      <span>{formatDistanceToNow(new Date(page.lastAccessed), { addSuffix: true })}</span>
                    </div>
                    <div className="flex items-center gap-1 justify-end">
                      <Eye className="h-3 w-3" />
                      <span>{page.accessCount} view{page.accessCount !== 1 ? 's' : ''}</span>
                    </div>
                  </div>
                </div>
              </div>

              {/* Tags */}
              {page.tags && page.tags.length > 0 && !compact && (
                <div className="flex flex-wrap gap-1 mt-2">
                  {page.tags.slice(0, 3).map((tag) => (
                    <Badge key={tag} variant="secondary" className="text-xs px-1.5 py-0.5">
                      {tag}
                    </Badge>
                  ))}
                  {page.tags.length > 3 && (
                    <Badge variant="secondary" className="text-xs px-1.5 py-0.5">
                      +{page.tags.length - 3}
                    </Badge>
                  )}
                </div>
              )}
            </div>

            {/* Actions */}
            <div className="flex-shrink-0">
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button 
                    variant="ghost" 
                    size="xs" 
                    className="opacity-0 group-hover:opacity-100 transition-opacity h-6 w-6 p-0"
                    onClick={(e) => e.stopPropagation()}
                  >
                    <MoreHorizontal className="h-3 w-3" />
                    <span className="sr-only">Page actions</span>
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onClick={handleEdit} className="gap-2">
                    <Edit className="h-4 w-4" />
                    Edit
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={handleDuplicate} className="gap-2">
                    <Copy className="h-4 w-4" />
                    Duplicate
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={handlePreview} className="gap-2">
                    <ExternalLink className="h-4 w-4" />
                    Preview
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={handleDelete} className="gap-2 text-destructive">
                    <Trash2 className="h-4 w-4" />
                    Remove from Recent
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>
        </CardContent>
      </Card>
    );
  }

  // Grid view
  return (
    <Card className="hover:shadow-lg transition-all duration-200 cursor-pointer group" onClick={handlePreview}>
      <CardHeader className="p-0">
        <div className="aspect-video w-full bg-muted rounded-t-lg overflow-hidden border-b">
          <img 
            src={thumbnailUrl}
            alt={`${page.displayName} thumbnail`}
            className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200"
            loading="lazy"
          />
        </div>
      </CardHeader>
      <CardContent className={cn("p-3", compact && "p-2")}>
        <div className="space-y-2">
          {/* Header */}
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0 flex-1">
              <h3 className={cn(
                "font-semibold truncate group-hover:text-primary transition-colors",
                compact ? "text-sm" : "text-base"
              )}>
                {page.displayName}
              </h3>
              <p className="text-xs text-muted-foreground mt-1 truncate">
                {page.route}
              </p>
            </div>
            <Badge variant="outline" className={cn("text-xs flex-shrink-0 px-1.5 py-0.5", statusColor)}>
              {page.status}
            </Badge>
          </div>

          {/* Description */}
          {page.description && !compact && (
            <p className="text-sm text-muted-foreground line-clamp-2">
              {page.description}
            </p>
          )}

          {/* Metadata */}
          <div className="flex items-center justify-between text-xs text-muted-foreground">
            <div className="flex items-center gap-1">
              <Clock className="h-3 w-3" />
              <span>{formatDistanceToNow(new Date(page.lastAccessed), { addSuffix: true })}</span>
            </div>
            <div className="flex items-center gap-1">
              <Eye className="h-3 w-3" />
              <span>{page.accessCount}</span>
            </div>
          </div>

          {/* Tags */}
          {page.tags && page.tags.length > 0 && !compact && (
            <div className="flex flex-wrap gap-1">
              {page.tags.slice(0, 2).map((tag) => (
                <Badge key={tag} variant="secondary" className="text-xs px-1.5 py-0.5">
                  {tag}
                </Badge>
              ))}
              {page.tags.length > 2 && (
                <Badge variant="secondary" className="text-xs px-1.5 py-0.5">
                  +{page.tags.length - 2}
                </Badge>
              )}
            </div>
          )}

          {/* Actions */}
          <div className="flex items-center justify-between pt-1.5 border-t">
            <div className="flex items-center gap-0.5">
              <TooltipProvider>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button variant="ghost" size="xs" onClick={handleEdit} className="h-6 w-6 p-0">
                      <Edit className="h-3 w-3" />
                      <span className="sr-only">Edit page</span>
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>Edit</TooltipContent>
                </Tooltip>
              </TooltipProvider>

              <TooltipProvider>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button variant="ghost" size="xs" onClick={handleDuplicate} className="h-6 w-6 p-0">
                      <Copy className="h-3 w-3" />
                      <span className="sr-only">Duplicate page</span>
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>Duplicate</TooltipContent>
                </Tooltip>
              </TooltipProvider>
            </div>

            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="xs" className="h-6 w-6 p-0" onClick={(e) => e.stopPropagation()}>
                  <MoreHorizontal className="h-3 w-3" />
                  <span className="sr-only">More actions</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onClick={handlePreview} className="gap-2">
                  <ExternalLink className="h-4 w-4" />
                  Preview
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={handleDelete} className="gap-2 text-destructive">
                  <Trash2 className="h-4 w-4" />
                  Remove from Recent
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>
      </CardContent>
    </Card>
  );
};

// ============================================================================
// Loading Skeleton Component
// ============================================================================

const PageCardSkeleton: React.FC<{ viewMode: ViewMode; compact?: boolean }> = ({ viewMode, compact }) => {
  if (viewMode === 'list') {
    return (
      <Card>
        <CardContent className="p-4">
          <div className="flex items-center gap-4">
            <Skeleton className="w-20 h-12 rounded" />
            <div className="flex-1 space-y-2">
              <Skeleton className="h-4 w-1/3" />
              <Skeleton className="h-3 w-1/2" />
              {!compact && <Skeleton className="h-3 w-2/3" />}
            </div>
            <div className="flex-shrink-0 space-y-1">
              <Skeleton className="h-5 w-16" />
              <Skeleton className="h-3 w-20" />
            </div>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader className="p-0">
        <Skeleton className="aspect-video w-full rounded-t-lg" />
      </CardHeader>
      <CardContent className={cn("p-4 space-y-3", compact && "p-3")}>
        <div className="flex justify-between items-start">
          <div className="space-y-1 flex-1">
            <Skeleton className="h-4 w-2/3" />
            <Skeleton className="h-3 w-1/2" />
          </div>
          <Skeleton className="h-5 w-16" />
        </div>
        {!compact && <Skeleton className="h-3 w-full" />}
        <div className="flex justify-between">
          <Skeleton className="h-3 w-20" />
          <Skeleton className="h-3 w-12" />
        </div>
      </CardContent>
    </Card>
  );
};

// ============================================================================
// Main Component
// ============================================================================

export const RecentPagesList: React.FC<RecentPagesListProps> = ({
  limit = 20,
  showControls = true,
  defaultViewMode = 'grid',
  onEdit,
  onDuplicate,
  onDelete,
  onPreview,
  className,
  autoRefresh = true,
  compact = false
}) => {
  // State
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [sortBy, setSortBy] = useState<SortOption>('recent');
  const [viewMode, setViewMode] = useState<ViewMode>(defaultViewMode);

  // Fetch recent pages
  const { recentPages, isLoading, statistics, actions } = useRecentPages({
    limit,
    autoRefresh,
    searchTerm: searchTerm || undefined,
    status: statusFilter === 'all' ? undefined : statusFilter
  });

  // Filter and sort pages
  const filteredAndSortedPages = useMemo(() => {
    let pages = [...recentPages];

    // Apply client-side search if not handled by the hook
    if (searchTerm) {
      const searchLower = searchTerm.toLowerCase();
      pages = pages.filter(page => 
        page.displayName.toLowerCase().includes(searchLower) ||
        page.route.toLowerCase().includes(searchLower) ||
        page.description?.toLowerCase().includes(searchLower) ||
        page.tags?.some(tag => tag.toLowerCase().includes(searchLower))
      );
    }

    // Sort pages
    pages.sort((a, b) => {
      switch (sortBy) {
        case 'name':
          return a.displayName.localeCompare(b.displayName);
        case 'created':
          return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
        case 'updated':
          return new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime();
        case 'access-count':
          return b.accessCount - a.accessCount;
        case 'recent':
        default:
          return new Date(b.lastAccessed).getTime() - new Date(a.lastAccessed).getTime();
      }
    });

    return pages;
  }, [recentPages, searchTerm, sortBy]);

  // Event handlers
  const handleClearSearch = useCallback(() => {
    setSearchTerm('');
  }, []);

  const handleClearAll = useCallback(() => {
    if (confirm('Are you sure you want to clear all recent pages?')) {
      actions.clearAll();
    }
  }, [actions]);

  const handleRefresh = useCallback(() => {
    actions.refresh();
  }, [actions]);

  const handleDeletePage = useCallback((page: RecentPageMetadata) => {
    actions.removePage(page.id);
    onDelete?.(page);
  }, [actions, onDelete]);

  // Render states
  if (isLoading) {
    return (
      <div className={cn("space-y-4", className)}>
        {showControls && (
          <div className="flex flex-col sm:flex-row gap-4">
            <Skeleton className="h-10 flex-1" />
            <div className="flex gap-2">
              <Skeleton className="h-10 w-32" />
              <Skeleton className="h-10 w-32" />
              <Skeleton className="h-10 w-20" />
            </div>
          </div>
        )}
        <div className={cn(
          "grid gap-4",
          viewMode === 'grid' 
            ? "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
            : "grid-cols-1"
        )}>
          {Array.from({ length: compact ? 4 : 8 }).map((_, i) => (
            <PageCardSkeleton key={i} viewMode={viewMode} compact={compact} />
          ))}
        </div>
      </div>
    );
  }

  if (filteredAndSortedPages.length === 0 && !searchTerm && statusFilter === 'all') {
    return (
      <div className={cn("text-center py-12", className)}>
        <div className="mx-auto max-w-sm">
          <div className="mx-auto h-12 w-12 text-muted-foreground mb-4">
            <Clock className="h-full w-full" />
          </div>
          <h3 className="text-lg font-medium mb-2">No Recent Pages</h3>
          <p className="text-muted-foreground text-sm mb-6">
            Start working on pages to see them appear here. Your recently accessed pages will be tracked automatically.
          </p>
          <Button variant="outline" size="xs" onClick={handleRefresh} className="gap-xs">
            <RefreshCw className="h-3 w-3" />
            Refresh
          </Button>
        </div>
      </div>
    );
  }

  if (filteredAndSortedPages.length === 0) {
    return (
      <div className={cn("space-y-4", className)}>
        {showControls && (
          <div className="flex flex-col sm:flex-row gap-4">
            {/* Search */}
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search recent pages..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10 pr-10"
              />
              {searchTerm && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={handleClearSearch}
                  className="absolute right-1 top-1/2 transform -translate-y-1/2 h-6 w-6 p-0"
                >
                  <X className="h-3 w-3" />
                </Button>
              )}
            </div>

            {/* Filters and Controls */}
            <div className="flex gap-2">
              <Select value={statusFilter} onValueChange={(value) => setStatusFilter(value as StatusFilter)}>
                <SelectTrigger className="w-32">
                  <Filter className="h-4 w-4 mr-2" />
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Status</SelectItem>
                  <SelectItem value="draft">Draft</SelectItem>
                  <SelectItem value="published">Published</SelectItem>
                  <SelectItem value="archived">Archived</SelectItem>
                </SelectContent>
              </Select>

              <Select value={sortBy} onValueChange={(value) => setSortBy(value as SortOption)}>
                <SelectTrigger className="w-32">
                  <SortDesc className="h-4 w-4 mr-2" />
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="recent">Recent</SelectItem>
                  <SelectItem value="name">Name</SelectItem>
                  <SelectItem value="created">Created</SelectItem>
                  <SelectItem value="updated">Updated</SelectItem>
                  <SelectItem value="access-count">Most Viewed</SelectItem>
                </SelectContent>
              </Select>

              <div className="flex border rounded-md">
                <Button
                  variant={viewMode === 'grid' ? 'default' : 'ghost'}
                  size="sm"
                  onClick={() => setViewMode('grid')}
                  className="rounded-r-none border-r"
                >
                  <Grid3X3 className="h-4 w-4" />
                </Button>
                <Button
                  variant={viewMode === 'list' ? 'default' : 'ghost'}
                  size="sm"
                  onClick={() => setViewMode('list')}
                  className="rounded-l-none"
                >
                  <List className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </div>
        )}

        <Alert>
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>
            No pages match your current filters. Try adjusting your search or filter criteria.
          </AlertDescription>
        </Alert>
      </div>
    );
  }

  return (
    <div className={cn("space-y-4", className)}>
      {/* Controls */}
      {showControls && (
        <div className="space-y-3">
          <div className="flex flex-col sm:flex-row gap-4">
            {/* Search */}
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search recent pages..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10 pr-10"
              />
              {searchTerm && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={handleClearSearch}
                  className="absolute right-1 top-1/2 transform -translate-y-1/2 h-6 w-6 p-0"
                >
                  <X className="h-3 w-3" />
                </Button>
              )}
            </div>

            {/* Filters and Controls */}
            <div className="flex gap-2">
              <Select value={statusFilter} onValueChange={(value) => setStatusFilter(value as StatusFilter)}>
                <SelectTrigger className="w-32">
                  <Filter className="h-4 w-4 mr-2" />
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Status</SelectItem>
                  <SelectItem value="draft">Draft</SelectItem>
                  <SelectItem value="published">Published</SelectItem>
                  <SelectItem value="archived">Archived</SelectItem>
                </SelectContent>
              </Select>

              <Select value={sortBy} onValueChange={(value) => setSortBy(value as SortOption)}>
                <SelectTrigger className="w-32">
                  <SortDesc className="h-4 w-4 mr-2" />
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="recent">Recent</SelectItem>
                  <SelectItem value="name">Name</SelectItem>
                  <SelectItem value="created">Created</SelectItem>
                  <SelectItem value="updated">Updated</SelectItem>
                  <SelectItem value="access-count">Most Viewed</SelectItem>
                </SelectContent>
              </Select>

              <div className="flex border rounded-md">
                <Button
                  variant={viewMode === 'grid' ? 'default' : 'ghost'}
                  size="sm"
                  onClick={() => setViewMode('grid')}
                  className="rounded-r-none border-r"
                >
                  <Grid3X3 className="h-4 w-4" />
                </Button>
                <Button
                  variant={viewMode === 'list' ? 'default' : 'ghost'}
                  size="sm"
                  onClick={() => setViewMode('list')}
                  className="rounded-l-none"
                >
                  <List className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </div>

          {/* Stats and Actions */}
          <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
            <div className="flex flex-wrap gap-4 text-sm text-muted-foreground">
              <span>{filteredAndSortedPages.length} page{filteredAndSortedPages.length !== 1 ? 's' : ''}</span>
              {statistics.totalPages > 0 && (
                <>
                  <Separator orientation="vertical" className="h-4" />
                  <span>{statistics.draftPages} draft</span>
                  <span>{statistics.publishedPages} published</span>
                  <span>{statistics.archivedPages} archived</span>
                </>
              )}
            </div>

            <div className="flex gap-2">
              <Button variant="outline" size="xs" onClick={handleRefresh} className="gap-xs">
                <RefreshCw className="h-3 w-3" />
                Refresh
              </Button>
              {statistics.totalPages > 0 && (
                <Button variant="outline" size="xs" onClick={handleClearAll} className="gap-xs text-destructive">
                  <Trash2 className="h-3 w-3" />
                  Clear All
                </Button>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Pages Grid */}
      <div className={cn(
        "grid gap-4",
        viewMode === 'grid' 
          ? compact 
            ? "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3"
            : "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
          : "grid-cols-1"
      )}>
        {filteredAndSortedPages.map((page) => (
          <PageCard
            key={page.id}
            page={page}
            viewMode={viewMode}
            compact={compact}
            onEdit={onEdit}
            onDuplicate={onDuplicate}
            onDelete={handleDeletePage}
            onPreview={onPreview}
          />
        ))}
      </div>
    </div>
  );
};

export default RecentPagesList;