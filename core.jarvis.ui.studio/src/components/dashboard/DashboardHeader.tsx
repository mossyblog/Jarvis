import { Circle, ChevronDown } from 'lucide-react';
import { cn } from '../../lib/utils';
import { LucideIcon } from '../ui/icon';

interface DashboardHeaderProps {
  projectName: string;
  projectStatus: 'active' | 'paused' | 'inactive';
  tables: number;
  functions: number;
  replicas: number;
}

export function DashboardHeader({ 
  projectName, 
  projectStatus = 'active',
  tables,
  functions,
  replicas 
}: DashboardHeaderProps) {
  const statusColors = {
    active: 'text-brand',
    paused: 'text-warning',
    inactive: 'text-muted-foreground'
  };

  return (
    <div className="border-b border-muted">
      <div className="px-lg py-8">
        <div>
          <div className="flex flex-col gap-6">
            {/* Project Info */}
            <div className="flex flex-col md:flex-row md:items-center gap-6 justify-between">
              <div className="flex flex-col md:flex-row md:items-center gap-3">
                <h1 className="text-3xl font-normal">{projectName}</h1>
                <span className="text-sm text-muted-foreground bg-muted px-2 py-1 rounded">
                  NANO
                </span>
              </div>
              
              <div className="flex items-center gap-2">
                <LucideIcon 
                  icon={Circle}
                  size="xs"
                  className={cn('fill-current', statusColors[projectStatus])}
                />
                <span className="text-sm font-medium">Project Status</span>
              </div>
            </div>

            {/* Statistics */}
            <div className="flex flex-wrap gap-8 text-sm">
              <div className="flex flex-col gap-1">
                <span className="text-muted-foreground">Tables</span>
                <span className="text-2xl font-light">{tables}</span>
              </div>
              <div className="flex flex-col gap-1">
                <span className="text-muted-foreground">Functions</span>
                <span className="text-2xl font-light">{functions}</span>
              </div>
              <div className="flex flex-col gap-1">
                <span className="text-muted-foreground">Replicas</span>
                <span className="text-2xl font-light">{replicas}</span>
              </div>
            </div>

            {/* Time Range Selector */}
            <div className="flex items-center gap-2 text-sm">
              <span className="text-muted-foreground">Last 60 minutes</span>
              <button className="text-muted-foreground hover:text-foreground transition-colors p-xs rounded-sm hover:bg-muted/50">
                <LucideIcon 
                  icon={ChevronDown}
                  size="sm"
                />
              </button>
              <span className="text-muted-foreground ml-4">
                Statistics for last 60 minutes
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}