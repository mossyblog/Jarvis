/**
 * Brand Compliant Component Example
 * 
 * This component demonstrates proper usage of:
 * ✅ Semantic color tokens
 * ✅ Approved font weights and families
 * ✅ Lucide React icons only
 * ✅ Standard animation timing
 * ✅ 8px grid spacing system
 */

import React, { useState } from 'react';
import { 
  User, 
  Settings, 
  Bell, 
  Shield, 
  ChevronRight,
  CheckCircle,
  AlertCircle
} from 'lucide-react';

interface UserProfile {
  name: string;
  email: string;
  role: 'admin' | 'user' | 'viewer';
  notifications: number;
  isVerified: boolean;
}

export function BrandCompliantExample() {
  const [user] = useState<UserProfile>({
    name: 'John Doe',
    email: 'john.doe@example.com',
    role: 'admin',
    notifications: 3,
    isVerified: true
  });

  const [isExpanded, setIsExpanded] = useState(false);

  return (
    <div className="bg-card border-border rounded-lg p-lg max-w-md">
      {/* Header with semantic colors and approved typography */}
      <div className="flex items-center justify-between mb-md">
        <h2 className="font-semibold text-foreground text-xl">
          User Profile
        </h2>
        
        {/* Notification badge with semantic colors */}
        {user.notifications > 0 && (
          <div className="bg-destructive text-destructive-foreground rounded-full px-sm py-xs">
            <span className="font-medium text-xs">
              {user.notifications}
            </span>
          </div>
        )}
      </div>

      {/* User info with proper color hierarchy */}
      <div className="flex items-center gap-md mb-lg">
        <div className="bg-muted rounded-full p-md">
          <User className="text-muted-foreground" size={24} />
        </div>
        
        <div className="flex-1">
          <h3 className="font-semibold text-foreground text-base">
            {user.name}
          </h3>
          <p className="text-muted-foreground text-sm">
            {user.email}
          </p>
          
          {/* Status indicator with semantic colors */}
          <div className="flex items-center gap-xs mt-xs">
            {user.isVerified ? (
              <CheckCircle className="text-accent" size={16} />
            ) : (
              <AlertCircle className="text-destructive" size={16} />
            )}
            <span className="text-muted-foreground text-xs font-medium">
              {user.isVerified ? 'Verified' : 'Unverified'}
            </span>
          </div>
        </div>
      </div>

      {/* Role badge with proper contrast */}
      <div className="mb-lg">
        <div className="inline-flex items-center gap-xs bg-primary/10 border border-primary/20 rounded-md px-md py-sm">
          <Shield className="text-primary" size={16} />
          <span className="font-medium text-primary text-sm capitalize">
            {user.role}
          </span>
        </div>
      </div>

      {/* Expandable section with smooth animation */}
      <div className="border-t border-border pt-md">
        <button
          onClick={() => setIsExpanded(!isExpanded)}
          className="flex items-center justify-between w-full text-left group"
        >
          <span className="font-medium text-foreground text-sm">
            Quick Actions
          </span>
          <ChevronRight 
            className={`text-muted-foreground transition-transform duration-200 ${
              isExpanded ? 'rotate-90' : ''
            }`}
            size={16} 
          />
        </button>

        {/* Expandable content with proper timing */}
        <div className={`overflow-hidden transition-all duration-300 ${
          isExpanded ? 'max-h-40 mt-md' : 'max-h-0'
        }`}>
          <div className="space-y-sm">
            <ActionButton 
              icon={Settings}
              label="Account Settings"
              onClick={() => console.log('Settings clicked')}
            />
            <ActionButton 
              icon={Bell}
              label="Notification Preferences"
              onClick={() => console.log('Notifications clicked')}
            />
          </div>
        </div>
      </div>

      {/* Primary action with proper hierarchy */}
      <div className="mt-lg pt-md border-t border-border">
        <button className="w-full bg-primary text-primary-foreground font-medium py-md px-lg rounded-md hover:bg-primary/90 transition-colors duration-200">
          Edit Profile
        </button>
      </div>
    </div>
  );
}

// Helper component demonstrating reusable patterns
interface ActionButtonProps {
  icon: React.ComponentType<{ size?: number; className?: string }>;
  label: string;
  onClick: () => void;
}

function ActionButton({ icon: Icon, label, onClick }: ActionButtonProps) {
  return (
    <button
      onClick={onClick}
      className="flex items-center gap-md w-full p-sm rounded-md text-left hover:bg-muted/50 transition-colors duration-150 group"
    >
      <Icon 
        className="text-muted-foreground group-hover:text-foreground transition-colors duration-150" 
        size={16} 
      />
      <span className="text-muted-foreground font-normal text-sm group-hover:text-foreground transition-colors duration-150">
        {label}
      </span>
    </button>
  );
}

export default BrandCompliantExample;