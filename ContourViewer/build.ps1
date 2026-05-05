$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root "dist"
$build = Join-Path $root "build"
$spec = Join-Path $root "ContourViewer.spec"

Push-Location $root
try {
    python -m PyInstaller `
        --noconfirm `
        --clean `
        --windowed `
        --name ContourViewer `
        --paths (Join-Path $root "src") `
        --specpath $root `
        --distpath $dist `
        --workpath $build `
        (Join-Path $root "run_contour_viewer.py")
}
finally {
    Pop-Location
}
