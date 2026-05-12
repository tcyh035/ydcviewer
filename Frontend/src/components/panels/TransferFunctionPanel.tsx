import { Box, Typography, ToggleButton, ToggleButtonGroup, Divider } from '@mui/material';

interface TransferFunctionPanelProps {
  value: string;
  onChange: (value: string) => void;
}

export default function TransferFunctionPanel({ value, onChange }: TransferFunctionPanelProps) {
  return (
    <>
      <Box sx={{ p: 2 }}>
        <Typography variant="subtitle2" gutterBottom>Transfer Function</Typography>
        <ToggleButtonGroup
          value={value}
          exclusive
          onChange={(_, v) => v && onChange(v)}
          size="small"
          orientation="vertical"
          fullWidth
        >
          <ToggleButton value="default" sx={{ textTransform: 'none' }}>Default</ToggleButton>
          <ToggleButton value="bone" sx={{ textTransform: 'none' }}>Bone</ToggleButton>
          <ToggleButton value="soft_tissue" sx={{ textTransform: 'none' }}>Soft Tissue</ToggleButton>
        </ToggleButtonGroup>
      </Box>
      <Divider />
    </>
  );
}
