/**
 * RibbonTabBar - A reusable ribbon-style tab bar component
 * 
 * Provides a modern ribbon interface with tab navigation and
 * associated content panels. Designed for toolbars and complex
 * interface layouts.
 */

import React from 'react';
import { cn } from '@/lib/utils';
import { motion } from 'framer-motion';

// ============================================================================
// Types
// ============================================================================

export interface RibbonTab {
  id: string;
  label: string;
  icon?: React.ReactNode;
  color?: 'blue' | 'green' | 'purple' | 'orange' | 'red' | 'gray';
  disabled?: boolean;
}

export interface RibbonTabBarProps {
  tabs: RibbonTab[];
  activeTab: string;
  onTabChange: (tabId: string) => void;
  className?: string;
  size?: 'sm' | 'md' | 'lg';
  variant?: 'default' | 'minimal';
}

// ============================================================================
// Color Configuration
// ============================================================================

const colorVariants = {
  blue: {
    active: 'bg-blue-500/20 text-blue-500',
    indicator: 'bg-blue-500'
  },
  green: {
    active: 'bg-green-500/20 text-green-500',
    indicator: 'bg-green-500'
  },
  purple: {
    active: 'bg-purple-500/20 text-purple-500',
    indicator: 'bg-purple-500'
  },
  orange: {
    active: 'bg-orange-500/20 text-orange-500',
    indicator: 'bg-orange-500'
  },
  red: {
    active: 'bg-red-500/20 text-red-500',
    indicator: 'bg-red-500'
  },
  gray: {
    active: 'bg-gray-500/20 text-gray-500',
    indicator: 'bg-gray-500'
  }
};

// ============================================================================
// Size Configuration
// ============================================================================

const sizeVariants = {
  sm: {
    height: 'h-8',
    padding: 'px-3 py-2',
    text: 'text-xs',
    icon: 'w-3 h-3'
  },
  md: {
    height: 'h-12',
    padding: 'px-6 py-3',
    text: 'text-sm',
    icon: 'w-4 h-4'
  },
  lg: {
    height: 'h-16',
    padding: 'px-8 py-4',
    text: 'text-base',
    icon: 'w-5 h-5'
  }
};

// ============================================================================
// Main Component
// ============================================================================

export const RibbonTabBar: React.FC<RibbonTabBarProps> = ({
  tabs,
  activeTab,
  onTabChange,
  className,
  size = 'md',
  variant = 'default'
}) => {
  const sizeConfig = sizeVariants[size];
  
  const activeTabData = tabs.find(tab => tab.id === activeTab);
  const activeColor = activeTabData?.color || 'blue';

  return (
    <div className={cn(
      "flex items-center gap-1 relative",
      sizeConfig.height,
      className
    )}>
      {tabs.map((tab) => {
        const isActive = tab.id === activeTab;
        const color = tab.color || 'blue';
        const colorConfig = colorVariants[color];

        return (
          <button
            key={tab.id}
            type="button"
            onClick={() => !tab.disabled && onTabChange(tab.id)}
            disabled={tab.disabled}
            className={cn(
              "flex items-center gap-2 rounded-t-md transition-all duration-200 font-medium",
              "hover:bg-muted/50 focus:outline-none focus:ring-2 focus:ring-primary/20",
              sizeConfig.padding,
              sizeConfig.text,
              sizeConfig.height,
              isActive 
                ? colorConfig.active
                : "text-muted-foreground hover:text-foreground",
              tab.disabled && "opacity-50 cursor-not-allowed hover:bg-transparent hover:text-muted-foreground",
              variant === 'minimal' && "rounded-md"
            )}
          >
            {tab.icon && (
              <span className={cn("flex-shrink-0", sizeConfig.icon)}>
                {tab.icon}
              </span>
            )}
            <span className="uppercase tracking-wide whitespace-nowrap">
              {tab.label}
            </span>
          </button>
        );
      })}
      
      {/* Active Tab Indicator */}
      {variant === 'default' && (
        <motion.div
          className={cn(
            "absolute bottom-0 h-0.5 transition-colors duration-200",
            colorVariants[activeColor].indicator
          )}
          initial={false}
          animate={{
            left: `${tabs.findIndex(tab => tab.id === activeTab) * (100 / tabs.length)}%`,
            width: `${100 / tabs.length}%`
          }}
          transition={{ duration: 0.3, ease: 'easeInOut' }}
        />
      )}
    </div>
  );
};

// ============================================================================
// Compound Components
// ============================================================================

/**
 * RibbonTabContent - Content container for ribbon tabs
 */
export interface RibbonTabContentProps {
  activeTab: string;
  children: React.ReactNode;
  className?: string;
  animate?: boolean;
}

export const RibbonTabContent: React.FC<RibbonTabContentProps> = ({
  activeTab,
  children,
  className,
  animate = true
}) => {
  return (
    <div className={cn("relative overflow-hidden", className)}>
      {animate ? (
        <motion.div
          key={activeTab}
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -10 }}
          transition={{ duration: 0.2, ease: 'easeInOut' }}
        >
          {children}
        </motion.div>
      ) : (
        children
      )}
    </div>
  );
};

/**
 * RibbonTabPanel - Individual tab panel wrapper
 */
export interface RibbonTabPanelProps {
  tabId: string;
  activeTab: string;
  children: React.ReactNode;
  className?: string;
}

export const RibbonTabPanel: React.FC<RibbonTabPanelProps> = ({
  tabId,
  activeTab,
  children,
  className
}) => {
  if (tabId !== activeTab) return null;

  return (
    <div className={cn("w-full", className)}>
      {children}
    </div>
  );
};

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Create ribbon tabs with consistent configuration
 */
export const createRibbonTabs = (
  tabConfigs: Array<{
    id: string;
    label: string;
    icon?: React.ReactNode;
    color?: RibbonTab['color'];
    disabled?: boolean;
  }>
): RibbonTab[] => {
  return tabConfigs.map(config => ({
    ...config,
    color: config.color || 'blue'
  }));
};

// ============================================================================
// Exports
// ============================================================================

export default RibbonTabBar;