import { Box, Typography, Divider } from '@mui/material';
import type { DicomResult } from '../../types';

interface PatientInfoPanelProps {
  result: DicomResult;
}

export default function PatientInfoPanel({ result }: PatientInfoPanelProps) {
  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="subtitle2" gutterBottom>Patient Info</Typography>
      <Box sx={{ display: 'grid', gridTemplateColumns: 'auto 1fr', gap: '2px 8px', fontSize: 13 }}>
        <Typography variant="body2" color="text.secondary">Name:</Typography>
        <Typography variant="body2">{result.patientName || 'N/A'}</Typography>
        <Typography variant="body2" color="text.secondary">Modality:</Typography>
        <Typography variant="body2">{result.modality}</Typography>
        <Typography variant="body2" color="text.secondary">Size:</Typography>
        <Typography variant="body2">{result.width} x {result.height}</Typography>
        <Typography variant="body2" color="text.secondary">Slices:</Typography>
        <Typography variant="body2">{result.sliceCount}</Typography>
        <Typography variant="body2" color="text.secondary">Bits:</Typography>
        <Typography variant="body2">{result.bitsAllocated}</Typography>
      </Box>
    </Box>
  );
}
