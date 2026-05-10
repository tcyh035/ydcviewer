using OpenTK.Mathematics;

namespace YdcViewer.Renderer;

public class TransferFunction
{
    private readonly List<ControlPoint> _points = new();

    public record struct ControlPoint(float Value, Vector4 Color);

    public TransferFunction()
    {
        // Default: black to white gradient
        _points.Add(new ControlPoint(0f, new Vector4(0, 0, 0, 0)));
        _points.Add(new ControlPoint(1f, new Vector4(1, 1, 1, 1)));
    }

    public void AddPoint(float value, Vector4 color)
    {
        _points.Add(new ControlPoint(value, color));
        _points.Sort((a, b) => a.Value.CompareTo(b.Value));
    }

    public void Clear()
    {
        _points.Clear();
    }

    public Vector4 Evaluate(float normalizedValue)
    {
        normalizedValue = Math.Clamp(normalizedValue, 0f, 1f);

        if (_points.Count == 0)
            return Vector4.Zero;

        if (_points.Count == 1)
            return _points[0].Color;

        // Find surrounding control points
        for (int i = 0; i < _points.Count - 1; i++)
        {
            var p0 = _points[i];
            var p1 = _points[i + 1];

            if (normalizedValue >= p0.Value && normalizedValue <= p1.Value)
            {
                var t = (p1.Value - p0.Value) > 0.0001f
                    ? (normalizedValue - p0.Value) / (p1.Value - p0.Value)
                    : 0f;
                return Vector4.Lerp(p0.Color, p1.Color, t);
            }
        }

        return _points[^1].Color;
    }

    public byte[] GenerateTextureData(int size = 256)
    {
        // RGBA float texture, 1D
        var data = new byte[size * 4];
        for (int i = 0; i < size; i++)
        {
            var t = i / (float)(size - 1);
            var color = Evaluate(t);
            data[i * 4 + 0] = (byte)(color.X * 255);
            data[i * 4 + 1] = (byte)(color.Y * 255);
            data[i * 4 + 2] = (byte)(color.Z * 255);
            data[i * 4 + 3] = (byte)(color.W * 255);
        }
        return data;
    }

    public static TransferFunction CreateBone()
    {
        var tf = new TransferFunction();
        tf.Clear();
        tf.AddPoint(0f, new Vector4(0, 0, 0, 0));
        tf.AddPoint(0.3f, new Vector4(0.3f, 0.3f, 0.3f, 0.1f));
        tf.AddPoint(0.6f, new Vector4(0.7f, 0.7f, 0.6f, 0.5f));
        tf.AddPoint(0.9f, new Vector4(1f, 1f, 0.9f, 0.9f));
        tf.AddPoint(1f, new Vector4(1f, 1f, 1f, 1f));
        return tf;
    }

    public static TransferFunction CreateSoftTissue()
    {
        var tf = new TransferFunction();
        tf.Clear();
        tf.AddPoint(0f, new Vector4(0, 0, 0, 0));
        tf.AddPoint(0.2f, new Vector4(0.8f, 0.2f, 0.2f, 0.0f));
        tf.AddPoint(0.4f, new Vector4(0.9f, 0.3f, 0.3f, 0.4f));
        tf.AddPoint(0.7f, new Vector4(1f, 0.6f, 0.5f, 0.7f));
        tf.AddPoint(1f, new Vector4(1f, 1f, 1f, 1f));
        return tf;
    }
}
