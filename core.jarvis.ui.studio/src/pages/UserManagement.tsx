import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../components/ui/button';
import { DropdownMenu, DropdownMenuTrigger, DropdownMenuContent, DropdownMenuItem } from '../components/ui/dropdown-menu';
import { DashboardLayout } from '../components/layout/DashboardLayout';
import { ContentHeader } from '../components/layout/ContentHeader';
import { useNavigation } from '../hooks/useNavigation';
import { graphqlService } from '../services/graphql/graphqlService';
import { AlertCircle, RefreshCw, MoreVertical, Users, UserCheck, Shield, Search } from 'lucide-react';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../components/ui/table';
import { Badge } from '../components/ui/badge';
import { NotificationCard } from '../components/ui/notification-card';
import { TableSkeleton } from '../components/ui/table-skeleton';
import { Skeleton } from '../components/ui/skeleton';
import { SelectionCheckbox } from '../components/ui/selection-checkbox';
import { BulkActionsToolbar } from '../components/ui/bulk-actions-toolbar';
import { useSelectionState, type BulkAction } from '../hooks/useSelectionState';
import { cn } from '../lib/utils';

// Account type based on database schema
interface Account {
  id: string;
  ownerEntityId: string;
  email: string;
  authMethod: string;
  isActive: boolean;
  createdAt: string;
  lastUpdated: string;
  ipAddress: string | null;
  userAgent: string | null;
  profile: {
    name: string;
    roleIds: string[];
    permissionIds: string[];
  } | null;
}

