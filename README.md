# YdcViewer - DICOM 3D Medical Image Viewer

A web-based DICOM medical image viewer with real-time 3D volume rendering, supporting macOS and Windows.

## Tech Stack

### Frontend
- React 18 + TypeScript + Vite
- MUI (Material Design)
- cornerstone3D (2D viewing)
- Zustand (state management)
- WebSocket (3D render stream)

### Backend
- ASP.NET Core 8 Web API
- fo-dicom (DICOM parsing)
- OpenTK (OpenGL volume rendering)
- ASP.NET Core Identity + JWT (auth)
- SQLite (metadata & user storage)

## Architecture

```
Browser (React + MUI)
    │
    ├── REST API ──────────┐
    └── WebSocket (3D流) ──┤
                           ▼
              Backend (ASP.NET Core 8)
              ├── DICOM Module (fo-dicom)
              ├── Render Engine (OpenTK/OpenGL)
              ├── Auth Module (Identity + JWT)
              └── Data Layer (SQLite + File System)
```

3D rendering runs on the backend using OpenGL Ray Casting. The frontend sends raw input events (mouse/pointer) via WebSocket, the backend owns camera state, renders frames and pushes JPEG images back at ~30fps.

## Project Structure

```
ydcviewer/
├── Backend/
│   ├── YdcViewer.Api/          # Web API entry point
│   ├── YdcViewer.Dicom/        # DICOM parsing & data management
│   ├── YdcViewer.Renderer/     # OpenGL 3D render engine
│   ├── YdcViewer.Auth/         # Authentication & authorization
│   └── YdcViewer.Data/         # Database & file storage
├── Frontend/                    # React SPA
│   └── src/
│       ├── api/                # REST API clients
│       ├── hooks/              # Custom hooks (WebSocket, etc.)
│       ├── stores/             # Zustand stores
│       ├── pages/              # Route pages
│       ├── components/         # UI components
│       └── theme/              # MUI theme
├── docs/                        # Documentation
└── YdcViewer.sln
```

## Getting Started

### Prerequisites
- .NET 10 SDK
- Node.js 24+
- OpenGL 4.3+ capable GPU

### Backend

```bash
cd Backend
dotnet restore
dotnet run --project YdcViewer.Api
```

The API server starts at `http://localhost:5000`.

### Frontend

```bash
cd Frontend
npm install
npm run dev
```

The dev server starts at `http://localhost:5173`.

## Build & Deploy

### One-command build (frontend + backend → publish/)

```bash
./build.sh
```

This builds the frontend (`npm run build`), copies `dist/` into `Backend/YdcViewer.Api/wwwroot/`, then publishes the backend to `publish/`.

### Run the published app

```bash
cd publish
dotnet YdcViewer.Api.dll
# Open http://localhost:5000
```

### CI/CD

GitHub Actions runs on every push/PR to `main`:
- Builds frontend and backend on Linux, Windows, macOS
- Runs backend unit tests (`dotnet test`)
- Uploads `publish/` as build artifact

### Development mode

```bash
# Terminal 1: backend
cd Backend && dotnet run --project YdcViewer.Api

# Terminal 2: frontend (hot reload)
cd Frontend && npm run dev
```

## Testing

```bash
# Backend unit tests
dotnet test

# Frontend E2E tests (requires Playwright)
cd Frontend && npx playwright install && npm run test:e2e
```

## Features

### Current (MVP)
- DICOM file import and parsing
- 3D volume rendering (Ray Casting)
- Real-time interactive rotation/zoom/pan
- Transfer function presets (bone, soft tissue, skin)

### Planned
- 2D viewing with window/level adjustment
- MPR (multi-planar reconstruction)
- Surface rendering (Marching Cubes)
- Clipping planes
- Measurement & annotation tools
- User roles & audit logging

## License

TBD
