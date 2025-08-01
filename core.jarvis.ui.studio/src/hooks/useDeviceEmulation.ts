import { useEffect } from 'react';
import { useEditMode } from '@/contexts/EditModeContext';

// Device viewport dimensions and characteristics
const DEVICE_CONFIGS = {
  desktop: {
    width: '100%',
    height: '100%',
    maxWidth: 'none',
    userAgent: 'desktop',
    className: 'device-desktop'
  },
  tablet: {
    width: '768px',
    height: '1024px',
    maxWidth: '768px',
    userAgent: 'Mozilla/5.0 (iPad; CPU OS 16_0 like Mac OS X) AppleWebKit/605.1.15',
    className: 'device-tablet'
  },
  mobile: {
    width: '375px', 
    height: '812px',
    maxWidth: '375px',
    userAgent: 'Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X) AppleWebKit/605.1.15',
    className: 'device-mobile'
  }
} as const;

export const useDeviceEmulation = () => {
  const { currentDevice } = useEditMode();

  useEffect(() => {
    const config = DEVICE_CONFIGS[currentDevice];
    const root = document.documentElement;
    
    // Set CSS custom properties for device emulation
    if (currentDevice !== 'desktop') {
      root.style.setProperty('--device-max-width', config.maxWidth);
      // Add device class to body for styling
      document.body.classList.add('device-emulation-active', config.className);
    } else {
      root.style.removeProperty('--device-max-width');
      // Remove all device classes
      Object.values(DEVICE_CONFIGS).forEach(({ className }) => {
        document.body.classList.remove(className);
      });
      document.body.classList.remove('device-emulation-active');
    }
    
    // Cleanup function
    return () => {
      root.style.removeProperty('--device-max-width');
      Object.values(DEVICE_CONFIGS).forEach(({ className }) => {
        document.body.classList.remove(className);
      });
      document.body.classList.remove('device-emulation-active');
    };
  }, [currentDevice]);

  return {
    currentDevice,
    deviceConfig: DEVICE_CONFIGS[currentDevice],
    isEmulating: currentDevice !== 'desktop'
  };
};