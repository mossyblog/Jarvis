import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { DashboardLayout } from '../components/layout/DashboardLayout';
import { TablesList } from '../components/table-editor/TablesList';
import { TableToolbar } from '../components/table-editor/TableToolbar';
import { TableDataGrid } from '../components/table-editor/TableDataGrid';
import { TableStatusBar } from '../components/table-editor/TableStatusBar';

interface SchemaData {
  [key: string]: string[];
}

const TableEditor = () => {
  const navigate = useNavigate();
  const [selectedTable, setSelectedTable] = useState('accounts');
  const [selectedSchema] = useState('public');
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);

  // Mock data for tables
  const schemas: SchemaData = {
    public: ['accounts', 'projects', 'organizations', 'audit_logs', 'permissions']
  };

  // Mock table data
  const tableData = [
    {
      id: '05b62eed-11bc-4e07-9a3b-fc09f1a5b65',
      email: 'test@pypers.com',
      json: '{"dob":"2025-05-06T14:00:00.000Z","nar...',
      dtlastupdated: '2025-05-22 15:38:31.643+00'
    },
    {
      id: '06e7b1a0-ac4d-447a-a7e6-762e37947487',
      email: 'john@example.com',
      json: '{"status":"active","role":"admin"}',
      dtlastupdated: '2025-05-22 16:15:22.123+00'
    },
    {
      id: '0a5900fb-d9fc-4264-8240-00e4a0e29dc',
      email: 'sarah@demo.io',
      json: '{"preferences":{"theme":"dark"}}',
      dtlastupdated: '2025-05-22 17:45:10.456+00'
    },
    {
      id: '0ffc8490-3915-42ff-8630-0e3464923013',
      email: 'mike@test.org',
      json: '{"settings":{"notifications":true}}',
      dtlastupdated: '2025-05-22 18:22:45.789+00'
    }
  ];

  const handleSidebarItemClick = (itemId: string) => {
    if (itemId === 'home') {
      navigate('/');
    }
  };

  return (
    <DashboardLayout activeItem="table-editor" onItemClick={handleSidebarItemClick}>
      <div className="flex h-full">
        {!isSidebarCollapsed && (
          <TablesList 
            selectedSchema={selectedSchema}
            selectedTable={selectedTable}
            schemas={schemas}
            onTableSelect={setSelectedTable}
          />
        )}

        {/* Main Content Area */}
        <div className="flex-1 flex flex-col bg-[#0a0a0a] transition-all duration-200 min-w-0">
          <TableToolbar 
            tableName={selectedTable}
            isSidebarCollapsed={isSidebarCollapsed}
            onToggleSidebar={() => setIsSidebarCollapsed(!isSidebarCollapsed)}
          />
          <TableDataGrid data={tableData} />
          <TableStatusBar 
            currentPage={1}
            totalPages={1}
            totalRows={100}
            recordCount={tableData.length}
          />
        </div>
      </div>
    </DashboardLayout>
  );
};

export default TableEditor;