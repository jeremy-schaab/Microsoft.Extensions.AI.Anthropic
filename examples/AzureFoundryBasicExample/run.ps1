# Azure Foundry Basic Example - PowerShell Runner
# This script helps you quickly run the example with environment variables

param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceName,

    [Parameter(Mandatory=$false)]
    [string]$ApiKey,

    [Parameter(Mandatory=$false)]
    [switch]$UseAzureIdentity
)

Write-Host "Azure Foundry Basic Example - Quick Start" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check if .env file exists
if (Test-Path ".env") {
    Write-Host "Loading environment variables from .env file..." -ForegroundColor Green
    Get-Content .env | ForEach-Object {
        if ($_ -match '^([^=]+)=(.+)$') {
            $name = $matches[1].Trim()
            $value = $matches[2].Trim()
            if (-not [string]::IsNullOrWhiteSpace($name) -and -not $name.StartsWith("#")) {
                [Environment]::SetEnvironmentVariable($name, $value, "Process")
                Write-Host "  $name = $value" -ForegroundColor Gray
            }
        }
    }
    Write-Host ""
}

# Override with command-line parameters if provided
if ($ResourceName) {
    Write-Host "Setting ANTHROPIC_FOUNDRY_RESOURCE from parameter..." -ForegroundColor Yellow
    $env:ANTHROPIC_FOUNDRY_RESOURCE = $ResourceName
}

if ($ApiKey) {
    Write-Host "Setting ANTHROPIC_FOUNDRY_API_KEY from parameter..." -ForegroundColor Yellow
    $env:ANTHROPIC_FOUNDRY_API_KEY = $ApiKey
}

if ($UseAzureIdentity) {
    Write-Host "Using Azure Identity (removing API key)..." -ForegroundColor Yellow
    $env:ANTHROPIC_FOUNDRY_API_KEY = $null
}

# Validate configuration
$resource = $env:ANTHROPIC_FOUNDRY_RESOURCE
$apiKey = $env:ANTHROPIC_FOUNDRY_API_KEY

if ([string]::IsNullOrWhiteSpace($resource)) {
    Write-Host "ERROR: ANTHROPIC_FOUNDRY_RESOURCE is not set!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\run.ps1 -ResourceName <your-resource-name> -ApiKey <your-api-key>"
    Write-Host "  .\run.ps1 -ResourceName <your-resource-name> -UseAzureIdentity"
    Write-Host ""
    Write-Host "Or create a .env file with your configuration (see .env.example)"
    exit 1
}

Write-Host "Configuration:" -ForegroundColor Green
Write-Host "  Resource: $resource"
Write-Host "  Authentication: $(if ([string]::IsNullOrWhiteSpace($apiKey)) { 'Azure Identity (DefaultAzureCredential)' } else { 'API Key' })"
Write-Host ""

# Run the example
Write-Host "Running example..." -ForegroundColor Cyan
Write-Host ""
dotnet run

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Example completed successfully!" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Example failed with exit code: $LASTEXITCODE" -ForegroundColor Red
}
