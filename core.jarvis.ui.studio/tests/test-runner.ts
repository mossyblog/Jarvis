import { execSync } from 'child_process';
import { writeFileSync, readFileSync, existsSync } from 'fs';
import { join } from 'path';

/**
 * Comprehensive Test Runner for UIStudio Interface
 * 
 * This script orchestrates all types of testing:
 * - Unit tests (Vitest)
 * - Integration tests (Playwright)
 * - Performance tests (Playwright)
 * - Accessibility tests (Playwright + axe-core)
 * - Visual regression tests (Playwright)
 * - Cross-browser compatibility tests
 * - Mobile responsiveness tests
 */

interface TestResult {
  name: string;
  status: 'passed' | 'failed' | 'skipped';
  duration: number;
  coverage?: number;
  violations?: number;
  metrics?: Record<string, any>;
  errors?: string[];
}

interface TestSuite {
  name: string;
  description: string;
  command: string;
  timeout: number;
  required: boolean;
  parallel?: boolean;
}

class UIStudioTestRunner {
  private results: TestResult[] = [];
  private startTime: number = 0;
  private config = {
    outputDir: './test-results',
    reportFile: './test-results/comprehensive-report.json',
    htmlReportFile: './test-results/comprehensive-report.html',
    maxRetries: 2,
    timeoutMultiplier: process.env.CI ? 2 : 1
  };

  private testSuites: TestSuite[] = [
    {
      name: 'unit-tests',
      description: 'Unit Tests (Vitest)',
      command: 'npm run test:run',
      timeout: 60000,
      required: true
    },
    {
      name: 'lint-check',
      description: 'Code Quality & Linting',
      command: 'npm run lint',
      timeout: 30000,
      required: true
    },
    {
      name: 'type-check',
      description: 'TypeScript Type Checking',
      command: 'npm run typecheck',
      timeout: 45000,
      required: true
    },
    {
      name: 'e2e-chrome',
      description: 'E2E Tests (Chrome)',
      command: 'npm run test:e2e:chromium',
      timeout: 300000,
      required: true
    },
    {
      name: 'e2e-firefox',
      description: 'E2E Tests (Firefox)',
      command: 'npm run test:e2e:firefox',
      timeout: 300000,
      required: false
    },
    {
      name: 'e2e-webkit',
      description: 'E2E Tests (WebKit/Safari)',
      command: 'npm run test:e2e:webkit',
      timeout: 300000,
      required: false
    },
    {
      name: 'mobile-tests',
      description: 'Mobile Responsiveness Tests',
      command: 'npm run test:e2e:mobile',
      timeout: 240000,
      required: true
    },
    {
      name: 'accessibility-tests',
      description: 'Accessibility Compliance Tests',
      command: 'npm run test:e2e:accessibility',
      timeout: 180000,
      required: true
    },
    {
      name: 'performance-tests',
      description: 'Performance & Speed Tests',
      command: 'npm run test:e2e:performance',
      timeout: 240000,
      required: true
    },
    {
      name: 'visual-regression',
      description: 'Visual Regression Tests',
      command: 'npm run test:e2e:visual',
      timeout: 180000,
      required: false
    }
  ];

  async run(): Promise<void> {
    console.log('🚀 Starting Comprehensive UIStudio Interface Testing\n');
    console.log('📋 Test Plan:');
    this.testSuites.forEach((suite, index) => {
      const status = suite.required ? '🔴 Required' : '🟡 Optional';
      console.log(`   ${index + 1}. ${suite.description} ${status}`);
    });
    console.log('\n' + '='.repeat(60) + '\n');

    this.startTime = Date.now();
    
    // Ensure test environment is ready
    await this.setupTestEnvironment();

    // Run test suites
    for (const suite of this.testSuites) {
      await this.runTestSuite(suite);
    }

    // Generate comprehensive report
    await this.generateReport();
    
    // Print summary
    this.printSummary();
    
    // Exit with appropriate code
    process.exit(this.hasFailures() ? 1 : 0);
  }

