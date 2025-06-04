import { Box, Check, ChevronsUpDown, Plus } from 'lucide-react'
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
  project: {
    name: 'Jarvis Core',
    ref: 'jarvis-core'
  }
}

export const ProjectDropdown = () => {
  const [open, setOpen] = useState(false)
  
  return (
    <>
      <a href={`/project/${HARDCODED_DATA.project.ref}`} className="flex items-center gap-2 flex-shrink-0 text-sm">
        <Box size={14} strokeWidth={1.5} className="text-foreground-lighter" />
        <span className="text-foreground">{HARDCODED_DATA.project.name}</span>
      </a>
      <DropdownMenu open={open} onOpenChange={setOpen} modal={false}>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className="px-1.5 py-4 [&_svg]:w-5 [&_svg]:h-5 ml-1"
          >
            <ChevronsUpDown strokeWidth={1.5} />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-[320px] p-0">
          <div className="flex flex-col">
            <div className="px-2 py-1.5">
              <input
                type="text"
                placeholder="Find project..."
                className="w-full px-2 py-1 text-sm bg-background border rounded focus:outline-none focus:ring-1 focus:ring-ring"
              />
            </div>
            <DropdownMenuSeparator className="my-0" />
            <div className="py-1 max-h-[210px] overflow-y-auto">
              <DropdownMenuItem className="cursor-pointer px-2 py-1.5">
                <div className="flex items-center justify-between w-full">
                  <span className="text-sm">{HARDCODED_DATA.project.name}</span>
                  <Check size={14} />
                </div>
              </DropdownMenuItem>
            </div>
            <DropdownMenuSeparator className="my-0" />
            <div className="py-1">
              <DropdownMenuItem className="cursor-pointer px-2 py-1.5">
                <Plus size={14} strokeWidth={1.5} className="mr-2" />
                <span className="text-sm">New project</span>
              </DropdownMenuItem>
            </div>
          </div>
        </DropdownMenuContent>
      </DropdownMenu>
    </>
  )
}