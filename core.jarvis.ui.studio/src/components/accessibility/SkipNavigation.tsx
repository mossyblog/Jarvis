import { cn } from '@/lib/utils';

interface SkipNavigationProps {
  className?: string;
}

export function SkipNavigation({ className }: SkipNavigationProps) {
  return (
    <div className={cn("sr-only focus-within:not-sr-only", className)}>
      <a
        href="#main-content"
        className="absolute top-2 left-2 z-[9999] bg-primary text-primary-foreground px-4 py-2 rounded-md text-sm font-medium focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 transition-all"
      >
        Skip to main content
      </a>
      <a
        href="#sidebar-navigation"
        className="absolute top-2 left-40 z-[9999] bg-primary text-primary-foreground px-4 py-2 rounded-md text-sm font-medium focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 transition-all"
      >
        Skip to navigation
      </a>
    </div>
  );
}