import { Moon, Sun, Palette } from 'lucide-react';
import { useTheme, type ThemeName } from '../../contexts/ThemeContext';
import { Button } from './button';
import { IconButton } from './icon-button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from './dropdown-menu';
import { themes } from '../../styles/themes';
import { LucideIcon as Icon } from './icon';

export function ThemeSwitcher() {
  const { theme, mode, setTheme, setMode } = useTheme();

  const handleModeToggle = () => {
    setMode(mode === 'light' ? 'dark' : 'light');
  };

  return (
    <div className="flex items-center gap-2">
      {/* Mode Toggle Button */}
      <IconButton
        variant="ghost"
        size="sm"
        onClick={handleModeToggle}
        title={`Switch to ${mode === 'light' ? 'dark' : 'light'} mode`}
        aria-label="Toggle theme mode"
      >
        {mode === 'light' ? (
          <Icon icon={Moon} size="xs" />
        ) : (
          <Icon icon={Sun} size="xs" />
        )}
        <span className="sr-only">Toggle theme mode</span>
      </IconButton>

      {/* Theme Selection Dropdown */}
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <IconButton 
            variant="ghost" 
            size="sm" 
            title="Select theme"
            aria-label="Select theme"
          >
            <Icon icon={Palette} size="xs" />
            <span className="sr-only">Select theme</span>
          </IconButton>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-[200px]">
          <DropdownMenuLabel>Theme</DropdownMenuLabel>
          <DropdownMenuSeparator />
          <DropdownMenuRadioGroup value={theme} onValueChange={(value) => setTheme(value as ThemeName)}>
            {Object.values(themes).map((themeInfo) => (
              <DropdownMenuRadioItem 
                key={themeInfo.name} 
                value={themeInfo.name}
                className="cursor-pointer"
              >
                <div>
                  <div className="font-medium">{themeInfo.displayName}</div>
                  <div className="text-xs text-muted-foreground">
                    {themeInfo.description}
                  </div>
                </div>
              </DropdownMenuRadioItem>
            ))}
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}

export interface ThemeSwitcherCompactProps {
  showLabel?: boolean;
}

export function ThemeSwitcherCompact({ showLabel = false }: ThemeSwitcherCompactProps) {
  const { theme, mode, setTheme, setMode } = useTheme();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="sm" className="gap-2">
          {mode === 'light' ? (
            <Icon icon={Sun} size="xs" />
          ) : (
            <Icon icon={Moon} size="xs" />
          )}
          {showLabel && (
            <span className="text-sm capitalize">
              {themes[theme]?.displayName || theme} • {mode}
            </span>
          )}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-[200px]">
        <DropdownMenuLabel>Appearance</DropdownMenuLabel>
        <DropdownMenuSeparator />
        
        {/* Mode Selection */}
        <DropdownMenuLabel className="text-xs font-normal text-muted-foreground">
          Mode
        </DropdownMenuLabel>
        <DropdownMenuItem onClick={() => setMode('light')} className="gap-2">
          <Icon icon={Sun} size="xs" />
          Light
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => setMode('dark')} className="gap-2">
          <Icon icon={Moon} size="xs" />
          Dark
        </DropdownMenuItem>
        
        <DropdownMenuSeparator />
        
        {/* Theme Selection */}
        <DropdownMenuLabel className="text-xs font-normal text-muted-foreground">
          Theme
        </DropdownMenuLabel>
        <DropdownMenuRadioGroup value={theme} onValueChange={(value) => setTheme(value as ThemeName)}>
          {Object.values(themes).map((themeInfo) => (
            <DropdownMenuRadioItem 
              key={themeInfo.name} 
              value={themeInfo.name}
              className="cursor-pointer"
            >
              {themeInfo.displayName}
            </DropdownMenuRadioItem>
          ))}
        </DropdownMenuRadioGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}