Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-Command([string]$name) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
        throw "Required command '$name' not found on PATH."
    }
}

function Get-AzdEnvValue([string]$key) {
    # azd env get-values outputs lines like KEY=VALUE
    $lines = & azd env get-values 2>$null
    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -match '^\s*#') { continue }
        $parts = $line.Split('=', 2)
        if ($parts.Length -ne 2) { continue }
        if ($parts[0].Trim() -eq $key) { return $parts[1].Trim() }
    }
    return $null
}

function New-AppClientSecret {
    param(
        [Parameter(Mandatory = $true)][string]$appId,
        [Parameter(Mandatory = $true)][string]$displayName
    )
    $candidateDays = @(365, 180, 90, 60, 30, 14, 7, 1)
    foreach ($days in $candidateDays) {
        $endDate = (Get-Date).ToUniversalTime().AddDays($days).ToString('yyyy-MM-ddTHH:mm:ssZ')
        Write-Host "Trying client secret expiry: $days days (endDate=$endDate)"

        # az writes policy failures to stderr; treat them as expected retries and keep output clean.
        $secret = & az ad app credential reset `
            --id $appId `
            --append `
            --display-name $displayName `
            --end-date $endDate `
            --query password -o tsv 2>$null

        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($secret)) {
            return $secret.Trim()
        }

        Write-Host "Client secret expiry $days days was rejected; trying shorter."
    }

    throw 'Failed to create client secret. Your tenant policy may require an even shorter lifetime, or your account may lack permission to create secrets.'
}

Assert-Command 'az'
Assert-Command 'azd'

# This hook always creates a new identity and overwrites azd environment values.

# Verify az login early (gives a clearer error than later az ad calls).
$tenantId = (& az account show --query tenantId -o tsv 2>$null).Trim()
if ([string]::IsNullOrWhiteSpace($tenantId)) {
    throw "Azure CLI is not logged in. Run: az login"
}

$azdEnvName = if ($env:AZD_ENV_NAME) { $env:AZD_ENV_NAME } else { 'azd' }
$displayName = "systempromptswitchinggptbot-$(Get-Date -Format 'yyyy-MM-dd-HHmmss')"

Write-Host "Creating Entra app registration: $displayName"
$appId = (& az ad app create `
    --display-name $displayName `
    --sign-in-audience AzureADMyOrg `
    --query appId -o tsv).Trim()

if ([string]::IsNullOrWhiteSpace($appId)) {
    throw 'Failed to create app registration (no appId returned).'
}

# Ensure a service principal exists for the app.
& az ad sp create --id $appId | Out-Null

Write-Host 'Creating client secret...'
$secret = New-AppClientSecret -appId $appId -displayName 'azd-bot-secret'

# These keys are consumed by infra/main.bicep and flow into App Service settings.
& azd env set microsoftAppType 'SingleTenant' | Out-Null
& azd env set microsoftAppId $appId | Out-Null
& azd env set microsoftAppPassword $secret | Out-Null
& azd env set microsoftAppTenantId $tenantId | Out-Null

Write-Host 'Done. Bot identity stored in azd environment values.'
