# Architecture Design

## System Overview

YdcViewer is a client-server DICOM viewer. The backend handles DICOM parsing, 3D rendering, and data persistence. The frontend provides the UI and 2D viewing capabilities.

## Threading Model

The backend runs two key thread types:

### ASP.NET Request Threads (Thread Pool)
- Handle HTTP REST requests (DICOM upload, auth, annotations)
- Handle WebSocket connections and message receiving
- On `camera_update`, push params to the render thread queue

### OpenGL Render Thread
- Owns the OpenGL context (OpenGL contexts are thread-bound)
- Reads latest camera params from the queue (drops stale frames)
- Executes Ray Casting shader → FBO → glReadPixels → JPEG encode
- Pushes encoded frames back via callback to WebSocket

```
┌─────────────────┐        ┌──────────────────┐
│  ASP.NET 线程池  │        │  OpenGL 渲染线程   │
│                 │  队列   │                  │
│ WebSocket recv ─┼───────►│ 取最新输入事件     │
│  input events   │        │ 更新相机状态      │
│                 │        │ Ray Casting      │
│ WebSocket send ◄┼───────┤ glReadPixels     │
│  frame bytes    │  回调   │ JPEG encode      │
└─────────────────┘        └──────────────────┘
```

**Key design**: The frontend sends raw mouse/pointer input events (dx, dy, buttons, scroll). The render thread owns the camera state and applies input deltas to update it. The queue only keeps the latest input; stale inputs are dropped. The backend is the single source of truth for camera position.

## Module Responsibilities

### YdcViewer.Api
ASP.NET Core Web API entry point. Configures DI, middleware, CORS, authentication. Contains controllers for REST endpoints and the WebSocket handler.

### YdcViewer.Dicom
Parses DICOM files using fo-dicom. Extracts pixel data and metadata. Assembles 3D volume data from a series of slices. Manages DICOM file storage on disk.

### YdcViewer.Renderer
Self-contained OpenGL rendering engine. Manages GL context creation, shader compilation, texture uploads, FBO rendering, and frame encoding. Uses `IRenderStrategy` interface for pluggable rendering modes.

### YdcViewer.Auth
User management with ASP.NET Core Identity. JWT token generation and validation. Role-based access control. Audit logging for login and data access events.

### YdcViewer.Data
EF Core DbContext with SQLite provider. Repositories for DICOM file metadata, user data, annotations, and audit logs.

## 3D Rendering Pipeline

1. **Volume Data Upload**: DICOM pixel data → OpenGL 3D Texture (R16 or R8 format)
2. **Transfer Function**: Color/opacity mapping uploaded as 1D texture
3. **Ray Casting Shader**: Fragment shader marches rays through the volume, sampling the 3D texture and applying the transfer function
4. **Frame Output**: Render to FBO → `glReadPixels` → JPEG encode → WebSocket push

## Extensibility: Render Strategies

```csharp
public interface IRenderStrategy
{
    void Setup(VolumeData volume);
    void Render(RenderSession session, Camera camera);
    void Dispose();
}
```

| Strategy | Status | Description |
|----------|--------|-------------|
| VolumeRenderStrategy | MVP | Ray Casting volume rendering |
| SurfaceRenderStrategy | Future | Marching Cubes isosurface extraction |
| MPRRenderStrategy | Future | Multi-planar reconstruction (axial/sagittal/coronal) |
| ClippingRenderStrategy | Future | Clipping plane rendering |

## Communication Protocol

### WebSocket (3D Render Only)
- Endpoint: `/ws/render`
- Frontend → Backend: JSON messages (`input` with raw mouse events, `render_params`, `load_series`)
- Backend → Frontend: Binary JPEG frames (4-byte sequence prefix + JPEG bytes)
- Backend owns camera state; frontend only sends input deltas

### REST API (Everything Else)
- `/api/dicom/*` - DICOM file management
- `/api/auth/*` - Authentication
- `/api/annotations/*` - Measurements and annotations (future)
