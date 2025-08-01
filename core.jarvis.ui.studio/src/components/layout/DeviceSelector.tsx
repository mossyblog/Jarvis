import React from 'react';
import { Monitor, Tablet, Smartphone } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { useEditMode } from '@/contexts/EditModeContext';

export const DeviceSelector: React.FC = () => {
  const { currentDevice, setDevice } = useEditMode();

  const devices = [
    { id: 'desktop' as const, icon: Monitor, label: 'Desktop' },
    { id: 'tablet' as const, icon: Tablet, label: 'Tablet' },
    { id: 'mobile' as const, icon: Smartphone, label: 'Mobile' }
  ];

  return (
    <div className="flex items-center bg-muted/30 rounded-md p-0.5">
      {devices.map((device) => {
        const Icon = device.icon;
        return (
          <Button
            key={device.id}
            variant="ghost"
            size="sm"
            onClick={() => setDevice(device.id)}
            className={cn(
              "h-8 px-2",
              currentDevice === device.id && "bg-background shadow-sm"
            )}
            title={device.label}
          >
            <Icon className="h-4 w-4" />
          </Button>
        );
      })}
    </div>
  );
};