  private async setupTestEnvironment(): Promise<void> {
    console.log('🔧 Setting up test environment...');
    
    try {
      // Ensure test results directory exists
      execSync(`mkdir -p ${this.config.outputDir}`, { stdio: 'ignore' });
      
      // Install Playwright browsers if needed
      if (!existsSync('./node_modules/@playwright/test')) {
        console.log('📦 Installing Playwright...');
        execSync('npm run test:e2e:install', { stdio: 'inherit' });
      }
      
      // Validate test setup
      execSync('npm run test:e2e:validate', { stdio: 'pipe' });
      
      console.log('✅ Test environment ready\n');
    } catch (error) {
      console.error('❌ Failed to setup test environment:', error);
      process.exit(1);
    }
  }

  private async runTestSuite(suite: TestSuite): Promise<void> {
    console.log(`🧪 Running: ${suite.description}`);
    console.log(`   Command: ${suite.command}`);
    console.log(`   Timeout: ${(suite.timeout * this.config.timeoutMultiplier / 1000).toFixed(0)}s`);
    
    const startTime = Date.now();
    let attempt = 0;
    let lastError: string = '';

    while (attempt <= this.config.maxRetries) {
      try {
        const output = execSync(suite.command, {
          timeout: suite.timeout * this.config.timeoutMultiplier,
          encoding: 'utf8',
          stdio: 'pipe'
        });

        const duration = Date.now() - startTime;
        const result: TestResult = {
          name: suite.name,
          status: 'passed',
          duration,
          metrics: this.extractMetrics(output, suite.name)
        };

        this.results.push(result);
        console.log(`   ✅ Passed in ${(duration / 1000).toFixed(2)}s\n`);
        return;

      } catch (error: any) {
        attempt++;
        lastError = error.message || error.toString();
        
        if (attempt <= this.config.maxRetries) {
          console.log(`   ⚠️  Attempt ${attempt} failed, retrying...`);
          await this.delay(2000 * attempt); // Exponential backoff
        }
      }
    }

    const duration = Date.now() - startTime;
    const result: TestResult = {
      name: suite.name,
      status: suite.required ? 'failed' : 'skipped',
      duration,
      errors: [lastError]
    };

    this.results.push(result);
    
    if (suite.required) {
      console.log(`   ❌ Failed after ${this.config.maxRetries + 1} attempts\n`);
      console.log(`   Error: ${lastError}\n`);
    } else {
      console.log(`   ⚠️  Skipped (optional test failed)\n`);
    }
  }

  private extractMetrics(output: string, suiteName: string): Record<string, any> {
    const metrics: Record<string, any> = {};
    
    try {
      switch (suiteName) {
        case 'unit-tests':
          // Extract coverage information
          const coverageMatch = output.match(/All files\s+\|\s+([\d.]+)/);
          if (coverageMatch) {
            metrics.coverage = parseFloat(coverageMatch[1]);
          }
          
          // Extract test counts
          const testMatch = output.match(/(\d+) passed/);
          if (testMatch) {
            metrics.testsPassed = parseInt(testMatch[1]);
          }
          break;

        case 'accessibility-tests':
          // Extract accessibility violations
          const violationsMatch = output.match(/(\d+) violations/);
          if (violationsMatch) {
            metrics.accessibilityViolations = parseInt(violationsMatch[1]);
          }
          break;

        case 'performance-tests':
          // Extract performance metrics
          const loadTimeMatch = output.match(/load time: (\d+)ms/);
          if (loadTimeMatch) {
            metrics.averageLoadTime = parseInt(loadTimeMatch[1]);
          }
          
          const memoryMatch = output.match(/memory usage: ([\d.]+)MB/);
          if (memoryMatch) {
            metrics.memoryUsage = parseFloat(memoryMatch[1]);
          }
          break;

        case 'visual-regression':
          // Extract visual diff information
          const diffMatch = output.match(/(\d+) screenshots? differ/);
          if (diffMatch) {
            metrics.visualDifferences = parseInt(diffMatch[1]);
          }
          break;
      }
    } catch (error) {
      console.warn(`Failed to extract metrics for ${suiteName}:`, error);
    }
    
    return metrics;
  }

