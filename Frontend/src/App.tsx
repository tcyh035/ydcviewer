import { useState, useEffect, useCallback } from 'react';
import {
  ThemeProvider,
  CssBaseline,
  AppBar,
  Toolbar,
  Typography,
  Box,
  Button,
  Paper,
  CircularProgress,
  Alert,
  Slider,
  ToggleButton,
  ToggleButtonGroup,
  Divider,
} from '@mui/material';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import ThreeDRotationIcon from '@mui/icons-material/ThreeDRotation';
import PanToolIcon from '@mui/icons-material/PanTool';
import ZoomInIcon from '@mui/icons-material/ZoomIn';
import TuneIcon from '@mui/icons-material/Tune';
import StraightenIcon from '@mui/icons-material/Straighten';
import theme from './theme/theme';
import Dicom2DViewer from './components/viewer/Dicom2DViewer';
import PatientInfoPanel from './components/panels/PatientInfoPanel';
import WindowLevelPanel from './components/panels/WindowLevelPanel';
import TransferFunctionPanel from './components/panels/TransferFunctionPanel';
import AnnotationsPanel from './components/panels/AnnotationsPanel';
import type { ToolMode, ViewMode, DicomResult, WLPreset } from './types';

const CURSOR_MAP: Record<ToolMode, string> = {
  pan: 'grab',
  rotate: 'crosshair',
  zoom: 'zoom-in',
  windowing: 'col-resize',
  annotate: 'crosshair',
};

const WL_PRESETS: WLPreset[] = [
  { name: 'Default', wc: 0, ww: 0 },
  { name: 'CT Bone', wc: 400, ww: 1800 },
  { name: 'CT Soft', wc: 40, ww: 400 },
  { name: 'CT Lung', wc: -600, ww: 1500 },
  { name: 'CT Brain', wc: 40, ww: 80 },
  { name: 'CT Abdomen', wc: 60, ww: 400 },
];

