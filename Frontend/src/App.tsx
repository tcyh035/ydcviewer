import { useState, useEffect, useCallback } from 'react';
import {
  ThemeProvider,
  CssBaseline,
  AppBar,
  Toolbar,
  Typography,
  Container,
  Box,
  Button,
  Paper,
  CircularProgress,
  Alert,
  Slider,
} from '@mui/material';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import ThreeDRotationIcon from '@mui/icons-material/ThreeDRotation';
import theme from './theme/theme';
import Dicom2DViewer from './components/viewer/Dicom2DViewer';

interface DicomResult {
  seriesId: string;
  patientName: string;
  modality: string;
  width: number;
  height: number;
  bitsAllocated: number;
  windowCenter: number;
  windowWidth: number;
  sliceCount: number;
  imageBase64: string;
}

function App() {
  const [loading, setLoading] = useState(false);
  const [rendering3D, setRendering3D] = useState(false);
  const [result, setResult] = useState<DicomResult | null>(null);
  const [image3D, setImage3D] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [transferFn, setTransferFn] = useState('default');
  const [viewMode, setViewMode] = useState<'2d' | '3d'>('2d');

  // 2D viewer state
  const [sliceIndex, setSliceIndex] = useState(0);
  const [windowCenter, setWindowCenter] = useState(0);
  const [windowWidth, setWindowWidth] = useState(0);
  const [currentImageSrc, setCurrentImageSrc] = useState('');
  const [sliceLoading, setSliceLoading] = useState(false);

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setLoading(true);
    setError(null);
    setResult(null);
    setImage3D(null);

    const formData = new FormData();
    formData.append('file', file);

    try {
      const res = await fetch('/api/dicom/upload', {
        method: 'POST',
        body: formData,
      });

      if (!res.ok) {
        throw new Error(`Upload failed: ${res.status} ${res.statusText}`);
      }

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
    } catch {
      // ignore
    } finally {
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
          seriesId: result.seriesId,
          width: 512,
          height: 512,
          yaw: 30,
          pitch: 20,
          transferFunction: transferFn,
        }),
      });

      if (!res.ok) {
        throw new Error(`3D render failed: ${res.status}`);
      }

      const blob = await res.blob();
      setImage3D(URL.createObjectURL(blob));
      setViewMode('3d');
    } catch (err) {
      setError(err instanceof Error ? err.message : '3D render failed');
    } finally {
      setRendering3D(false);
    }
  };

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
        <AppBar position="static">
          <Toolbar>
            <Typography variant="h6">YdcViewer</Typography>
          </Toolbar>
        </AppBar>

        <Box sx={{ display: 'flex', height: 'calc(100vh - 64px)' }}>
          {/* Main viewer area */}
          <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
            {/* Toolbar */}
            <Paper sx={{ p: 1.5, display: 'flex', gap: 2, alignItems: 'center', borderRadius: 0 }}>
              <Button
                variant="contained"
                component="label"
                size="small"
                startIcon={loading ? <CircularProgress size={16} /> : <CloudUploadIcon />}
                disabled={loading}
              >
                {loading ? 'Processing...' : 'Upload'}
                <input type="file" hidden accept=".dcm,.dicom,*" onChange={handleUpload} />
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
                    3D
                  </Button>
                  <Box sx={{ flex: 1 }} />
                  <Typography variant="body2" color="text.secondary">
                    {viewMode === '2d'
                      ? `Slice ${sliceIndex + 1} / ${result.sliceCount}`
                      : '3D View'}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {result.patientName || 'N/A'} | {result.modality}
                  </Typography>
                </>
              )}
            </Paper>

            {/* Image area */}
            <Box sx={{ flex: 1, position: 'relative', overflow: 'hidden' }}>
              {!result ? (
                <Box sx={{ height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <Typography color="text.secondary">Upload a DICOM file to start</Typography>
                </Box>
              ) : viewMode === '2d' ? (
                <Dicom2DViewer src={currentImageSrc} alt="DICOM slice" />
              ) : image3D ? (
                <Box sx={{ height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: '#000' }}>
                  <img src={image3D} alt="3D" style={{ maxWidth: '100%', maxHeight: '100%' }} />
                </Box>
              ) : null}

              {sliceLoading && (
                <CircularProgress
                  size={24}
                  sx={{ position: 'absolute', top: 8, right: 8, color: 'rgba(255,255,255,0.5)' }}
                />
              )}
            </Box>

            {/* Slice slider */}
            {result && result.sliceCount > 1 && viewMode === '2d' && (
              <Paper sx={{ p: 1, borderRadius: 0 }}>
                <Box sx={{ px: 2 }}>
                  <Typography variant="caption" color="text.secondary">
                    Slice
                  </Typography>
                  <Slider
                    value={sliceIndex}
                    min={0}
                    max={result.sliceCount - 1}
                    step={1}
                    onChange={(_, v) => setSliceIndex(v as number)}
                    size="small"
                    valueLabelDisplay="auto"
                  />
                </Box>
              </Paper>
            )}
          </Box>

          {/* Right panel: Window/Level controls */}
          {result && (
            <Paper sx={{ width: 220, borderRadius: 0, p: 2, overflow: 'auto' }}>
              <Typography variant="subtitle2" gutterBottom>Window / Level</Typography>

              <Typography variant="caption" color="text.secondary">Center</Typography>
              <Slider
                value={windowCenter}
                min={-1024}
                max={3071}
                step={1}
                onChange={(_, v) => setWindowCenter(v as number)}
                size="small"
              />

              <Typography variant="caption" color="text.secondary">Width</Typography>
              <Slider
                value={windowWidth}
                min={1}
                max={4096}
                step={1}
                onChange={(_, v) => setWindowWidth(v as number)}
                size="small"
              />

              <Typography variant="subtitle2" gutterBottom sx={{ mt: 2 }}>Presets</Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5 }}>
                {[
                  { name: 'Default', wc: result.windowCenter, ww: result.windowWidth },
                  { name: 'CT Bone', wc: 400, ww: 1800 },
                  { name: 'CT Soft', wc: 40, ww: 400 },
                  { name: 'CT Lung', wc: -600, ww: 1500 },
                  { name: 'CT Brain', wc: 40, ww: 80 },
                  { name: 'CT Abdomen', wc: 60, ww: 400 },
                ].map(p => (
                  <Button
                    key={p.name}
                    size="small"
                    variant={windowCenter === p.wc && windowWidth === p.ww ? 'contained' : 'text'}
                    onClick={() => { setWindowCenter(p.wc); setWindowWidth(p.ww); }}
                    sx={{ justifyContent: 'flex-start', textTransform: 'none' }}
                  >
                    {p.name}
                  </Button>
                ))}
              </Box>

              <Box sx={{ mt: 3 }}>
                <Typography variant="caption" color="text.secondary">
                  Mouse Controls
                </Typography>
                <Typography variant="body2" sx={{ mt: 0.5 }}>
                  Drag: Pan<br />
                  Scroll: Zoom<br />
                  Double-click: Reset
                </Typography>
              </Box>
            </Paper>
          )}
        </Box>

        {error && (
          <Alert severity="error" sx={{ position: 'fixed', bottom: 16, left: '50%', transform: 'translateX(-50%)' }}>
            {error}
          </Alert>
        )}
      </Box>
    </ThemeProvider>
  );
}

export default App;
