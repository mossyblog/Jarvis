import { RefreshCw } from 'lucide-react';
import { cn } from '@/lib/utils';

interface LoadingSpinnerProps {
  size?: number;
  className?: string;
}

export function LoadingSpinner({ size = 16, className }: LoadingSpinnerProps) {
  return (
    <RefreshCw 
      size={size} 
      className={cn("animate-spin text-muted-foreground", className)} 
    />
  );
}

interface LoadingStateProps {
  children: React.ReactNode;
  className?: string;
}

export function LoadingState({ children, className }: LoadingStateProps) {
  return (
    <div className={cn("flex items-center justify-center py-8", className)}>
      <div className="flex items-center gap-2 text-muted-foreground">
        <LoadingSpinner size={20} />
        <span className="text-sm">{children}</span>
      </div>
    </div>
  );
}