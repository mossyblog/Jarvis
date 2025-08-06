# Tweakcn Theme Generation Guide

This guide will help you generate the theme CSS files using tweakcn.com based on the colors documented in THEME_COLORS.md.

## Step 1: Access Tweakcn

1. Visit [tweakcn.com](https://tweakcn.com)
2. You'll see a visual theme editor for shadcn/ui components

## Step 2: Generate Supabase Dark Theme

1. In the tweakcn editor, set the following colors:
   - **Background**: `hsl(0, 0%, 9%)` - #171717
   - **Foreground**: `hsl(0, 0%, 98%)` - #fafafa
   - **Card**: `hsl(0, 0%, 11%)` - #1c1c1c
   - **Card Foreground**: `hsl(0, 0%, 98%)` - #fafafa
   - **Primary**: `hsl(142, 75%, 57%)` - #3fcf8e (green)
   - **Primary Foreground**: `hsl(0, 0%, 9%)` - #171717
   - **Secondary**: `hsl(0, 0%, 13%)` - #212121
   - **Muted**: `hsl(0, 0%, 13%)` - #212121
   - **Muted Foreground**: `hsl(0, 0%, 55%)` - #8c8c8c
   - **Accent**: `hsl(142, 75%, 57%)` - #3fcf8e (same as primary)
   - **Destructive**: `hsl(0, 84.2%, 60.2%)` - #f56565
   - **Border**: `hsl(0, 0%, 17%)` - #2b2b2b
   - **Input**: `hsl(0, 0%, 13%)` - #212121
   - **Ring**: `hsl(142, 75%, 57%)` - #3fcf8e

2. Set border radius to `0.5rem`

3. Copy the generated CSS variables

4. Replace the content in `/src/styles/themes/supabase/dark.css` with:
```css
[data-theme="supabase"][data-mode="dark"] {
  /* Paste the generated CSS variables here */
}
```

## Step 3: Generate Supabase Light Theme

1. In tweakcn, adjust colors for light mode:
   - **Background**: `hsl(0, 0%, 100%)` - #ffffff
   - **Foreground**: `hsl(0, 0%, 9%)` - #171717
   - **Primary**: `hsl(142, 75%, 42%)` - #1bc271 (darker green for light mode)
   - **Secondary**: `hsl(0, 0%, 96%)` - #f5f5f5
   - **Muted Foreground**: `hsl(0, 0%, 45%)` - #737373
   - **Border**: `hsl(0, 0%, 90%)` - #e5e5e5
   - **Input**: `hsl(0, 0%, 96%)` - #f5f5f5

2. Replace the content in `/src/styles/themes/supabase/light.css`

## Step 4: Generate Default Dark Theme

1. Set neutral colors:
   - **Background**: `hsl(0, 0%, 3.9%)` - #0a0a0a
   - **Primary**: `hsl(0, 0%, 98%)` - #fafafa (white as primary)
   - **Secondary**: `hsl(0, 0%, 14.9%)` - #262626
   - **Destructive**: `hsl(0, 62.8%, 30.6%)` - #7f1d1d
   - **Ring**: `hsl(0, 0%, 83.1%)` - #d4d4d4

2. Replace the content in `/src/styles/themes/default/dark.css`

## Step 5: Generate Default Light Theme

1. Standard light theme colors:
   - **Background**: `hsl(0, 0%, 100%)` - #ffffff
   - **Foreground**: `hsl(0, 0%, 3.9%)` - #0a0a0a
   - **Primary**: `hsl(0, 0%, 9%)` - #171717 (dark as primary)
   - **Ring**: `hsl(0, 0%, 3.9%)` - #0a0a0a

2. Replace the content in `/src/styles/themes/default/light.css`

## Step 6: Add Additional Variables

For each theme file, also add these additional variables:

### Sidebar Colors (example for supabase dark):
```css
--sidebar-background: 240 5.9% 10%;
--sidebar-foreground: 240 4.8% 95.9%;
--sidebar-primary: 224.3 76.3% 48%;
--sidebar-primary-foreground: 0 0% 100%;
--sidebar-accent: 240 3.7% 15.9%;
--sidebar-accent-foreground: 240 4.8% 95.9%;
--sidebar-border: 240 3.7% 15.9%;
--sidebar-ring: 217.2 91.2% 59.8%;
```

### Extended Supabase Colors:
```css
--dash-sidebar: 0 0% 9%;
--default: 0 0% 17%;
--brand: 142 75% 57%;
--brand-600: 142 75% 47%;
--gray-100: 0 0% 94%;
--gray-200: 0 0% 85%;
--gray-300: 0 0% 74%;
--gray-400: 0 0% 55%;
--gray-500: 0 0% 45%;
--gray-600: 0 0% 35%;
--gray-700: 0 0% 25%;
--gray-800: 0 0% 13%;
--gray-900: 0 0% 9%;
```

### Helper Colors:
```css
--foreground-light: 0 0% 63.9%;
--foreground-lighter: 0 0% 45.1%;
--border-stronger: 0 0% 22%;
```

## Step 7: Test the Themes

1. Run `npm run dev`
2. Navigate to the application
3. Use the theme switcher in the header to test:
   - Supabase Dark
   - Supabase Light
   - Default Dark
   - Default Light
4. Verify that preferences persist on page reload

## Tips

- Tweakcn may export colors in different formats. Convert them to HSL space-separated format
- Make sure to wrap all variables in the appropriate data attribute selectors
- Test each theme thoroughly, especially form inputs and interactive components
- Check contrast ratios for accessibility