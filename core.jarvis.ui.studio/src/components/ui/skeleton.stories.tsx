import type { Meta, StoryObj } from '@storybook/react-vite';
import { Skeleton } from './skeleton';

const meta: Meta<typeof Skeleton> = {
  title: 'UI/Skeleton',
  component: Skeleton,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    className: 'w-[100px] h-[20px]',
  },
};

export const Circle: Story = {
  args: {
    className: 'h-2xl w-2xl rounded-full',
  },
};

export const Card: Story = {
  render: () => (
    <div className="flex items-center space-x-4">
      <Skeleton className="h-2xl w-2xl rounded-full" />
      <div className="space-y-2">
        <Skeleton className="h-xs w-[250px]" />
        <Skeleton className="h-xs w-[200px]" />
      </div>
    </div>
  ),
};

export const ArticleLoading: Story = {
  render: () => (
    <div className="w-full max-w-md space-y-3">
      <Skeleton className="h-[200px] w-full rounded-lg" />
      <div className="space-y-2">
        <Skeleton className="h-xs w-full" />
        <Skeleton className="h-xs w-xs/5" />
        <Skeleton className="h-xs w-2xs/5" />
      </div>
      <div className="flex items-center space-x-2">
        <Skeleton className="h-lg w-lg rounded-full" />
        <Skeleton className="h-xs w-[100px]" />
      </div>
    </div>
  ),
};

export const TableLoading: Story = {
  render: () => (
    <div className="w-full max-w-2xl space-y-3">
      {Array.from({ length: 5 }).map((_, i) => (
        <div key={i} className="flex items-center space-x-4">
          <Skeleton className="h-xs w-xs" />
          <Skeleton className="h-xs flex-1" />
          <Skeleton className="h-xs w-[100px]" />
          <Skeleton className="h-xs w-[80px]" />
        </div>
      ))}
    </div>
  ),
};

export const DashboardLoading: Story = {
  render: () => (
    <div className="w-full max-w-4xl space-y-6">
      {/* Header skeleton */}
      <div className="flex items-center justify-between">
        <div className="space-y-2">
          <Skeleton className="h-lg w-[200px]" />
          <Skeleton className="h-xs w-[300px]" />
        </div>
        <Skeleton className="h-xl w-[120px]" />
      </div>
      
      {/* Stats grid skeleton */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="p-4 border rounded-lg space-y-3">
            <div className="flex items-center justify-between">
              <Skeleton className="h-xs w-xs rounded" />
              <Skeleton className="h-xs w-[60px]" />
            </div>
            <Skeleton className="h-lg w-[80px]" />
            <Skeleton className="h-2xl w-full" />
          </div>
        ))}
      </div>
      
      {/* Content skeleton */}
      <div className="space-y-4">
        <Skeleton className="h-md w-[150px]" />
        <div className="space-y-2">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-2xl w-full" />
          ))}
        </div>
      </div>
    </div>
  ),
  parameters: {
    layout: 'fullscreen',
  },
};