import { useMemo, memo } from 'react';
import { Database, Shield, HardDrive, Radio, type LucideIcon } from 'lucide-react';

interface MetricCardProps {
  title: string;
  type: 'database' | 'auth' | 'storage' | 'realtime';
  requests: number;
  data: number[];
  timeLabels: string[];
  className?: string;
  onChartClick?: (index: number) => void;
  formatNumber?: (value: number) => string;
  loading?: boolean;
}

const iconMap: Record<MetricCardProps['type'], LucideIcon> = {
  database: Database,
  auth: Shield,
  storage: HardDrive,
  realtime: Radio,
} as const;

// Memoized chart bar component to prevent re-renders
const ChartBar = memo<{ value: number; maxValue: number; index: number; onClick?: (index: number) => void }>(
  ({ value, maxValue, index, onClick }) => {
    const height = useMemo(() => {
      const percentage = maxValue > 0 ? (value / maxValue) * 100 : 0;
      return `${percentage}%`;
    }, [value, maxValue]);

    return (
      <div
        className="flex-1 flex flex-col items-center gap-xs"
        onClick={onClick ? () => onClick(index) : undefined}
        style={{ cursor: onClick ? 'pointer' : 'default' }}
      >
        <div
          className="w-full bg-brand rounded-t transition-all duration-300 hover:opacity-80"
          style={{
            height,
            minHeight: value > 0 ? '2px' : '0'
          }}
        />
      </div>
    );
  }
);

ChartBar.displayName = 'ChartBar';

// Memoized loading skeleton
const LoadingSkeleton = memo(() => (
  <div className="animate-pulse">
    <div className="h-xs bg-muted rounded w-2xs/4 mb-2" />
    <div className="h-lg bg-muted rounded w-xsxs/2 mb-4" />
    <div className="h-232 bg-muted rounded" />
  </div>
));

LoadingSkeleton.displayName = 'LoadingSkeleton';

export const MetricCard = memo<MetricCardProps>(({
  title,
  type,
  requests,
  data,
  timeLabels,
  className = '',
  onChartClick,
  formatNumber = (value) => value.toLocaleString(),
  loading = false
}) => {
  const Icon = iconMap[type];
  
  // Memoize expensive calculations
  const maxValue = useMemo(() => Math.max(...data, 1), [data]);
  
  const metricLabel = useMemo(() => {
    if (type === 'auth') return 'Auth Requests';
    return `${type.charAt(0).toUpperCase() + type.slice(1)} Requests`;
  }, [type]);

  // Memoize time label display
  const displayTimeLabels = useMemo(() => ({
    start: timeLabels[0],
    end: timeLabels[timeLabels.length - 1]
  }), [timeLabels]);

  return (
    <div className={`bg-card border border-default rounded-lg p-sm flex flex-col gap-sm ${className}`}>
      {/* Header */}
      <div className="flex items-center gap-sm">
        <div className="p-xs bg-muted rounded">
          <Icon className="w-md h-md text-muted-foreground" />
        </div>
        <h3 className="text-base font-medium">{title}</h3>
      </div>

      {loading ? (
        <LoadingSkeleton />
      ) : (
        <>
          {/* Metric */}
          <div>
            <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">
              {metricLabel}
            </div>
            <div className="text-2xl font-light">{formatNumber(requests)}</div>
          </div>

          {/* Chart */}
          <div className="h-232 flex items-end gap-xs" role="img" aria-label={`Chart showing ${title} over time`}>
            {data.map((value, index) => (
              <ChartBar
                key={index}
                value={value}
                maxValue={maxValue}
                index={index}
                onClick={onChartClick}
              />
            ))}
          </div>

          {/* Time Labels */}
          <div className="flex justify-between text-xs text-muted-foreground" aria-label="Time range">
            <span>{displayTimeLabels.start}</span>
            <span>{displayTimeLabels.end}</span>
          </div>
        </>
      )}
    </div>
  );
});

MetricCard.displayName = 'MetricCard';

// Export a non-memoized version for cases where memo is not needed
export const MetricCardBase = ({
  title,
  type,
  requests,
  data,
  timeLabels,
  className = '',
  onChartClick,
  formatNumber = (value) => value.toLocaleString(),
  loading = false
}: MetricCardProps) => {
  const Icon = iconMap[type];
  const maxValue = Math.max(...data, 1);
  const metricLabel = type === 'auth' ? 'Auth Requests' : `${type.charAt(0).toUpperCase() + type.slice(1)} Requests`;

  return (
    <div className={`bg-card border border-default rounded-lg p-sm flex flex-col gap-sm ${className}`}>
      {/* Same JSX as memoized version */}
      <div className="flex items-center gap-sm">
        <div className="p-xs bg-muted rounded">
          <Icon className="w-md h-md text-muted-foreground" />
        </div>
        <h3 className="text-base font-medium">{title}</h3>
      </div>

      {loading ? (
        <LoadingSkeleton />
      ) : (
        <>
          <div>
            <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">
              {metricLabel}
            </div>
            <div className="text-2xl font-light">{formatNumber(requests)}</div>
          </div>

          <div className="h-232 flex items-end gap-xs" role="img" aria-label={`Chart showing ${title} over time`}>
            {data.map((value, index) => (
              <div
                key={index}
                className="flex-1 flex flex-col items-center gap-xs"
                onClick={onChartClick ? () => onChartClick(index) : undefined}
                style={{ cursor: onChartClick ? 'pointer' : 'default' }}
              >
                <div
                  className="w-full bg-brand rounded-t transition-all duration-300 hover:opacity-80"
                  style={{
                    height: `${(value / maxValue) * 100}%`,
                    minHeight: value > 0 ? '2px' : '0'
                  }}
                />
              </div>
            ))}
          </div>

          <div className="flex justify-between text-xs text-muted-foreground" aria-label="Time range">
            <span>{timeLabels[0]}</span>
            <span>{timeLabels[timeLabels.length - 1]}</span>
          </div>
        </>
      )}
    </div>
  );
};