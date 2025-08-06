import React from 'react';
import { DashboardLayout } from '../components/layout/DashboardLayout';
import { Skeleton } from '../components/ui/skeleton';
import { TableSkeleton } from '../components/ui/table-skeleton';

export default function SkeletonTest() {
  return (
    <DashboardLayout activeItem="skeleton-test" onItemClick={() => {}}>
      <div className="p-8">
        <h1 className="text-2xl font-bold mb-8">Skeleton Component Test</h1>
        
        <div className="space-y-8">
          {/* Basic Skeletons */}
          <div className="space-y-4">
            <h2 className="text-lg font-semibold mb-4">Basic Skeleton Components</h2>
            <div className="space-y-2">
              <div className="flex items-center gap-4">
                <span className="text-sm w-4xl">Default:</span>
                <Skeleton className="h-xs w-lgxl" />
              </div>
              <div className="flex items-center gap-4">
                <span className="text-sm w-4xl">Small:</span>
                <Skeleton className="h-xs w-5xl" />
              </div>
              <div className="flex items-center gap-4">
                <span className="text-sm w-4xl">Large:</span>
                <Skeleton className="h-xs w-xlxl" />
              </div>
              <div className="flex items-center gap-4">
                <span className="text-sm w-4xl">Square:</span>
                <Skeleton className="h-sm w-sm" />
              </div>
              <div className="flex items-center gap-4">
                <span className="text-sm w-4xl">Circle:</span>
                <Skeleton className="h-2xl w-2xl rounded-full" />
              </div>
            </div>
          </div>

          {/* Table Skeleton */}
          <div>
            <h2 className="text-lg font-semibold mb-4">Table Skeleton</h2>
            <TableSkeleton rows={5} columns={8} />
          </div>

          {/* Card Skeleton */}
          <div>
            <h2 className="text-lg font-semibold mb-4">Card Skeleton</h2>
            <div className="border border-border rounded-lg p-4 space-y-3">
              <Skeleton className="h-xs w-2xs/4" />
              <Skeleton className="h-xs w-xsxs/2" />
              <div className="flex gap-2 mt-4">
                <Skeleton className="h-sm w-4xl" />
                <Skeleton className="h-sm w-4xl" />
              </div>
            </div>
          </div>

          {/* Color Debug Info */}
          <div className="mt-8 p-4 border border-border rounded-lg">
            <h2 className="text-lg font-semibold mb-4">Theme Debug Info</h2>
            <div className="space-y-2 text-sm">
              <p>Current theme: <span className="font-mono" id="current-theme">checking...</span></p>
              <p>Muted color: <span className="font-mono" id="muted-color">checking...</span></p>
              <p>Background color: <span className="font-mono" id="bg-color">checking...</span></p>
            </div>
          </div>
        </div>
      </div>
      
      <script dangerouslySetInnerHTML={{
        __html: `
          setTimeout(() => {
            const theme = document.documentElement.getAttribute('data-theme') || 'default';
            const mode = document.documentElement.getAttribute('data-mode') || 'light';
            const computedStyle = getComputedStyle(document.body);
            
            document.getElementById('current-theme').textContent = theme + ' / ' + mode;
            document.getElementById('muted-color').textContent = computedStyle.getPropertyValue('--muted');
            document.getElementById('bg-color').textContent = computedStyle.getPropertyValue('--background');
          }, 100);
        `
      }} />
    </DashboardLayout>
  );
}