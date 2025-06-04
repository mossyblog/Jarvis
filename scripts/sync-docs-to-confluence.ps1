#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Syncs markdown documentation files to Confluence pages
.DESCRIPTION
    This script helps sync local markdown documentation to Confluence pages.
    It can update existing pages or create new ones.
.PARAMETER MarkdownFile
    Path to the markdown file to sync
.PARAMETER PageId
    Confluence page ID to update (use 'new' to create a new page)
.PARAMETER ParentId
    Parent page ID when creating a new child page
.PARAMETER Title
    Page title (optional, will extract from markdown if not provided)
.PARAMETER Space
    Space key when creating a new page without parent
.EXAMPLE
    # Update existing page
    ./sync-docs-to-confluence.ps1 -MarkdownFile ./docs/README.md -PageId 12345678
    
    # Create new child page
    ./sync-docs-to-confluence.ps1 -MarkdownFile ./docs/guide.md -PageId new -ParentId 12345678 -Title "User Guide"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$MarkdownFile,
    
    [Parameter(Mandatory=$true)]
    [string]$PageId,
    
    [string]$ParentId,
    
    [string]$Title,
    
    [string]$Space
)

# Check if dotnet-script is installed
if (-not (Get-Command "dotnet-script" -ErrorAction SilentlyContinue)) {
    Write-Host "dotnet-script is not installed. Installing..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-script
    
    # Refresh PATH
    $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("PATH", "User")
}

# Check environment variables
if (-not $env:CONFLUENCE_EMAIL -or -not $env:CONFLUENCE_API_TOKEN) {
    Write-Host "Confluence credentials not found in environment variables." -ForegroundColor Yellow
    
    if (-not $env:CONFLUENCE_EMAIL) {
        $email = Read-Host "Enter your Confluence email"
        $env:CONFLUENCE_EMAIL = $email
    }
    
    if (-not $env:CONFLUENCE_API_TOKEN) {
        Write-Host "You need an API token from: https://id.atlassian.com/manage-profile/security/api-tokens" -ForegroundColor Cyan
        $token = Read-Host "Enter your Confluence API token" -AsSecureString
        $env:CONFLUENCE_API_TOKEN = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($token))
    }
}

# Build the command
$scriptPath = Join-Path $PSScriptRoot "markdown-to-confluence.csx"
$args = @($MarkdownFile, $PageId)

if ($PageId -eq "new") {
    if ($ParentId) {
        $args += "--create-child", $ParentId
    } elseif ($Space) {
        $args += "--space", $Space
    } else {
        Write-Host "Error: When creating a new page, you must specify either -ParentId or -Space" -ForegroundColor Red
        exit 1
    }
    
    if ($Title) {
        $args += "--title", "`"$Title`""
    }
} elseif ($Title) {
    $args += "--title", "`"$Title`""
}

# Execute the script
Write-Host "Syncing $MarkdownFile to Confluence..." -ForegroundColor Green
dotnet script $scriptPath $args

# Example batch sync function
function Sync-AllDocs {
    <#
    .SYNOPSIS
        Sync multiple documentation files to Confluence
    .EXAMPLE
        Sync-AllDocs
    #>
    
    $docMappings = @{
        "./docs/architecture/concurrency-control.md" = "115802116"
        "./docs/architecture/handler-pattern.md" = "PAGE_ID_HERE"
        "./docs/guides/snapshot-versioning.md" = "PAGE_ID_HERE"
        # Add more mappings as needed
    }
    
    foreach ($doc in $docMappings.GetEnumerator()) {
        if (Test-Path $doc.Key) {
            Write-Host "`nSyncing $($doc.Key)..." -ForegroundColor Cyan
            & $PSScriptRoot/sync-docs-to-confluence.ps1 -MarkdownFile $doc.Key -PageId $doc.Value
        } else {
            Write-Host "Warning: $($doc.Key) not found" -ForegroundColor Yellow
        }
    }
}

# Export the function for use in PowerShell sessions
Export-ModuleMember -Function Sync-AllDocs