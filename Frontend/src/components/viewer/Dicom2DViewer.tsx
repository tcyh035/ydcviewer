import { useRef, useState, useCallback, useEffect } from 'react';
import { Box } from '@mui/material';

type ToolMode = 'pan' | 'rotate' | 'zoom' | 'windowing' | 'annotate';

interface Dicom2DViewerProps {
  src: string;
  alt?: string;
  cursor?: string;
  tool?: ToolMode;
  onWindowingChange?: (deltaWidth: number, deltaCenter: number) => void;
}

export default function Dicom2DViewer({
  src,
  alt,
  cursor = 'grab',
  tool = 'pan',
  onWindowingChange,
}: Dicom2DViewerProps) {
  const [scale, setScale] = useState(1);
  const [offset, setOffset] = useState({ x: 0, y: 0 });
  const [dragging, setDragging] = useState(false);
  const [dragStart, setDragStart] = useState({ x: 0, y: 0 });
  const [offsetStart, setOffsetStart] = useState({ x: 0, y: 0 });

  const handleWheel = useCallback((e: React.WheelEvent) => {
    e.preventDefault();
    const delta = e.deltaY > 0 ? 0.9 : 1.1;
    setScale(s => Math.max(0.1, Math.min(20, s * delta)));
  }, []);

  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    if (e.button !== 0) return;
    setDragging(true);
    setDragStart({ x: e.clientX, y: e.clientY });
    setOffsetStart({ ...offset });
  }, [offset]);

  const handleMouseMove = useCallback((e: React.MouseEvent) => {
    if (!dragging) return;
    const dx = e.clientX - dragStart.x;
    const dy = e.clientY - dragStart.y;

    switch (tool) {
      case 'pan':
        setOffset({ x: offsetStart.x + dx, y: offsetStart.y + dy });
        break;
      case 'zoom': {
        const zoomFactor = 1 + dy * -0.005;
        setScale(s => Math.max(0.1, Math.min(20, s * zoomFactor)));
        break;
      }
      case 'windowing':
        onWindowingChange?.(dx * 2, dy * -2);
        break;
      case 'annotate':
        // TODO: draw annotation line
        break;
    }
  }, [dragging, dragStart, offsetStart, tool, onWindowingChange]);

  const handleMouseUp = useCallback(() => {
    setDragging(false);
  }, []);

  const handleDoubleClick = useCallback(() => {
    setScale(1);
    setOffset({ x: 0, y: 0 });
  }, []);

  useEffect(() => {
    setScale(1);
    setOffset({ x: 0, y: 0 });
  }, [src]);

  return (
    <Box
      sx={{
        width: '100%',
        height: '100%',
        overflow: 'hidden',
        bgcolor: '#000',
        cursor: dragging ? (tool === 'zoom' ? 'zoom-in' : tool === 'windowing' ? 'col-resize' : cursor) : cursor,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        position: 'relative',
        userSelect: 'none',
      }}
      onWheel={handleWheel}
      onMouseDown={handleMouseDown}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUp}
      onMouseLeave={handleMouseUp}
      onDoubleClick={handleDoubleClick}
    >
      <img
        src={src}
        alt={alt || 'DICOM'}
        draggable={false}
        style={{
          transform: `translate(${offset.x}px, ${offset.y}px) scale(${scale})`,
          maxWidth: '100%',
          maxHeight: '100%',
          imageRendering: 'pixelated',
        }}
      />
      <Box
        sx={{
          position: 'absolute',
          bottom: 8,
          right: 8,
          color: 'rgba(255,255,255,0.4)',
          fontSize: 12,
          pointerEvents: 'none',
        }}
      >
        {Math.round(scale * 100)}%
      </Box>
    </Box>
  );
}
