# CLAUDE.md - YdcViewer Project Context

## Project Overview
DICOM 3D medical image viewer. React frontend (Material Design) + C# backend with OpenGL volume rendering. LAN deployment with full RBAC auth.

## Architecture Decisions
- 3D rendering on **backend** (OpenGL Ray Casting), pushed to frontend via WebSocket frames
- 2D viewing on **frontend** (cornerstone3D)
- Annotations/measurements via **HTTP REST** (persistent CRUD)
- 3D render stream via **WebSocket** (transient, real-time)
- Single OpenGL context on one render thread, serves all users
- Render thread receives latest camera params from a queue (drop stale frames)

## Backend (.NET)
- Solution: `YdcViewer.sln` with 5 projects
- Target: `net10.0`
- Key deps: fo-dicom, OpenTK, EF Core + SQLite, ASP.NET Core Identity + JWT
- Rendering uses `IRenderStrategy` interface for extensibility (volume → surface → MPR → clipping)
- Shaders in `YdcViewer.Renderer/Shaders/` (GLSL)

## Frontend (React)
- Vite + React 18 + TypeScript
- MUI for Material Design UI
- Zustand for state management
- cornerstone3D for 2D DICOM viewing
- WebSocket hook (`useRenderSocket`) manages 3D render connection

## Conventions
- C#: PascalCase for public members, _camelCase for private fields
- TypeScript: camelCase for functions/variables, PascalCase for components/types
- API routes: `/api/{resource}` for REST, `/ws/render` for WebSocket
- All DICOM files stored on server filesystem, metadata indexed in SQLite

## Key Files
- `Backend/YdcViewer.Api/Program.cs` - API startup & DI config
- `Backend/YdcViewer.Renderer/RenderEngine.cs` - OpenGL lifecycle
- `Backend/YdcViewer.Renderer/Shaders/volume.frag` - Ray Casting shader
- `Frontend/src/hooks/useRenderSocket.ts` - WebSocket render client
- `Frontend/src/pages/ViewerPage.tsx` - Main viewer layout