function App() {
  const [loading, setLoading] = useState(false);
  const [rendering3D, setRendering3D] = useState(false);
  const [result, setResult] = useState<DicomResult | null>(null);
  const [image3D, setImage3D] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [transferFn, setTransferFn] = useState('default');
  const [viewMode, setViewMode] = useState<ViewMode>('2d');
  const [tool, setTool] = useState<ToolMode>('pan');

  // 2D viewer state
  const [sliceIndex, setSliceIndex] = useState(0);
  const [windowCenter, setWindowCenter] = useState(0);
  const [windowWidth, setWindowWidth] = useState(0);
  const [currentImageSrc, setCurrentImageSrc] = useState('');
  const [sliceLoading, setSliceLoading] = useState(false);

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files || files.length === 0) return;

    setLoading(true);
    setError(null);
    setResult(null);
    setImage3D(null);

    const formData = new FormData();
    for (let i = 0; i < files.length; i++) {
      formData.append('files', files[i]);
    }

    try {
      const res = await fetch('/api/dicom/upload', { method: 'POST', body: formData });
      if (!res.ok) throw new Error(`Upload failed: ${res.status} ${res.statusText}`);
      const data = await res.json();
      setResult(data);
      setSliceIndex(0);
      setWindowCenter(data.windowCenter);
      setWindowWidth(data.windowWidth);
      setCurrentImageSrc(`data:image/png;base64,${data.imageBase64}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed');
    } finally {
      setLoading(false);
    }
  };

  const fetchSlice = useCallback(async (index: number, wc: number, ww: number) => {
    if (!result?.seriesId) return;
    setSliceLoading(true);
    try {
      const url = `/api/dicom/series/${result.seriesId}/slice/${index}?windowCenter=${wc}&windowWidth=${ww}`;
      const blob = await (await fetch(url)).blob();
      const oldSrc = currentImageSrc;
      setCurrentImageSrc(URL.createObjectURL(blob));
      if (oldSrc.startsWith('blob:')) URL.revokeObjectURL(oldSrc);
    } catch { /* ignore */ } finally {
      setSliceLoading(false);
    }
  }, [result?.seriesId, currentImageSrc]);

  useEffect(() => {
    if (result?.seriesId && result.sliceCount > 0) {
      fetchSlice(sliceIndex, windowCenter, windowWidth);
    }
  }, [sliceIndex, windowCenter, windowWidth]);

  const handleRender3D = async () => {
    if (!result?.seriesId) return;
    setRendering3D(true);
    setError(null);
    try {
      const res = await fetch('/api/dicom/render3d', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          seriesId: result.seriesId, width: 512, height: 512,
          yaw: 30, pitch: 20, transferFunction: transferFn,
        }),
      });
      if (!res.ok) throw new Error(`3D render failed: ${res.status}`);
      const blob = await res.blob();
      setImage3D(URL.createObjectURL(blob));
      setViewMode('3d');
      setTool('rotate');
    } catch (err) {
      setError(err instanceof Error ? err.message : '3D render failed');
    } finally {
      setRendering3D(false);
    }
  };

  const activePreset = WL_PRESETS.find(p => p.wc === windowCenter && p.ww === windowWidth);

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Box sx={{ height: '100vh', display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        {/* Top Bar */}
        <AppBar position="static" sx={{ bgcolor: '#1a1a2e' }}>
          <Toolbar variant="dense" sx={{ gap: 2 }}>
            <Typography variant="h6" sx={{ flexGrow: 0 }}>YdcViewer</Typography>
            <Button
              variant="contained"
              size="small"
              component="label"
              startIcon={loading ? <CircularProgress size={16} /> : <CloudUploadIcon />}
              disabled={loading}
            >
              {loading ? 'Uploading...' : 'Upload DICOM'}
              <input type="file" hidden accept=".dcm,.dicom,*" multiple onChange={handleUpload} />
            </Button>
            {result && (
              <>
                <Button
                  variant="outlined"
                  size="small"
                  startIcon={rendering3D ? <CircularProgress size={16} /> : <ThreeDRotationIcon />}
                  onClick={handleRender3D}
                  disabled={rendering3D}
                >
                  3D Render
                </Button>
                <Divider orientation="vertical" flexItem sx={{ borderColor: 'rgba(255,255,255,0.2)' }} />
                <Typography variant="body2" color="text.secondary">
                  {result.patientName || 'N/A'} | {result.modality} | {result.sliceCount} slices
                </Typography>
              </>
            )}
          </Toolbar>
        </AppBar>

        {/* Toolbar */}
        {result && (
          <Paper sx={{ display: 'flex', alignItems: 'center', gap: 1, px: 2, py: 0.5, borderRadius: 0, borderBottom: 1, borderColor: 'divider' }}>
            <ToggleButtonGroup
              value={tool}
              exclusive
              onChange={(_, v) => v && setTool(v)}
              size="small"
            >
              <ToggleButton value="pan" title="Pan (drag to move image)">
                <PanToolIcon fontSize="small" />
              </ToggleButton>
              {viewMode === '3d' && (
                <ToggleButton value="rotate" title="Rotate (drag to rotate 3D view)">
                  <ThreeDRotationIcon fontSize="small" />
                </ToggleButton>
              )}
              <ToggleButton value="zoom" title="Zoom (drag or scroll to zoom)">
                <ZoomInIcon fontSize="small" />
              </ToggleButton>
              <ToggleButton value="windowing" title="Window/Level (drag to adjust brightness)">
                <TuneIcon fontSize="small" />
              </ToggleButton>
              <ToggleButton value="annotate" title="Annotate (click and drag to measure)">
                <StraightenIcon fontSize="small" />
              </ToggleButton>
            </ToggleButtonGroup>

            <Divider orientation="vertical" flexItem />

            <ToggleButtonGroup
              value={viewMode}
              exclusive
              onChange={(_, v) => { if (v) { setViewMode(v); setTool(v === '3d' ? 'rotate' : 'pan'); } }}
              size="small"
            >
              <ToggleButton value="2d">2D</ToggleButton>
              <ToggleButton value="3d" disabled={!image3D}>3D</ToggleButton>
            </ToggleButtonGroup>

            <Divider orientation="vertical" flexItem />

            <Typography variant="caption" color="text.secondary" sx={{ ml: 1 }}>
              {tool === 'pan' && 'Drag to pan'}
              {tool === 'rotate' && 'Drag to rotate 3D'}
              {tool === 'zoom' && 'Drag/scroll to zoom'}
              {tool === 'windowing' && 'Drag: horizontal=width, vertical=center'}
              {tool === 'annotate' && 'Click and drag to measure'}
            </Typography>
          </Paper>
        )}

        {/* Main Content */}
        <Box sx={{ flex: 1, display: 'flex', overflow: 'hidden' }}>
          {/* Viewport */}
          <Box sx={{ flex: 1, position: 'relative', overflow: 'hidden' }}>
            {!result ? (
              <Box sx={{ height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', flexDirection: 'column', gap: 2 }}>
                <Typography variant="h5" color="text.secondary">Upload a DICOM file to start</Typography>
                <Typography variant="body2" color="text.secondary">
                  Supports .dcm files. Select multiple files to load a series.
                </Typography>
              </Box>
            ) : viewMode === '2d' ? (
              <Dicom2DViewer
                src={currentImageSrc}
                cursor={CURSOR_MAP[tool]}
                tool={tool}
                onWindowingChange={(dw, dc) => {
                  setWindowWidth(w => Math.max(1, Math.round(w + dw)));
                  setWindowCenter(c => Math.round(c + dc));
                }}
              />
            ) : image3D ? (
              <Box sx={{ height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: '#000' }}>
                <img src={image3D} alt="3D" style={{ maxWidth: '100%', maxHeight: '100%' }} />
              </Box>
            ) : null}

            {sliceLoading && (
              <CircularProgress size={20} sx={{ position: 'absolute', top: 8, right: 8, color: 'rgba(255,255,255,0.4)' }} />
            )}
          </Box>

          {/* Right Panel */}
          {result && (
            <Paper sx={{ width: 240, borderRadius: 0, borderLeft: 1, borderColor: 'divider', overflow: 'auto', display: 'flex', flexDirection: 'column' }}>
              <PatientInfoPanel result={result} />
              <Divider />

              {/* Window/Level */}
              <WindowLevelPanel
                windowCenter={windowCenter}
                windowWidth={windowWidth}
                presets={WL_PRESETS}
                activePreset={activePreset}
                onWindowCenterChange={setWindowCenter}
                onWindowWidthChange={setWindowWidth}
                onPresetSelect={(p) => { setWindowCenter(p.wc); setWindowWidth(p.ww); }}
              />

              <Divider />

              {/* Transfer Function (3D) */}
              {viewMode === '3d' && (
                <TransferFunctionPanel
                  value={transferFn}
                  onChange={setTransferFn}
                />
              )}

              {/* Annotations */}
              <AnnotationsPanel />
            </Paper>
          )}
        </Box>

        {/* Bottom Bar */}
        {result && result.sliceCount > 1 && viewMode === '2d' && (
          <Paper sx={{ display: 'flex', alignItems: 'center', gap: 2, px: 2, py: 0.5, borderRadius: 0, borderTop: 1, borderColor: 'divider' }}>
            <Typography variant="caption" color="text.secondary">Slice:</Typography>
            <Slider
              value={sliceIndex}
              min={0}
              max={result.sliceCount - 1}
              step={1}
              onChange={(_, v) => setSliceIndex(v as number)}
              size="small"
              sx={{ flex: 1 }}
              valueLabelDisplay="auto"
            />
            <Typography variant="caption" color="text.secondary" sx={{ minWidth: 60, textAlign: 'right' }}>
              {sliceIndex + 1} / {result.sliceCount}
            </Typography>
          </Paper>
        )}

        {/* Error Toast */}
        {error && (
          <Alert
            severity="error"
            onClose={() => setError(null)}
            sx={{ position: 'fixed', bottom: 60, left: '50%', transform: 'translateX(-50%)', zIndex: 9999 }}
          >
            {error}
          </Alert>
        )}
      </Box>
    </ThemeProvider>
  );
}

export default App;
