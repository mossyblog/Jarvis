import type { Meta, StoryObj } from '@storybook/react'
import { IconButton } from './icon-button'
import { Bell, Search, Settings, Plus, Home, Heart } from 'lucide-react'
import { Badge } from './badge'

const meta: Meta<typeof IconButton> = {
  title: 'UI/IconButton',
  component: IconButton,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
  argTypes: {
    variant: {
      control: { type: 'select' },
      options: ['default', 'ghost', 'solid'],
    },
    size: {
      control: { type: 'select' },
      options: ['sm', 'md', 'lg'],
    },
  },
}

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    variant: 'default',
    size: 'sm',
    children: <Bell className="h-xs w-xs" />,
  },
}

export const Ghost: Story = {
  args: {
    variant: 'ghost',
    size: 'sm',
    children: <Search className="h-xs w-xs" />,
  },
}

export const Solid: Story = {
  args: {
    variant: 'solid',
    size: 'sm',
    children: <Settings className="h-xs w-xs" />,
  },
}

export const Sizes: Story = {
  render: () => (
    <div className="flex items-center gap-4">
      <IconButton variant="default" size="sm">
        <Plus className="h-xs w-xs" />
      </IconButton>
      <IconButton variant="default" size="md">
        <Plus className="h-xs w-xs" />
      </IconButton>
      <IconButton variant="default" size="lg">
        <Plus className="h-xs w-xs" />
      </IconButton>
    </div>
  ),
}

export const Variants: Story = {
  render: () => (
    <div className="flex items-center gap-4">
      <IconButton variant="default" size="sm">
        <Home className="h-xs w-xs" />
      </IconButton>
      <IconButton variant="ghost" size="sm">
        <Search className="h-xs w-xs" />
      </IconButton>
      <IconButton variant="solid" size="sm">
        <Settings className="h-xs w-xs" />
      </IconButton>
    </div>
  ),
}

export const WithBadge: Story = {
  render: () => (
    <div className="flex items-center gap-4">
      <IconButton variant="ghost" size="sm">
        <div className="relative">
          <Bell className="h-xs w-xs" />
          <Badge 
            variant="destructive" 
            className="absolute -top-1 -right-1 h-sm w-sm p-0 flex items-center justify-center text-xs min-w-sm"
          >
            3
          </Badge>
        </div>
      </IconButton>
      <IconButton variant="ghost" size="sm">
        <div className="relative">
          <Heart className="h-xs w-xs" />
          <Badge 
            variant="destructive" 
            className="absolute -top-1 -right-1 h-sm w-sm p-0 flex items-center justify-center text-xs min-w-sm"
          >
            9+
          </Badge>
        </div>
      </IconButton>
    </div>
  ),
}

export const HoverStates: Story = {
  render: () => (
    <div className="flex flex-col gap-4 p-8">
      <div className="text-sm font-medium text-muted-foreground">Hover over buttons to see enhanced states:</div>
      <div className="flex items-center gap-4">
        <IconButton variant="default" size="sm">
          <Bell className="h-xs w-xs" />
        </IconButton>
        <IconButton variant="ghost" size="sm">
          <Search className="h-xs w-xs" />
        </IconButton>
        <IconButton variant="solid" size="sm">
          <Settings className="h-xs w-xs" />
        </IconButton>
      </div>
    </div>
  ),
}