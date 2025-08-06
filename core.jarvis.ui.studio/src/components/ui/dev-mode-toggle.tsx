import { useEffect, useState } from 'react';
import { Switch } from './switch';
import { Label } from './label';
import { AlertCircle } from 'lucide-react';
import { setDevMode, shouldPersistTokens } from '../../utils/tokenUtils';
import { NotificationCard } from './notification-card';

export function DevModeToggle() {
  const [isDevMode, setIsDevMode] = useState(shouldPersistTokens());

  useEffect(() => {
    // Check if we're in development environment
    if (!import.meta.env.DEV) {
      return;
    }
  }, []);

  const handleToggle = (checked: boolean) => {
    setDevMode(checked);
    setIsDevMode(checked);
  };

  // Only show in development
  if (!import.meta.env.DEV) {
    return null;
  }

  return (
    <NotificationCard
      variant="warning"
      icon={AlertCircle}
      title="Development Mode"
      description="Token persistence is automatically enabled in development. Auth tokens will persist across server restarts."
    >
      <div className="flex items-center space-x-2">
        <Switch
          id="dev-mode"
          checked={isDevMode}
          onCheckedChange={handleToggle}
          disabled={true}
        />
        <Label htmlFor="dev-mode" className="text-xs">
          Always enabled in dev
        </Label>
      </div>
      <p className="text-xs text-muted-foreground">
        Tokens will be stored in localStorage and automatically refreshed when expired.
      </p>
    </NotificationCard>
  );
}