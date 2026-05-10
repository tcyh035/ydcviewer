using FluentAssertions;
using OpenTK.Mathematics;
using YdcViewer.Renderer;

namespace YdcViewer.Tests.Renderer;

public class CameraTests
{
    [Fact]
    public void Camera_DefaultValues_AreCorrect()
    {
        var camera = new Camera();

        camera.Yaw.Should().Be(0);
        camera.Pitch.Should().Be(0);
        camera.Distance.Should().Be(3f);
        camera.Pan.Should().Be(Vector2.Zero);
    }

    [Fact]
    public void Rotate_UpdatesYawAndPitch()
    {
        var camera = new Camera();

        camera.Rotate(100, 50);

        camera.Yaw.Should().BeApproximately(30f, 0.01f);
        camera.Pitch.Should().BeApproximately(15f, 0.01f);
    }

    [Fact]
    public void Rotate_ClampsPitchTo89Degrees()
    {
        var camera = new Camera();

        camera.Rotate(0, 1000);

        camera.Pitch.Should().Be(89f);
    }

    [Fact]
    public void Rotate_ClampsPitchToMinus89Degrees()
    {
        var camera = new Camera();

        camera.Rotate(0, -1000);

        camera.Pitch.Should().Be(-89f);
    }

    [Fact]
    public void Zoom_DecreasesDistance()
    {
        var camera = new Camera(distance: 5f);

        camera.Zoom(1000);

        camera.Distance.Should().BeLessThan(5f);
    }

    [Fact]
    public void Zoom_IncreasesDistance()
    {
        var camera = new Camera(distance: 3f);

        camera.Zoom(-1000);

        camera.Distance.Should().BeGreaterThan(3f);
    }

    [Fact]
    public void Zoom_ClampsToMinDistance()
    {
        var camera = new Camera(distance: 1.5f);

        camera.Zoom(10000);

        camera.Distance.Should().BeGreaterThanOrEqualTo(1f);
    }

    [Fact]
    public void Zoom_ClampsToMaxDistance()
    {
        var camera = new Camera(distance: 8f);

        camera.Zoom(-10000);

        camera.Distance.Should().BeLessThanOrEqualTo(10f);
    }

    [Fact]
    public void PanXY_UpdatesPan()
    {
        var camera = new Camera();

        camera.PanXY(100, 50, 800, 600);

        camera.Pan.X.Should().NotBe(0);
        camera.Pan.Y.Should().NotBe(0);
    }

    [Fact]
    public void GetPosition_AtZeroYawPitch_ReturnsForwardVector()
    {
        var camera = new Camera(distance: 3f);

        var pos = camera.GetPosition();

        // At yaw=0, pitch=0, camera should be on Z axis
        pos.X.Should().BeApproximately(0, 0.01f);
        pos.Y.Should().BeApproximately(0, 0.01f);
        pos.Z.Should().BeApproximately(3f, 0.01f);
    }

    [Fact]
    public void GetViewMatrix_ReturnsNonIdentity()
    {
        var camera = new Camera();
        camera.Rotate(45, 30);

        var view = camera.GetViewMatrix();

        view.Should().NotBe(Matrix4.Identity);
    }

    [Fact]
    public void GetProjectionMatrix_ReturnsNonIdentity()
    {
        var camera = new Camera();

        var proj = camera.GetProjectionMatrix(16f / 9f);

        proj.Should().NotBe(Matrix4.Identity);
    }

    [Fact]
    public void ApplyInput_RotateAction_RotatesCamera()
    {
        var camera = new Camera();

        camera.ApplyInput("rotate", 100, 0, 0, 800, 600);

        camera.Yaw.Should().BeApproximately(30f, 0.01f);
    }

    [Fact]
    public void ApplyInput_ZoomAction_ZoomsCamera()
    {
        var camera = new Camera(distance: 5f);

        camera.ApplyInput("zoom", 0, 0, 1000, 800, 600);

        camera.Distance.Should().BeLessThan(5f);
    }
}
