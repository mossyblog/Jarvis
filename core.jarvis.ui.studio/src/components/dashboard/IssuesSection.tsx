import { useState } from 'react';
import { cn } from '../../lib/utils';
import { Shield, Activity, AlertCircle, Info, ExternalLink } from 'lucide-react';
import { LucideIcon as Icon } from '../ui/icon';

interface Issue {
  id: string;
  type: 'security' | 'performance';
  severity: 'warning' | 'info';
  title: string;
  description: string;
  action?: string;
}

const issues: Issue[] = [
  {
    id: '1',
    type: 'security',
    severity: 'warning',
    title: 'Function',
    description: '`public.ensure_project_schema` has a role mutable search_path',
    action: 'View'
  },
  {
    id: '2',
    type: 'security',
    severity: 'warning',
    title: 'Function',
    description: '`public.update_updated_at_column` has a role mutable search_path',
    action: 'View'
  },
  {
    id: '3',
    type: 'security',
    severity: 'info',
    title: 'Email Provider',
    description: 'We have detected that you have enabled the email provider with the OTP expiry set to more than 24hrs. Jarvis Auth prevents the use of compromised passwords by checking against them with the HaveIBeenPwned Passwords API.',
    action: 'Learn more'
  },
  {
    id: '4',
    type: 'performance',
    severity: 'info',
    title: 'Auth',
    description: 'Jarvis Auth prevents the use of compromised passwords by checking against them with the HaveIBeenPwned Passwords API.',
    action: 'Learn more'
  }
];

export function IssuesSection() {
  const [activeTab, setActiveTab] = useState<'security' | 'performance'>('security');
  
  const filteredIssues = issues.filter(issue => issue.type === activeTab);
  const issueCount = issues.filter(i => i.severity === 'warning').length;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-2">
        <span className="text-2xl font-light">{issueCount}</span>
        <span className="text-base">issues need</span>
        <span className="text-base text-warning">attention</span>
      </div>

      {/* Tabs */}
      <div className="flex items-center gap-1">
          <button
            onClick={() => setActiveTab('security')}
            className={cn(
              "flex items-center gap-2 px-4 py-2 text-sm rounded-md transition-colors",
              activeTab === 'security' 
                ? "bg-warning/20 text-warning" 
                : "text-muted-foreground hover:text-foreground"
            )}
          >
            <Icon icon={Shield} size="sm" />
            <span className="uppercase tracking-wide text-xs font-medium">Security</span>
            <span className={cn(
              "px-1.5 py-0.5 rounded text-xs",
              activeTab === 'security' ? "bg-warning text-warning-foreground" : "bg-muted"
            )}>
              {issues.filter(i => i.type === 'security' && i.severity === 'warning').length}
            </span>
          </button>
          
          <button
            onClick={() => setActiveTab('performance')}
            className={cn(
              "flex items-center gap-2 px-4 py-2 text-sm rounded-md transition-colors",
              activeTab === 'performance' 
                ? "bg-warning/20 text-warning" 
                : "text-muted-foreground hover:text-foreground"
            )}
          >
            <Icon icon={Activity} size="sm" />
            <span className="uppercase tracking-wide text-xs font-medium">Performance</span>
            <span className={cn(
              "px-1.5 py-0.5 rounded text-xs",
              activeTab === 'performance' ? "bg-warning text-warning-foreground" : "bg-muted"
            )}>
              {issues.filter(i => i.type === 'performance' && i.severity === 'warning').length}
            </span>
          </button>
      </div>

      {/* Issues List */}
      <div className="border border-default rounded-lg overflow-hidden">
        {filteredIssues.map((issue, index) => (
          <div
            key={issue.id}
            className={cn(
              "flex items-start gap-4 p-4 bg-card",
              index !== filteredIssues.length - 1 && "border-b border-default"
            )}
          >
            {/* Icon */}
            <div className="mt-0.5">
              {issue.severity === 'warning' ? (
                <Icon icon={AlertCircle} size="sm" className="text-warning" />
              ) : (
                <Icon icon={Info} size="sm" className="text-muted-foreground" />
              )}
            </div>

            {/* Content */}
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2 mb-1">
                <span className="text-sm font-medium">{issue.title}</span>
                <span className="text-xs text-muted-foreground">
                  {issue.description}
                </span>
              </div>
            </div>

            {/* Action */}
            {issue.action && (
              <button className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground transition-colors">
                <span>{issue.action}</span>
                <Icon icon={ExternalLink} size="xs" />
              </button>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}