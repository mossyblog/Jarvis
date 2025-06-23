import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import type { NavigationItem } from '../services/api/types';

export function useNavigation() {
  const navigate = useNavigate();
  const location = useLocation();
  const { navigation, hasPermission } = useAuth();

  const navigateToItem = (item: NavigationItem) => {
    if (item.requiredPermission && !hasPermission(item.requiredPermission)) {
      console.warn(`No permission to navigate to ${item.label}`);
      return;
    }
    navigate(item.href);
  };

  const getCurrentItem = (): NavigationItem | undefined => {
    return navigation.find(item => item.href === location.pathname);
  };

  const isItemActive = (item: NavigationItem): boolean => {
    return item.href === location.pathname;
  };

  return {
    navigation,
    navigateToItem,
    getCurrentItem,
    isItemActive,
    currentPath: location.pathname
  };
}