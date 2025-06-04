import { Button } from '../ui/button';
import { MoreHorizontal, Copy, Key, Type, Hash, Calendar } from 'lucide-react';

interface TableDataGridProps {
  data: Array<{
    id: string;
    email: string;
    json: string;
    dtlastupdated: string;
  }>;
}

export function TableDataGrid({ data }: TableDataGridProps) {
  return (
    <div className="flex-1 overflow-auto bg-[#0a0a0a] relative">
      <table className="w-full relative">
        <thead className="sticky top-0 bg-[#141414] border-b border-[#1e1e1e] z-10">
          <tr>
            <th className="text-left px-4 py-2 w-10">
              <input type="checkbox" className="rounded border-[#262626] bg-[#0a0a0a] h-3.5 w-3.5" />
            </th>
            <th className="text-left px-4 py-2">
              <div className="flex items-center gap-1">
                <Key className="h-3 w-3 text-[#3fcf8e]" />
                <span className="font-normal text-[#888888] text-xs">id</span>
                <span className="text-[#555555] text-[11px]">uuid</span>
              </div>
            </th>
            <th className="text-left px-4 py-2">
              <div className="flex items-center gap-1">
                <Type className="h-3 w-3 text-[#666666]" />
                <span className="font-normal text-[#888888] text-xs">email</span>
                <span className="text-[#555555] text-[11px]">text</span>
              </div>
            </th>
            <th className="text-left px-4 py-2">
              <div className="flex items-center gap-1">
                <Hash className="h-3 w-3 text-[#666666]" />
                <span className="font-normal text-[#888888] text-xs">json</span>
                <span className="text-[#555555] text-[11px]">jsonb</span>
              </div>
            </th>
            <th className="text-left px-4 py-2">
              <div className="flex items-center gap-1">
                <Calendar className="h-3 w-3 text-[#666666]" />
                <span className="font-normal text-[#888888] text-xs">dtlastupdated</span>
                <span className="text-[#555555] text-[11px]">timestamptz</span>
              </div>
            </th>
            <th className="w-10"></th>
          </tr>
        </thead>
        <tbody>
          {data.map((row, index) => (
            <tr key={index} className={`border-b border-[#1e1e1e] group ${
              index % 2 === 0 ? 'bg-[#0a0a0a]' : 'bg-[#0a0a0a]'
            } hover:bg-[#141414]`}>
              <td className="px-4 py-2">
                <input type="checkbox" className="rounded border-[#262626] bg-[#0a0a0a] h-3.5 w-3.5" />
              </td>
              <td className="px-4 py-2">
                <div className="flex items-center gap-1.5 group">
                  <span className="font-mono text-xs text-[#666666] truncate max-w-[200px]">{row.id}</span>
                  <button className="opacity-0 group-hover:opacity-100 transition-opacity">
                    <Copy className="h-3 w-3 text-[#666666] hover:text-[#888888]" />
                  </button>
                </div>
              </td>
              <td className="px-4 py-2 text-sm text-[#fafafa]">{row.email}</td>
              <td className="px-4 py-2">
                <span className="font-mono text-xs text-[#666666] truncate block max-w-[200px]">{row.json}</span>
              </td>
              <td className="px-4 py-2 text-sm text-[#fafafa]">{row.dtlastupdated}</td>
              <td className="px-4 py-2">
                <Button variant="ghost" size="sm" className="h-7 w-7 p-0 opacity-0 group-hover:opacity-100 transition-opacity hover:bg-[#262626]">
                  <MoreHorizontal className="h-3.5 w-3.5 text-[#666666]" />
                </Button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}