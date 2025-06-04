import { Database, Shield, HardDrive, Radio } from 'lucide-react';

interface MetricCardProps {
  title: string;
  type: 'database' | 'auth' | 'storage' | 'realtime';
  requests: number;
  data: number[];
  timeLabels: string[];
}

const iconMap = {
  database: Database,
  auth: Shield,
  storage: HardDrive,
  realtime: Radio,
};

export function MetricCard({ title, type, requests, data, timeLabels }: MetricCardProps) {
  const Icon = iconMap[type];
  const maxValue = Math.max(...data, 1);
  
  return (
    <div className="bg-card border border-default rounded-lg p-6 flex flex-col gap-4">
      {/* Header */}
      <div className="flex items-center gap-3">
        <div className="p-2 bg-muted rounded">
          <Icon size={20} className="text-muted-foreground" />
        </div>
        <h3 className="text-base font-medium">{title}</h3>
      </div>

      {/* Metric */}
      <div>
        <div className="text-xs text-muted-foreground uppercase tracking-wide mb-1">
          {type === 'auth' ? 'Auth Requests' : `${type.charAt(0).toUpperCase() + type.slice(1)} Requests`}
        </div>
        <div className="text-2xl font-light">{requests.toLocaleString()}</div>
      </div>

      {/* Chart */}
      <div className="h-32 flex items-end gap-1">
        {data.map((value, index) => (
          <div
            key={index}
            className="flex-1 flex flex-col items-center gap-1"
          >
            <div
              className="w-full bg-brand rounded-t"
              style={{ 
                height: `${(value / maxValue) * 100}%`,
                minHeight: value > 0 ? '2px' : '0'
              }}
            />
          </div>
        ))}
      </div>

      {/* Time Labels */}
      <div className="flex justify-between text-xs text-muted-foreground">
        <span>{timeLabels[0]}</span>
        <span>{timeLabels[timeLabels.length - 1]}</span>
      </div>
    </div>
  );
}