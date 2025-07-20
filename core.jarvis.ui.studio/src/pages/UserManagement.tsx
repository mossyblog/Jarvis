import { mockUsers } from '../services/api/mockData';
import type { User as BaseUser } from '../services/api/types';

import { Button } from '../components/ui/button';
import { DropdownMenu, DropdownMenuTrigger, DropdownMenuContent, DropdownMenuItem } from '../components/ui/dropdown-menu';
import { DashboardLayout } from '../components/layout/DashboardLayout';
import { COMPONENT_SPACING } from '../utils/spacing';

// Extend User type for UI fields
interface UserWithUIFields extends BaseUser {
  username: string;
  phone: string;
  status: string;
}

export default function UserManagement() {
  // Temporary: Always use mock data for testing
  const users: UserWithUIFields[] = mockUsers.map((u, i) => ({
    ...u,
    username: u.email.split('@')[0],
    phone: '+1' + String(8000000000 + i * 123456).slice(0,10),
    status: ['Active', 'Suspended', 'Inactive', 'Invited'][i % 4],
  }));


  const handleEdit = (user: UserWithUIFields) => {
    // TODO: Implement edit dialog
    alert(`Edit user: ${user.name}`);
  };

  const handleDelete = (user: UserWithUIFields) => {
    // TODO: Implement delete logic
    alert(`Delete user: ${user.name}`);
  };

  return (
    <DashboardLayout>
      <section className="flex flex-col flex-1 p-lg text-xs">
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-sm pb-md">
          <div className="flex flex-col gap-xs">
            <h2 className="text-2xl font-medium m-0">Users</h2>
            <div className="text-xs text-muted-foreground mt-xs">Manage users, roles, and permissions for your workspace.</div>
          </div>
          <div className="flex flex-row gap-sm items-center w-full md:w-auto mt-sm md:mt-0 justify-between md:justify-end">
            <input
              type="text"
              placeholder="Search email, phone or UID"
              className="bg-card border border-border rounded px-sm py-xs text-xs w-56 focus:outline-none focus:ring-2 focus:ring-primary/40"
              style={{ minWidth: 180 }}
            />
            <select className="bg-card border border-border rounded px-sm py-xs text-xs">
              <option>All users</option>
            </select>
            <select className="bg-card border border-border rounded px-sm py-xs text-xs">
              <option>Provider</option>
            </select>
            <select className="bg-card border border-border rounded px-sm py-xs text-xs">
              <option>All columns</option>
            </select>
            <select className="bg-card border border-border rounded px-sm py-xs text-xs">
              <option>Sorted by created at</option>
            </select>
            <Button size="icon" variant="ghost" className="h-8 w-8" onClick={() => window.location.reload()} title="Refresh">
              <svg width="16" height="16" fill="none" viewBox="0 0 24 24"><path d="M4 4v5h5M20 20v-5h-5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/><path d="M5.07 19A9 9 0 1 1 12 21a9 9 0 0 0 7-3.07" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/></svg>
            </Button>
          </div>
        </div>
        
        <div className="overflow-x-auto">
          <table className="min-w-full border border-border bg-card text-card-foreground">
            <thead className="bg-muted">
              <tr>
                <th className="table-cell-sm text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  UID <span className="inline align-middle text-muted-foreground cursor-pointer">&#8597;</span>
                </th>
                <th className="table-cell-sm text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Display name <span className="inline align-middle text-muted-foreground cursor-pointer">&#8597;</span>
                </th>
                <th className="table-cell-sm text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Email <span className="inline align-middle text-muted-foreground cursor-pointer">&#8597;</span>
                </th>
                <th className="table-cell-sm text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Phone <span className="inline align-middle text-muted-foreground cursor-pointer">&#8597;</span>
                </th>
                <th className="table-cell-sm text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Providers
                </th>
                <th className="table-cell-sm text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Provider type
                </th>
                <th className="table-cell-sm text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Created at <span className="inline align-middle text-muted-foreground cursor-pointer">&#8597;</span>
                </th>
                <th className="table-cell-sm text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Last sign in
                </th>
                <th className="table-cell-sm text-right text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody>
              {users && users.length > 0 ? users.map(user => (
                <tr key={user.id} className="border-b border-border last:border-0 hover:bg-muted/60 transition-colors">
                  <td className="table-cell-sm font-mono">{user.id}</td>
                  <td className="table-cell-sm">{user.username}</td>
                  <td className="table-cell-sm">{user.email}</td>
                  <td className="table-cell-sm">{user.phone}</td>
                  <td className="table-cell-sm">Email</td>
                  <td className="table-cell-sm">-</td>
                  <td className="table-cell-sm font-mono">{new Date().toUTCString().slice(0, 25)}</td>
                  <td className="table-cell-sm text-muted-foreground">Waiting for verification</td>
                  <td className="table-cell-sm text-right">
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button size="icon" variant="ghost" className="h-8 w-8">
                          <span className="sr-only">Open menu</span>
                          <svg width="18" height="18" fill="none" viewBox="0 0 24 24"><circle cx="12" cy="5" r="1.5" fill="currentColor"/><circle cx="12" cy="12" r="1.5" fill="currentColor"/><circle cx="12" cy="19" r="1.5" fill="currentColor"/></svg>
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem onClick={() => handleEdit(user)}>Edit</DropdownMenuItem>
                        <DropdownMenuItem onClick={() => handleDelete(user)}>Delete</DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </td>
                </tr>
              )) : (
                <tr>
                  <td colSpan={9} className="table-cell-sm text-center text-muted-foreground">No users found</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
        
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-sm pt-lg">
          <div className="text-xs text-muted-foreground">{`0 of ${users?.length ?? 0} row(s) selected.`}</div>
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
      </section>
    </DashboardLayout>
  );
}