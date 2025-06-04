import { Button } from '../ui/button';

interface TableStatusBarProps {
  currentPage: number;
  totalPages: number;
  totalRows: number;
  recordCount: number;
}

export function TableStatusBar({ currentPage, totalPages, totalRows, recordCount }: TableStatusBarProps) {
  return (
    <div className="border-t border-[#1e1e1e] bg-[#141414]">
      <div className="flex items-center justify-between px-4 py-2">
        <div className="flex items-center gap-4 text-xs text-[#666666]">
          <div className="flex items-center gap-2">
            <span>Page</span>
            <span className="text-[#fafafa]">{currentPage}</span>
            <span>of {totalPages}</span>
          </div>
          <span>{totalRows} rows</span>
          <span className="text-[#fafafa]">{recordCount} record{recordCount !== 1 ? 's' : ''}</span>
        </div>
        <div className="flex items-center gap-1">
          <Button variant="ghost" size="sm" className="h-7 px-3 text-xs text-[#888888] hover:text-[#fafafa] hover:bg-[#262626] font-normal">
            Refresh
          </Button>
          <div className="flex">
            <Button variant="ghost" size="sm" className="h-7 px-3 text-xs rounded-r-none border-r border-[#262626] text-[#fafafa] bg-[#1a1a1a] font-normal">
              Data
            </Button>
            <Button variant="ghost" size="sm" className="h-7 px-3 text-xs rounded-l-none text-[#888888] hover:text-[#fafafa] hover:bg-[#262626] font-normal">
              Definition
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}