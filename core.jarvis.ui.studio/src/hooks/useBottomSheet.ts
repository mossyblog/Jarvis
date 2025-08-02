/**
 * Bottom Sheet Hook
 * 
 * State management hook for bottom sheet components.
 */

import { useState, useCallback } from 'react';

export const useBottomSheet = (initialState = false) => {
  const [isOpen, setIsOpen] = useState(initialState);

  const open = useCallback(() => setIsOpen(true), []);
  const close = useCallback(() => setIsOpen(false), []);
  const toggle = useCallback(() => setIsOpen(prev => !prev), []);

  return {
    isOpen,
    open,
    close,
    toggle,
  };
};