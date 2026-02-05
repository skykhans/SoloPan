# Restart backend for PanSystem
# Usage: PowerShell -ExecutionPolicy Bypass -File .\restart-backend.ps1

$projectDir = "C:\test\PanSystem\panApi"
$processName = "PanSystem"

Get-Process $processName -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Process -FilePath "dotnet" -ArgumentList @("run","--project","PanSystem.csproj","--launch-profile","https") -WorkingDirectory $projectDir

Write-Host "Backend restarted (project: $projectDir)." -ForegroundColor Green
