@echo off
setlocal

set "ROOT_DIR=%~dp0"
pushd "%ROOT_DIR%"

if exist "%ROOT_DIR%run_contour_viewer.py" (
    where python >nul 2>nul
    if %errorlevel%==0 (
        python run_contour_viewer.py
        goto :done
    )

    where py >nul 2>nul
    if %errorlevel%==0 (
        py -3 run_contour_viewer.py
        goto :done
    )
)

echo Python was not found, or run_contour_viewer.py is missing.
pause

:done
popd
endlocal
