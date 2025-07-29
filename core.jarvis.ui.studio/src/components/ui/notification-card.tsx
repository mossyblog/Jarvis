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
  warning: 'border-yellow-500/20 bg-yellow-500/5',
  info: 'border-blue-500/20 bg-blue-500/5',
  success: 'border-green-500/20 bg-green-500/5',
  error: 'border-red-500/20 bg-red-500/5',
};

const iconStyles: Record<NotificationVariant, string> = {
  warning: 'text-yellow-500',
  info: 'text-blue-500',
  success: 'text-green-500',
  error: 'text-red-500',
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