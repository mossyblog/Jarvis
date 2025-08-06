#!/bin/bash

echo "🔍 Starting Dashboard Sizing Analysis..."
echo "========================================"

# Check if dev server is running
if ! curl -f http://localhost:5173 >/dev/null 2>&1; then
    echo "❌ Dev server not running on localhost:5173"
    echo "Please start the dev server with 'npm run dev' first"
    exit 1
fi

echo "✅ Dev server is running"

# Install playwright if not already installed
if ! command -v npx &> /dev/null; then
    echo "❌ npx not found. Please install Node.js"
    exit 1
fi

# Run the analysis
echo "🚀 Running analysis script..."
node inspect-dashboard-sizing.js

echo "✅ Analysis complete! Check the generated files:"
echo "   - dashboard-analysis-screenshot.png"
echo "   - dashboard-sizing-analysis.json" 
echo "   - DASHBOARD_SIZING_WIREFRAME.md"