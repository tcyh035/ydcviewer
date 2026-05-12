import { Box, Typography, Slider, Divider } from '@mui/material';
import PresetsPanel from './PresetsPanel';
import type { WLPreset } from '../../types';

interface WindowLevelPanelProps {
  windowCenter: number;
  windowWidth: number;
  presets: WLPreset[];
  activePreset?: WLPreset;
  onWindowCenterChange: (value: number) => void;
  onWindowWidthChange: (value: number) => void;
  onPresetSelect: (preset: WLPreset) => void;
}

export default function WindowLevelPanel({
  windowCenter,
  windowWidth,
  presets,
  activePreset,
  onWindowCenterChange,
  onWindowWidthChange,
  onPresetSelect,
}: WindowLevelPanelProps) {
  return (
    <>
      <Box sx={{ p: 2 }}>
        <Typography variant="subtitle2" gutterBottom>Window / Level</Typography>
        <Typography variant="caption" color="text.secondary">Center</Typography>
        <Slider
          value={windowCenter}
          min={-1024}
          max={3071}
          step={1}
          onChange={(_, v) => onWindowCenterChange(v as number)}
          size="small"
        />
        <Typography variant="caption" color="text.secondary">Width</Typography>
        <Slider
          value={windowWidth}
          min={1}
          max={4096}
          step={1}
          onChange={(_, v) => onWindowWidthChange(v as number)}
          size="small"
        />
      </Box>
      <Divider />
      <PresetsPanel
        presets={presets}
        activePreset={activePreset}
        onSelect={onPresetSelect}
      />
    </>
  );
}
