# Simple test to verify Swagger is accessible
# Run this after starting the functions with: func start

Write-Host "Testing Swagger endpoints..." -ForegroundColor Cyan

# Test Swagger UI
Write-Host "`nTesting Swagger UI at http://localhost:7071/api/swagger/ui"
try {
    $response = Invoke-WebRequest -Uri "http://localhost:7071/api/swagger/ui" -Method GET -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ Swagger UI returned status 200" -ForegroundColor Green
        if ($response.Content -like "*swagger-ui*") {
            Write-Host "✓ Swagger UI HTML content detected" -ForegroundColor Green
        }
    }
} catch {
    Write-Host "✗ Failed to access Swagger UI: $_" -ForegroundColor Red
}

# Test OpenAPI JSON
Write-Host "`nTesting OpenAPI JSON at http://localhost:7071/api/swagger.json"
try {
    $response = Invoke-WebRequest -Uri "http://localhost:7071/api/swagger.json" -Method GET -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ OpenAPI JSON returned status 200" -ForegroundColor Green
        $json = $response.Content | ConvertFrom-Json
        Write-Host "  OpenAPI Version: $($json.openapi)" -ForegroundColor Gray
        Write-Host "  API Title: $($json.info.title)" -ForegroundColor Gray
        Write-Host "  API Version: $($json.info.version)" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Failed to access OpenAPI JSON: $_" -ForegroundColor Red
}

Write-Host "`nTo view Swagger UI in browser, navigate to:" -ForegroundColor Yellow
Write-Host "http://localhost:7071/api/swagger/ui" -ForegroundColor Cyan