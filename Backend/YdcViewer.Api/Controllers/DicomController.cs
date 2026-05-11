using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using YdcViewer.Dicom;
using YdcViewer.Renderer;

namespace YdcViewer.Api.Controllers;

[ApiController]
[Route("api/dicom")]
public class DicomController : ControllerBase
{
    private readonly DicomParser _parser = new();
    private static readonly ConcurrentDictionary<string, DicomSeries> _seriesStore = new();
    private static readonly Lazy<RenderEngine> _renderEngine = new(() =>
    {
        var engine = new RenderEngine();
        engine.Initialize();
        return engine;
    });

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No file uploaded");

        DicomSeries? series = null;
        string? seriesId = null;
        DicomFileResult? firstResult = null;

        foreach (var file in files)
        {
            if (file == null || file.Length == 0) continue;

            var tempPath = Path.GetTempFileName();
            try
            {
                await using (var stream = System.IO.File.Create(tempPath))
                {
                    await file.CopyToAsync(stream);
                }

                var result = await _parser.ParseFileAsync(tempPath);
                var metadata = result.Metadata;

                if (seriesId == null)
                {
                    seriesId = metadata.SeriesInstanceUid;
                    if (string.IsNullOrEmpty(seriesId))
                        seriesId = Guid.NewGuid().ToString();
                }

                series = _seriesStore.GetOrAdd(seriesId, _ => new DicomSeries());
                series.AddSlice(result);

                firstResult ??= result;
            }
            finally
            {
                System.IO.File.Delete(tempPath);
            }
        }

        if (series == null || firstResult == null)
            return BadRequest("No valid DICOM files found");

        var firstMeta = firstResult.Metadata;
        var imageBytes = ConvertToGrayscalePng(
            firstResult.PixelData,
            firstMeta.Width,
            firstMeta.Height,
            firstMeta.BitsAllocated,
            firstMeta.IsSigned,
            firstMeta.RescaleSlope,
            firstMeta.RescaleIntercept,
            firstMeta.WindowCenter,
            firstMeta.WindowWidth);

