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
} from '@mui/material';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import theme from './theme/theme';

interface DicomResult {
  patientName: string;
  modality: string;
  width: number;
  height: number;
  bitsAllocated: number;
  windowCenter: number;
  windowWidth: number;
  imageBase64: string;
}

function App() {
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<DicomResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setLoading(true);
    setError(null);
    setResult(null);

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
                <Typography><strong>Bits:</strong> {result.bitsAllocated}</Typography>
                <Typography><strong>W/L:</strong> {result.windowWidth} / {result.windowCenter}</Typography>
              </Box>

              <Typography variant="h6" gutterBottom>
                Image
              </Typography>
              <Box sx={{ textAlign: 'center', bgcolor: '#000', p: 2, borderRadius: 1 }}>
                <img
                  src={`data:image/png;base64,${result.imageBase64}`}
                  alt="DICOM"
                  style={{ maxWidth: '100%', maxHeight: '600px', imageRendering: 'pixelated' }}
                />
              </Box>
            </Paper>
          )}
        </Container>
      </Box>
    </ThemeProvider>
  );
}

export default App;
