import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import { ArrowLeft, Save, X, Upload, User, UserCog, Shield, Activity, Key, AlertTriangle } from 'lucide-react';
import { DashboardLayout } from '../components/layout/DashboardLayout';
import { ContentHeader } from '../components/layout/ContentHeader';
import { Button } from '../components/ui/button';
import { Input } from '../components/ui/input';
import { Label } from '../components/ui/label';
import { 
  Select, 
  SelectContent, 
  SelectItem, 
  SelectTrigger, 
  SelectValue 
} from '../components/ui/select';
import { Separator } from '../components/ui/separator';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card';
import { Switch } from '../components/ui/switch';
import { cn } from '../lib/utils';

interface AccountFormData {
  email: string;
  displayName: string;
  status: 'active' | 'inactive';
  authMethod: 'password' | 'otp' | 'pin';
  avatarUrl: string;
  preferences: {
    theme: 'light' | 'dark' | 'system';
    notifications: boolean;
    avatarVisibility: boolean;
  };
}

interface AccountData {
  id: string;
  ownerEntityId: string;
  email: string;
  authMethod: string;
  isActive: boolean;
  createdAt: string;
  lastUpdated: string;
  profile?: {
    name: string;
    avatarUrl?: string;
    preferences?: Record<string, unknown>;
  };
}

