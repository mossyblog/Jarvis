/**
 * BentoGridDemo - Demo component showcasing the Bento Grid System
 * 
 * This component demonstrates how to use the Bento Grid System with
 * sample components and configurations.
 */

import React, { useState, useCallback } from 'react';
import { BentoGrid } from './BentoGrid';
import type { BentoGrid as BentoGridType, GridComponent } from '@/types/bento';
import { DeviceType } from '@/types/bento';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

// ============================================================================
// Demo Data
// ============================================================================

const createSampleGrid = (): BentoGridType => ({
  id: 'demo-grid',
  name: 'Demo Grid',
  device: DeviceType.Desktop,
  columns: 12,
  gap: 16,
  rowHeight: 100,
  components: [
    {
      id: 'component-1',
      componentType: 'metric',
      position: { x: 0, y: 0, w: 3, h: 2 },
      props: {
        title: 'Total Users',
        value: '1,234',
        change: '+12%'
      }
    },
    {
      id: 'component-2',
      componentType: 'metric',
      position: { x: 3, y: 0, w: 3, h: 2 },
      props: {
        title: 'Revenue',
        value: '$45,678',
        change: '+8%'
      }
    },
    {
      id: 'component-3',
      componentType: 'metric',
      position: { x: 6, y: 0, w: 3, h: 2 },
      props: {
        title: 'Orders',
        value: '567',
        change: '+23%'
      }
    },
    {
      id: 'component-4',
      componentType: 'metric',
      position: { x: 9, y: 0, w: 3, h: 2 },
      props: {
        title: 'Conversion',
        value: '3.2%',
        change: '+0.5%'
      }
    },
    {
      id: 'component-5',
      componentType: 'chart',
      position: { x: 0, y: 2, w: 8, h: 4 },
      props: {
        title: 'Sales Overview',
        type: 'line'
      }
    },
    {
      id: 'component-6',
      componentType: 'text',
      position: { x: 8, y: 2, w: 4, h: 2 },
      props: {
        text: 'Welcome to the Bento Grid System! This is a flexible layout system for building dashboard interfaces.'
      }
    },
    {
      id: 'component-7',
      componentType: 'placeholder',
      position: { x: 8, y: 4, w: 4, h: 2 }
    }
  ],
  settings: {
    showGrid: false,
    snapToGrid: true,
    gridColor: '#e5e7eb',
    allowOverlap: false,
    compactMode: 'vertical'
  },
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString()
});

// ============================================================================
// Main Component
// ============================================================================

