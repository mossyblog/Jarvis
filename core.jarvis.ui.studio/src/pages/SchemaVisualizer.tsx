// Schema Visualizer page: interactive ERD/diagram for database schemas, inspired by Supabase's visualizer.
import { DashboardLayout } from '../components/layout/DashboardLayout';
import { useEffect, useState, memo } from 'react';
import {
  ReactFlow,
  MiniMap,
  Controls,
  Background,
  useNodesState,
  useEdgesState,
  Handle,
  Position,
} from '@xyflow/react';
import type { Node, Edge } from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import { useNavigation } from '../hooks/useNavigation';

interface Column {
  name: string;
  type: string;
  pk?: boolean;
}

interface TableNodeData {
  label: string;
  columns: Column[];
}

interface SchemaNode {
  id: string;
  position: { x: number; y: number };
  data: TableNodeData;
  type?: string;
}

interface SchemaEdge {
  id: string;
  source: string;
  target: string;
  type?: string;
  animated?: boolean;
  style?: React.CSSProperties;
}

// Fake async fetch for schema data
function fetchFakeSchema() {
  return new Promise<{ nodes: SchemaNode[]; edges: SchemaEdge[] }>((resolve) => {
    setTimeout(() => {
      resolve({
        nodes: [
          {
            id: 'accounts',
            position: { x: 0, y: 0 },
            data: {
              label: 'accounts',
              columns: [
                { name: 'id', type: 'uuid', pk: true },
                { name: 'email', type: 'text' },
                { name: 'json', type: 'jsonb' },
                { name: 'dtlastupdated', type: 'timestamptz' },
              ],
            },
            type: 'tableNode',
          },
          {
            id: 'auth.users',
            position: { x: 350, y: 0 },
            data: {
              label: 'auth.users',
              columns: [
                { name: 'id', type: 'uuid', pk: true },
                { name: 'email', type: 'text' },
              ],
            },
            type: 'tableNode',
          },
        ],
        edges: [
          {
            id: 'e-accounts-auth.users',
            source: 'accounts',
            target: 'auth.users',
            animated: true,
          },
        ],
      });
    }, 900);
  });
}

// Custom Table Node for ERD
const TableNode = memo(({ data }: { data: TableNodeData }) => (
  <div className="rounded-lg bg-[#181818] border border-[#262626] shadow-md min-w-[180px] relative">
    {/* Handles for edge connections */}
    <Handle type="target" position={Position.Left} style={{ background: '#3fcf8e', width: 10, height: 10, borderRadius: 5, left: -5 }} />
    <Handle type="source" position={Position.Right} style={{ background: '#3fcf8e', width: 10, height: 10, borderRadius: 5, right: -5 }} />
    <div className="px-3 py-2 border-b border-[#262626] font-semibold text-[#fafafa] text-sm flex items-center gap-2">
      <span className="inline-block bg-[#232323] px-2 py-0.5 rounded text-xs text-[#3fcf8e] font-mono">{data.label}</span>
    </div>
    <div className="px-3 py-2">
      <ul className="space-y-1">
        {data.columns?.map((col: Column) => (
          <li key={col.name} className="flex items-center text-xs text-[#bdbdbd] font-mono">
            {col.pk && <span className="text-[#3fcf8e] mr-1">◆</span>}
            <span>{col.name}</span>
            <span className="ml-2 text-[#666]">{col.type}</span>
          </li>
        ))}
      </ul>
    </div>
  </div>
));

const SchemaVisualizer = () => {
  const [loading, setLoading] = useState(true);
  const [nodes, setNodes, onNodesChange] = useNodesState<Node>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);
  const { navigateToItem, navigation } = useNavigation();

  const nodeTypes = { tableNode: TableNode };

  const handleSidebarItemClick = (itemId: string) => {
    const item = navigation.find(i => i.id === itemId);
    if (item) {
      navigateToItem(item);
    }
  };

  useEffect(() => {
    setLoading(true);
    fetchFakeSchema().then(({ nodes, edges }) => {
      setNodes(nodes as unknown as Node[]);
      setEdges(edges as unknown as Edge[]);
      setLoading(false);
    });
  }, [setNodes, setEdges]);

  const isClient = typeof window !== 'undefined';

  console.log('loading', loading, 'nodes', nodes, 'edges', edges);
  return (
    <DashboardLayout activeItem="schema-visualizer" onItemClick={handleSidebarItemClick}>
      <div className="flex-1 flex flex-col bg-[#0a0a0a] text-[#fafafa] min-h-0 min-w-0 overflow-auto">
        {loading ? (
          <div className="flex-1 flex items-center justify-center text-[#888888] text-lg">Loading schema...</div>
        ) : (
          isClient && (
            <div style={{ width: '100%', height: '80vh' }}>
              <ReactFlow
                nodes={nodes}
                edges={edges}
                onNodesChange={onNodesChange}
                onEdgesChange={onEdgesChange}
                nodeTypes={nodeTypes}
                fitView
                className="bg-[#141414]"
              >
                <MiniMap />
                <Controls />
                <Background />
              </ReactFlow>
            </div>
          )
        )}
      </div>
    </DashboardLayout>
  );
};

export default SchemaVisualizer; 