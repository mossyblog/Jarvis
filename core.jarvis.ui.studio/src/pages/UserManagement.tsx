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
  const [authMethodFilter, setAuthMethodFilter] = useState<'all' | 'email' | 'oauth'>('all');
  const [searchQuery, setSearchQuery] = useState('');

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
        setAccounts(data);
      } catch (err) {
        console.error('Failed to fetch accounts:', err);
        setError('Failed to load accounts. Please try again later.');
      } finally {
        setLoading(false);
      }
    }

    fetchAccounts();
  }, []);


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

  // Filter accounts based on active filters
  const filteredAccounts = accounts.filter(account => {
    // Status filter
    if (activeFilter === 'active' && !account.isActive) return false;
    if (activeFilter === 'inactive' && account.isActive) return false;
    
    // Search filter
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      return (
        account.email.toLowerCase().includes(query) ||
        account.ownerEntityId.toLowerCase().includes(query) ||
        (account.profile?.name?.toLowerCase().includes(query))
      );
    }
    
    return true;
  });

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
            <span className="text-2xl font-light">{accounts.length}</span>
            <span className="text-base">accounts</span>
            <span className="text-base text-muted-foreground">total</span>
          </div>

          {/* Filter Tabs */}
          <div className="space-y-6">
            {/* Status Filter Tabs */}
            <div className="flex items-center gap-1">
              <button
                onClick={() => setActiveFilter('all')}
                className={cn(
                  "flex items-center gap-2 px-4 py-2 text-sm rounded-md transition-colors",
                  activeFilter === 'all' 
                    ? "bg-blue-500/20 text-blue-500" 
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                <Users size={16} />
                <span className="uppercase tracking-wide text-xs font-medium">All Users</span>
                <span className={cn(
                  "px-1.5 py-0.5 rounded text-xs",
                  activeFilter === 'all' ? "bg-blue-500 text-white" : "bg-muted"
                )}>
                  {accounts.length}
                </span>
              </button>
              
              <button
                onClick={() => setActiveFilter('active')}
                className={cn(
                  "flex items-center gap-2 px-4 py-2 text-sm rounded-md transition-colors",
                  activeFilter === 'active' 
                    ? "bg-green-500/20 text-green-500" 
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                <UserCheck size={16} />
                <span className="uppercase tracking-wide text-xs font-medium">Active</span>
                <span className={cn(
                  "px-1.5 py-0.5 rounded text-xs",
                  activeFilter === 'active' ? "bg-green-500 text-white" : "bg-muted"
                )}>
                  {accounts.filter(a => a.isActive).length}
                </span>
              </button>

              <button
                onClick={() => setActiveFilter('inactive')}
                className={cn(
                  "flex items-center gap-2 px-4 py-2 text-sm rounded-md transition-colors",
                  activeFilter === 'inactive' 
                    ? "bg-gray-500/20 text-gray-500" 
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                <Shield size={16} />
                <span className="uppercase tracking-wide text-xs font-medium">Inactive</span>
                <span className={cn(
                  "px-1.5 py-0.5 rounded text-xs",
                  activeFilter === 'inactive' ? "bg-gray-500 text-white" : "bg-muted"
                )}>
                  {accounts.filter(a => !a.isActive).length}
                </span>
              </button>
            </div>

            {/* Search and Controls */}
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-4">
                <div className="relative">
                  <Search size={16} className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground" />
                  <input
                    type="text"
                    placeholder="Search email, phone or UID"
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="bg-card border border-border rounded pl-10 pr-4 py-2 text-sm w-80 focus:outline-none focus:ring-2 focus:ring-primary/40"
                  />
                </div>
              </div>
              
              <div className="flex items-center gap-2">
                <Button 
                  size="icon" 
                  variant="ghost" 
                  className="h-8 w-8" 
                  onClick={async () => {
                    setLoading(true);
                    try {
                      const data = await graphqlService.getAccounts();
                      setAccounts(data);
                      setError(null);
                    } catch (err) {
                      setError('Failed to refresh accounts');
                    } finally {
                      setLoading(false);
                    }
                  }} 
                  title="Refresh"
                  disabled={loading}
                >
                  <RefreshCw className={cn("h-4 w-4", loading && "animate-spin")} />
                </Button>
              </div>
            </div>
          </div>
          
          {/* Data Table */}
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <RefreshCw className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          ) : error ? (
            <NotificationCard
              variant="error"
              icon={AlertCircle}
              title="Error Loading Accounts"
              description={error}
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
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
                {filteredAccounts.length > 0 ? filteredAccounts.map(account => (
                  <TableRow key={account.id}>
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
                )) : (
                  <TableRow>
                    <TableCell colSpan={8} className="text-center text-muted-foreground">
                      {searchQuery ? `No accounts found matching "${searchQuery}"` : `No ${activeFilter === 'all' ? '' : activeFilter + ' '}accounts found`}
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          )}
          
          {/* Pagination */}
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-sm">
            <div className="text-xs text-muted-foreground">
              {filteredAccounts.length} of {accounts.length} account{accounts.length !== 1 ? 's' : ''} shown
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