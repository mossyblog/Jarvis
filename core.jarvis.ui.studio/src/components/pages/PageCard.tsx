/**
 * Refined PageCard Component
 * 
 * A brand-consistent, visually refined card component for displaying
 * page information with reduced visual weight and improved hierarchy.
 */

import React, { useCallback } from 'react';
import { 
  Clock, 
  Eye, 
  Edit, 
  Copy, 
  Trash2, 
  MoreHorizontal,
  ExternalLink,
  Globe,
  Archive,
  FileText
} from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';

// UI Components
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';

// Types
import { RecentPageMetadata } from '@/utils/recentPagesManager';
import { cn } from '@/lib/utils';

interface PageCardProps {
  page: RecentPageMetadata;
  viewMode: 'grid' | 'list';
  compact?: boolean;
  onEdit?: (page: RecentPageMetadata) => void;
  onDuplicate?: (page: RecentPageMetadata) => void;
  onDelete?: (page: RecentPageMetadata) => void;
  onPreview?: (page: RecentPageMetadata) => void;
}

const generateThumbnailUrl = (pageId: string): string => {
  const colors = ['#eff6ff', '#f0fdf4', '#fef7ff', '#fff7ed', '#fdf2f8', '#f0f9ff'];
  const colorIndex = pageId.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0) % colors.length;
  const bgColor = colors[colorIndex];
  
  return `data:image/svg+xml,${encodeURIComponent(`
    <svg width="320" height="180" xmlns="http://www.w3.org/2000/svg">
      <rect width="100%" height="100%" fill="${bgColor}"/>
      <rect x="16" y="16" width="288" height="32" fill="#e2e8f0" rx="6"/>
      <rect x="16" y="64" width="200" height="16" fill="#cbd5e1" rx="4"/>
      <rect x="16" y="88" width="160" height="16" fill="#cbd5e1" rx="4"/>
      <rect x="240" y="64" width="64" height="48" fill="#94a3b8" rx="6"/>
      <circle cx="280" cy="140" r="8" fill="#60a5fa"/>
      <circle cx="300" cy="140" r="6" fill="#34d399"/>
    </svg>
  `)}`;
};

