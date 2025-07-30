import type { Preview } from '@storybook/react-vite'
import React from 'react';
import '../src/index.css';
import './docs.css';
import { ThemeProvider } from '../src/contexts/ThemeContext';

const preview: Preview = {
  parameters: {
    layout: 'centered',
  },
  globalTypes: {
    theme: {
      description: 'Theme',
      defaultValue: 'supabase',
      toolbar: {
        title: 'Theme',
        icon: 'paintbrush',
        items: [
          { value: 'default', title: 'Default' },
          { value: 'supabase', title: 'Supabase' },
        ],
        dynamicTitle: true,
      },
    },
    mode: {
      description: 'Color mode',
      defaultValue: 'dark',
      toolbar: {
        title: 'Mode',
        icon: 'circlehollow',
        items: [
          { value: 'light', title: 'Light' },
          { value: 'dark', title: 'Dark' },
        ],
        dynamicTitle: true,
      },
    },
  },
  decorators: [
    (Story, context) => {
      const theme = context.globals.theme || 'supabase';
      const mode = context.globals.mode || 'dark';
      
      React.useEffect(() => {
        const root = document.documentElement;
        
        // Remove all theme classes
        root.classList.remove('default', 'supabase', 'light', 'dark');
        
        // Add current theme classes
        root.classList.add(theme, mode);
        
        // Set data attributes
        root.setAttribute('data-theme', theme);
        root.setAttribute('data-mode', mode);
        
        // Apply full theme to Storybook interface
        const storybook = document.querySelector('body');
        if (storybook) {
          storybook.classList.remove('default', 'supabase', 'light', 'dark');
          storybook.classList.add(theme, mode);
          storybook.setAttribute('data-theme', theme);
          storybook.setAttribute('data-mode', mode);
        }
        
        // Apply theme to all possible Storybook containers
        const containers = [
          '.docs-story',
          '#storybook-docs', 
          '.sbdocs',
          '.sbdocs-wrapper',
          '.sb-show-main',
          '#storybook-panel-root',
          '.sidebar-container'
        ];
        
        containers.forEach(selector => {
          const elements = document.querySelectorAll(selector);
          elements.forEach(element => {
            element.classList.remove('default', 'supabase', 'light', 'dark');
            element.classList.add(theme, mode);
            element.setAttribute('data-theme', theme);
            element.setAttribute('data-mode', mode);
          });
        });
      }, [theme, mode]);

      return React.createElement(
        ThemeProvider,
        { defaultTheme: theme, defaultMode: mode },
        React.createElement(Story)
      );
    },
  ],
};

export default preview;