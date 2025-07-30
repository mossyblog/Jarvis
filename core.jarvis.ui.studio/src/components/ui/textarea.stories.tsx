import type { Meta, StoryObj } from '@storybook/react';
import { Textarea } from './textarea';
import { Label } from './label';

const meta: Meta<typeof Textarea> = {
  title: 'UI/Textarea',
  component: Textarea,
  parameters: {
    layout: 'centered',
  },
  tags: ['autodocs'],
  argTypes: {
    disabled: {
      control: { type: 'boolean' },
    },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    placeholder: 'Type your message here...',
  },
};

export const WithLabel: Story = {
  render: () => (
    <div className="grid w-full max-w-sm items-center gap-1.5">
      <Label htmlFor="message">Your message</Label>
      <Textarea placeholder="Type your message here..." id="message" />
    </div>
  ),
};

export const WithText: Story = {
  args: {
    value: 'This is some sample text that has been pre-filled in the textarea component.',
  },
};

export const Disabled: Story = {
  args: {
    disabled: true,
    placeholder: 'This textarea is disabled',
  },
};

export const ResizeNone: Story = {
  args: {
    className: 'resize-none',
    placeholder: 'This textarea cannot be resized',
  },
};

export const Large: Story = {
  args: {
    className: 'min-h-[120px]',
    placeholder: 'This is a larger textarea for longer content...',
  },
};

export const ContactForm: Story = {
  render: () => (
    <div className="w-full max-w-md space-y-4">
      <div>
        <h3 className="text-lg font-semibold mb-4">Contact Us</h3>
        
        <div className="space-y-4">
          <div className="grid gap-1.5">
            <Label htmlFor="name">Name</Label>
            <input 
              type="text" 
              id="name" 
              placeholder="Your name"
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
            />
          </div>
          
          <div className="grid gap-1.5">
            <Label htmlFor="email">Email</Label>
            <input 
              type="email" 
              id="email" 
              placeholder="your@email.com"
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
            />
          </div>
          
          <div className="grid gap-1.5">
            <Label htmlFor="message">Message</Label>
            <Textarea 
              id="message"
              placeholder="Tell us how we can help you..."
              className="min-h-[100px]"
            />
          </div>
          
          <button className="w-full h-9 px-4 py-2 bg-primary text-primary-foreground shadow hover:bg-primary/90 inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50">
            Send Message
          </button>
        </div>
      </div>
    </div>
  ),
};

export const CodeEditor: Story = {
  render: () => (
    <div className="w-full max-w-2xl">
      <div className="space-y-2">
        <Label htmlFor="code">SQL Query</Label>
        <Textarea 
          id="code"
          className="min-h-[200px] font-mono text-sm"
          placeholder="SELECT * FROM users WHERE..."
          value={`SELECT u.id, u.name, u.email, p.title as role
FROM users u
LEFT JOIN profiles p ON u.profile_id = p.id
WHERE u.active = true
ORDER BY u.created_at DESC
LIMIT 10;`}
        />
        <p className="text-xs text-muted-foreground">
          Write your SQL query above. Use Ctrl+Enter to execute.
        </p>
      </div>
    </div>
  ),
};