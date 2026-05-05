@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "APPDIR=%~dp0"
set "SETTINGS=%APPDIR%settings.ini"
set "EXAMPLE=C:\ProgramData\Bentley\Engineering\STAAD.Pro 2025\Sections"

title STAAD Legacy Sections Setup
echo.
echo Configure Legacy Sections path for StaadSteelApp
echo.
echo Example:
echo   %EXAMPLE%
echo.
set /p "LEGACY_PATH=Enter full path to the Sections folder (press Enter to use example): "

if not defined LEGACY_PATH set "LEGACY_PATH=%EXAMPLE%"

if not exist "%LEGACY_PATH%" (
    echo.
    echo Folder not found:
    echo   %LEGACY_PATH%
    echo.
    pause
    exit /b 1
)

if not exist "%LEGACY_PATH%\SectionDatabaseInfo.xml" (
    echo.
    echo SectionDatabaseInfo.xml was not found in:
    echo   %LEGACY_PATH%
    echo.
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$settingsPath = [IO.Path]::GetFullPath('%SETTINGS%');" ^
    "$legacyPath = [IO.Path]::GetFullPath('%LEGACY_PATH%');" ^
    "$lines = @(); if (Test-Path $settingsPath) { $lines = Get-Content -Path $settingsPath };" ^
    "$updated = $false;" ^
    "$output = New-Object System.Collections.Generic.List[string];" ^
    "foreach ($line in $lines) {" ^
    "  if ($line -match '^\s*LegacySectionsPath\s*=') {" ^
    "    $output.Add('LegacySectionsPath=' + $legacyPath);" ^
    "    $updated = $true;" ^
    "  } else {" ^
    "    $output.Add($line);" ^
    "  }" ^
    "};" ^
    "if (-not $updated) { $output.Add('LegacySectionsPath=' + $legacyPath) };" ^
    "Set-Content -Path $settingsPath -Value $output -Encoding ASCII"

if errorlevel 1 (
    echo.
    echo Failed to update settings.ini
    echo.
    pause
    exit /b 1
)

echo.
echo Saved LegacySectionsPath to:
echo   %SETTINGS%
echo.
echo Value:
echo   %LEGACY_PATH%
echo.
pause
