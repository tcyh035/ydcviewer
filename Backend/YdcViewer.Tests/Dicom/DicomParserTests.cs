using FluentAssertions;
using YdcViewer.Dicom;

namespace YdcViewer.Tests.Dicom;

public class DicomParserTests
{
    private readonly DicomParser _parser = new();

    [Fact]
    public void ExtractMetadata_FromEmptyDataset_ReturnsDefaults()
    {
        var dataset = new FellowOakDicom.DicomDataset();

        var metadata = _parser.ExtractMetadata(dataset);

        metadata.SeriesInstanceUid.Should().BeEmpty();
        metadata.PatientName.Should().BeEmpty();
        metadata.Width.Should().Be(0);
        metadata.Height.Should().Be(0);
        metadata.BitsAllocated.Should().Be(16);
        metadata.IsSigned.Should().BeFalse();
        metadata.RescaleSlope.Should().Be(1.0);
        metadata.RescaleIntercept.Should().Be(0.0);
    }

    [Fact]
    public void ExtractMetadata_FromPopulatedDataset_ExtractsCorrectly()
    {
        var dataset = new FellowOakDicom.DicomDataset
        {
            { FellowOakDicom.DicomTag.SeriesInstanceUID, "1.2.3.4.5" },
            { FellowOakDicom.DicomTag.PatientName, "TestPatient" },
            { FellowOakDicom.DicomTag.Modality, "CT" },
            { FellowOakDicom.DicomTag.Rows, (ushort)512 },
            { FellowOakDicom.DicomTag.Columns, (ushort)512 },
            { FellowOakDicom.DicomTag.BitsAllocated, (ushort)16 },
            { FellowOakDicom.DicomTag.PixelRepresentation, (ushort)1 }, // signed
            { FellowOakDicom.DicomTag.RescaleSlope, "1.5" },
            { FellowOakDicom.DicomTag.RescaleIntercept, "-1024" },
            { FellowOakDicom.DicomTag.WindowCenter, "40" },
            { FellowOakDicom.DicomTag.WindowWidth, "400" }
        };

        var metadata = _parser.ExtractMetadata(dataset);

        metadata.SeriesInstanceUid.Should().Be("1.2.3.4.5");
        metadata.PatientName.Should().Be("TestPatient");
        metadata.Modality.Should().Be("CT");
        metadata.Width.Should().Be(512);
        metadata.Height.Should().Be(512);
        metadata.BitsAllocated.Should().Be(16);
        metadata.IsSigned.Should().BeTrue();
        metadata.RescaleSlope.Should().Be(1.5);
        metadata.RescaleIntercept.Should().Be(-1024.0);
        metadata.WindowCenter.Should().Be(40);
        metadata.WindowWidth.Should().Be(400);
    }

    [Fact]
    public void ParseFileAsync_WithNonExistentFile_ThrowsException()
    {
        var act = () => _parser.ParseFileAsync("/nonexistent/path/file.dcm");

        act.Should().ThrowAsync<Exception>();
    }
}