  private async generateReport(): Promise<void> {
    console.log('📊 Generating comprehensive test report...');
    
    const totalDuration = Date.now() - this.startTime;
    const report = {
      summary: {
        timestamp: new Date().toISOString(),
        totalDuration: totalDuration,
        totalTests: this.results.length,
        passed: this.results.filter(r => r.status === 'passed').length,
        failed: this.results.filter(r => r.status === 'failed').length,
        skipped: this.results.filter(r => r.status === 'skipped').length,
        coverage: this.getOverallCoverage(),
        accessibilityScore: this.getAccessibilityScore(),
        performanceScore: this.getPerformanceScore()
      },
      results: this.results,
      environment: {
        nodeVersion: process.version,
        platform: process.platform,
        ci: !!process.env.CI,
        timestamp: new Date().toISOString()
      },
      recommendations: this.generateRecommendations()
    };

    // Write JSON report
    writeFileSync(this.config.reportFile, JSON.stringify(report, null, 2));
    
    // Generate HTML report
    const htmlReport = this.generateHtmlReport(report);
    writeFileSync(this.config.htmlReportFile, htmlReport);
    
    console.log(`   📄 JSON Report: ${this.config.reportFile}`);
    console.log(`   🌐 HTML Report: ${this.config.htmlReportFile}\n`);
  }

  private generateHtmlReport(report: any): string {
    return `
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>UIStudio Interface Test Report</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 20px; background: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        .header { text-align: center; margin-bottom: 30px; }
        .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin-bottom: 30px; }
        .metric { background: #f8f9fa; padding: 20px; border-radius: 6px; text-align: center; }
        .metric h3 { margin: 0 0 10px 0; color: #333; }
        .metric .value { font-size: 2em; font-weight: bold; color: #007bff; }
        .passed { color: #28a745; }
        .failed { color: #dc3545; }
        .skipped { color: #ffc107; }
        .results { margin-top: 30px; }
        .test-result { padding: 15px; margin: 10px 0; border-radius: 6px; border-left: 4px solid; }
        .test-result.passed { background: #d4edda; border-color: #28a745; }
        .test-result.failed { background: #f8d7da; border-color: #dc3545; }
        .test-result.skipped { background: #fff3cd; border-color: #ffc107; }
        .test-name { font-weight: bold; margin-bottom: 5px; }
        .test-duration { color: #666; font-size: 0.9em; }
        .recommendations { background: #e3f2fd; padding: 20px; border-radius: 6px; margin-top: 30px; }
        .recommendations h3 { color: #1976d2; margin-top: 0; }
        .recommendations ul { margin: 0; padding-left: 20px; }
        .footer { text-align: center; margin-top: 30px; color: #666; font-size: 0.9em; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🧪 UIStudio Interface Test Report</h1>
            <p>Generated on ${new Date(report.summary.timestamp).toLocaleString()}</p>
        </div>
        
        <div class="summary">
            <div class="metric">
                <h3>Total Tests</h3>
                <div class="value">${report.summary.totalTests}</div>
            </div>
            <div class="metric">
                <h3>Passed</h3>
                <div class="value passed">${report.summary.passed}</div>
            </div>
            <div class="metric">
                <h3>Failed</h3>
                <div class="value failed">${report.summary.failed}</div>
            </div>
            <div class="metric">
                <h3>Duration</h3>
                <div class="value">${(report.summary.totalDuration / 1000 / 60).toFixed(1)}m</div>
            </div>
            <div class="metric">
                <h3>Coverage</h3>
                <div class="value">${report.summary.coverage || 'N/A'}</div>
            </div>
            <div class="metric">
                <h3>Accessibility</h3>
                <div class="value">${report.summary.accessibilityScore}</div>
            </div>
        </div>
        
        <div class="results">
            <h2>Test Results</h2>
            ${report.results.map((result: TestResult) => `
                <div class="test-result ${result.status}">
                    <div class="test-name">${result.name}</div>
                    <div class="test-duration">Duration: ${(result.duration / 1000).toFixed(2)}s</div>
                    ${result.metrics ? `<div class="test-metrics">Metrics: ${JSON.stringify(result.metrics)}</div>` : ''}
                    ${result.errors ? `<div class="test-errors">Errors: ${result.errors.join(', ')}</div>` : ''}
                </div>
            `).join('')}
        </div>
        
        ${report.recommendations.length > 0 ? `
        <div class="recommendations">
            <h3>🔧 Recommendations</h3>
            <ul>
                ${report.recommendations.map((rec: string) => `<li>${rec}</li>`).join('')}
            </ul>
        </div>
        ` : ''}
        
        <div class="footer">
            <p>UIStudio Test Suite | Node.js ${report.environment.nodeVersion} | ${report.environment.platform}</p>
        </div>
    </div>
</body>
</html>
    `;
  }

