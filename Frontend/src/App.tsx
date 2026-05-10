import { useState } from 'react';
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
  ToggleButton,
  ToggleButtonGroup,
} from '@mui/material';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import ThreeDRotationIcon from '@mui/icons-material/ThreeDRotation';
import theme from './theme/theme';

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
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed');
    } finally {
      setLoading(false);
    }
  };

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
            <Typography variant="h6">YdcViewer - DICOM Viewer</Typography>
          </Toolbar>
        </AppBar>

        <Container maxWidth="md" sx={{ py: 4 }}>
          <Paper sx={{ p: 3, mb: 3 }}>
            <Typography variant="h6" gutterBottom>
              Upload DICOM File
            </Typography>
            <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
              <Button
                variant="contained"
                component="label"
                startIcon={loading ? <CircularProgress size={20} /> : <CloudUploadIcon />}
                disabled={loading}
              >
                {loading ? 'Processing...' : 'Select DICOM File'}
                <input
                  type="file"
                  hidden
                  accept=".dcm,.dicom,*"
                  onChange={handleUpload}
                />
              </Button>

              {result && (
                <Button
                  variant="outlined"
                  startIcon={rendering3D ? <CircularProgress size={20} /> : <ThreeDRotationIcon />}
                  onClick={handleRender3D}
                  disabled={rendering3D}
                >
                  {rendering3D ? 'Rendering...' : '3D Render'}
                </Button>
              )}

              {result && (
                <ToggleButtonGroup
                  value={viewMode}
                  exclusive
                  onChange={(_, v) => v && setViewMode(v)}
                  size="small"
                >
                  <ToggleButton value="2d">2D</ToggleButton>
                  <ToggleButton value="3d" disabled={!image3D}>3D</ToggleButton>
                </ToggleButtonGroup>
              )}
            </Box>

            {result && (
              <Box sx={{ mt: 2 }}>
                <Typography variant="body2" color="text.secondary">
                  Transfer Function:
                </Typography>
                <ToggleButtonGroup
                  value={transferFn}
                  exclusive
                  onChange={(_, v) => v && setTransferFn(v)}
                  size="small"
                  sx={{ mt: 1 }}
                >
                  <ToggleButton value="default">Default</ToggleButton>
                  <ToggleButton value="bone">Bone</ToggleButton>
                  <ToggleButton value="soft_tissue">Soft Tissue</ToggleButton>
                </ToggleButtonGroup>
              </Box>
            )}
          </Paper>

          {error && (
            <Alert severity="error" sx={{ mb: 3 }}>
              {error}
            </Alert>
          )}

          {result && (
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" gutterBottom>
                Result
              </Typography>
              <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1, mb: 2 }}>
                <Typography><strong>Patient:</strong> {result.patientName || 'N/A'}</Typography>
                <Typography><strong>Modality:</strong> {result.modality || 'N/A'}</Typography>
                <Typography><strong>Size:</strong> {result.width} x {result.height}</Typography>
                <Typography><strong>Slices:</strong> {result.sliceCount}</Typography>
                <Typography><strong>Bits:</strong> {result.bitsAllocated}</Typography>
                <Typography><strong>W/L:</strong> {result.windowWidth} / {result.windowCenter}</Typography>
              </Box>

              <Typography variant="h6" gutterBottom>
                {viewMode === '3d' ? '3D Volume Rendering' : '2D Slice'}
              </Typography>
              <Box sx={{ textAlign: 'center', bgcolor: '#000', p: 2, borderRadius: 1 }}>
                {viewMode === '3d' && image3D ? (
                  <img
                    src={image3D}
                    alt="3D Volume"
                    style={{ maxWidth: '100%', maxHeight: '600px' }}
                  />
                ) : (
                  <img
                    src={`data:image/png;base64,${result.imageBase64}`}
                    alt="DICOM"
                    style={{ maxWidth: '100%', maxHeight: '600px', imageRendering: 'pixelated' }}
                  />
                )}
              </Box>
            </Paper>
          )}
        </Container>
      </Box>
    </ThemeProvider>
  );
}

export default App;
