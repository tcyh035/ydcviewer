namespace YdcViewer.Dicom;

public class VolumeData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Depth { get; set; }
    public int BitsPerVoxel { get; set; }
    public bool IsSigned { get; set; }

    /// <summary>
    /// Raw voxel data, one value per voxel. For 16-bit: ushort[] or short[] cast to byte[].
    /// Layout: slice[depth] → row[height] → col[width], row-major.
    /// </summary>
    public byte[] VoxelBytes { get; set; } = Array.Empty<byte>();

    public double PixelSpacingX { get; set; }
    public double PixelSpacingY { get; set; }
    public double SliceSpacing { get; set; }
    public double RescaleSlope { get; set; } = 1.0;
    public double RescaleIntercept { get; set; }

    public long VoxelCount => (long)Width * Height * Depth;
}
