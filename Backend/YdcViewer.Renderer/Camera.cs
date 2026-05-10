using OpenTK.Mathematics;

namespace YdcViewer.Renderer;

public class Camera
{
    public float Yaw { get; private set; }
    public float Pitch { get; private set; }
    public float Distance { get; private set; }
    public Vector2 Pan { get; private set; }
    public float Fov { get; private set; } = 45f;

    private float _minDistance = 1f;
    private float _maxDistance = 10f;
    private float _sensitivity = 0.3f;

    public Camera(float distance = 3f)
    {
        Distance = distance;
        Yaw = 0;
        Pitch = 0;
        Pan = Vector2.Zero;
    }

    public void Rotate(float dx, float dy)
    {
        Yaw += dx * _sensitivity;
        Pitch += dy * _sensitivity;
        Pitch = Math.Clamp(Pitch, -89f, 89f);
    }

    public void PanXY(float dx, float dy, int canvasWidth, int canvasHeight)
    {
        var scale = Distance * 0.002f;
        Pan += new Vector2(-dx * scale, dy * scale);
    }

    public void Zoom(float delta)
    {
        Distance -= delta * 0.001f * Distance;
        Distance = Math.Clamp(Distance, _minDistance, _maxDistance);
    }

    public void ApplyInput(string action, float dx, float dy, float scrollDelta, int canvasWidth, int canvasHeight)
    {
        switch (action)
        {
            case "rotate":
                Rotate(dx, dy);
                break;
            case "pan":
                PanXY(dx, dy, canvasWidth, canvasHeight);
                break;
            case "zoom":
                Zoom(scrollDelta);
                break;
        }
    }

    public Matrix4 GetViewMatrix()
    {
        var position = GetPosition();
        var target = new Vector3(Pan.X, Pan.Y, 0);
        return Matrix4.LookAt(position, target, Vector3.UnitY);
    }

    public Matrix4 GetProjectionMatrix(float aspectRatio)
    {
        return Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(Fov),
            aspectRatio,
            0.1f,
            100f);
    }

    public Vector3 GetPosition()
    {
        var pitchRad = MathHelper.DegreesToRadians(Pitch);
        var yawRad = MathHelper.DegreesToRadians(Yaw);

        var x = Distance * MathF.Cos(pitchRad) * MathF.Sin(yawRad);
        var y = Distance * MathF.Sin(pitchRad);
        var z = Distance * MathF.Cos(pitchRad) * MathF.Cos(yawRad);

        return new Vector3(x + Pan.X, y + Pan.Y, z);
    }
}
