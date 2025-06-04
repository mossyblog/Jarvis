import { AnimatePresence, motion } from 'framer-motion'
import { useState } from 'react'
import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { 
  Home, 
  HelpCircle, 
  MessageSquare,
  Bell,
  User,
  LogOut,
  Settings,
  Plug,
  FileEdit,
  Sparkles,
  GitBranch
} from 'lucide-react'
import { OrganizationDropdown } from './dropdowns/OrganizationDropdown'
import { ProjectDropdown } from './dropdowns/ProjectDropdown'
import { BranchDropdown } from './dropdowns/BranchDropdown'

// Component implementations
const HomeIcon = () => (
  <a href="/" className="flex items-center">
    <Home size={14} strokeWidth={1.5} className="text-foreground-lighter" />
  </a>
)

const Connect = () => (
  <Button variant="outline" size="sm" className="h-auto px-2 py-1">
    <Plug className="mr-2 h-4 w-4" />
    Connect
  </Button>
)

const EnableBranchingButton = () => (
  <Button variant="outline" size="sm" className="h-auto px-2 py-1">
    <GitBranch className="mr-2 h-4 w-4" />
    Enable Branching
  </Button>
)

const BreadcrumbsView = ({ defaultValue }: { defaultValue: any[] }) => {
  if (!defaultValue.length) return null
  
  return (
    <div className="flex items-center ml-4 text-sm text-muted-foreground">
      {defaultValue.map((crumb, index) => (
        <span key={index} className="flex items-center">
          {index > 0 && <span className="mx-2">/</span>}
          <span>{crumb.label || crumb}</span>
        </span>
      ))}
    </div>
  )
}

const FeedbackDropdown = () => (
  <DropdownMenu>
    <DropdownMenuTrigger asChild>
      <Button variant="ghost" size="icon">
        <MessageSquare className="h-4 w-4" />
      </Button>
    </DropdownMenuTrigger>
    <DropdownMenuContent align="end">
      <DropdownMenuLabel>Feedback</DropdownMenuLabel>
      <DropdownMenuSeparator />
      <DropdownMenuItem>Report an issue</DropdownMenuItem>
      <DropdownMenuItem>Share feedback</DropdownMenuItem>
    </DropdownMenuContent>
  </DropdownMenu>
)

const NotificationsPopoverV2 = () => (
  <DropdownMenu>
    <DropdownMenuTrigger asChild>
      <Button variant="ghost" size="icon" className="relative">
        <Bell className="h-4 w-4" />
        <span className="absolute top-0 right-0 h-2 w-2 bg-red-500 rounded-full" />
      </Button>
    </DropdownMenuTrigger>
    <DropdownMenuContent align="end" className="w-80">
      <DropdownMenuLabel>Notifications</DropdownMenuLabel>
      <DropdownMenuSeparator />
      <div className="p-4 text-sm text-muted-foreground">
        No new notifications
      </div>
    </DropdownMenuContent>
  </DropdownMenu>
)

const HelpPopover = () => (
  <DropdownMenu>
    <DropdownMenuTrigger asChild>
      <Button variant="ghost" size="icon">
        <HelpCircle className="h-4 w-4" />
      </Button>
    </DropdownMenuTrigger>
    <DropdownMenuContent align="end">
      <DropdownMenuLabel>Help & Support</DropdownMenuLabel>
      <DropdownMenuSeparator />
      <DropdownMenuItem>Documentation</DropdownMenuItem>
      <DropdownMenuItem>API Reference</DropdownMenuItem>
      <DropdownMenuItem>Contact Support</DropdownMenuItem>
    </DropdownMenuContent>
  </DropdownMenu>
)

const UserDropdown = () => (
  <DropdownMenu>
    <DropdownMenuTrigger asChild>
      <Button variant="ghost" size="icon">
        <User className="h-4 w-4" />
      </Button>
    </DropdownMenuTrigger>
    <DropdownMenuContent align="end">
      <DropdownMenuLabel>{HARDCODED_DATA.user.name}</DropdownMenuLabel>
      <DropdownMenuItem className="text-xs text-muted-foreground">
        {HARDCODED_DATA.user.email}
      </DropdownMenuItem>
      <DropdownMenuSeparator />
      <DropdownMenuItem>
        <Settings className="mr-2 h-4 w-4" />
        Settings
      </DropdownMenuItem>
      <DropdownMenuItem>
        <LogOut className="mr-2 h-4 w-4" />
        Sign out
      </DropdownMenuItem>
    </DropdownMenuContent>
  </DropdownMenu>
)

const InlineEditorButton = () => (
  <Button variant="ghost" size="icon">
    <FileEdit className="h-4 w-4" />
  </Button>
)

const AssistantButton = () => (
  <Button variant="ghost" size="icon">
    <Sparkles className="h-4 w-4" />
  </Button>
)

const LayoutHeaderDivider = ({ className, ...props }: React.HTMLProps<HTMLSpanElement>) => (
  <span className={cn('px-2', className)} {...props}>
    <svg
      viewBox="0 0 24 24"
      width="16"
      height="16"
      stroke="currentColor"
      strokeWidth="1"
      strokeLinecap="round"
      strokeLinejoin="round"
      fill="none"
      shapeRendering="geometricPrecision"
      className="text-muted-foreground/40"
    >
      <path d="M16 3.549L7.12 20.600" />
    </svg>
  </span>
)