export default function AccountEdit() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [activeItem] = useState('accounts');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [account, setAccount] = useState<AccountData | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [avatarPreview, setAvatarPreview] = useState<string>('');
  const [activeTab, setActiveTab] = useState<'details' | 'security' | 'activity' | 'api' | 'danger'>('details');
  const fileInputRef = useRef<HTMLInputElement>(null);

  const {
    control,
    handleSubmit,
    watch,
    reset,
    setValue,
    formState: { isDirty, errors }
  } = useForm<AccountFormData>({
    defaultValues: {
      email: '',
      displayName: '',
      status: 'active',
      authMethod: 'password',
      avatarUrl: '',
      preferences: {
        theme: 'system',
        notifications: true,
        avatarVisibility: true
      }
    }
  });

  const displayName = watch('displayName');

  // Fetch account data
  useEffect(() => {
    async function fetchAccount() {
      if (!id) return;
      
      try {
        setLoading(true);
        setError(null);
        // For now, using mock data - replace with actual API call
        const mockAccount: AccountData = {
          id: id,
          ownerEntityId: '550e8400-e29b-41d4-a716-446655440000',
          email: 'user@example.com',
          authMethod: 'password',
          isActive: true,
          createdAt: '2024-01-15T10:30:00Z',
          lastUpdated: '2024-01-20T14:45:00Z',
          profile: {
            name: 'John Doe',
            avatarUrl: 'https://api.dicebear.com/7.x/avataaars/svg?seed=John',
            preferences: { theme: 'dark', notifications: true }
          }
        };
        
        setAccount(mockAccount);
        
        // Reset form with fetched data
        const prefs = mockAccount.profile?.preferences || {};
        reset({
          email: mockAccount.email,
          displayName: mockAccount.profile?.name || '',
          status: mockAccount.isActive ? 'active' : 'inactive',
          authMethod: mockAccount.authMethod as AccountFormData['authMethod'],
          avatarUrl: mockAccount.profile?.avatarUrl || '',
          preferences: {
            theme: (prefs.theme as 'light' | 'dark' | 'system') || 'system',
            notifications: prefs.notifications !== false,
            avatarVisibility: prefs.avatarVisibility !== false
          }
        });
        setAvatarPreview(mockAccount.profile?.avatarUrl || '');
      } catch (err) {
        console.error('Failed to fetch account:', err);
        setError('Failed to load account details');
      } finally {
        setLoading(false);
      }
    }

    fetchAccount();
  }, [id, reset]);

  const onSubmit = async (data: AccountFormData) => {
    try {
      setSaving(true);
      setError(null);
      
      // TODO: Implement actual save logic
      console.log('Saving account:', data);
      
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      // Show success message or navigate back
      navigate('/accounts');
    } catch (err) {
      console.error('Failed to save account:', err);
      setError('Failed to save account changes');
    } finally {
      setSaving(false);
    }
  };

  const handleDiscard = () => {
    if (account) {
      const prefs = account.profile?.preferences || {};
      reset({
        email: account.email,
        displayName: account.profile?.name || '',
        status: account.isActive ? 'active' : 'inactive',
        authMethod: account.authMethod as AccountFormData['authMethod'],
        avatarUrl: account.profile?.avatarUrl || '',
        preferences: {
          theme: (prefs.theme as 'light' | 'dark' | 'system') || 'system',
          notifications: prefs.notifications !== false,
          avatarVisibility: prefs.avatarVisibility !== false
        }
      });
      setAvatarPreview(account.profile?.avatarUrl || '');
    }
  };

  const handleAvatarUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onloadend = () => {
        const base64String = reader.result as string;
        setAvatarPreview(base64String);
        setValue('avatarUrl', base64String, { shouldDirty: true });
      };
      reader.readAsDataURL(file);
    }
  };

  const handleRemoveAvatar = () => {
    setAvatarPreview('');
    setValue('avatarUrl', '', { shouldDirty: true });
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const getInitials = (name: string): string => {
    return name
      .split(' ')
      .map(part => part[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  if (loading) {
    return (
      <DashboardLayout activeItem={activeItem} onItemClick={() => {}}>
        <div className="flex items-center justify-center h-full">
          <div className="text-muted-foreground">Loading account details...</div>
        </div>
      </DashboardLayout>
    );
  }

  if (error && !account) {
    return (
      <DashboardLayout activeItem={activeItem} onItemClick={() => {}}>
        <div className="flex flex-col items-center justify-center h-full gap-md">
          <div className="text-destructive">{error}</div>
          <Button onClick={() => navigate('/accounts')}>Back to Accounts</Button>
        </div>
      </DashboardLayout>
    );
  }

  return (
    <DashboardLayout activeItem={activeItem} onItemClick={() => {}}>
      <form onSubmit={handleSubmit(onSubmit)} className="@container">
        {/* Content Header */}
        <ContentHeader
          title="Edit Account"
          description={`Manage account details and settings for ${account?.email || 'user'}`}
          breadcrumbs={
            <button
              type="button"
              onClick={() => navigate('/accounts')}
              className="flex items-center text-sm text-muted-foreground hover:text-foreground"
            >
              <ArrowLeft className="h-4 w-4 mr-1" />
              Back to Accounts
            </button>
          }
          actions={
            <div className="flex items-center gap-sm">
              {isDirty && (
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={handleDiscard}
                  className="gap-xs"
                >
                  <X className="h-4 w-4" />
                  Discard
                </Button>
              )}
              <Button 
                type="submit" 
                size="sm" 
                disabled={saving || !isDirty}
                className="gap-xs"
              >
                <Save className="h-4 w-4" />
                Save Changes
              </Button>
            </div>
          }
        />

        {/* Content Body */}
        <div className="px-lg py-lg space-y-lg">
          {/* Dashboard-Style Tabs */}
          <div className="space-y-0">
            <div className="flex items-center gap-1 relative">
              <button
                type="button"
                onClick={() => setActiveTab('details')}
                className={cn(
                  "flex items-center gap-2 px-6 py-3 text-sm rounded-t-md transition-colors",
                  activeTab === 'details' 
                    ? "bg-blue-500/20 text-blue-500" 
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                <UserCog size={16} />
                <span className="uppercase tracking-wide text-xs font-medium">Details</span>
              </button>
              
              <button
                type="button"
                onClick={() => setActiveTab('security')}
                className={cn(
                  "flex items-center gap-2 px-6 py-3 text-sm rounded-t-md transition-colors",
                  activeTab === 'security' 
                    ? "bg-green-500/20 text-green-500" 
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                <Shield size={16} />
                <span className="uppercase tracking-wide text-xs font-medium">Security</span>
              </button>

              <button
                type="button"
                onClick={() => setActiveTab('activity')}
                className={cn(
                  "flex items-center gap-2 px-6 py-3 text-sm rounded-t-md transition-colors",
                  activeTab === 'activity' 
                    ? "bg-purple-500/20 text-purple-500" 
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                <Activity size={16} />
                <span className="uppercase tracking-wide text-xs font-medium">Activity</span>
              </button>

              <button
                type="button"
                onClick={() => setActiveTab('api')}
                className={cn(
                  "flex items-center gap-2 px-6 py-3 text-sm rounded-t-md transition-colors",
                  activeTab === 'api' 
                    ? "bg-orange-500/20 text-orange-500" 
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                <Key size={16} />
                <span className="uppercase tracking-wide text-xs font-medium">API</span>
              </button>

              <button
                type="button"
                onClick={() => setActiveTab('danger')}
                className={cn(
                  "flex items-center gap-2 px-6 py-3 text-sm rounded-t-md transition-colors",
                  activeTab === 'danger' 
                    ? "bg-red-500/20 text-red-500" 
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                <AlertTriangle size={16} />
                <span className="uppercase tracking-wide text-xs font-medium">Danger Zone</span>
              </button>
              
              {/* Full width line under all tabs */}
              <div className={cn(
                "absolute bottom-0 left-0 right-0 h-0.5 transition-colors",
                activeTab === 'details' && "bg-blue-500",
                activeTab === 'security' && "bg-green-500",
                activeTab === 'activity' && "bg-purple-500",
                activeTab === 'api' && "bg-orange-500",
                activeTab === 'danger' && "bg-red-500"
              )} />
            </div>

            {/* Tab Content */}
            <div className="border border-t-0 rounded-b-lg bg-card">
            {activeTab === 'details' && (
                    <div className="p-lg space-y-lg">
                      {/* Account Information Card */}
                      <Card>
                        <CardHeader>
                          <CardTitle>Account Information</CardTitle>
                        </CardHeader>
                        <CardContent>
                          <div className="grid grid-cols-1 md:grid-cols-2 gap-md">
                            <div className="space-y-xs">
                              <Label htmlFor="email">Email</Label>
                              <Controller
                                name="email"
                                control={control}
                                render={({ field }) => (
                                  <Input
                                    {...field}
                                    id="email"
                                    type="email"
                                    disabled
                                    className="bg-muted w-64"
                                  />
                                )}
                              />
                            </div>

                            <div className="space-y-xs">
                              <Label htmlFor="displayName">Display Name</Label>
                              <Controller
                                name="displayName"
                                control={control}
                                rules={{ required: 'Display name is required' }}
                                render={({ field }) => (
                                  <Input
                                    {...field}
                                    id="displayName"
                                    placeholder="Enter display name"
                                    className={cn("w-64", errors.displayName && "border-destructive")}
                                  />
                                )}
                              />
                              {errors.displayName && (
                                <p className="text-xs text-destructive">{errors.displayName.message}</p>
                              )}
                            </div>

                            <div className="space-y-xs">
                              <Label htmlFor="status">Status</Label>
                              <Controller
                                name="status"
                                control={control}
                                render={({ field }) => (
                                  <Select
                                    value={field.value}
                                    onValueChange={field.onChange}
                                  >
                                    <SelectTrigger id="status" className="w-40">
                                      <SelectValue />
                                    </SelectTrigger>
                                    <SelectContent>
                                      <SelectItem value="active">Active</SelectItem>
                                      <SelectItem value="inactive">Inactive</SelectItem>
                                    </SelectContent>
                                  </Select>
                                )}
                              />
                            </div>

                            <div className="space-y-xs">
                              <Label htmlFor="authMethod">Auth Method</Label>
                              <Controller
                                name="authMethod"
                                control={control}
                                render={({ field }) => (
                                  <Select
                                    value={field.value}
                                    onValueChange={field.onChange}
                                  >
                                    <SelectTrigger id="authMethod" className="w-40">
                                      <SelectValue />
                                    </SelectTrigger>
                                    <SelectContent>
                                      <SelectItem value="password">Password</SelectItem>
                                      <SelectItem value="otp">OTP</SelectItem>
                                      <SelectItem value="pin">PIN</SelectItem>
                                    </SelectContent>
                                  </Select>
                                )}
                              />
                            </div>
                          </div>
                        </CardContent>
                      </Card>

                      {/* Profile Settings - Split into Avatar and Preferences */}
                      <div className="grid grid-cols-1 lg:grid-cols-2 gap-lg">
                        {/* Avatar Card */}
                        <Card>
                          <CardHeader>
                            <CardTitle>Avatar</CardTitle>
                          </CardHeader>
                          <CardContent>
                            <div className="flex flex-col items-center space-y-md">
                              {/* Avatar Preview */}
                              <div className="relative">
                                {avatarPreview ? (
                                  <img
                                    src={avatarPreview}
                                    alt="Avatar"
                                    className="w-32 h-32 rounded-full border-2 border-border object-cover"
                                  />
                                ) : (
                                  <div className="w-32 h-32 rounded-full border-2 border-border bg-muted flex items-center justify-center">
                                    {displayName ? (
                                      <span className="text-2xl font-medium text-muted-foreground">
                                        {getInitials(displayName)}
                                      </span>
                                    ) : (
                                      <User className="w-12 h-12 text-muted-foreground" />
                                    )}
                                  </div>
                                )}
                              </div>

                              {/* Upload/Remove Buttons */}
                              <div className="flex gap-sm">
                                <Button
                                  type="button"
                                  variant="outline"
                                  size="sm"
                                  onClick={() => fileInputRef.current?.click()}
                                  className="gap-xs"
                                >
                                  <Upload className="h-4 w-4" />
                                  Upload Avatar
                                </Button>
                                {avatarPreview && (
                                  <Button
                                    type="button"
                                    variant="ghost"
                                    size="sm"
                                    onClick={handleRemoveAvatar}
                                  >
                                    Remove
                                  </Button>
                                )}
                              </div>

                              {/* Hidden File Input */}
                              <input
                                ref={fileInputRef}
                                type="file"
                                accept="image/*"
                                onChange={handleAvatarUpload}
                                className="hidden"
                              />

                              <p className="text-xs text-muted-foreground text-center">
                                Recommended: Square image, at least 256x256px
                              </p>
                            </div>
                          </CardContent>
                        </Card>

                        {/* Preferences Card */}
                        <Card>
                          <CardHeader>
                            <CardTitle>Preferences</CardTitle>
                          </CardHeader>
                          <CardContent>
                            <div className="space-y-md">
                              {/* Theme Selection */}
                              <div className="space-y-xs">
                                <Label htmlFor="theme">Theme</Label>
                                <Controller
                                  name="preferences.theme"
                                  control={control}
                                  render={({ field }) => (
                                    <Select
                                      value={field.value}
                                      onValueChange={field.onChange}
                                    >
                                      <SelectTrigger id="theme" className="w-32">
                                        <SelectValue />
                                      </SelectTrigger>
                                      <SelectContent>
                                        <SelectItem value="light">Light</SelectItem>
                                        <SelectItem value="dark">Dark</SelectItem>
                                        <SelectItem value="system">System</SelectItem>
                                      </SelectContent>
                                    </Select>
                                  )}
                                />
                              </div>

                              {/* Notifications Toggle */}
                              <div className="flex items-center justify-between space-x-sm">
                                <div className="space-y-0.5">
                                  <Label htmlFor="notifications" className="text-base font-normal">
                                    Notifications
                                  </Label>
                                  <p className="text-xs text-muted-foreground">
                                    Receive email notifications about account activity
                                  </p>
                                </div>
                                <Controller
                                  name="preferences.notifications"
                                  control={control}
                                  render={({ field }) => (
                                    <Switch
                                      id="notifications"
                                      checked={field.value}
                                      onCheckedChange={field.onChange}
                                    />
                                  )}
                                />
                              </div>

                              <Separator />

                              {/* Avatar Visibility Toggle */}
                              <div className="flex items-center justify-between space-x-sm">
                                <div className="space-y-0.5">
                                  <Label htmlFor="avatarVisibility" className="text-base font-normal">
                                    Avatar Visibility
                                  </Label>
                                  <p className="text-xs text-muted-foreground">
                                    Show your avatar to other users
                                  </p>
                                </div>
                                <Controller
                                  name="preferences.avatarVisibility"
                                  control={control}
                                  render={({ field }) => (
                                    <Switch
                                      id="avatarVisibility"
                                      checked={field.value}
                                      onCheckedChange={field.onChange}
                                    />
                                  )}
                                />
                              </div>

                              <Separator />

                              <p className="text-sm text-muted-foreground italic">
                                More preferences coming soon...
                              </p>
                            </div>
                          </CardContent>
                        </Card>
                      </div>

                      {/* Metadata Section */}
                      <div className="bg-muted/30 rounded-lg p-lg">
                        <h3 className="text-base font-medium mb-md">Metadata</h3>
                        <div className="space-y-sm text-sm">
                          <div className="flex justify-between items-center py-xs">
                            <span className="text-muted-foreground">Entity ID</span>
                            <span className="font-mono text-xs">{account?.ownerEntityId}</span>
                          </div>
                          <Separator className="my-xs" />
                          <div className="flex justify-between items-center py-xs">
                            <span className="text-muted-foreground">Created</span>
                            <span className="text-xs">{account ? formatDate(account.createdAt) : '-'}</span>
                          </div>
                          <Separator className="my-xs" />
                          <div className="flex justify-between items-center py-xs">
                            <span className="text-muted-foreground">Last Modified</span>
                            <span className="text-xs">{account ? formatDate(account.lastUpdated) : '-'}</span>
                          </div>
                        </div>
                      </div>
                    </div>
            )}

            {activeTab === 'security' && (
              <div className="p-lg">
                <Card>
                  <CardHeader>
                    <CardTitle>Security Settings</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-sm text-muted-foreground">
                      Security settings will be implemented here.
                    </p>
                  </CardContent>
                </Card>
              </div>
            )}

            {activeTab === 'activity' && (
              <div className="p-lg">
                <Card>
                  <CardHeader>
                    <CardTitle>Activity Log</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-sm text-muted-foreground">
                      User activity log will be displayed here.
                    </p>
                  </CardContent>
                </Card>
              </div>
            )}

            {activeTab === 'api' && (
              <div className="p-lg">
                <Card>
                  <CardHeader>
                    <CardTitle>API Access</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-sm text-muted-foreground">
                      API keys and access tokens will be managed here.
                    </p>
                  </CardContent>
                </Card>
              </div>
            )}

            {activeTab === 'danger' && (
              <div className="p-lg">
                <Card className="border-destructive">
                  <CardHeader>
                    <CardTitle className="text-destructive">Danger Zone</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-sm text-muted-foreground mb-md">
                      Irreversible and destructive actions.
                    </p>
                    <Button variant="destructive" size="sm">
                      Delete Account
                    </Button>
                  </CardContent>
                </Card>
              </div>
            )}
            </div>
          </div>
        </div>
      </form>
    </DashboardLayout>
  );
}