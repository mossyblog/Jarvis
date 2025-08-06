import { Button } from '../ui/button';
import { MoreHorizontal, Copy, Key, Type, Hash, Calendar } from 'lucide-react';
import { SelectionCheckbox } from '../ui/selection-checkbox';
import { BulkActionsToolbar } from '../ui/bulk-actions-toolbar';
import { useSelectionState, type BulkAction } from '../../hooks/useSelectionState';
import { cn } from '../../lib/utils';

interface TableDataGridRow {
  id: string;
  email: string;
  json: string;
  dtlastupdated: string;
}

interface TableDataGridProps {
  data: TableDataGridRow[];
  /** Called when bulk actions are performed */
  onBulkAction?: (actionId: string, selectedItems: TableDataGridRow[]) => void;
  /** Enable selection functionality */
  enableSelection?: boolean;
  /** Custom bulk actions */
  customBulkActions?: BulkAction<TableDataGridRow>[];
}

export function TableDataGrid({ 
  data, 
  onBulkAction,
  enableSelection = true,
  customBulkActions = []
}: TableDataGridProps) {
  // Default bulk actions for table data
  const defaultBulkActions: BulkAction<TableDataGridRow>[] = [
    {
      id: 'delete',
      label: 'Delete',
      icon: 'trash',
      action: (items) => {
        console.log('Delete items:', items.map(i => i.id));
      },
      destructive: true,
      tooltip: 'Delete selected items',
      shortcut: 'Del'
    },
    {
      id: 'copy',
      label: 'Copy IDs',
      icon: 'copy',
      action: (items) => {
        const ids = items.map(i => i.id).join(', ');
        navigator.clipboard.writeText(ids);
      },
      tooltip: 'Copy selected item IDs to clipboard',
      shortcut: 'Ctrl+C'
    },
    {
      id: 'export',
      label: 'Export',
      icon: 'share',
      action: (items) => {
        console.log('Export items:', items);
      },
      tooltip: 'Export selected items as JSON'
    }
  ];

  const bulkActions = [...defaultBulkActions, ...customBulkActions];

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
  } = useSelectionState(data, {
    mode: 'multiple',
    getId: (item) => item.id,
    enableKeyboard: true,
    enableRangeSelection: true,
    enableSelectAll: true,
    bulkActions,
    onBulkAction: (action, items) => {
      onBulkAction?.(action.id, items);
    }
  });

  const handleBulkAction = (actionId: string) => {
    executeBulkAction(actionId);
  };
  return (
    <div className="flex-1 overflow-auto bg-[#0a0a0a] relative" data-selectable-container>
      <table className="w-full relative">
        <thead className="sticky top-0 bg-[#141414] border-b border-[#1e1e1e] z-10">
          <tr>
            <th className="text-left px-4 py-2 w-xl">
              {enableSelection && (
                <SelectionCheckbox
                  size="sm"
                  {...getSelectAllProps()}
                  className="border-[#262626] bg-[#0a0a0a]"
                />
              )}
            </th>
            <th className="text-left px-4 py-2">
              <div className="flex items-center gap-1">
                <Key className="h-xs w-xs text-[#3fcf8e]" />
                <span className="font-normal text-[#888888] text-xs">id</span>
                <span className="text-[#555555] text-[11px]">uuid</span>
              </div>
            </th>
            <th className="text-left px-4 py-2">
              <div className="flex items-center gap-1">
                <Type className="h-xs w-xs text-[#666666]" />
                <span className="font-normal text-[#888888] text-xs">email</span>
                <span className="text-[#555555] text-[11px]">text</span>
              </div>
            </th>
            <th className="text-left px-4 py-2">
              <div className="flex items-center gap-1">
                <Hash className="h-xs w-xs text-[#666666]" />
                <span className="font-normal text-[#888888] text-xs">json</span>
                <span className="text-[#555555] text-[11px]">jsonb</span>
              </div>
            </th>
            <th className="text-left px-4 py-2">
              <div className="flex items-center gap-1">
                <Calendar className="h-xs w-xs text-[#666666]" />
                <span className="font-normal text-[#888888] text-xs">dtlastupdated</span>
                <span className="text-[#555555] text-[11px]">timestamptz</span>
              </div>
            </th>
            <th className="w-xl"></th>
          </tr>
        </thead>
        <tbody>
          {data.map((row, index) => {
            const rowProps = enableSelection ? getTableRowProps(row) : {};
            const selected = enableSelection ? isSelected(row) : false;
            
            return (
            <tr 
              key={row.id} 
              className={cn(
                'border-b border-[#1e1e1e] group hover:bg-[#141414] transition-colors',
                selected && 'bg-[#1a1a1a] border-[#3fcf8e]/20',
                'cursor-pointer'
              )}
              {...rowProps}
            >
              <td className="px-4 py-2">
                {enableSelection && (
                  <SelectionCheckbox
                    size="sm"
                    {...getItemProps(row)}
                    className="border-[#262626] bg-[#0a0a0a]"
                  />
                )}
              </td>
              <td className="px-4 py-2">
                <div className="flex items-center gap-1.5 group">
                  <span className="font-mono text-xs text-[#666666] truncate max-w-[200px]">{row.id}</span>
                  <button className="opacity-0 group-hover:opacity-100 transition-opacity">
                    <Copy className="h-xs w-xs text-[#666666] hover:text-[#888888]" />
                  </button>
                </div>
              </td>
              <td className="px-4 py-2 text-sm text-[#fafafa]">{row.email}</td>
              <td className="px-4 py-2">
                <span className="font-mono text-xs text-[#666666] truncate block max-w-[200px]">{row.json}</span>
              </td>
              <td className="px-4 py-2 text-sm text-[#fafafa]">{row.dtlastupdated}</td>
              <td className="px-4 py-2">
                <Button variant="ghost" size="sm" className="h-sm w-sm p-0 opacity-0 group-hover:opacity-100 transition-opacity hover:bg-[#262626]">
                  <MoreHorizontal className="h-xs w-xs text-[#666666]" />
                </Button>
              </td>
            </tr>
            );
          })}
        </tbody>
      </table>
      
      {/* Bulk Actions Toolbar */}
      {enableSelection && (
        <BulkActionsToolbar
          selectedCount={selectedItems.length}
          actions={bulkToolbarActions}
          visible={showBulkToolbar}
          onAction={handleBulkAction}
          onDismiss={deselectAll}
          position="bottom"
        />
      )}
    </div>
  );
}