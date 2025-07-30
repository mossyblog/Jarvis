import { Skeleton } from "./skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "./table";

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
                <Skeleton className="h-4 w-24" />
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
                    className={`h-4 ${
                      colIndex === 0 ? 'w-16' : // Entity ID column
                      colIndex === 1 ? 'w-32' : // Email column  
                      colIndex === 2 ? 'w-24' : // Profile Name column
                      colIndex === 3 ? 'w-16' : // Auth Method column
                      colIndex === 4 ? 'w-16' : // Status column
                      colIndex === 5 ? 'w-20' : // Created column
                      colIndex === 6 ? 'w-20' : // Last Updated column
                      'w-8' // Actions column
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