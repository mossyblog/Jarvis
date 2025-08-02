import React from 'react';
import { Card, CardContent } from './card';
import { cn } from '../../lib/utils';
import type { LucideIcon } from 'lucide-react';

export type NotificationVariant = 'warning' | 'info' | 'success' | 'error';

interface NotificationCardProps {
  variant?: NotificationVariant;
  icon?: LucideIcon;
  title: string;
  description?: string;
  children?: React.ReactNode;
  className?: string;
}

const variantStyles: Record<NotificationVariant, string> = {
  warning: 'border-warning/20 bg-warning/5',
  info: 'border-primary/20 bg-primary/5',
  success: 'border-success/20 bg-success/5',
  error: 'border-destructive/20 bg-destructive/5',
};

const iconStyles: Record<NotificationVariant, string> = {
  warning: 'text-warning',
  info: 'text-primary',
  success: 'text-success',
  error: 'text-destructive',
};

export function NotificationCard({
  variant = 'info',
  icon: Icon,
  title,
  description,
  children,
  className,
}: NotificationCardProps) {
  return (
    <Card className={cn(variantStyles[variant], className)}>
      <CardContent className="p-4">
        <div className="flex items-start space-x-3">
          {Icon && (
            <Icon className={cn('h-5 w-5 mt-0.5', iconStyles[variant])} />
          )}
          <div className="flex-1 space-y-3">
            <div>
              <h4 className="text-sm font-medium">{title}</h4>
              {description && (
                <p className="text-xs text-muted-foreground mt-1">
                  {description}
                </p>
              )}
            </div>
            {children}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}