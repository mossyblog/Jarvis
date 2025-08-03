import { ReactNode } from 'react';
import { cn } from '@/lib/utils';

interface LiveRegionProps {
  children: ReactNode;
  level?: 'polite' | 'assertive' | 'off';
  atomic?: boolean;
  relevant?: 'text' | 'additions' | 'additions removals' | 'additions text' | 'all' | 'removals' | 'removals additions' | 'removals text' | 'text additions' | 'text removals';
  busy?: boolean;
  className?: string;
  id?: string;
}

export function LiveRegion({
  children,
  level = 'polite',
  atomic = false,
  relevant = 'additions text',
  busy = false,
  className,
  id
}: LiveRegionProps) {
  return (
    <div
      id={id}
      className={cn("sr-only", className)}
      aria-live={level}
      aria-atomic={atomic}
      aria-relevant={relevant}
      aria-busy={busy}
      role="status"
    >
      {children}
    </div>
  );
}

export function PoliteAnnouncement({ children, ...props }: Omit<LiveRegionProps, 'level'>) {
  return (
    <LiveRegion level="polite" {...props}>
      {children}
    </LiveRegion>
  );
}

export function AssertiveAnnouncement({ children, ...props }: Omit<LiveRegionProps, 'level'>) {
  return (
    <LiveRegion level="assertive" {...props}>
      {children}
    </LiveRegion>
  );
}