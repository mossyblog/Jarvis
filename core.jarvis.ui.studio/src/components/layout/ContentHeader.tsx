import { ReactNode } from 'react';

interface ContentHeaderProps {
  title: string;
  description?: string;
  breadcrumbs?: ReactNode;
  actions?: ReactNode;
  className?: string;
}

export function ContentHeader({ 
  title, 
  description, 
  breadcrumbs,
  actions 
}: ContentHeaderProps) {
  return (
    <header className="border-b border-border/30 bg-background/95 backdrop-blur-sm" role="banner">
      <div className="px-6 py-5">
        <div className="flex flex-col gap-4">
          {breadcrumbs && (
            <nav className="text-xs text-muted-foreground/80" aria-label="Breadcrumb navigation">
              {breadcrumbs}
            </nav>
          )}
          
          <div className="flex flex-col md:flex-row md:items-start md:justify-between gap-3">
            <div className="flex flex-col gap-1.5">
              <h1 className="text-2xl font-medium tracking-tight">{title}</h1>
              {description && (
                <p className="text-muted-foreground/80 text-sm max-w-2xl leading-relaxed">
                  {description}
                </p>
              )}
            </div>
            
            {actions && (
              <div className="flex items-center gap-2" role="toolbar" aria-label="Page actions">
                {actions}
              </div>
            )}
          </div>
        </div>
      </div>
    </header>
  );
}