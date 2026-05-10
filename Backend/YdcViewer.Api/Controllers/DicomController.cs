using Microsoft.AspNetCore.Mvc;
using YdcViewer.Dicom;

namespace YdcViewer.Api.Controllers;

[ApiController]
[Route("api/dicom")]
public class DicomController : ControllerBase
{
    private readonly DicomParser _parser = new();

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var tempPath = Path.GetTempFileName();
        try
        {
            await using var stream = System.IO.File.Create(tempPath);
            await file.CopyToAsync(stream);

            var result = await _parser.ParseFileAsync(tempPath);
            var metadata = result.Metadata;

            // Convert pixel data to displayable grayscale image
            var imageBytes = ConvertToGrayscalePng(
                result.PixelData,
                metadata.Width,
                metadata.Height,
                metadata.BitsAllocated,
                metadata.IsSigned,
                metadata.RescaleSlope,
                metadata.RescaleIntercept,
                metadata.WindowCenter,
                metadata.WindowWidth);

            return Ok(new
            {
                metadata.PatientName,
                metadata.Modality,
                metadata.Width,
                metadata.Height,
                metadata.BitsAllocated,
                metadata.WindowCenter,
                metadata.WindowWidth,
                imageBase64 = Convert.ToBase64String(imageBytes)
            });
        }
        finally
        {
            System.IO.File.Delete(tempPath);
        }
    }

    private static byte[] ConvertToGrayscalePng(
        byte[] pixelData, int width, int height,
        int bitsAllocated, bool isSigned,
        double slope, double intercept,
        int windowCenter, int windowWidth)
    {
        var voxelCount = width * height;

        // Convert raw bytes to double values (apply rescale)
        var values = new double[voxelCount];
        for (int i = 0; i < voxelCount; i++)
        {
            double raw;
            if (bitsAllocated == 16)
            {
                raw = isSigned
                    ? BitConverter.ToInt16(pixelData, i * 2)
                    : BitConverter.ToUInt16(pixelData, i * 2);
            }
            else // 8-bit
            {
                raw = isSigned
                    ? (sbyte)pixelData[i]
                    : pixelData[i];
            }
            values[i] = raw * slope + intercept;
        }

        // Apply window/level to map to 0-255
        double center = windowCenter;
        double wWidth = windowWidth;
        if (wWidth <= 0)
        {
            // Auto-window: use full range
            var min = values.Min();
            var max = values.Max();
            center = (min + max) / 2.0;
            wWidth = max - min;
            if (wWidth <= 0) wWidth = 1;
        }

        var lower = center - wWidth / 2.0;

        var grayscale = new byte[voxelCount];
        for (int i = 0; i < voxelCount; i++)
        {
            var normalized = (values[i] - lower) / wWidth;
            grayscale[i] = (byte)Math.Clamp(normalized * 255.0, 0, 255);
        }

        // Encode as grayscale PNG
        return EncodeGrayscalePng(grayscale, width, height);
    }

    private static byte[] EncodeGrayscalePng(byte[] grayscale, int width, int height)
    {
        // Minimal PNG encoder for 8-bit grayscale
        using var ms = new MemoryStream();

        // PNG signature
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        // IHDR chunk
        WriteChunk(ms, "IHDR", [
            ..ToBigEndian(width),
            ..ToBigEndian(height),
            8,  // bit depth
            0,  // color type: grayscale
            0,  // compression
            0,  // filter
            0   // interlace
        ]);

        // IDAT chunk (raw image data with zlib)
        var rawRowSize = 1 + width; // filter byte + row data
        var rawData = new byte[rawRowSize * height];
        for (int y = 0; y < height; y++)
        {
            rawData[y * rawRowSize] = 0; // no filter
            Buffer.BlockCopy(grayscale, y * width, rawData, y * rawRowSize + 1, width);
        }

        using var compressed = new MemoryStream();
        using (var zlib = new System.IO.Compression.ZLibStream(compressed, System.IO.Compression.CompressionLevel.Fastest))
        {
            zlib.Write(rawData);
        }
        WriteChunk(ms, "IDAT", compressed.ToArray());

        // IEND chunk
        WriteChunk(ms, "IEND", []);

        return ms.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        var length = ToBigEndian(data.Length);
        stream.Write(length);
        stream.Write(typeBytes);
        stream.Write(data);

        // CRC32 over type + data
        var crcData = new byte[typeBytes.Length + data.Length];
        Buffer.BlockCopy(typeBytes, 0, crcData, 0, typeBytes.Length);
        Buffer.BlockCopy(data, 0, crcData, typeBytes.Length, data.Length);
        var crc = Crc32(crcData);
        stream.Write(ToBigEndian((int)crc));
    }

    private static byte[] ToBigEndian(int value)
    {
        return [
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value
        ];
    }

    private static uint Crc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }
        return crc ^ 0xFFFFFFFF;
    }
}
