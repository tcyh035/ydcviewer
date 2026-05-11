import { useRef, useState, useCallback, useEffect } from 'react';
import { Box } from '@mui/material';

interface Dicom2DViewerProps {
  src: string;
  alt?: string;
}

export default function Dicom2DViewer({ src, alt }: Dicom2DViewerProps) {
  const containerRef = useRef<HTMLDivElement>(null);
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
    if (e.button === 0) {
      setDragging(true);
      setDragStart({ x: e.clientX, y: e.clientY });
      setOffsetStart({ ...offset });
    }
  }, [offset]);

  const handleMouseMove = useCallback((e: React.MouseEvent) => {
    if (dragging) {
      setOffset({
        x: offsetStart.x + (e.clientX - dragStart.x),
        y: offsetStart.y + (e.clientY - dragStart.y),
      });
    }
  }, [dragging, dragStart, offsetStart]);

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
      ref={containerRef}
      sx={{
        width: '100%',
        height: '100%',
        overflow: 'hidden',
        bgcolor: '#000',
        cursor: dragging ? 'grabbing' : 'grab',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        position: 'relative',
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
          userSelect: 'none',
        }}
      />
      <Box
        sx={{
          position: 'absolute',
          bottom: 8,
          right: 8,
          color: 'rgba(255,255,255,0.5)',
          fontSize: 12,
          pointerEvents: 'none',
        }}
      >
        {Math.round(scale * 100)}%
      </Box>
    </Box>
  );
}
