import { Skeleton } from "./skeleton";
import { TableBody, TableCell, TableHead, TableHeader, TableRow } from "./table";

interface TableSkeletonProps {
  rows?: number;
  columns?: number;
}

export function TableSkeleton({ rows = 5, columns = 8 }: TableSkeletonProps) {
  return (
    <div className="w-full overflow-hidden">
      <table className="w-full caption-bottom text-sm">
        <TableHeader>
          <TableRow>
            {Array.from({ length: columns }).map((_, index) => (
              <TableHead key={index}>
                <Skeleton className="h-xs w-4xl" />
              </TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {Array.from({ length: rows }).map((_, rowIndex) => (
            <TableRow key={rowIndex}>
              {Array.from({ length: columns }).map((_, colIndex) => (
                <TableCell key={colIndex}>
                  <Skeleton 
                    className={`h-xs ${
                      colIndex === 0 ? 'w-3xl' : // Entity ID column
                      colIndex === 1 ? 'w-mdxl' : // Email column  
                      colIndex === 2 ? 'w-4xl' : // Profile Name column
                      colIndex === 3 ? 'w-3xl' : // Auth Method column
                      colIndex === 4 ? 'w-3xl' : // Status column
                      colIndex === 5 ? 'w-4xl' : // Created column
                      colIndex === 6 ? 'w-4xl' : // Last Updated column
                      'w-sm' // Actions column
                    }`} 
                  />
                </TableCell>
              ))}
            </TableRow>
          ))}
        </TableBody>
      </table>
    </div>
  );
}