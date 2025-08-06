import { Building, Check, ChevronsUpDown, Plus } from 'lucide-react'
import { LucideIcon as Icon } from '@/components/ui/icon'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'

// Hardcoded data for now
const HARDCODED_DATA = {
  organization: {
    name: 'Risksec',
    slug: 'risksec'
  }
}

export const OrganizationDropdown = () => {
  const [open, setOpen] = useState(false)
  
  return (
    <>
      <a href={`/org/${HARDCODED_DATA.organization.slug}`} className="flex items-center gap-2 flex-shrink-0 text-sm">
        <Icon icon={Building} size="sm" className="text-foreground-lighter" strokeWidth={1.5} />
        <span className="text-foreground">{HARDCODED_DATA.organization.name}</span>
      </a>
      <DropdownMenu open={open} onOpenChange={setOpen} modal={false}>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className="px-1.5 py-4 ml-1"
          >
            <Icon icon={ChevronsUpDown} size="sm" strokeWidth={1.5} />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-[320px] p-0">
          <div className="flex flex-col">
            <div className="px-2 py-1.5">
              <input
                type="text"
                placeholder="Find organization..."
                className="w-full px-2 py-1 text-sm bg-background border rounded focus:outline-none focus:ring-1 focus:ring-ring"
              />
            </div>
            <DropdownMenuSeparator className="my-0" />
            <div className="py-1">
              <DropdownMenuItem className="cursor-pointer px-2 py-1.5">
                <div className="flex items-center justify-between w-full">
                  <span className="text-sm">{HARDCODED_DATA.organization.name}</span>
                  <Icon icon={Check} size="sm" />
                </div>
              </DropdownMenuItem>
            </div>
            <DropdownMenuSeparator className="my-0" />
            <div className="py-1">
              <DropdownMenuItem className="cursor-pointer px-2 py-1.5">
                <span className="text-sm">All Organizations</span>
              </DropdownMenuItem>
            </div>
            <DropdownMenuSeparator className="my-0" />
            <div className="py-1">
              <DropdownMenuItem className="cursor-pointer px-2 py-1.5">
                <Icon icon={Plus} size="sm" className="mr-2" strokeWidth={1.5} />
                <span className="text-sm">New organization</span>
              </DropdownMenuItem>
            </div>
          </div>
        </DropdownMenuContent>
      </DropdownMenu>
    </>
  )
}