export default function UserManagement() {
  const navigate = useNavigate();
  const { navigateToItem, navigation } = useNavigation();
  const [activeItem, setActiveItem] = useState('accounts');
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeFilter, setActiveFilter] = useState<'all' | 'active' | 'inactive'>('all');
  // const [authMethodFilter, setAuthMethodFilter] = useState<'all' | 'email' | 'oauth'>('all'); // TODO: implement auth method filtering
  const [searchQuery, setSearchQuery] = useState('');
  const [searchLoading, setSearchLoading] = useState(false);
  const [enableSelection, setEnableSelection] = useState(false);
  
  // Bulk actions for user management
  const bulkActions: BulkAction<Account>[] = [
    {
      id: 'activate',
      label: 'Activate',
      icon: 'check',
      action: (accounts) => {
        console.log('Activating accounts:', accounts.map(a => a.email));
        // TODO: Implement activation API call
      },
      tooltip: 'Activate selected accounts',
      validate: (accounts) => accounts.some(a => !a.isActive)
    },
    {
      id: 'deactivate',
      label: 'Deactivate',
      icon: 'x',
      action: (accounts) => {
        console.log('Deactivating accounts:', accounts.map(a => a.email));
        // TODO: Implement deactivation API call
      },
      tooltip: 'Deactivate selected accounts',
      validate: (accounts) => accounts.some(a => a.isActive)
    },
    {
      id: 'delete',
      label: 'Delete',
      icon: 'trash',
      action: (accounts) => {
        const confirmed = window.confirm(
          `Are you sure you want to delete ${accounts.length} account(s)?\n\n` +
          'This action cannot be undone and will remove all associated data.'
        );
        if (confirmed) {
          console.log('Deleting accounts:', accounts.map(a => a.email));
          // TODO: Implement bulk delete API call
        }
      },
      destructive: true,
      tooltip: 'Delete selected accounts',
      shortcut: 'Del'
    },
    {
      id: 'export',
      label: 'Export',
      icon: 'download',
      action: (accounts) => {
        const data = JSON.stringify(accounts, null, 2);
        const blob = new Blob([data], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `accounts-${new Date().toISOString().split('T')[0]}.json`;
        a.click();
        URL.revokeObjectURL(url);
      },
      tooltip: 'Export selected accounts as JSON'
    }
  ];

  const handleBulkAction = (actionId: string) => {
    // Will be defined after filteredAccounts
  };

  const handleItemClick = (itemId: string) => {
    setActiveItem(itemId);
    const item = navigation.find(nav => nav.id === itemId);
    if (item) {
      navigateToItem(item);
    }
  };

  // Fetch accounts data on mount
  useEffect(() => {
    async function fetchAccounts() {
      try {
        setLoading(true);
        setError(null);
        const data = await graphqlService.getAccounts();
        setAccounts(data as unknown as Account[]);
      } catch (err) {
        console.error('Failed to fetch accounts:', err);
        setError('Failed to load accounts. Please try again later.');
      } finally {
        setLoading(false);
      }
    }

    fetchAccounts();
  }, []);

  // Handle search when search icon is clicked
  const handleSearch = async () => {
    if (!searchQuery.trim()) {
      // If search is empty, fetch all accounts
      try {
        setSearchLoading(true);
        setError(null);
        const data = await graphqlService.getAccounts();
        setAccounts(data as unknown as Account[]);
      } catch (err) {
        console.error('Failed to fetch accounts:', err);
        setError('Failed to load accounts. Please try again later.');
      } finally {
        setSearchLoading(false);
      }
      return;
    }

    try {
      setSearchLoading(true);
      setError(null);
      const data = await graphqlService.searchAccounts(searchQuery);
      setAccounts(data as unknown as Account[]);
    } catch (err) {
      console.error('Failed to search accounts:', err);
      setError('Failed to search accounts. Please try again later.');
    } finally {
      setSearchLoading(false);
    }
  };

  // Handle enter key in search input
  const handleSearchKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      handleSearch();
    }
  };


  const handleEdit = (account: Account) => {
    // Navigate to the edit page
    navigate(`/accounts/${account.id}/edit`);
  };

  const handleDelete = (account: Account) => {
    // Implement proper delete confirmation and API call
    const confirmed = window.confirm(
      `Are you sure you want to delete account "${account.email}"?\n\n` +
      'This action cannot be undone and will remove all associated data.'
    );
    
    if (confirmed) {
      console.log('Deleting account:', account);
      alert(`Account "${account.email}" would be deleted.\nFeature: API integration required`);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  // Filter accounts based on active filters (search is now handled by GraphQL)
  const filteredAccounts = accounts.filter(account => {
    // Status filter
    if (activeFilter === 'active' && !account.isActive) return false;
    if (activeFilter === 'inactive' && account.isActive) return false;
    
    return true;
  });

  // Selection state management
  const {
    selectedItems,
    isSelected,
    getSelectAllProps,
    getItemProps,
    getTableRowProps,
    showBulkToolbar,
    bulkToolbarActions,
    executeBulkAction,
    deselectAll
  } = useSelectionState(filteredAccounts, {
    mode: 'multiple',
    getId: (account) => account.id,
    enableKeyboard: true,
    enableRangeSelection: true,
    enableSelectAll: true,
    bulkActions,
    onBulkAction: (action, accounts) => {
      console.log(`Bulk action ${action.id} on`, accounts.length, 'accounts');
    }
  });

  const realHandleBulkAction = (actionId: string) => {
    executeBulkAction(actionId);
  };

  return (
    <DashboardLayout activeItem={activeItem} onItemClick={handleItemClick}>
      <div className="@container">
        {/* Content Header */}
        <ContentHeader
          title="Accounts"
          description="Manage accounts, roles, and permissions for your workspace."
        />

        {/* Content Body */}
        <div className="px-lg py-lg space-y-lg">
          {/* Filter Header */}
          <div className="flex items-center gap-2">
            {loading ? (
              <Skeleton className="h-8 w-8" />
            ) : (
              <span className="text-2xl font-light">{accounts.length}</span>
            )}
            <span className="text-base">accounts</span>
            <span className="text-base text-muted-foreground">total</span>
          </div>

          {/* Filter Tabs and Search */}
          <div className="space-y-6">
            {/* Selection Toggle */}
            <div className="flex items-center gap-4">
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={enableSelection}
                  onChange={(e) => setEnableSelection(e.target.checked)}
                  className="rounded border-border"
                />
                Enable selection for bulk operations
              </label>
              {enableSelection && selectedItems.length > 0 && (
                <span className="text-sm text-muted-foreground">
                  {selectedItems.length} account{selectedItems.length !== 1 ? 's' : ''} selected
                </span>
              )}
            </div>
            
            {/* Status Filter Tabs and Search on same row */}
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-1">
                <button
                  onClick={() => setActiveFilter('all')}
                  className={cn(
                    "flex items-center gap-2 px-4 py-2 text-sm rounded-md transition-colors",
                    activeFilter === 'all' 
                      ? "bg-primary/20 text-primary" 
                      : "text-muted-foreground hover:text-foreground"
                  )}
                >
                  <Users size={16} />
                  <span className="uppercase tracking-wide text-xs font-medium">All Users</span>
                  <span className={cn(
                    "px-1.5 py-0.5 rounded text-xs",
                    activeFilter === 'all' ? "bg-primary text-primary-foreground" : "bg-muted"
                  )}>
                    {loading ? (
                      <Skeleton className="h-3 w-4" />
                    ) : (
                      accounts.length
                    )}
                  </span>
                </button>
                
                <button
                  onClick={() => setActiveFilter('active')}
                  className={cn(
                    "flex items-center gap-2 px-4 py-2 text-sm rounded-md transition-colors",
                    activeFilter === 'active' 
                      ? "bg-success/20 text-success" 
                      : "text-muted-foreground hover:text-foreground"
                  )}
                >
                  <UserCheck size={16} />
                  <span className="uppercase tracking-wide text-xs font-medium">Active</span>
                  <span className={cn(
                    "px-1.5 py-0.5 rounded text-xs",
                    activeFilter === 'active' ? "bg-success text-success-foreground" : "bg-muted"
                  )}>
                    {loading ? (
                      <Skeleton className="h-3 w-4" />
                    ) : (
                      accounts.filter(a => a.isActive).length
                    )}
                  </span>
                </button>

                <button
                  onClick={() => setActiveFilter('inactive')}
                  className={cn(
                    "flex items-center gap-2 px-4 py-2 text-sm rounded-md transition-colors",
                    activeFilter === 'inactive' 
                      ? "bg-muted-foreground/20 text-muted-foreground" 
                      : "text-muted-foreground hover:text-foreground"
                  )}
                >
                  <Shield size={16} />
                  <span className="uppercase tracking-wide text-xs font-medium">Inactive</span>
                  <span className={cn(
                    "px-1.5 py-0.5 rounded text-xs",
                    activeFilter === 'inactive' ? "bg-muted-foreground text-muted-foreground-foreground" : "bg-muted"
                  )}>
                    {loading ? (
                      <Skeleton className="h-3 w-4" />
                    ) : (
                      accounts.filter(a => !a.isActive).length
                    )}
                  </span>
                </button>
              </div>

              {/* Search and Controls on the right */}
              <div className="flex items-center gap-2">
                <div className="relative">
                  <Search size={16} className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground" />
                  <input
                    type="text"
                    placeholder="Search email, phone or UID"
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    onKeyPress={handleSearchKeyPress}
                    className="bg-card border border-border rounded pl-10 pr-4 py-2 text-sm w-80 focus:outline-none focus:ring-2 focus:ring-primary/40"
                    disabled={searchLoading}
                  />
                </div>
                
                <Button 
                  size="icon" 
                  variant="ghost" 
                  className="h-8 w-8" 
                  onClick={handleSearch} 
                  title="Search accounts"
                  disabled={loading || searchLoading}
                >
                  <RefreshCw className={cn("h-4 w-4", (loading || searchLoading) && "animate-spin")} />
                </Button>
              </div>
            </div>
          </div>
          
          {/* Data Table */}
          {loading ? (
            <TableSkeleton rows={6} columns={8} />
          ) : error ? (
            <NotificationCard
              variant="error"
              icon={AlertCircle}
              title="Error Loading Accounts"
              description={error}
            />
          ) : (
            <div data-selectable-container>
              <Table>
                <TableHeader>
                  <TableRow>
                    {enableSelection && (
                      <TableHead className="w-12">
                        <SelectionCheckbox
                          size="sm"
                          {...getSelectAllProps()}
                        />
                      </TableHead>
                    )}
                    <TableHead className="font-mono">Entity ID</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Profile Name</TableHead>
                  <TableHead>Auth Method</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead>Last Updated</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filteredAccounts.length > 0 ? filteredAccounts.map(account => {
                    const rowProps = enableSelection ? getTableRowProps(account) : {};
                    const selected = enableSelection ? isSelected(account) : false;
                    
                    return (
                    <TableRow 
                      key={account.id}
                      className={cn(
                        'transition-colors',
                        selected && 'bg-accent/50',
                        enableSelection && 'cursor-pointer'
                      )}
                      {...rowProps}
                    >
                      {enableSelection && (
                        <TableCell>
                          <SelectionCheckbox
                            size="sm"
                            {...getItemProps(account)}
                          />
                        </TableCell>
                      )}
                    <TableCell className="font-mono text-xs">
                      {account.ownerEntityId.slice(0, 8)}...
                    </TableCell>
                    <TableCell>{account.email}</TableCell>
                    <TableCell>
                      {account.profile?.name || <span className="text-muted-foreground">No profile</span>}
                    </TableCell>
                    <TableCell>
                      <Badge variant="secondary" className="text-xs">
                        {account.authMethod}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={account.isActive ? "default" : "secondary"}>
                        {account.isActive ? "Active" : "Inactive"}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-xs">
                      {formatDate(account.createdAt)}
                    </TableCell>
                    <TableCell className="text-xs">
                      {formatDate(account.lastUpdated)}
                    </TableCell>
                    <TableCell className="text-right">
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button size="icon" variant="ghost" className="h-8 w-8">
                            <span className="sr-only">Open menu</span>
                            <MoreVertical className="h-4 w-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                          <DropdownMenuItem onClick={() => handleEdit(account)}>
                            Edit Account
                          </DropdownMenuItem>
                          <DropdownMenuItem 
                            onClick={() => handleDelete(account)}
                            className="text-destructive"
                          >
                            Delete Account
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </TableCell>
                    </TableRow>
                    );
                  }) : (
                    <TableRow>
                      <TableCell colSpan={enableSelection ? 9 : 8} className="text-center text-muted-foreground">
                      {`No ${activeFilter === 'all' ? '' : activeFilter + ' '}accounts found`}
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
              
              {/* Bulk Actions Toolbar */}
              {enableSelection && (
                <BulkActionsToolbar
                  selectedCount={selectedItems.length}
                  actions={bulkToolbarActions}
                  visible={showBulkToolbar}
                  onAction={realHandleBulkAction}
                  onDismiss={deselectAll}
                  position="bottom"
                />
              )}
            </div>
          )}
          
          {/* Pagination */}
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-sm">
            <div className="text-xs text-muted-foreground">
              {loading ? (
                <Skeleton className="h-4 w-32" />
              ) : (
                `${filteredAccounts.length} of ${accounts.length} account${accounts.length !== 1 ? 's' : ''} shown`
              )}
            </div>
            <div className="flex items-center gap-md">
              <span className="text-xs">Rows per page</span>
              <select className="bg-card border border-border rounded px-sm py-xs text-xs">
                <option value="10">10</option>
                <option value="20">20</option>
              </select>
              <span className="text-xs">Page 1 of 1</span>
              <Button size="icon" variant="ghost" className="h-8 w-8"><span className="sr-only">First</span>&laquo;</Button>
              <Button size="icon" variant="ghost" className="h-8 w-8"><span className="sr-only">Prev</span>&lsaquo;</Button>
              <Button size="icon" variant="ghost" className="h-8 w-8"><span className="sr-only">Next</span>&rsaquo;</Button>
              <Button size="icon" variant="ghost" className="h-8 w-8"><span className="sr-only">Last</span>&raquo;</Button>
            </div>
          </div>
        </div>
      </div>
    </DashboardLayout>
  );
}