import { Moon, Sun, Palette } from 'lucide-react';
import { useTheme, type ThemeName } from '../../contexts/ThemeContext';
import { Button } from './button';
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

export function ThemeSwitcher() {
  const { theme, mode, setTheme, setMode } = useTheme();

  const handleModeToggle = () => {
    setMode(mode === 'light' ? 'dark' : 'light');
  };

  return (
    <div className="flex items-center gap-2">
      {/* Mode Toggle Button */}
      <Button
        variant="ghost"
        size="icon"
        onClick={handleModeToggle}
        className="h-8 w-8"
        title={`Switch to ${mode === 'light' ? 'dark' : 'light'} mode`}
      >
        {mode === 'light' ? (
          <Moon className="h-4 w-4" />
        ) : (
          <Sun className="h-4 w-4" />
        )}
        <span className="sr-only">Toggle theme mode</span>
      </Button>

      {/* Theme Selection Dropdown */}
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button 
            variant="ghost" 
            size="icon" 
            className="h-8 w-8"
            title="Select theme"
          >
            <Palette className="h-4 w-4" />
            <span className="sr-only">Select theme</span>
          </Button>
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
            <Sun className="h-4 w-4" />
          ) : (
            <Moon className="h-4 w-4" />
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
          <Sun className="h-4 w-4" />
          Light
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => setMode('dark')} className="gap-2">
          <Moon className="h-4 w-4" />
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