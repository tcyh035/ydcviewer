import { Box, Typography, Button } from '@mui/material';
import type { WLPreset } from '../../types';

interface PresetsPanelProps {
  presets: WLPreset[];
  activePreset?: WLPreset;
  onSelect: (preset: WLPreset) => void;
}

export default function PresetsPanel({ presets, activePreset, onSelect }: PresetsPanelProps) {
  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="subtitle2" gutterBottom>Presets</Typography>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5 }}>
        {presets.map(p => (
          <Button
            key={p.name}
            size="small"
            variant={activePreset?.name === p.name ? 'contained' : 'text'}
            onClick={() => onSelect(p)}
            sx={{ justifyContent: 'flex-start', textTransform: 'none', fontSize: 13 }}
          >
            {p.name}
          </Button>
        ))}
      </Box>
    </Box>
  );
}
