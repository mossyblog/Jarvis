# Test script to verify Swagger is working

Write-Host "Testing Swagger API endpoints..." -ForegroundColor Cyan

# Test Swagger UI
Write-Host "`nTesting Swagger UI endpoint..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:7071/api/swagger/ui" -Method GET
    if ($response.StatusCode -eq 200 -and $response.Content -like "*swagger-ui*") {
        Write-Host "✓ Swagger UI is working!" -ForegroundColor Green
    } else {
        Write-Host "✗ Swagger UI returned unexpected content" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Failed to access Swagger UI: $_" -ForegroundColor Red
}

# Test OpenAPI document (JSON)
Write-Host "`nTesting OpenAPI JSON endpoint..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:7071/api/swagger.json" -Method GET
    $json = $response.Content | ConvertFrom-Json
    if ($json.openapi -and $json.info -and $json.paths) {
        Write-Host "✓ OpenAPI JSON document is valid!" -ForegroundColor Green
        Write-Host "  API Title: $($json.info.title)" -ForegroundColor Gray
        Write-Host "  Version: $($json.info.version)" -ForegroundColor Gray
        Write-Host "  Endpoints found: $($json.paths.PSObject.Properties.Count)" -ForegroundColor Gray
    } else {
        Write-Host "✗ OpenAPI JSON is missing required fields" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Failed to access OpenAPI JSON: $_" -ForegroundColor Red
}

# Test OpenAPI document (YAML)
Write-Host "`nTesting OpenAPI YAML endpoint..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:7071/api/swagger.yaml" -Method GET
    if ($response.StatusCode -eq 200 -and $response.Content -like "*openapi:*") {
        Write-Host "✓ OpenAPI YAML document is working!" -ForegroundColor Green
    } else {
        Write-Host "✗ OpenAPI YAML returned unexpected content" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Failed to access OpenAPI YAML: $_" -ForegroundColor Red
}

Write-Host "`nSwagger testing complete!" -ForegroundColor Cyan