        return Ok(new
        {
            seriesId,
            firstMeta.PatientName,
            firstMeta.Modality,
            firstMeta.Width,
            firstMeta.Height,
            firstMeta.BitsAllocated,
            firstMeta.WindowCenter,
            firstMeta.WindowWidth,
            sliceCount = series.Slices.Count,
            imageBase64 = Convert.ToBase64String(imageBytes)
        });
    }

    [HttpGet("series")]
    public IActionResult ListSeries()
    {
        var series = _seriesStore.Select(kv => new
        {
            seriesId = kv.Key,
            kv.Value.Metadata.PatientName,
            kv.Value.Metadata.Modality,
            sliceCount = kv.Value.Slices.Count
        });
        return Ok(series);
    }

    [HttpGet("series/{seriesId}/slice/{sliceIndex}")]
    public IActionResult GetSlice(string seriesId, int sliceIndex,
        [FromQuery] int windowCenter = 0, [FromQuery] int windowWidth = 0)
    {
        if (!_seriesStore.TryGetValue(seriesId, out var series))
            return NotFound("Series not found");

        if (sliceIndex < 0 || sliceIndex >= series.Slices.Count)
            return BadRequest($"Slice index {sliceIndex} out of range [0, {series.Slices.Count - 1}]");

        var slice = series.Slices[sliceIndex];
        var metadata = slice.Metadata;

        var wc = windowCenter != 0 ? windowCenter : metadata.WindowCenter;
        var ww = windowWidth != 0 ? windowWidth : metadata.WindowWidth;

        var imageBytes = ConvertToGrayscalePng(
            slice.PixelData,
            metadata.Width,
            metadata.Height,
            metadata.BitsAllocated,
            metadata.IsSigned,
            metadata.RescaleSlope,
            metadata.RescaleIntercept,
            wc, ww);

        return File(imageBytes, "image/png");
    }

    [HttpPost("render3d")]
    public IActionResult Render3D([FromBody] Render3DRequest request)
    {
        if (!_seriesStore.TryGetValue(request.SeriesId, out var series))
            return NotFound("Series not found");

        try
        {
            var engine = _renderEngine.Value;
            var volume = series.AssembleVolume();

            engine.UploadVolume(volume);

            var tf = request.TransferFunction switch
            {
                "bone" => TransferFunction.CreateBone(),
                "soft_tissue" => TransferFunction.CreateSoftTissue(),
                _ => new TransferFunction()
            };
            engine.UploadTransferFunction(tf);

            var camera = new Camera(distance: 2.5f);
            if (request.Yaw != 0 || request.Pitch != 0)
            {
                camera.ApplyInput("rotate", request.Yaw / 0.3f, request.Pitch / 0.3f, 0, 512, 512);
            }

            var pixels = engine.RenderFrame(camera, request.Width, request.Height);

            // Encode as RGB PNG
            var pngBytes = EncodeRgbPng(pixels, request.Width, request.Height);

            return File(pngBytes, "image/png");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Render error: {ex.Message}");
        }
    }

    private static byte[] EncodeRgbPng(byte[] rgb, int width, int height)
    {
        using var ms = new MemoryStream();

        // PNG signature
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        // IHDR chunk (RGB)
        WriteChunk(ms, "IHDR", [
            ..ToBigEndian(width),
            ..ToBigEndian(height),
            8,  // bit depth
            2,  // color type: RGB
            0, 0, 0
        ]);

        // IDAT chunk
        var rawRowSize = 1 + width * 3;
        var rawData = new byte[rawRowSize * height];
        for (int y = 0; y < height; y++)
        {
            rawData[y * rawRowSize] = 0; // no filter
            System.Buffer.BlockCopy(rgb, y * width * 3, rawData, y * rawRowSize + 1, width * 3);
        }

        using var compressed = new MemoryStream();
        using (var zlib = new System.IO.Compression.ZLibStream(compressed, System.IO.Compression.CompressionLevel.Fastest))
        {
            zlib.Write(rawData);
        }
        WriteChunk(ms, "IDAT", compressed.ToArray());
        WriteChunk(ms, "IEND", []);

        return ms.ToArray();
    }

    private static byte[] ConvertToGrayscalePng(
        byte[] pixelData, int width, int height,
        int bitsAllocated, bool isSigned,
        double slope, double intercept,
        int windowCenter, int windowWidth)
    {
        var voxelCount = width * height;

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
            else
            {
                raw = isSigned
                    ? (sbyte)pixelData[i]
                    : pixelData[i];
            }
            values[i] = raw * slope + intercept;
        }

        double center = windowCenter;
        double wWidth = windowWidth;
        if (wWidth <= 0)
        {
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

        return EncodeGrayscalePng(grayscale, width, height);
    }

    private static byte[] EncodeGrayscalePng(byte[] grayscale, int width, int height)
    {
        using var ms = new MemoryStream();
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        WriteChunk(ms, "IHDR", [
            ..ToBigEndian(width),
            ..ToBigEndian(height),
            8, 0, 0, 0, 0
        ]);

        var rawRowSize = 1 + width;
        var rawData = new byte[rawRowSize * height];
        for (int y = 0; y < height; y++)
        {
            rawData[y * rawRowSize] = 0;
            System.Buffer.BlockCopy(grayscale, y * width, rawData, y * rawRowSize + 1, width);
        }

        using var compressed = new MemoryStream();
        using (var zlib = new System.IO.Compression.ZLibStream(compressed, System.IO.Compression.CompressionLevel.Fastest))
        {
            zlib.Write(rawData);
        }
        WriteChunk(ms, "IDAT", compressed.ToArray());
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

        var crcData = new byte[typeBytes.Length + data.Length];
        System.Buffer.BlockCopy(typeBytes, 0, crcData, 0, typeBytes.Length);
        System.Buffer.BlockCopy(data, 0, crcData, typeBytes.Length, data.Length);
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

public class Render3DRequest
{
    public string SeriesId { get; set; } = string.Empty;
    public int Width { get; set; } = 512;
    public int Height { get; set; } = 512;
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public string TransferFunction { get; set; } = "default";
}
