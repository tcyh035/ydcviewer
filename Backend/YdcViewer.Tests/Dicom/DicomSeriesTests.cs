using FluentAssertions;
using YdcViewer.Dicom;

namespace YdcViewer.Tests.Dicom;

public class DicomSeriesTests
{
    [Fact]
    public void AddSlice_FirstSlice_SetsMetadata()
    {
        var series = new DicomSeries();
        var slice = CreateSlice(width: 64, height: 64, bitsAllocated: 16);

        series.AddSlice(slice);

        series.Metadata.Width.Should().Be(64);
        series.Metadata.Height.Should().Be(64);
        series.Metadata.SliceCount.Should().Be(1);
        series.Slices.Should().HaveCount(1);
    }

    [Fact]
    public void AddSlice_MultipleSlices_IncrementsCount()
    {
        var series = new DicomSeries();

        series.AddSlice(CreateSlice(64, 64, 16));
        series.AddSlice(CreateSlice(64, 64, 16));
        series.AddSlice(CreateSlice(64, 64, 16));

        series.Metadata.SliceCount.Should().Be(3);
        series.Slices.Should().HaveCount(3);
    }

    [Fact]
    public void AssembleVolume_WithNoSlices_Throws()
    {
        var series = new DicomSeries();

        var act = () => series.AssembleVolume();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No slices*");
    }

    [Fact]
    public void AssembleVolume_SingleSlice_CorrectDimensions()
    {
        var series = new DicomSeries();
        var slice = CreateSlice(width: 4, height: 4, bitsAllocated: 16);
        series.AddSlice(slice);

        var volume = series.AssembleVolume();

        volume.Width.Should().Be(4);
        volume.Height.Should().Be(4);
        volume.Depth.Should().Be(1);
        volume.BitsPerVoxel.Should().Be(16);
        volume.VoxelBytes.Should().HaveCount(4 * 4 * 2); // 16-bit = 2 bytes per voxel
    }

    [Fact]
    public void AssembleVolume_MultipleSlices_DataConcatenated()
    {
        var series = new DicomSeries();
        var width = 2;
        var height = 2;
        var depth = 3;
        var bytesPerVoxel = 2; // 16-bit

        for (int i = 0; i < depth; i++)
        {
            var data = new byte[width * height * bytesPerVoxel];
            // Fill each slice with a different pattern
            for (int j = 0; j < data.Length; j++)
                data[j] = (byte)(i * 10 + j);
            series.AddSlice(CreateSliceWithData(width, height, 16, data));
        }

        var volume = series.AssembleVolume();

        volume.Width.Should().Be(width);
        volume.Height.Should().Be(height);
        volume.Depth.Should().Be(depth);
        volume.VoxelBytes.Should().HaveCount(width * height * depth * bytesPerVoxel);

        // Verify first slice data is at offset 0
        volume.VoxelBytes[0].Should().Be(0);  // slice 0, byte 0
        volume.VoxelBytes[1].Should().Be(1);  // slice 0, byte 1

        // Verify second slice data starts at sliceSize offset
        var sliceSize = width * height * bytesPerVoxel;
        volume.VoxelBytes[sliceSize].Should().Be(10); // slice 1, byte 0
        volume.VoxelBytes[sliceSize + 1].Should().Be(11); // slice 1, byte 1
    }

    [Fact]
    public void AssembleVolume_PreservesSpacing()
    {
        var series = new DicomSeries();
        var slice = CreateSlice(64, 64, 16);
        slice.Metadata.PixelSpacingX = 0.5;
        slice.Metadata.PixelSpacingY = 0.5;
        slice.Metadata.SliceThickness = 2.0;
        slice.Metadata.RescaleSlope = 1.5;
        slice.Metadata.RescaleIntercept = -1024;
        series.AddSlice(slice);

        var volume = series.AssembleVolume();

        volume.PixelSpacingX.Should().Be(0.5);
        volume.PixelSpacingY.Should().Be(0.5);
        volume.SliceSpacing.Should().Be(2.0);
        volume.RescaleSlope.Should().Be(1.5);
        volume.RescaleIntercept.Should().Be(-1024);
    }

    private static DicomFileResult CreateSlice(int width, int height, int bitsAllocated)
    {
        var bytesPerVoxel = bitsAllocated / 8;
        var data = new byte[width * height * bytesPerVoxel];
        return CreateSliceWithData(width, height, bitsAllocated, data);
    }

    private static DicomFileResult CreateSliceWithData(int width, int height, int bitsAllocated, byte[] pixelData)
    {
        return new DicomFileResult
        {
            Metadata = new DicomMetadata
            {
                Width = width,
                Height = height,
                BitsAllocated = bitsAllocated,
                IsSigned = false,
                RescaleSlope = 1.0,
                RescaleIntercept = 0.0,
                PixelSpacingX = 1.0,
                PixelSpacingY = 1.0,
                SliceThickness = 1.0
            },
            PixelData = pixelData
        };
    }
}
