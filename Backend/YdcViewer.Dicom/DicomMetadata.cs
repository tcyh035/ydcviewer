namespace YdcViewer.Dicom;

public class DicomMetadata
{
    public string SeriesInstanceUid { get; set; } = string.Empty;
    public string StudyInstanceUid { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Modality { get; set; } = string.Empty;
    public string StudyDate { get; set; } = string.Empty;
    public string SeriesDescription { get; set; } = string.Empty;
    public int SliceCount { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double PixelSpacingX { get; set; }
    public double PixelSpacingY { get; set; }
    public double SliceThickness { get; set; }
    public double SliceSpacing { get; set; }
    public int BitsAllocated { get; set; }
    public bool IsSigned { get; set; }
    public double RescaleSlope { get; set; } = 1.0;
    public double RescaleIntercept { get; set; }
    public int WindowCenter { get; set; }
    public int WindowWidth { get; set; }
}
