export interface TestUser {
  id: string;
  email: string;
  password: string;
  permissions: string[];
  firstName?: string;
  lastName?: string;
}

export interface TestComponent {
  id: string;
  name: string;
  type: string;
  properties?: Record<string, any>;
}

export interface TestLayout {
  id: string;
  name: string;
  components: Array<{
    componentId: string;
    row: number;
    col: number;
    width: number;
    height: number;
  }>;
}

export class TestDataFactory {
  static createTestUser(overrides: Partial<TestUser> = {}): TestUser {
    const timestamp = Date.now();
    return {
      id: `test-user-${timestamp}`,
      email: `test-${timestamp}@example.com`,
      password: 'TestPassword123!',
      permissions: ['admin', 'table-editor', 'schema-visualizer'],
      firstName: 'Test',
      lastName: 'User',
      ...overrides
    };
  }

  static createAdminUser(): TestUser {
    return this.createTestUser({
      email: 'admin@test.com',
      permissions: ['admin', 'table-editor', 'schema-visualizer', 'user-management']
    });
  }

  static createLimitedUser(): TestUser {
    return this.createTestUser({
      email: 'limited@test.com',
      permissions: ['table-editor']
    });
  }

  static createTestComponent(overrides: Partial<TestComponent> = {}): TestComponent {
    const timestamp = Date.now();
    return {
      id: `test-component-${timestamp}`,
      name: `Test Component ${timestamp}`,
      type: 'card',
      properties: {
        title: 'Test Title',
        content: 'Test content for component',
        color: 'blue'
      },
      ...overrides
    };
  }

  static createTestComponents(count: number): TestComponent[] {
    return Array.from({ length: count }, (_, i) => 
      this.createTestComponent({
        name: `Test Component ${i + 1}`,
        type: ['card', 'chart', 'table', 'image'][i % 4]
      })
    );
  }

  static createTestLayout(overrides: Partial<TestLayout> = {}): TestLayout {
    const timestamp = Date.now();
    return {
      id: `test-layout-${timestamp}`,
      name: `Test Layout ${timestamp}`,
      components: [
        {
          componentId: 'comp-1',
          row: 0,
          col: 0,
          width: 2,
          height: 1
        },
        {
          componentId: 'comp-2',
          row: 0,
          col: 2,
          width: 1,
          height: 2
        },
        {
          componentId: 'comp-3',
          row: 1,
          col: 0,
          width: 2,
          height: 1
        }
      ],
      ...overrides
    };
  }

  static createMockApiResponse(endpoint: string, data: any) {
    return {
      url: `**/api${endpoint}`,
      response: {
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          data,
          timestamp: Date.now()
        })
      }
    };
  }

  static createErrorResponse(endpoint: string, errorCode: number = 500, message: string = 'Internal Server Error') {
    return {
      url: `**/api${endpoint}`,
      response: {
        status: errorCode,
        contentType: 'application/json',
        body: JSON.stringify({
          error: true,
          message,
          code: errorCode,
          timestamp: Date.now()
        })
      }
    };
  }

  static createSlowResponse(endpoint: string, data: any, delay: number = 2000) {
    return {
      url: `**/api${endpoint}`,
      response: {
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          data,
          timestamp: Date.now()
        })
      },
      delay
    };
  }

  static generateLargeDataset(itemCount: number) {
    return Array.from({ length: itemCount }, (_, i) => ({
      id: i + 1,
      name: `Item ${i + 1}`,
      description: `Description for item ${i + 1}`,
      category: ['Category A', 'Category B', 'Category C'][i % 3],
      value: Math.random() * 1000,
      status: ['active', 'inactive', 'pending'][i % 3],
      createdAt: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000).toISOString(),
      metadata: {
        priority: ['high', 'medium', 'low'][i % 3],
        tags: [`tag-${i % 5}`, `tag-${(i + 1) % 5}`]
      }
    }));
  }

  static createFormTestData() {
    const timestamp = Date.now();
    return {
      text: `Test text input ${timestamp}`,
      email: `test-${timestamp}@example.com`,
      password: 'SecurePassword123!',
      number: Math.floor(Math.random() * 1000),
      date: new Date().toISOString().split('T')[0],
      textarea: `This is a longer text content for textarea testing.\nIt includes multiple lines\nand various characters: !@#$%^&*()`,
      select: 'option-2',
      checkbox: true,
      radio: 'option-a',
      url: 'https://example.com',
      phone: '+1-555-123-4567'
    };
  }

  static createPerformanceTestData() {
    return {
      users: this.generateLargeDataset(1000),
      components: this.createTestComponents(50),
      layouts: Array.from({ length: 10 }, () => this.createTestLayout()),
      settings: {
        theme: 'dark',
        language: 'en',
        notifications: true,
        autoSave: true
      }
    };
  }
}

export const TEST_USERS = {
  ADMIN: TestDataFactory.createAdminUser(),
  LIMITED: TestDataFactory.createLimitedUser(),
  STANDARD: TestDataFactory.createTestUser()
};

export const TEST_COMPONENTS = TestDataFactory.createTestComponents(10);

export const TEST_LAYOUTS = [
  TestDataFactory.createTestLayout({ name: 'Dashboard Layout' }),
  TestDataFactory.createTestLayout({ name: 'Analytics Layout' }),
  TestDataFactory.createTestLayout({ name: 'Report Layout' })
];

// Mock data for different scenarios
export const MOCK_RESPONSES = {
  SUCCESS: {
    LOGIN: TestDataFactory.createMockApiResponse('/auth/login', {
      token: 'mock-jwt-token',
      user: TEST_USERS.ADMIN,
      expiresIn: 3600
    }),
    
    USERS_LIST: TestDataFactory.createMockApiResponse('/users', 
      TestDataFactory.generateLargeDataset(25)
    ),
    
    COMPONENTS_LIST: TestDataFactory.createMockApiResponse('/components', 
      TEST_COMPONENTS
    ),
    
    LAYOUTS_LIST: TestDataFactory.createMockApiResponse('/layouts', 
      TEST_LAYOUTS
    )
  },
  
  ERROR: {
    UNAUTHORIZED: TestDataFactory.createErrorResponse('/auth/login', 401, 'Invalid credentials'),
    FORBIDDEN: TestDataFactory.createErrorResponse('/admin/users', 403, 'Access denied'),
    NOT_FOUND: TestDataFactory.createErrorResponse('/users/999', 404, 'User not found'),
    SERVER_ERROR: TestDataFactory.createErrorResponse('/components', 500, 'Internal server error')
  },
  
  SLOW: {
    SLOW_LOGIN: TestDataFactory.createSlowResponse('/auth/login', {
      token: 'mock-jwt-token',
      user: TEST_USERS.ADMIN
    }, 3000),
    
    SLOW_DATA_LOAD: TestDataFactory.createSlowResponse('/data/large-dataset',
      TestDataFactory.generateLargeDataset(1000), 5000
    )
  }
};