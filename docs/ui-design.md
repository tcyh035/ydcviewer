# UI Design - YdcViewer

## Layout Overview

```
┌──────────────────────────────────────────────────────────────────────────┐
│ □ YdcViewer              [Upload] [Patient: Zhang San | CT | 2026-05-11]│
├──────────────────────────────────────────────────────────────────────────┤
│ [↔Pan] [⤢Rotate] [⊕Zoom] [◎W/L] [✎Annotate] | [2D|3D] | [Bone|Soft] │
├────────────────────────────────────────────────────────┬─────────────────┤
│                                                        │ Patient Info    │
│                                                        │ ─────────────── │
│                                                        │ Name: Zhang San │
│                                                        │ ID:   P001234   │
│                                                        │ Modality: CT    │
│                                                        │ Study: 2026-05  │
│                                                        │ Series: Head    │
│                                                        │ Size: 512×512   │
│                                                        │ Slices: 128     │
│                                                        │ ─────────────── │
│                     DICOM IMAGE                        │ W/L Controls    │
│                                                        │ ─────────────── │
│                   (main viewport)                      │ Center: 40      │
│                                                        │ ━━━━━━━━●━━━━━━ │
│                                                        │ Width:  400     │
│                                                        │ ━━━━━━━━━━●━━━━ │
│                                                        │ ─────────────── │
│                                                        │ Presets         │
│                                                        │ ○ Default       │
│                                                        │ ○ CT Bone       │
│                                                        │ ○ CT Soft       │
│                                                        │ ○ CT Lung       │
│                                                        │ ○ CT Brain      │
│                                                        │ ─────────────── │
│                                                        │ Annotations     │
│                                                        │ (list)          │
├────────────────────────────────────────────────────────┴─────────────────┤
│ Slice: ━━━━━━━━━━━━━━━━━━●━━━━━━━━━━━━━━━━━━━━━   45 / 128    100%     │
└──────────────────────────────────────────────────────────────────────────┘
```

## Toolbar Actions (Radio Buttons)

| Action     | Icon | Mouse Cursor    | Left Drag       | Right Drag    | Scroll     |
|------------|------|-----------------|-----------------|---------------|------------|
| Pan        | ↔    | `grab`          | Pan image       | -             | -          |
| Rotate     | ⚢    | `crosshair`     | Rotate 3D view  | -             | -          |
| Zoom       | ⊕    | `zoom-in`       | Zoom in/out     | -             | Zoom       |
| Window/Level| ◎   | `col-resize`    | Adjust W/L      | -             | -          |
| Annotate   | ✎    | `crosshair`     | Draw annotation | -             | -          |

## Mouse Behavior by Mode

### 2D Mode
- **Pan**: Left drag moves image, scroll zooms
- **Zoom**: Left drag zooms (up=in, down=out), scroll zooms
- **W/L**: Left drag horizontal=width, vertical=center
- **Annotate**: Left click+drag to draw measurement line

### 3D Mode
- **Rotate**: Left drag rotates 3D model
- **Pan**: Left drag pans camera
- **Zoom**: Left drag zooms, scroll zooms
- **W/L**: Left drag adjusts transfer function threshold

## Right Panel Sections

1. **Patient Info** - Always visible, shows DICOM metadata
2. **W/L Controls** - Sliders for window center/width, preset buttons
3. **Annotations** - List of measurements/annotations (future)

## Color Scheme (Dark Theme)

- Background: `#121212`
- Panel: `#1e1e1e`
- Image viewport: `#000000`
- Active tool: `#90caf9` (blue highlight)
- Text primary: `#ffffff`
- Text secondary: `#9ca3af`
