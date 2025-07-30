import React from 'react';
import { AlertCircle, RefreshCw } from 'lucide-react';
import { useApiStatus } from '../../contexts/ApiStatusContext';
import { cn } from '../../lib/utils';

export const ApiStatusBanner: React.FC = () => {
  const { isApiReachable, lastError, checkApiStatus } = useApiStatus();
  const [isChecking, setIsChecking] = React.useState(false);
  const [lastCheckTime, setLastCheckTime] = React.useState<Date | null>(null);

  const handleRetry = async () => {
    setIsChecking(true);
    try {
      console.log('Manually checking API status...');
      await checkApiStatus();
      setLastCheckTime(new Date());
      // Give a small delay to show the spinning animation
      await new Promise(resolve => setTimeout(resolve, 500));
    } catch (error) {
      console.error('Manual API check failed:', error);
      setLastCheckTime(new Date());
    } finally {
      setIsChecking(false);
    }
  };

  React.useEffect(() => {
    // Add padding to body when banner is shown
    if (!isApiReachable) {
      document.body.style.paddingTop = '32px';
    } else {
      document.body.style.paddingTop = '0';
    }
    
    return () => {
      document.body.style.paddingTop = '0';
    };
  }, [isApiReachable]);

  if (isApiReachable) {
    return null;
  }

  return (
    <div className="fixed top-0 left-0 right-0 z-50 h-8 bg-destructive flex items-center justify-center px-4">
      <div className="flex items-center gap-2 text-destructive-foreground">
        <AlertCircle size={16} />
        <span className="text-xs font-medium">
          API Unreachable{lastError ? `: ${lastError}` : ''}
          {lastCheckTime && (
            <span className="ml-2 opacity-75">
              (checked {lastCheckTime.toLocaleTimeString()})
            </span>
          )}
        </span>
        <button
          onClick={handleRetry}
          disabled={isChecking}
          className={cn(
            "ml-2 p-1 rounded hover:bg-destructive-foreground/10 transition-colors",
            isChecking && "opacity-50 cursor-not-allowed"
          )}
          title="Retry connection"
        >
          <RefreshCw size={14} className={cn(isChecking && "animate-spin")} />
        </button>
      </div>
    </div>
  );
};