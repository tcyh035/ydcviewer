namespace YdcViewer.Dicom;

public class DicomSeries
{
    public DicomMetadata Metadata { get; set; } = new();
    public List<DicomFileResult> Slices { get; set; } = new();

    public void AddSlice(DicomFileResult slice)
    {
        if (Slices.Count == 0)
        {
            Metadata = slice.Metadata;
            Metadata.SliceCount = 0;
        }
        Slices.Add(slice);
        Metadata.SliceCount = Slices.Count;
    }

    public void SortByInstanceNumber()
    {
        // Sort by file path as fallback; real sorting should use InstanceNumber or SliceLocation
        // For now, sort by the order they were added (assumes sequential import)
        // TODO: extract InstanceNumber from DICOM tags for proper sorting
    }

    public VolumeData AssembleVolume()
    {
        if (Slices.Count == 0)
            throw new InvalidOperationException("No slices to assemble");

        var first = Slices[0];
        var width = first.Metadata.Width;
        var height = first.Metadata.Height;
        var depth = Slices.Count;
        var bitsPerVoxel = first.Metadata.BitsAllocated;
        var bytesPerVoxel = bitsPerVoxel / 8;
        var sliceSize = width * height * bytesPerVoxel;
        var totalSize = sliceSize * depth;

        var volumeBytes = new byte[totalSize];

        for (int z = 0; z < depth; z++)
        {
            var sliceData = Slices[z].PixelData;
            var copyLength = Math.Min(sliceData.Length, sliceSize);
            Buffer.BlockCopy(sliceData, 0, volumeBytes, z * sliceSize, copyLength);
        }

        return new VolumeData
        {
            Width = width,
            Height = height,
            Depth = depth,
            BitsPerVoxel = bitsPerVoxel,
            IsSigned = first.Metadata.IsSigned,
            VoxelBytes = volumeBytes,
            PixelSpacingX = first.Metadata.PixelSpacingX,
            PixelSpacingY = first.Metadata.PixelSpacingY,
            SliceSpacing = first.Metadata.SliceSpacing > 0
                ? first.Metadata.SliceSpacing
                : first.Metadata.SliceThickness,
            RescaleSlope = first.Metadata.RescaleSlope,
            RescaleIntercept = first.Metadata.RescaleIntercept
        };
    }
}
