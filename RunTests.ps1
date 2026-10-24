# Resort Empire - Unity EditMode NUnit tests
# Save this file in the Unity project root (next to Assets, Packages and ProjectSettings).
# Run from PowerShell: powershell -ExecutionPolicy Bypass -File .\RunTests.ps1

$ErrorActionPreference = 'Stop'

# The project is the folder containing this script, regardless of the user's PC path.
$projectPath = $PSScriptRoot
$projectSettingsPath = Join-Path $projectPath 'ProjectSettings\ProjectVersion.txt'

if (-not (Test-Path -LiteralPath $projectSettingsPath)) {
    Write-Host 'ERROR: RunTests.ps1 must be in the Unity project root (next to Assets and ProjectSettings).' -ForegroundColor Red
    exit 1
}

# Use the exact Unity Editor version recorded by this project.
$versionLine = Get-Content -LiteralPath $projectSettingsPath | Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } | Select-Object -First 1
if (-not $versionLine) {
    Write-Host 'ERROR: Could not read the Unity version from ProjectVersion.txt.' -ForegroundColor Red
    exit 1
}
$editorVersion = ($versionLine -replace '^m_EditorVersion:\s*', '').Trim()

# UNITY_EDITOR_PATH can override the default Unity Hub installation location.
if ($env:UNITY_EDITOR_PATH) {
    $unityExe = $env:UNITY_EDITOR_PATH
} else {
    $unityExe = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
}

if (-not (Test-Path -LiteralPath $unityExe -PathType Leaf)) {
    Write-Host "ERROR: Unity Editor not found: $unityExe" -ForegroundColor Red
    Write-Host 'Install the required version in Unity Hub, or set UNITY_EDITOR_PATH to Unity.exe.'
    exit 1
}

# Put generated files in a separate folder, not inside Assets.
$outputDir = Join-Path $projectPath 'TestResults'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$resultsFile = Join-Path $outputDir 'EditModeResults.xml'
$logFile = Join-Path $outputDir 'EditMode.log'

# Avoid mistaking results from an earlier run for results from this one.
if (Test-Path -LiteralPath $resultsFile) {
    Remove-Item -LiteralPath $resultsFile -Force
}

Write-Host "Project: $projectPath"
Write-Host "Unity:   $unityExe"
Write-Host 'Running EditMode tests... (close the Unity Editor before using this script)'

# Quote path arguments because installation or project directories may contain spaces.
$unityArgs = @(
    '-batchmode',
    '-runTests',
    '-projectPath', "`"$projectPath`"",
    '-testPlatform', 'EditMode',
    '-testResults', "`"$resultsFile`"",
    '-logFile', "`"$logFile`""
)

try {
    $process = Start-Process -FilePath $unityExe -ArgumentList $unityArgs -Wait -PassThru
} catch {
    Write-Host "ERROR: Failed to start Unity: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path -LiteralPath $resultsFile)) {
    Write-Host "ERROR: Unity did not create the test results XML. Exit code: $($process.ExitCode)" -ForegroundColor Red
    Write-Host "Check the log: $logFile"
    exit 1
}

try {
    [xml]$report = Get-Content -LiteralPath $resultsFile -Raw
    $run = $report.SelectSingleNode('/test-run')
    if ($null -eq $run) { throw 'NUnit test-run element is missing.' }
    $total = [int]$run.GetAttribute('total')
    $passed = [int]$run.GetAttribute('passed')
    $failed = [int]$run.GetAttribute('failed')
    $skipped = [int]$run.GetAttribute('skipped')
    $result = $run.GetAttribute('result')
} catch {
    Write-Host "ERROR: Could not read the test results XML: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "XML: $resultsFile"
    exit 1
}

Write-Host "`nResult: $result | Total: $total | Passed: $passed | Failed: $failed | Skipped: $skipped"
Write-Host "XML: $resultsFile"
Write-Host "Log: $logFile"

if ($process.ExitCode -ne 0 -or $result -ne 'Passed' -or $total -eq 0 -or $failed -gt 0) {
    Write-Host "TESTS FAILED (Unity exit code: $($process.ExitCode))." -ForegroundColor Red
    exit 1
}

Write-Host 'ALL EDITMODE TESTS PASSED.' -ForegroundColor Green
exit 0