export const PageCard: React.FC<PageCardProps> = ({
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

  const getStatusVariant = (status: string) => {
    switch (status) {
      case 'published': return 'success';
      case 'draft': return 'warning';
      case 'archived': return 'secondary';
      default: return 'outline';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'published': return Globe;
      case 'draft': return Edit;
      case 'archived': return Archive;
      default: return FileText;
    }
  };

  const StatusIcon = getStatusIcon(page.status);
  const thumbnailUrl = page.thumbnailUrl || generateThumbnailUrl(page.id);
  
  if (viewMode === 'list') {
    return (
      <Card className="group hover:border-border/80 transition-all duration-200 cursor-pointer" onClick={handlePreview}>
        <CardContent className="p-3">
          <div className="flex items-center gap-3">
            {/* Thumbnail */}
            <div className="flex-shrink-0">
              <div className="w-3xl h-xl bg-surface-tertiary rounded-md overflow-hidden border border-border/30">
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
                  <div className="flex items-center gap-2">
                    <h3 className="font-medium text-sm text-primary truncate group-hover:text-accent transition-colors">
                      {page.displayName}
                    </h3>
                    <StatusIcon className="h-2xs w-2xs text-muted flex-shrink-0" />
                  </div>
                  <p className="text-xs text-tertiary mt-0.5 truncate">
                    {page.route}
                  </p>
                  {page.description && !compact && (
                    <p className="text-xs text-muted mt-1 line-clamp-1">
                      {page.description}
                    </p>
                  )}
                </div>

                {/* Metadata */}
                <div className="flex-shrink-0 text-right">
                  <Badge 
                    variant={getStatusVariant(page.status)} 
                    className="text-xs mb-1.5 px-1.5 py-0.5"
                  >
                    {page.status}
                  </Badge>
                  <div className="text-xs text-muted space-y-0.5">
                    <div className="flex items-center gap-1 justify-end">
                      <Clock className="h-xs.5 w-xs.5" />
                      <span>{formatDistanceToNow(new Date(page.lastAccessed), { addSuffix: true })}</span>
                    </div>
                    <div className="flex items-center gap-1 justify-end">
                      <Eye className="h-xs.5 w-xs.5" />
                      <span>{page.accessCount}</span>
                    </div>
                  </div>
                </div>
              </div>

              {/* Tags */}
              {page.tags && page.tags.length > 0 && !compact && (
                <div className="flex flex-wrap gap-1 mt-2">
                  {page.tags.slice(0, 2).map((tag) => (
                    <Badge key={tag} variant="outline" className="text-xs px-1.5 py-0 h-sm">
                      {tag}
                    </Badge>
                  ))}
                  {page.tags.length > 2 && (
                    <Badge variant="outline" className="text-xs px-1.5 py-0 h-sm">
                      +{page.tags.length - 2}
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
                    size="sm" 
                    className="opacity-0 group-hover:opacity-100 transition-opacity h-md w-md p-0"
                    onClick={(e) => e.stopPropagation()}
                  >
                    <MoreHorizontal className="h-2xs.5 w-2xs.5" />
                    <span className="sr-only">Page actions</span>
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-2xs6">
                  <DropdownMenuItem onClick={handleEdit} className="gap-2 text-xs">
                    <Edit className="h-2xs w-2xs" />
                    Edit
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={handleDuplicate} className="gap-2 text-xs">
                    <Copy className="h-2xs w-2xs" />
                    Duplicate
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={handlePreview} className="gap-2 text-xs">
                    <ExternalLink className="h-2xs w-2xs" />
                    Preview
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={handleDelete} className="gap-2 text-xs text-destructive">
                    <Trash2 className="h-2xs w-2xs" />
                    Remove
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
    <Card className="group hover:border-border/80 hover:shadow-md transition-all duration-200 cursor-pointer" onClick={handlePreview}>
      <CardHeader className="p-0">
        <div className="aspect-[16/9] w-full bg-surface-tertiary rounded-t-lg overflow-hidden border-b border-border/30">
          <img 
            src={thumbnailUrl}
            alt={`${page.displayName} thumbnail`}
            className="w-full h-full object-cover group-hover:scale-[1.02] transition-transform duration-200"
            loading="lazy"
          />
        </div>
      </CardHeader>
      <CardContent className={cn("p-3", compact && "p-2.5")}>
        <div className="space-y-2.5">
          {/* Header */}
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0 flex-1">
              <div className="flex items-center gap-1.5">
                <h3 className={cn(
                  "font-medium truncate group-hover:text-accent transition-colors",
                  compact ? "text-sm" : "text-sm"
                )}>
                  {page.displayName}
                </h3>
                <StatusIcon className="h-2xs w-2xs text-muted flex-shrink-0" />
              </div>
              <p className="text-xs text-tertiary mt-0.5 truncate">
                {page.route}
              </p>
            </div>
            <Badge 
              variant={getStatusVariant(page.status)} 
              className="text-xs flex-shrink-0 px-1.5 py-0.5"
            >
              {page.status}
            </Badge>
          </div>

          {/* Description */}
          {page.description && !compact && (
            <p className="text-xs text-muted line-clamp-2 leading-relaxed">
              {page.description}
            </p>
          )}

          {/* Metadata */}
          <div className="flex items-center justify-between text-xs text-muted">
            <div className="flex items-center gap-1">
              <Clock className="h-xs.5 w-xs.5" />
              <span>{formatDistanceToNow(new Date(page.lastAccessed), { addSuffix: true })}</span>
            </div>
            <div className="flex items-center gap-1">
              <Eye className="h-xs.5 w-xs.5" />
              <span>{page.accessCount}</span>
            </div>
          </div>

          {/* Tags */}
          {page.tags && page.tags.length > 0 && !compact && (
            <div className="flex flex-wrap gap-1">
              {page.tags.slice(0, 2).map((tag) => (
                <Badge key={tag} variant="outline" className="text-xs px-1.5 py-0 h-sm">
                  {tag}
                </Badge>
              ))}
              {page.tags.length > 2 && (
                <Badge variant="outline" className="text-xs px-1.5 py-0 h-sm">
                  +{page.tags.length - 2}
                </Badge>
              )}
            </div>
          )}

          {/* Actions */}
          <div className="flex items-center justify-between pt-1.5 border-t border-border/30">
            <div className="flex items-center gap-0.5">
              <TooltipProvider>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button variant="ghost" size="sm" onClick={handleEdit} className="h-md w-md p-0">
                      <Edit className="h-2xs w-2xs" />
                      <span className="sr-only">Edit page</span>
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>Edit</TooltipContent>
                </Tooltip>
              </TooltipProvider>

              <TooltipProvider>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button variant="ghost" size="sm" onClick={handleDuplicate} className="h-md w-md p-0">
                      <Copy className="h-2xs w-2xs" />
                      <span className="sr-only">Duplicate page</span>
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>Duplicate</TooltipContent>
                </Tooltip>
              </TooltipProvider>
            </div>

            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="sm" className="h-md w-md p-0" onClick={(e) => e.stopPropagation()}>
                  <MoreHorizontal className="h-2xs w-2xs" />
                  <span className="sr-only">More actions</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-2xs6">
                <DropdownMenuItem onClick={handlePreview} className="gap-2 text-xs">
                  <ExternalLink className="h-2xs w-2xs" />
                  Preview
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={handleDelete} className="gap-2 text-xs text-destructive">
                  <Trash2 className="h-2xs w-2xs" />
                  Remove
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>
      </CardContent>
    </Card>
  );
};

export default PageCard;