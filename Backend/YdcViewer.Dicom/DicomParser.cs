using FellowOakDicom;
using FellowOakDicom.Imaging;

namespace YdcViewer.Dicom;

public class DicomParser
{
    public async Task<DicomFileResult> ParseFileAsync(string filePath)
    {
        var file = await DicomFile.OpenAsync(filePath);
        var dataset = file.Dataset;

        var metadata = ExtractMetadata(dataset);
        var pixelData = ExtractPixelData(dataset);

        return new DicomFileResult
        {
            FilePath = filePath,
            Metadata = metadata,
            PixelData = pixelData
        };
    }

    public DicomMetadata ExtractMetadata(DicomDataset dataset)
    {
        return new DicomMetadata
        {
            SeriesInstanceUid = dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
            StudyInstanceUid = dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
            PatientName = dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty),
            PatientId = dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty),
            Modality = dataset.GetSingleValueOrDefault(DicomTag.Modality, string.Empty),
            StudyDate = dataset.GetSingleValueOrDefault(DicomTag.StudyDate, string.Empty),
            SeriesDescription = dataset.GetSingleValueOrDefault(DicomTag.SeriesDescription, string.Empty),
            Width = dataset.GetSingleValueOrDefault(DicomTag.Columns, 0),
            Height = dataset.GetSingleValueOrDefault(DicomTag.Rows, 0),
            BitsAllocated = dataset.GetSingleValueOrDefault(DicomTag.BitsAllocated, 16),
            IsSigned = dataset.GetSingleValueOrDefault(DicomTag.PixelRepresentation, 0) == 1,
            RescaleSlope = dataset.GetSingleValueOrDefault(DicomTag.RescaleSlope, 1.0),
            RescaleIntercept = dataset.GetSingleValueOrDefault(DicomTag.RescaleIntercept, 0.0),
            WindowCenter = dataset.GetSingleValueOrDefault(DicomTag.WindowCenter, 0),
            WindowWidth = dataset.GetSingleValueOrDefault(DicomTag.WindowWidth, 0),
            PixelSpacingX = GetDoubleArrayFirst(dataset, DicomTag.PixelSpacing, 0),
            PixelSpacingY = GetDoubleArrayFirst(dataset, DicomTag.PixelSpacing, 1),
            SliceThickness = dataset.GetSingleValueOrDefault(DicomTag.SliceThickness, 0.0),
            SliceSpacing = dataset.GetSingleValueOrDefault(DicomTag.SpacingBetweenSlices, 0.0)
        };
    }

    public byte[] ExtractPixelData(DicomDataset dataset)
    {
        var pixelData = DicomPixelData.Create(dataset);
        if (pixelData.NumberOfFrames == 0)
            return Array.Empty<byte>();

        return pixelData.GetFrame(0).Data;
    }

    private static double GetDoubleArrayFirst(DicomDataset dataset, DicomTag tag, int index)
    {
        try
        {
            var values = dataset.GetValues<double>(tag);
            return values != null && values.Length > index ? values[index] : 0.0;
        }
        catch
        {
            return 0.0;
        }
    }
}

public class DicomFileResult
{
    public string FilePath { get; set; } = string.Empty;
    public DicomMetadata Metadata { get; set; } = new();
    public byte[] PixelData { get; set; } = Array.Empty<byte>();
}
