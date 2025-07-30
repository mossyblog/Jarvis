import type { Meta, StoryObj } from '@storybook/react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from './card';
import { Button } from './button';

const meta: Meta<typeof Card> = {
  title: 'UI/Card',
  component: Card,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => (
    <Card className="w-[380px]">
      <CardHeader>
        <CardTitle>Card Title</CardTitle>
        <CardDescription>Card description goes here</CardDescription>
      </CardHeader>
      <CardContent>
        <p>This is the card content area.</p>
      </CardContent>
      <CardFooter>
        <Button>Action</Button>
      </CardFooter>
    </Card>
  ),
};

export const Simple: Story = {
  render: () => (
    <Card className="w-[350px]">
      <CardContent className="p-6">
        <p>Simple card with just content.</p>
      </CardContent>
    </Card>
  ),
};

export const WithoutFooter: Story = {
  render: () => (
    <Card className="w-[380px]">
      <CardHeader>
        <CardTitle>Settings</CardTitle>
        <CardDescription>Manage your account settings and preferences.</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <label className="text-sm font-medium">Notifications</label>
            <Button variant="outline" size="sm">Configure</Button>
          </div>
          <div className="flex items-center justify-between">
            <label className="text-sm font-medium">Privacy</label>
            <Button variant="outline" size="sm">Configure</Button>
          </div>
        </div>
      </CardContent>
    </Card>
  ),
};

export const LongContent: Story = {
  render: () => (
    <Card className="w-[400px]">
      <CardHeader>
        <CardTitle>Article Title</CardTitle>
        <CardDescription>Published 2 days ago</CardDescription>
      </CardHeader>
      <CardContent>
        <p className="text-sm text-muted-foreground mb-4">
          Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor 
          incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis 
          nostrud exercitation ullamco laboris.
        </p>
        <p className="text-sm text-muted-foreground">
          Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore 
          eu fugiat nulla pariatur.
        </p>
      </CardContent>
      <CardFooter className="justify-between">
        <Button variant="outline">Share</Button>
        <Button>Read More</Button>
      </CardFooter>
    </Card>
  ),
};