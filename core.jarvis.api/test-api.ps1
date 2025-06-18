# PowerShell script to test the Jarvis API endpoints

$baseUrl = "http://localhost:7071/api"

Write-Host "Testing Jarvis API Endpoints" -ForegroundColor Green
Write-Host "============================" -ForegroundColor Green

# Test 1: Authentication
Write-Host "`nTest 1: Authentication (POST /api/security/auth)" -ForegroundColor Yellow
$authBody = @{
    id = [guid]::NewGuid().ToString()
    ownerEntityId = [guid]::NewGuid().ToString()
    updatedAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    email = "test@example.com"
    password = "TestPassword123!"
    clientId = "powershell-test"
} | ConvertTo-Json

try {
    $authResponse = Invoke-RestMethod -Uri "$baseUrl/security/auth" -Method Post -Body $authBody -ContentType "application/json"
    Write-Host "✓ Auth Success:" -ForegroundColor Green
    $authResponse | ConvertTo-Json -Depth 10
    $sessionId = $authResponse.sessionId
    $refreshToken = $authResponse.refreshToken
} catch {
    Write-Host "✗ Auth Failed: $_" -ForegroundColor Red
}

# Test 2: Token Validation
if ($sessionId) {
    Write-Host "`nTest 2: Token Validation (GET /api/security/validate)" -ForegroundColor Yellow
    try {
        $headers = @{ "X-Token-Id" = $sessionId }
        $validateResponse = Invoke-RestMethod -Uri "$baseUrl/security/validate" -Method Get -Headers $headers
        Write-Host "✓ Validation Success:" -ForegroundColor Green
        $validateResponse | ConvertTo-Json -Depth 10
    } catch {
        Write-Host "✗ Validation Failed: $_" -ForegroundColor Red
    }
}

# Test 3: Token Refresh
if ($refreshToken) {
    Write-Host "`nTest 3: Token Refresh (POST /api/security/refresh)" -ForegroundColor Yellow
    $refreshBody = @{
        id = [guid]::NewGuid().ToString()
        ownerEntityId = [guid]::NewGuid().ToString()
        updatedAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        refreshToken = $refreshToken
        clientId = "powershell-test"
    } | ConvertTo-Json
    
    try {
        $refreshResponse = Invoke-RestMethod -Uri "$baseUrl/security/refresh" -Method Post -Body $refreshBody -ContentType "application/json"
        Write-Host "✓ Refresh Success:" -ForegroundColor Green
        $refreshResponse | ConvertTo-Json -Depth 10
    } catch {
        Write-Host "✗ Refresh Failed: $_" -ForegroundColor Red
    }
}

# Test 4: Deauthentication
if ($sessionId) {
    Write-Host "`nTest 4: Deauthentication (POST /api/security/deauth)" -ForegroundColor Yellow
    $deauthBody = "`"$sessionId`""
    
    try {
        $deauthResponse = Invoke-RestMethod -Uri "$baseUrl/security/deauth" -Method Post -Body $deauthBody -ContentType "application/json"
        Write-Host "✓ Deauth Success:" -ForegroundColor Green
        Write-Host "Confirmation ID: $deauthResponse"
    } catch {
        Write-Host "✗ Deauth Failed: $_" -ForegroundColor Red
    }
}

# Test 5: Invalid Requests
Write-Host "`nTest 5: Invalid Request Tests" -ForegroundColor Yellow

# Invalid auth (missing email)
Write-Host "- Testing invalid auth (missing email)..." -ForegroundColor Cyan
$invalidAuthBody = @{
    id = [guid]::NewGuid().ToString()
    ownerEntityId = [guid]::NewGuid().ToString()
    updatedAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    password = "TestPassword123!"
} | ConvertTo-Json

try {
    $invalidResponse = Invoke-RestMethod -Uri "$baseUrl/security/auth" -Method Post -Body $invalidAuthBody -ContentType "application/json"
    Write-Host "✗ Should have failed but didn't" -ForegroundColor Red
} catch {
    Write-Host "✓ Correctly rejected: $($_.Exception.Message)" -ForegroundColor Green
}

# Invalid GUID
Write-Host "- Testing invalid GUID format..." -ForegroundColor Cyan
$invalidGuidBody = '"not-a-guid"'

try {
    $invalidResponse = Invoke-RestMethod -Uri "$baseUrl/security/deauth" -Method Post -Body $invalidGuidBody -ContentType "application/json"
    Write-Host "✗ Should have failed but didn't" -ForegroundColor Red
} catch {
    Write-Host "✓ Correctly rejected: $($_.Exception.Message)" -ForegroundColor Green
}

Write-Host "`nAll tests completed!" -ForegroundColor Green