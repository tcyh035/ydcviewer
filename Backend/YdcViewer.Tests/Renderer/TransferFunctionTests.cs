using FluentAssertions;
using OpenTK.Mathematics;
using YdcViewer.Renderer;

namespace YdcViewer.Tests.Renderer;

public class TransferFunctionTests
{
    [Fact]
    public void Evaluate_DefaultGradient_InterpolatesLinearly()
    {
        var tf = new TransferFunction();

        var atZero = tf.Evaluate(0f);
        var atHalf = tf.Evaluate(0.5f);
        var atOne = tf.Evaluate(1f);

        atZero.W.Should().BeApproximately(0f, 0.01f);
        atHalf.W.Should().BeApproximately(0.5f, 0.01f);
        atOne.W.Should().BeApproximately(1f, 0.01f);
    }

    [Fact]
    public void Evaluate_BelowRange_ClampsToFirst()
    {
        var tf = new TransferFunction();

        var result = tf.Evaluate(-1f);

        result.Should().Be(tf.Evaluate(0f));
    }

    [Fact]
    public void Evaluate_AboveRange_ClampsToLast()
    {
        var tf = new TransferFunction();

        var result = tf.Evaluate(2f);

        result.Should().Be(tf.Evaluate(1f));
    }

    [Fact]
    public void AddPoint_MaintainsSortOrder()
    {
        var tf = new TransferFunction();
        tf.AddPoint(0.8f, new Vector4(1, 0, 0, 1));
        tf.AddPoint(0.2f, new Vector4(0, 1, 0, 1));

        var at02 = tf.Evaluate(0.2f);
        var at08 = tf.Evaluate(0.8f);

        at02.Y.Should().BeApproximately(1f, 0.01f); // green at 0.2
        at08.X.Should().BeApproximately(1f, 0.01f); // red at 0.8
    }

    [Fact]
    public void GenerateTextureData_ReturnsCorrectSize()
    {
        var tf = new TransferFunction();

        var data = tf.GenerateTextureData(256);

        data.Should().HaveCount(256 * 4); // RGBA
    }

    [Fact]
    public void GenerateTextureData_FirstPixelIsTransparent()
    {
        var tf = new TransferFunction();

        var data = tf.GenerateTextureData(256);

        // First pixel (value=0) should have alpha=0
        data[3].Should().Be(0); // alpha of first pixel
    }

    [Fact]
    public void CreateBone_ReturnsNonDefault()
    {
        var bone = TransferFunction.CreateBone();

        var atMid = bone.Evaluate(0.5f);

        // Bone preset should not be a simple linear gradient
        atMid.Should().NotBe(new Vector4(0.5f, 0.5f, 0.5f, 0.5f));
    }

    [Fact]
    public void CreateSoftTissue_ReturnsNonDefault()
    {
        var soft = TransferFunction.CreateSoftTissue();

        var atMid = soft.Evaluate(0.5f);

        // Should have reddish tint
        atMid.X.Should().BeGreaterThan(atMid.Y); // R > G
    }
}
