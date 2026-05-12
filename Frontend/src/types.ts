export type ToolMode = 'pan' | 'rotate' | 'zoom' | 'windowing' | 'annotate';
export type ViewMode = '2d' | '3d';

export interface DicomResult {
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

export interface WLPreset {
  name: string;
  wc: number;
  ww: number;
}
