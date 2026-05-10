using FluentAssertions;
using YdcViewer.Dicom;

namespace YdcViewer.Tests.Dicom;

public class VolumeDataTests
{
    [Fact]
    public void VolumeData_DefaultValues_AreCorrect()
    {
        var volume = new VolumeData();

        volume.Width.Should().Be(0);
        volume.Height.Should().Be(0);
        volume.Depth.Should().Be(0);
        volume.VoxelBytes.Should().BeEmpty();
        volume.RescaleSlope.Should().Be(1.0);
        volume.RescaleIntercept.Should().Be(0.0);
    }

    [Fact]
    public void VolumeData_VoxelCount_MatchesDimensions()
    {
        var volume = new VolumeData
        {
            Width = 512,
            Height = 512,
            Depth = 256
        };

        volume.VoxelCount.Should().Be(512L * 512 * 256);
    }

    [Fact]
    public void VolumeData_VoxelBytes_CanStore16BitData()
    {
        var width = 4;
        var height = 4;
        var depth = 2;
        var voxelCount = width * height * depth;
        var bytes = new byte[voxelCount * 2]; // 16-bit per voxel

        // Write some test values (little-endian ushort)
        bytes[0] = 0x00; bytes[1] = 0x80; // 32768
        bytes[2] = 0xFF; bytes[3] = 0x7F; // 32767

        var volume = new VolumeData
        {
            Width = width,
            Height = height,
            Depth = depth,
            BitsPerVoxel = 16,
            VoxelBytes = bytes
        };

        volume.VoxelBytes.Should().HaveCount(voxelCount * 2);
        volume.VoxelCount.Should().Be(voxelCount);
    }
}
