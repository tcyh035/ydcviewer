import { Box, Typography } from '@mui/material';

export default function AnnotationsPanel() {
  return (
    <Box sx={{ p: 2, flex: 1 }}>
      <Typography variant="subtitle2" gutterBottom>Annotations</Typography>
      <Typography variant="body2" color="text.secondary">
        No annotations yet
      </Typography>
      <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
        Select the Annotate tool and drag on the image to create measurements.
      </Typography>
    </Box>
  );
}
