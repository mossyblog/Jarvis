# PowerShell script to run tests with coverage, copy the report, and open the dashboard

Write-Host "Running tests with code coverage..."
# Run tests and collect coverage.
# Use -NoBuild if build is already up-to-date to speed up.
dotnet test core.jarvis.sln --collect:"XPlat Code Coverage" --nologo

# Check if the last command was successful
if ($LASTEXITCODE -ne 0) {
    Write-Error "Dotnet test command failed. Aborting."
    exit $LASTEXITCODE
}

Write-Host "Tests finished. Locating and copying coverage report..."

# Define paths
$testProjectDir = ".\core.jarvis.tests"
$testResultsDir = Join-Path -Path $testProjectDir -ChildPath "TestResults"
$docsDir = Join-Path -Path $testProjectDir -ChildPath "docs"
$dashboardFile = Join-Path -Path $docsDir -ChildPath "coverage_dashboard.html"
$destCoverageFile = Join-Path -Path $docsDir -ChildPath "coverage.xml" # Fixed destination name

# Find the latest coverage file
$latestReport = Get-ChildItem -Path $testResultsDir -Recurse -Filter "coverage.cobertura.xml" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($latestReport) {
    Write-Host "Found coverage report: $($latestReport.FullName)"
    # Ensure docs directory exists
    if (-not (Test-Path $docsDir -PathType Container)) {
        New-Item -ItemType Directory -Path $docsDir | Out-Null
    }
    # Copy the file
    Copy-Item -Path $latestReport.FullName -Destination $destCoverageFile -Force
    Write-Host "Copied coverage report to '$destCoverageFile'."

    # Open the dashboard
    if (Test-Path $dashboardFile) {
        Write-Host "Opening coverage dashboard..."
        Start-Process $dashboardFile
        Write-Host "Dashboard should open shortly. Please refresh the page if it loaded before the coverage file was copied."
    } else {
        Write-Warning "Coverage dashboard HTML file not found at '$dashboardFile'."
    }
} else {
    Write-Warning "Could not find a coverage.cobertura.xml file in '$testResultsDir'."
}

Write-Host "Script finished."