@echo off
setlocal

set "APP_DIR=%~dp0dist\ContourViewer"
set "APP_EXE=%APP_DIR%\ContourViewer.exe"

if not exist "%APP_EXE%" (
    echo ContourViewer.exe was not found.
    echo Build the app first, then run this launcher again.
    pause
    exit /b 1
)

pushd "%APP_DIR%"
start "" "%APP_EXE%"
popd

endlocal
