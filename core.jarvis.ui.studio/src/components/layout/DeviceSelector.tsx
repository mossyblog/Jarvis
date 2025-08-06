import React, { useState } from 'react';
import { Monitor, Tablet, Smartphone, Info, Maximize2 } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { useEditMode } from '@/contexts/EditModeContext';
import { LucideIcon as Icon } from '@/components/ui/icon';

interface DeviceInfo {
  id: 'desktop' | 'tablet' | 'mobile';
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  description: string;
  dimensions: {
    width: string;
    height: string;
    minWidth?: string;
    maxWidth?: string;
  };
  gridColumns: number;
  features: string[];
}

export const DeviceSelector: React.FC = () => {
  const { currentDevice, setDevice } = useEditMode();
  const [showDetails, setShowDetails] = useState(false);

  const devices: DeviceInfo[] = [
    { 
      id: 'desktop', 
      icon: Monitor, 
      label: 'Desktop',
      description: 'Large screens and workstations',
      dimensions: {
        width: '1920px',
        height: '1080px',
        minWidth: '1024px'
      },
      gridColumns: 12,
      features: ['Full functionality', 'All components', 'Advanced layouts']
    },
    { 
      id: 'tablet', 
      icon: Tablet, 
      label: 'Tablet',
      description: 'Medium screens and touch devices',
      dimensions: {
        width: '768px',
        height: '1024px',
        minWidth: '768px',
        maxWidth: '1023px'
      },
      gridColumns: 8,
      features: ['Touch optimized', 'Responsive layouts', 'Simplified navigation']
    },
    { 
      id: 'mobile', 
      icon: Smartphone, 
      label: 'Mobile',
      description: 'Small screens and phones',
      dimensions: {
        width: '375px',
        height: '667px',
        maxWidth: '767px'
      },
      gridColumns: 4,
      features: ['Touch first', 'Compact layouts', 'Essential features']
    }
  ];

  const currentDeviceInfo = devices.find(d => d.id === currentDevice) || devices[0];

  return (
    <TooltipProvider>
      <div className="flex items-center gap-2">
        {/* Device Selector Buttons */}
        <div className="flex items-center bg-muted/30 rounded-md p-0.5">
          {devices.map((device) => {
            
            const isActive = currentDevice === device.id;
            
            return (
              <Tooltip key={device.id}>
                <TooltipTrigger asChild>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setDevice(device.id)}
                    className={cn(
                      "h-sm px-xs relative transition-all duration-200",
                      isActive && "bg-background shadow-sm scale-110",
                      !isActive && "hover:scale-105"
                    )}
                  >
                    <Icon icon={device.icon} size="sm" className={cn(
                      "transition-colors",
                      isActive && "text-primary"
                    )} />
                    {isActive && (
                      <div className="absolute -bottom-0.5 left-1/2 transform -translate-x-1/2 w-xs h-none.5 bg-primary rounded-full" />
                    )}
                  </Button>
                </TooltipTrigger>
                <TooltipContent side="bottom" className="max-w-xs">
                  <div className="space-y-2">
                    <div className="font-medium">{device.label}</div>
                    <div className="text-xs text-muted-foreground">{device.description}</div>
                    <div className="flex items-center gap-2 text-xs">
                      <Badge variant="outline" className="text-xs">
                        {device.gridColumns} cols
                      </Badge>
                      <span className="text-muted-foreground">
                        {device.dimensions.width} × {device.dimensions.height}
                      </span>
                    </div>
                  </div>
                </TooltipContent>
              </Tooltip>
            );
          })}
        </div>

        {/* Current Device Info */}
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Badge variant="secondary" className="font-mono text-xs">
            {currentDeviceInfo.gridColumns} columns
          </Badge>
          
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant="ghost"
                size="sm"
                className="h-xs w-xs p-0"
                onClick={() => setShowDetails(!showDetails)}
              >
                <Icon icon={Info} size="sm" />
              </Button>
            </TooltipTrigger>
            <TooltipContent side="bottom" className="max-w-sm">
              <div className="space-y-3">
                <div>
                  <div className="font-medium mb-1">{currentDeviceInfo.label} Preview</div>
                  <div className="text-xs text-muted-foreground">{currentDeviceInfo.description}</div>
                </div>
                
                <div>
                  <div className="text-xs font-medium mb-1">Dimensions</div>
                  <div className="text-xs text-muted-foreground space-y-1">
                    <div>Width: {currentDeviceInfo.dimensions.width}</div>
                    <div>Height: {currentDeviceInfo.dimensions.height}</div>
                    {currentDeviceInfo.dimensions.minWidth && (
                      <div>Min Width: {currentDeviceInfo.dimensions.minWidth}</div>
                    )}
                    {currentDeviceInfo.dimensions.maxWidth && (
                      <div>Max Width: {currentDeviceInfo.dimensions.maxWidth}</div>
                    )}
                  </div>
                </div>
                
                <div>
                  <div className="text-xs font-medium mb-1">Features</div>
                  <ul className="text-xs text-muted-foreground space-y-0.5">
                    {currentDeviceInfo.features.map((feature, index) => (
                      <li key={index}>• {feature}</li>
                    ))}
                  </ul>
                </div>
              </div>
            </TooltipContent>
          </Tooltip>
          
          <span className="text-xs font-mono">
            {currentDeviceInfo.dimensions.width}
          </span>
        </div>
      </div>
    </TooltipProvider>
  );
};

// Export device information for use in other components
export { type DeviceInfo };
export const getDeviceInfo = (deviceId: 'desktop' | 'tablet' | 'mobile'): DeviceInfo => {
  const devices: DeviceInfo[] = [
    { 
      id: 'desktop', 
      icon: Monitor, 
      label: 'Desktop',
      description: 'Large screens and workstations',
      dimensions: {
        width: '1920px',
        height: '1080px',
        minWidth: '1024px'
      },
      gridColumns: 12,
      features: ['Full functionality', 'All components', 'Advanced layouts']
    },
    { 
      id: 'tablet', 
      icon: Tablet, 
      label: 'Tablet',
      description: 'Medium screens and touch devices',
      dimensions: {
        width: '768px',
        height: '1024px',
        minWidth: '768px',
        maxWidth: '1023px'
      },
      gridColumns: 8,
      features: ['Touch optimized', 'Responsive layouts', 'Simplified navigation']
    },
    { 
      id: 'mobile', 
      icon: Smartphone, 
      label: 'Mobile',
      description: 'Small screens and phones',
      dimensions: {
        width: '375px',
        height: '667px',
        maxWidth: '767px'
      },
      gridColumns: 4,
      features: ['Touch first', 'Compact layouts', 'Essential features']
    }
  ];
  
  return devices.find(d => d.id === deviceId) || devices[0];
};