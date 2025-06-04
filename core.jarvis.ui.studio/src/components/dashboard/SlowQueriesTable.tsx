import { ExternalLink } from 'lucide-react';
import { cn } from '../../lib/utils';

interface SlowQuery {
  id: string;
  query: string;
  avgTime: number;
  calls: number;
}

const mockQueries: SlowQuery[] = [
  {
    id: '1',
    query: 'with records as ( select c.oid::int8 as "id", case c...',
    avgTime: 1.19,
    calls: 1
  },
  {
    id: '2',
    query: 'SELECT name FROM pg_timezone_names',
    avgTime: 0.09,
    calls: 69
  },
  {
    id: '3',
    query: 'with records as ( select c.oid::int8 as "id", case c...',
    avgTime: 0.28,
    calls: 1
  },
  {
    id: '4',
    query: 'with records as ( select c.oid::int8 as "id", case c...',
    avgTime: 0.25,
    calls: 1
  },
  {
    id: '5',
    query: 'with records as ( select c.oid::int8 as "id", case c...',
    avgTime: 0.21,
    calls: 1
  }
];

export function SlowQueriesTable() {
  return (
    <div className="px-8 pb-12">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-medium uppercase tracking-wide">Slow Queries</h3>
          <button className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors">
            <ExternalLink size={14} />
          </button>
        </div>

        {/* Table */}
        <div className="border border-default rounded-lg overflow-hidden">
          <table className="w-full">
            <thead>
              <tr className="border-b border-default bg-muted/50">
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">
                  Query
                </th>
                <th className="text-right px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">
                  Avg time
                </th>
                <th className="text-right px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">
                  Calls
                </th>
              </tr>
            </thead>
            <tbody>
              {mockQueries.map((query, index) => (
                <tr
                  key={query.id}
                  className={cn(
                    "bg-card",
                    index !== mockQueries.length - 1 && "border-b border-default"
                  )}
                >
                  <td className="px-4 py-3">
                    <code className="text-sm font-mono text-muted-foreground">
                      {query.query}
                    </code>
                  </td>
                  <td className="text-right px-4 py-3 text-sm">
                    {query.avgTime}s
                  </td>
                  <td className="text-right px-4 py-3 text-sm">
                    {query.calls}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}