export const BentoGridDemo: React.FC = () => {
  const [grid, setGrid] = useState<BentoGridType>(createSampleGrid);
  const [isEditing, setIsEditing] = useState(false);
  const [deviceType, setDeviceType] = useState<DeviceType>(DeviceType.Desktop);
  const [selectedComponentId, setSelectedComponentId] = useState<string | null>(null);

  // Check for collisions
  const checkCollision = useCallback((position: { x: number; y: number; w: number; h: number }, excludeId: string, components: GridComponent[]): boolean => {
    return components.some(component => {
      if (component.id === excludeId) return false;
      
      const overlap = !(
        position.x >= component.position.x + component.position.w ||
        position.x + position.w <= component.position.x ||
        position.y >= component.position.y + component.position.h ||
        position.y + position.h <= component.position.y
      );
      
      return overlap;
    });
  }, []);

  // Handle component movement
  const handleComponentMove = useCallback((componentId: string, newPosition: { x: number; y: number; w: number; h: number }) => {
    setGrid(prevGrid => {
      // Check if the new position would cause a collision
      if (checkCollision(newPosition, componentId, prevGrid.components)) {
        // Don't update if it would cause overlap
        return prevGrid;
      }

      return {
        ...prevGrid,
        components: prevGrid.components.map(component =>
          component.id === componentId
            ? { ...component, position: newPosition }
            : component
        ),
        updatedAt: new Date().toISOString()
      };
    });
  }, [checkCollision]);

  // Handle component resize
  const handleComponentResize = useCallback((componentId: string, newSize: { w: number; h: number; x?: number; y?: number }) => {
    setGrid(prevGrid => {
      const component = prevGrid.components.find(c => c.id === componentId);
      if (!component) return prevGrid;

      const newPosition = {
        ...component.position,
        w: newSize.w,
        h: newSize.h,
        ...(newSize.x !== undefined && { x: newSize.x }),
        ...(newSize.y !== undefined && { y: newSize.y })
      };

      // Check if the new size/position would cause a collision
      if (checkCollision(newPosition, componentId, prevGrid.components)) {
        // Don't update if it would cause overlap
        return prevGrid;
      }

      return {
        ...prevGrid,
        components: prevGrid.components.map(c =>
          c.id === componentId
            ? { ...c, position: newPosition }
            : c
        ),
        updatedAt: new Date().toISOString()
      };
    });
  }, [checkCollision]);

  // Handle component selection
  const handleComponentSelect = useCallback((componentId: string | null) => {
    setSelectedComponentId(componentId);
  }, []);

  // Handle component deletion
  const handleComponentDelete = useCallback((componentId: string) => {
    setGrid(prevGrid => ({
      ...prevGrid,
      components: prevGrid.components.filter(component => component.id !== componentId),
      updatedAt: new Date().toISOString()
    }));
    setSelectedComponentId(null);
  }, []);

  // Find empty position for new component
  const findEmptyPosition = useCallback((width: number, height: number, components: GridComponent[]): { x: number; y: number } | null => {
    const maxCols = deviceType === DeviceType.Desktop ? 12 : deviceType === DeviceType.Tablet ? 8 : 4;
    
    // Try to find an empty spot
    for (let y = 0; y < 20; y++) { // Max 20 rows
      for (let x = 0; x <= maxCols - width; x++) {
        const position = { x, y, w: width, h: height };
        if (!checkCollision(position, '', components)) {
          return { x, y };
        }
      }
    }
    
    return null;
  }, [deviceType, checkCollision]);

  // Add a new component
  const handleAddComponent = useCallback((componentType: string) => {
    const defaultSize = componentType === 'chart' ? { w: 4, h: 3 } : { w: 2, h: 2 };
    
    setGrid(prevGrid => {
      const emptyPos = findEmptyPosition(defaultSize.w, defaultSize.h, prevGrid.components);
      
      if (!emptyPos) {
        alert('No empty space available for new component');
        return prevGrid;
      }

      const newComponent: GridComponent = {
        id: `component-${Date.now()}`,
        componentType,
        position: { ...emptyPos, ...defaultSize },
        props: {
          title: `New ${componentType}`,
          ...(componentType === 'metric' && { value: '0', change: '0%' }),
          ...(componentType === 'text' && { text: 'New text component' })
        }
      };

      return {
        ...prevGrid,
        components: [...prevGrid.components, newComponent],
        updatedAt: new Date().toISOString()
      };
    });
  }, [findEmptyPosition]);

  // Reset grid to default
  const handleResetGrid = useCallback(() => {
    setGrid(createSampleGrid());
    setSelectedComponentId(null);
  }, []);

  return (
    <div className="bento-grid-demo p-6 space-y-6">
      {/* Controls */}
      <Card>
        <CardHeader>
          <CardTitle>Bento Grid Demo Controls</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap gap-4 items-center">
            <div className="flex items-center space-x-2">
              <Switch
                id="edit-mode"
                checked={isEditing}
                onCheckedChange={setIsEditing}
              />
              <label htmlFor="edit-mode" className="text-sm font-medium">
                Edit Mode
              </label>
            </div>

            {/* Removed show grid toggle - grid is progressive now */}

            <div className="flex items-center space-x-2">
              <label htmlFor="device-type" className="text-sm font-medium">
                Device:
              </label>
              <Select value={deviceType} onValueChange={(value) => setDeviceType(value as DeviceType)}>
                <SelectTrigger className="w-32">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={DeviceType.Desktop}>Desktop</SelectItem>
                  <SelectItem value={DeviceType.Tablet}>Tablet</SelectItem>
                  <SelectItem value={DeviceType.Mobile}>Mobile</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          {isEditing && (
            <div className="flex flex-wrap gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => handleAddComponent('metric')}
              >
                Add Metric
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => handleAddComponent('chart')}
              >
                Add Chart
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => handleAddComponent('text')}
              >
                Add Text
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => handleAddComponent('placeholder')}
              >
                Add Placeholder
              </Button>
              <Button
                variant="destructive"
                size="sm"
                onClick={handleResetGrid}
              >
                Reset Grid
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Grid Info */}
      <Card>
        <CardContent className="pt-6">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
            <div>
              <div className="font-medium text-muted-foreground">Components</div>
              <div className="text-lg font-semibold">{grid.components.length}</div>
            </div>
            <div>
              <div className="font-medium text-muted-foreground">Columns</div>
              <div className="text-lg font-semibold">{grid.columns}</div>
            </div>
            <div>
              <div className="font-medium text-muted-foreground">Device</div>
              <div className="text-lg font-semibold capitalize">{deviceType}</div>
            </div>
            <div>
              <div className="font-medium text-muted-foreground">Selected</div>
              <div className="text-lg font-semibold">
                {selectedComponentId ? selectedComponentId.slice(-8) : 'None'}
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Bento Grid */}
      <Card>
        <CardContent className="p-6">
          <div className="min-h-[600px] border border-border rounded-lg p-4 bg-muted/30">
            <BentoGrid
              grid={grid}
              deviceType={deviceType}
              isEditing={isEditing}
              onComponentMove={handleComponentMove}
              onComponentResize={handleComponentResize}
              onComponentSelect={handleComponentSelect}
              onComponentDelete={handleComponentDelete}
            />
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

BentoGridDemo.displayName = 'BentoGridDemo';