import type { StorybookConfig } from '@storybook/react-vite';
import path from 'path';

const config: StorybookConfig = {
  stories: [
    "../src/**/*.stories.@(js|jsx|mjs|ts|tsx)",
    "../src/**/*.mdx"
  ],
  addons: [
    "@storybook/addon-docs",
    "@storybook/addon-onboarding"
  ],
  framework: {
    name: "@storybook/react-vite",
    options: {}
  },
  async viteFinal(config) {
    if (config.resolve) {
      config.resolve.alias = {
        ...config.resolve.alias,
        "@": path.resolve(__dirname, "../src"),
      };
    }
    return config;
  },
};
export default config;