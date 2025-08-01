import type { Meta, StoryObj } from '@storybook/react-vite';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './select';
import { Label } from './label';

const meta: Meta<typeof Select> = {
  title: 'UI/Select',
  component: Select,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => (
    <Select>
      <SelectTrigger className="w-[180px]">
        <SelectValue placeholder="Select a fruit" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="apple">Apple</SelectItem>
        <SelectItem value="banana">Banana</SelectItem>
        <SelectItem value="blueberry">Blueberry</SelectItem>
        <SelectItem value="grapes">Grapes</SelectItem>
        <SelectItem value="pineapple">Pineapple</SelectItem>
      </SelectContent>
    </Select>
  ),
};

export const WithLabel: Story = {
  render: () => (
    <div className="grid w-full max-w-sm items-center gap-1.5">
      <Label htmlFor="framework">Framework</Label>
      <Select>
        <SelectTrigger>
          <SelectValue placeholder="Select a framework" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="next">Next.js</SelectItem>
          <SelectItem value="react">React</SelectItem>
          <SelectItem value="astro">Astro</SelectItem>
          <SelectItem value="nuxt">Nuxt.js</SelectItem>
        </SelectContent>
      </Select>
    </div>
  ),
};

export const Disabled: Story = {
  render: () => (
    <Select disabled>
      <SelectTrigger className="w-[180px]">
        <SelectValue placeholder="Select option" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="option1">Option 1</SelectItem>
        <SelectItem value="option2">Option 2</SelectItem>
      </SelectContent>
    </Select>
  ),
};

export const WithGroups: Story = {
  render: () => (
    <Select>
      <SelectTrigger className="w-[200px]">
        <SelectValue placeholder="Select a timezone" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="utc">UTC</SelectItem>
        <SelectItem value="est">Eastern Standard Time</SelectItem>
        <SelectItem value="cst">Central Standard Time</SelectItem>
        <SelectItem value="mst">Mountain Standard Time</SelectItem>
        <SelectItem value="pst">Pacific Standard Time</SelectItem>
      </SelectContent>
    </Select>
  ),
};

export const AllSizes: Story = {
  render: () => (
    <div className="space-y-4">
      <div className="space-y-2">
        <Label>Small Select</Label>
        <Select>
          <SelectTrigger className="w-[160px] h-8 text-xs">
            <SelectValue placeholder="Small" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="small1">Small Option 1</SelectItem>
            <SelectItem value="small2">Small Option 2</SelectItem>
          </SelectContent>
        </Select>
      </div>
      
      <div className="space-y-2">
        <Label>Default Select</Label>
        <Select>
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder="Default" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="default1">Default Option 1</SelectItem>
            <SelectItem value="default2">Default Option 2</SelectItem>
          </SelectContent>
        </Select>
      </div>
      
      <div className="space-y-2">
        <Label>Large Select</Label>
        <Select>
          <SelectTrigger className="w-[220px] h-12">
            <SelectValue placeholder="Large" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="large1">Large Option 1</SelectItem>
            <SelectItem value="large2">Large Option 2</SelectItem>
          </SelectContent>
        </Select>
      </div>
    </div>
  ),
};