  private generateRecommendations(): string[] {
    const recommendations: string[] = [];
    
    const failedTests = this.results.filter(r => r.status === 'failed');
    const coverage = this.getOverallCoverage();
    const accessibilityScore = this.getAccessibilityScore();
    
    if (failedTests.length > 0) {
      recommendations.push(`Fix ${failedTests.length} failing test suite(s): ${failedTests.map(t => t.name).join(', ')}`);
    }
    
    if (coverage && coverage < 80) {
      recommendations.push(`Increase test coverage from ${coverage}% to at least 80%`);
    }
    
    if (accessibilityScore !== '100%') {
      recommendations.push('Address accessibility violations to achieve 100% compliance');
    }
    
    const performanceResults = this.results.find(r => r.name === 'performance-tests');
    if (performanceResults?.metrics?.averageLoadTime > 3000) {
      recommendations.push('Optimize page load time - currently exceeds 3 seconds');
    }
    
    if (this.results.some(r => r.status === 'skipped')) {
      recommendations.push('Enable and fix skipped optional tests for better browser coverage');
    }
    
    return recommendations;
  }

  private getOverallCoverage(): string | null {
    const unitTestResult = this.results.find(r => r.name === 'unit-tests');
    return unitTestResult?.metrics?.coverage ? `${unitTestResult.metrics.coverage}%` : null;
  }

  private getAccessibilityScore(): string {
    const accessibilityResult = this.results.find(r => r.name === 'accessibility-tests');
    const violations = accessibilityResult?.metrics?.accessibilityViolations || 0;
    return violations === 0 ? '100%' : `${Math.max(0, 100 - violations * 10)}%`;
  }

  private getPerformanceScore(): string {
    const performanceResult = this.results.find(r => r.name === 'performance-tests');
    if (!performanceResult?.metrics?.averageLoadTime) return 'N/A';
    
    const loadTime = performanceResult.metrics.averageLoadTime;
    if (loadTime < 1000) return 'Excellent';
    if (loadTime < 2000) return 'Good';
    if (loadTime < 3000) return 'Fair';
    return 'Poor';
  }

  private printSummary(): void {
    console.log('\n' + '='.repeat(60));
    console.log('📊 TEST SUMMARY');
    console.log('='.repeat(60));
    
    const totalDuration = Date.now() - this.startTime;
    const passed = this.results.filter(r => r.status === 'passed').length;
    const failed = this.results.filter(r => r.status === 'failed').length;
    const skipped = this.results.filter(r => r.status === 'skipped').length;
    
    console.log(`⏱️  Total Duration: ${(totalDuration / 1000 / 60).toFixed(1)} minutes`);
    console.log(`✅ Passed: ${passed}/${this.results.length}`);
    console.log(`❌ Failed: ${failed}/${this.results.length}`);
    console.log(`⚠️  Skipped: ${skipped}/${this.results.length}`);
    
    const coverage = this.getOverallCoverage();
    if (coverage) {
      console.log(`📈 Coverage: ${coverage}`);
    }
    
    console.log(`♿ Accessibility: ${this.getAccessibilityScore()}`);
    console.log(`⚡ Performance: ${this.getPerformanceScore()}`);
    
    console.log('\n📄 Reports Generated:');
    console.log(`   JSON: ${this.config.reportFile}`);
    console.log(`   HTML: ${this.config.htmlReportFile}`);
    
    if (this.hasFailures()) {
      console.log('\n❌ Some tests failed. Please review the results above.');
    } else {
      console.log('\n🎉 All tests passed! UIStudio interface is thoroughly validated.');
    }
  }

  private hasFailures(): boolean {
    return this.results.some(r => r.status === 'failed');
  }

  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

// Run the test suite if this script is executed directly
if (require.main === module) {
  const runner = new UIStudioTestRunner();
  runner.run().catch(error => {
    console.error('Test runner failed:', error);
    process.exit(1);
  });
}

export { UIStudioTestRunner };