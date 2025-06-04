import { useState, useRef, useEffect } from 'react';
import { Search, ChevronDown, FolderOpen, FileText } from 'lucide-react';
import { Button } from '../ui/button';

interface TablesListProps {
  selectedSchema: string;
  selectedTable: string;
  schemas: { [key: string]: string[] };
  onTableSelect: (table: string) => void;
}

function PopoverSchemaSelector({ schemas, selectedSchema, onSelect }: { schemas: { [key: string]: string[] }, selectedSchema: string, onSelect: (schema: string) => void }) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const schemaList = Object.keys(schemas);
  // Close popover on outside click
  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    }
    if (open) document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, [open]);

  return (
    <div ref={ref} className="relative w-full">
      <button
        type="button"
        className="w-full flex items-center bg-[hsl(0,0%,9%)] border border-[hsl(0,0%,18%)] h-8 px-3 py-0 font-['Inter',sans-serif] rounded-[4px] focus:outline-none focus:ring-2 focus:ring-[#3fcf8e] focus:ring-offset-0 antialiased"
        onClick={() => setOpen((v) => !v)}
      >
        <span className="text-xs text-[#9ca3af] mr-2 font-['Inter',sans-serif] antialiased">schema</span>
        <span className="text-[14px] text-[#e5e7eb] font-['Inter',sans-serif] antialiased">{selectedSchema}</span>
        <ChevronDown className={`h-4 w-4 ml-auto transition-transform ${open ? 'rotate-180' : ''} opacity-50`} />
      </button>
      {open && (
        <div className="absolute left-0 z-10 mt-1 w-[240px] bg-[hsl(0,0%,12.9%)] border border-[hsl(0,0%,18%)] rounded-[4px] shadow-lg py-1 max-h-56 overflow-y-auto animate-fadeIn">
          {schemaList.map((schema) => (
            <button
              key={schema}
              className={`w-full text-left h-8 px-3 py-0 text-[14px] font-normal font-['Inter',sans-serif] antialiased flex items-center gap-2 hover:bg-[hsl(0,0%,19.2%)] ${schema === selectedSchema ? 'text-[#e5e7eb] bg-[hsl(0,0%,18%)]' : 'text-[#e5e7eb]'}`}
              onClick={() => { onSelect(schema); setOpen(false); }}
            >
              {schema}
              {schema === selectedSchema && <ChevronDown className="h-4 w-4 ml-auto opacity-50" />}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

export function TablesList({ selectedSchema, selectedTable, schemas, onTableSelect }: TablesListProps) {
  // Some tables have special coloring
  const specialTables = ['audit_event', 'position_component', 'test_component', 'velocity_component'];
  
  return (
    <div className="w-[280px] border-r border-[#1e1e1e] bg-[#141414] flex flex-col h-full">
      <div className="flex-1 overflow-y-auto">
        {/* Title */}
        <div className="px-6 py-4 border-b border-[#232323] bg-[#141414]">
  <span className="text-[17px] font-bold text-[#fff]">Table Editor</span>
</div>
        
        {/* Schema Selector */}
        <div className="px-3 pt-3 pb-2 relative">
          {/** Popover Schema Selector */}
          <PopoverSchemaSelector
            schemas={schemas}
            selectedSchema={selectedSchema}
            onSelect={onTableSelect}
          />
        </div>

        {/* New Table Button */}
        <div className="px-3 pb-2">
          <Button variant="outline" className="w-full bg-transparent border-[#262626] hover:bg-[#1a1a1a] text-[#888888] h-8 font-normal">
            <span className="text-sm">New table</span>
          </Button>
        </div>

        {/* Search Tables */}
        <div className="px-3 pb-2">
          <div className="relative">
            <Search className="absolute left-2.5 top-1/2 transform -translate-y-1/2 h-3.5 w-3.5 text-[#666666]" />
            <input
              type="text"
              placeholder="Search tables..."
              className="w-full pl-8 pr-3 py-1.5 text-sm border border-[#262626] rounded-md bg-[#0a0a0a] text-[#fafafa] placeholder:text-[#666666] focus:outline-none focus:ring-1 focus:ring-[#3fcf8e] focus:border-[#3fcf8e]"
            />
          </div>
        </div>

        {/* Tables List */}
        <div className="">
          {schemas[selectedSchema]?.map((table: string) => {
            const isSpecial = specialTables.includes(table);
            return (
              <div key={table} className="relative">
                {selectedTable === table && (
                  <div className="absolute left-0 top-0 bottom-0 w-[2px] bg-[#3fcf8e]"></div>
                )}
                <button
                  onClick={() => onTableSelect(table)}
                  className={`w-full flex items-center gap-2 px-3 py-1.5 transition-colors ${
                    selectedTable === table
                      ? 'bg-[#1a1a1a] text-[#fafafa]'
                      : 'text-[#888888] hover:text-[#fafafa] hover:bg-[#0a0a0a]'
                  }`}
                >
                  {isSpecial ? (
                    <FolderOpen className="h-4 w-4 flex-shrink-0 text-[#fb923c]" />
                  ) : (
                    <FileText className="h-4 w-4 flex-shrink-0 text-[#666666]" />
                  )}
                  <span className="text-sm font-normal truncate">{table}</span>
                </button>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}