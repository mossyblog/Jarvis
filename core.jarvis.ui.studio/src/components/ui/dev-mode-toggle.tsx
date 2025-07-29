import { useEffect, useState } from 'react';
import { Switch } from './switch';
import { Label } from './label';
import { Card, CardContent } from './card';
import { AlertCircle } from 'lucide-react';
import { setDevMode, shouldPersistTokens } from '../../utils/tokenUtils';

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
    <Card className="border-yellow-500/20 bg-yellow-500/5">
      <CardContent className="p-4">
        <div className="flex items-start space-x-3">
          <AlertCircle className="h-5 w-5 text-yellow-500 mt-0.5" />
          <div className="flex-1 space-y-3">
            <div>
              <h4 className="text-sm font-medium">Development Mode</h4>
              <p className="text-xs text-muted-foreground mt-1">
                Token persistence is automatically enabled in development. 
                Auth tokens will persist across server restarts.
              </p>
            </div>
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
          </div>
        </div>
      </CardContent>
    </Card>
  );
}