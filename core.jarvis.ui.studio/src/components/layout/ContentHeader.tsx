import { ReactNode } from 'react';

interface ContentHeaderProps {
  title: string;
  description?: string;
  breadcrumbs?: ReactNode;
  actions?: ReactNode;
}

export function ContentHeader({ 
  title, 
  description, 
  breadcrumbs,
  actions 
}: ContentHeaderProps) {
  return (
    <div className="border-b border-muted">
      <div className="px-lg py-8">
        <div className="flex flex-col gap-6">
          {breadcrumbs && (
            <div className="text-sm text-muted-foreground">
              {breadcrumbs}
            </div>
          )}
          
          <div className="flex flex-col md:flex-row md:items-start md:justify-between gap-4">
            <div className="flex flex-col gap-2">
              <h1 className="text-3xl font-normal">{title}</h1>
              {description && (
                <p className="text-muted-foreground text-sm max-w-2xl">
                  {description}
                </p>
              )}
            </div>
            
            {actions && (
              <div className="flex items-center gap-2">
                {actions}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}