# API Reference

## REST API

### Authentication

#### POST /api/auth/login
Login and receive JWT token.

**Request:**
```json
{
  "username": "string",
  "password": "string"
}
```

**Response:**
```json
{
  "token": "jwt-string",
  "expiration": "2026-05-11T10:00:00Z",
  "roles": ["admin", "viewer"]
}
```

#### POST /api/auth/register
Register a new user (admin only).

---

### DICOM Management

#### POST /api/dicom/upload
Upload DICOM files (multipart/form-data).

**Request:** Multipart with one or more `.dcm` files or a `.zip` archive.

**Response:**
```json
{
  "seriesId": "uuid",
  "patientName": "string",
  "modality": "CT",
  "sliceCount": 256,
  "dimensions": { "x": 512, "y": 512, "z": 256 }
}
```

#### GET /api/dicom/series
List all uploaded series.

**Response:**
```json
[
  {
    "seriesId": "uuid",
    "patientName": "string",
    "modality": "CT",
    "studyDate": "2026-05-10",
    "sliceCount": 256,
    "createdAt": "2026-05-10T10:00:00Z"
  }
]
```

#### GET /api/dicom/series/{id}
Get series details and metadata.

#### DELETE /api/dicom/series/{id}
Delete a series and its files.

---

### Annotations (Future)

#### POST /api/annotations
Create an annotation.

#### GET /api/annotations?seriesId={id}
Get all annotations for a series.

#### PUT /api/annotations/{id}
Update an annotation.

#### DELETE /api/annotations/{id}
Delete an annotation.

---

## WebSocket Protocol

### Endpoint: `/ws/render`

All messages from client to server are JSON. All messages from server to client are binary frames.

### Client → Server Messages

**Design**: The frontend sends raw input events (mouse state). The backend owns camera state and handles all rendering logic.

#### input
Forward mouse/pointer events to the backend. The backend uses these deltas to update its internal camera state.

```json
{
  "type": "input",
  "payload": {
    "action": "rotate",
    "mouse": {
      "dx": 5.2,
      "dy": -3.1,
      "buttons": 1,
      "scrollDelta": 0
    },
    "canvasSize": { "w": 800, "h": 600 }
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| action | string | Pointer mode: `"rotate"`, `"pan"`, `"zoom"`, `"windowing"` |
| mouse.dx | float | Mouse X delta in pixels since last event |
| mouse.dy | float | Mouse Y delta in pixels since last event |
| mouse.buttons | int | Bitmask: 0=none, 1=left, 2=right, 4=middle |
| mouse.scrollDelta | float | Scroll wheel delta |
| canvasSize.w | int | Canvas width in pixels (for delta normalization) |
| canvasSize.h | int | Canvas height in pixels |

#### render_params
Update rendering parameters.

```json
{
  "type": "render_params",
  "payload": {
    "transferFunction": "bone",
    "threshold": 300,
    "opacity": 0.8
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| transferFunction | string | Preset name: `"bone"`, `"soft_tissue"`, `"skin"`, `"custom"` |
| threshold | int | Density threshold (HU value) |
| opacity | float | Overall opacity multiplier (0.0-1.0) |

#### load_series
Load a DICOM series for rendering.

```json
{
  "type": "load_series",
  "payload": {
    "seriesId": "uuid"
  }
}
```

### Server → Client Messages

#### Binary Frame Format
```
[4 bytes: frame sequence number (big-endian uint32)]
[N bytes: JPEG encoded image data]
```

The sequence number allows the client to detect and skip stale frames (e.g., if a newer frame arrives before an older one is displayed).

#### error
```json
{
  "type": "error",
  "payload": {
    "code": "SERIES_NOT_FOUND",
    "message": "The requested series does not exist"
  }
}
```
