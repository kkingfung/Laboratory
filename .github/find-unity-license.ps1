# Unity License File Finder for Windows
# This script helps locate your Unity license file for CI/CD setup

Write-Host "🔍 Searching for Unity license file..." -ForegroundColor Yellow

# Common Unity license locations
$licenseLocations = @(
    "$env:ProgramData\Unity\Unity_lic.ulf",
    "$env:LOCALAPPDATA\Unity\Editor\Unity_lic.ulf",
    "$env:APPDATA\Unity\Unity_lic.ulf"
)

# Check each location
$foundLicense = $false
foreach ($location in $licenseLocations) {
    if (Test-Path $location) {
        Write-Host "✅ Found Unity license file:" -ForegroundColor Green
        Write-Host "   $location" -ForegroundColor White
        Write-Host ""

        # Show first few lines to verify it's correct
        Write-Host "📋 License file preview:" -ForegroundColor Cyan
        $content = Get-Content $location -TotalCount 10
        foreach ($line in $content) {
            if ($line -match "<?xml|License|Unity") {
                Write-Host "   $line" -ForegroundColor Gray
            }
        }

        Write-Host ""
        Write-Host "📋 Next steps:" -ForegroundColor Yellow
        Write-Host "1. Copy the ENTIRE contents of this file" -ForegroundColor White
        Write-Host "2. Go to GitHub: Settings → Secrets and variables → Actions" -ForegroundColor White
        Write-Host "3. Create secret 'UNITY_LICENSE' with the file contents" -ForegroundColor White
        Write-Host "4. Add 'UNITY_EMAIL' and 'UNITY_PASSWORD' secrets too" -ForegroundColor White

        $foundLicense = $true
        break
    }
}

if (-not $foundLicense) {
    Write-Host "❌ No Unity license file found in standard locations" -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 Solutions:" -ForegroundColor Yellow
    Write-Host "1. Open Unity Hub and ensure you're signed in" -ForegroundColor White
    Write-Host "2. Activate a Personal license (if not already done)" -ForegroundColor White
    Write-Host "3. Check Unity Hub → Preferences → License Management" -ForegroundColor White
    Write-Host "4. If you have a Professional license, use UNITY_SERIAL instead" -ForegroundColor White
    Write-Host ""
    Write-Host "📁 Manual search locations:" -ForegroundColor Cyan
    foreach ($location in $licenseLocations) {
        Write-Host "   $location" -ForegroundColor Gray
    }
}

# Check Unity Hub installation
Write-Host ""
Write-Host "🔍 Checking Unity Hub installation..." -ForegroundColor Yellow

$unityHubPaths = @(
    "${env:ProgramFiles}\Unity Hub\Unity Hub.exe",
    "${env:ProgramFiles(x86)}\Unity Hub\Unity Hub.exe",
    "$env:LOCALAPPDATA\Programs\Unity Hub\Unity Hub.exe"
)

$hubFound = $false
foreach ($hubPath in $unityHubPaths) {
    if (Test-Path $hubPath) {
        Write-Host "✅ Unity Hub found: $hubPath" -ForegroundColor Green
        $hubFound = $true
        break
    }
}

if (-not $hubFound) {
    Write-Host "❌ Unity Hub not found - install it first" -ForegroundColor Red
    Write-Host "   Download: https://unity3d.com/get-unity/download" -ForegroundColor White
}

Write-Host ""
Write-Host "🚀 For complete setup instructions, see:" -ForegroundColor Green
Write-Host "   .github/SETUP-UNITY-SECRETS.md" -ForegroundColor White

# Pause to let user read the output
Write-Host ""
Read-Host "Press Enter to close"