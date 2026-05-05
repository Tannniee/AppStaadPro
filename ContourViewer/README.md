# ContourViewer

Standalone desktop viewer shell for future STAAD geometry and contour visualization.

## Phase 1 scope

- Launch a dedicated desktop app window
- Keep the project isolated from the existing `App` folder
- Prepare packaging so the app can become `ContourViewer.exe`
- Connect to the active STAAD session
- Read nodes and beams from the active STAAD model
- Render a basic 3D wireframe viewport

## Development

```powershell
python -m pip install -r requirements.txt
python -m contour_viewer
```

## Build

```powershell
.\build.ps1
```