interface LayoutHeaderProps {
  customHeaderComponents?: ReactNode
  breadcrumbs?: any[]
  headerTitle?: string
  showProductMenu?: boolean
}

// Hardcoded data for now
const HARDCODED_DATA = {
  organization: {
    name: 'Risksec',
    slug: 'risksec'
  },
  project: {
    name: 'Jarvis Core',
    ref: 'jarvis-core',
    is_branch_enabled: true
  },
  branch: {
    name: 'main',
    branches: ['main', 'develop', 'feature/ui-update']
  },
  user: {
    name: 'John Doe',
    email: 'john.doe@risksec.com',
    avatar: null
  }
}

const LayoutHeader = ({
  customHeaderComponents,
  breadcrumbs = [],
  headerTitle,
  showProductMenu,
}: LayoutHeaderProps) => {
  const [, setMobileMenuOpen] = useState(false)
  
  // Hardcoded values
  const projectRef = HARDCODED_DATA.project.ref
  const selectedProject = HARDCODED_DATA.project
  const isBranchingEnabled = selectedProject?.is_branch_enabled === true
  const exceedingLimits = false // Hardcoded for now
  const showOrgSelection = true

  return (
    <header className={cn('flex h-12 items-center flex-shrink-0 border-b bg-background')}>
      {showProductMenu && (
        <div className="flex items-center justify-center border-r flex-0 md:hidden h-full aspect-square">
          <button
            title="Menu dropdown button"
            className={cn(
              'group/view-toggle flex justify-center flex-col border-none space-x-0 items-center gap-1 !bg-transparent rounded-md w-12 h-12'
            )}
            onClick={() => setMobileMenuOpen(true)}
          >
            <div className="h-px w-4 transition-all ease-out bg-foreground-lighter group-hover/view-toggle:bg-foreground" />
            <div className="h-px w-3 transition-all ease-out bg-foreground-lighter group-hover/view-toggle:bg-foreground" />
          </button>
        </div>
      )}
      <div
        className={cn(
          'flex items-center justify-between h-full px-3 flex-1 overflow-x-auto'
        )}
      >
        <div className="flex items-center text-sm gap-1">
          <HomeIcon />
          <div className="flex items-center">
            {showOrgSelection && (
              <>
                <LayoutHeaderDivider className="hidden md:block" />
                <OrganizationDropdown />
              </>
            )}
            <AnimatePresence>
              {projectRef && (
                <motion.div
                  className="flex items-center"
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  exit={{ opacity: 0, x: -20 }}
                  transition={{
                    duration: 0.15,
                    ease: 'easeOut',
                  }}
                >
                  <LayoutHeaderDivider />
                  <ProjectDropdown />

                  {exceedingLimits && (
                    <div className="ml-2">
                      <span className="inline-flex items-center rounded-md bg-red-50 px-2 py-1 text-xs font-medium text-red-700 ring-1 ring-inset ring-red-600/10">
                        Exceeding usage limits
                      </span>
                    </div>
                  )}

                  {selectedProject && isBranchingEnabled && (
                    <>
                      <LayoutHeaderDivider />
                      <BranchDropdown />
                    </>
                  )}
                </motion.div>
              )}
            </AnimatePresence>

            <AnimatePresence>
              {headerTitle && (
                <motion.div
                  className="flex items-center"
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  exit={{ opacity: 0, x: -20 }}
                  transition={{
                    duration: 0.15,
                    ease: 'easeOut',
                  }}
                >
                  <LayoutHeaderDivider />
                  <span className="text-foreground">{headerTitle}</span>
                </motion.div>
              )}
            </AnimatePresence>
          </div>

          <AnimatePresence>
            {projectRef && (
              <motion.div
                className="ml-3 items-center gap-x-3 flex"
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -20 }}
                transition={{
                  duration: 0.15,
                  ease: 'easeOut',
                }}
              >
                <Connect />
                {!isBranchingEnabled && <EnableBranchingButton />}
              </motion.div>
            )}
          </AnimatePresence>
          <BreadcrumbsView defaultValue={breadcrumbs} />
        </div>
        <div className="flex items-center gap-x-2">
          {customHeaderComponents && customHeaderComponents}
          <>
            <FeedbackDropdown />
            <NotificationsPopoverV2 />
            <HelpPopover />
            <UserDropdown />
          </>
        </div>
      </div>

      <AnimatePresence initial={false}>
        {!!projectRef && (
          <motion.div
            className="border-l h-full flex items-center justify-center flex-shrink-0"
            initial={{ opacity: 0, x: 0, width: 0 }}
            animate={{ opacity: 1, x: 0, width: 'auto' }}
            exit={{ opacity: 0, x: 0, width: 0 }}
            transition={{ duration: 0.15, ease: 'easeOut' }}
          >
            <div className="border-r h-full flex items-center justify-center px-2">
              <InlineEditorButton />
            </div>
            <div className="px-2">
              <AssistantButton />
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </header>
  )
}

export default LayoutHeader
