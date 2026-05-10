using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace YdcViewer.Renderer;

public class RenderEngine : IDisposable
{
    private NativeWindow? _window;
    private int _volumeTexture;
    private int _transferTexture;
    private int _shaderProgram;
    private int _vao;
    private int _vbo;
    private int _fbo;
    private int _fboTexture;
    private bool _initialized;
    private int _width = 512;
    private int _height = 512;

    public void Initialize()
    {
        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(1, 1),
            Title = "YdcViewer Render",
            APIVersion = new Version(4, 1),
            Flags = OpenTK.Windowing.Common.ContextFlags.Offscreen,
        };

        _window = new NativeWindow(nativeWindowSettings);
        _window.MakeCurrent();

        GL.ClearColor(0f, 0f, 0f, 1f);
        GL.Enable(EnableCap.DepthTest);

        CreateShaders();
        CreateQuad();
        CreateFBO();

        _initialized = true;
    }

    public void UploadVolume(YdcViewer.Dicom.VolumeData volume)
    {
        EnsureInitialized();

        // Upload 3D texture
        GL.DeleteTexture(_volumeTexture);
        _volumeTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture3D, _volumeTexture);

        var internalFormat = volume.BitsPerVoxel == 16
            ? (volume.IsSigned ? PixelInternalFormat.R16 : PixelInternalFormat.R16)
            : PixelInternalFormat.R8;

        var pixelFormat = volume.BitsPerVoxel == 16 ? PixelFormat.Red : PixelFormat.Red;
        var pixelType = volume.BitsPerVoxel == 16
            ? (volume.IsSigned ? PixelType.Short : PixelType.UnsignedShort)
            : PixelType.UnsignedByte;

        GL.TexImage3D(TextureTarget.Texture3D, 0, internalFormat,
            volume.Width, volume.Height, volume.Depth,
            0, pixelFormat, pixelType, volume.VoxelBytes);

        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
    }

    public void UploadTransferFunction(TransferFunction tf, int size = 256)
    {
        EnsureInitialized();

        var data = tf.GenerateTextureData(size);

        GL.DeleteTexture(_transferTexture);
        _transferTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture1D, _transferTexture);

        GL.TexImage1D(TextureTarget.Texture1D, 0, PixelInternalFormat.Rgba8,
            size, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

        GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
    }

    public byte[] RenderFrame(Camera camera, int width, int height)
    {
        EnsureInitialized();

        if (width != _width || height != _height)
        {
            _width = width;
            _height = height;
            ResizeFBO(width, height);
        }

        GL.Viewport(0, 0, width, height);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.UseProgram(_shaderProgram);

        // Set uniforms
        var view = camera.GetViewMatrix();
        var proj = camera.GetProjectionMatrix((float)width / height);
        var invViewProj = Matrix4.Invert(view * proj);

        SetMatrix4("uView", view);
        SetMatrix4("uProjection", proj);
        SetMatrix4("uInvViewProj", invViewProj);
        SetFloat("uStepSize", 0.005f);
        SetFloat("uThreshold", 0.1f);

        // Bind textures
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture3D, _volumeTexture);
        GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "uVolume"), 0);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture1D, _transferTexture);
        GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "uTransferFunction"), 1);

        // Draw fullscreen quad
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

        // Read pixels
        var pixels = new byte[width * height * 3];
        GL.ReadPixels(0, 0, width, height, PixelFormat.Rgb, PixelType.UnsignedByte, pixels);

        // Flip vertically (OpenGL origin is bottom-left)
        var flipped = new byte[pixels.Length];
        var rowSize = width * 3;
        for (int y = 0; y < height; y++)
        {
            System.Buffer.BlockCopy(pixels, y * rowSize, flipped, (height - 1 - y) * rowSize, rowSize);
        }

        return flipped;
    }

    private void CreateShaders()
    {
        var vertexSource = @"
#version 410 core
layout(location = 0) in vec2 aPos;
out vec2 vUV;
void main() {
    vUV = aPos * 0.5 + 0.5;
    gl_Position = vec4(aPos, 0.0, 1.0);
}";

        var fragmentSource = @"
#version 410 core
in vec2 vUV;
out vec4 FragColor;

uniform sampler3D uVolume;
uniform sampler1D uTransferFunction;
uniform mat4 uInvViewProj;
uniform float uStepSize;
uniform float uThreshold;

void main() {
    // Compute ray direction from screen UV
    vec4 ndc = vec4(vUV * 2.0 - 1.0, 1.0, 1.0);
    vec4 worldNear = uInvViewProj * ndc;
    worldNear /= worldNear.w;
    ndc.z = -1.0;
    vec4 worldFar = uInvViewProj * ndc;
    worldFar /= worldFar.w;

    vec3 rayOrigin = worldNear.xyz;
    vec3 rayDir = normalize(worldFar.xyz - worldNear.xyz);

    // Intersect with unit cube [0,1]
    vec3 tMin = (vec3(0.0) - rayOrigin) / rayDir;
    vec3 tMax = (vec3(1.0) - rayOrigin) / rayDir;
    vec3 t1 = min(tMin, tMax);
    vec3 t2 = max(tMin, tMax);
    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);

    if (tNear > tFar || tFar < 0.0) {
        FragColor = vec4(0.0);
        return;
    }

    tNear = max(tNear, 0.0);

    // March along ray
    vec3 pos = rayOrigin + rayDir * tNear;
    vec4 color = vec4(0.0);
    float t = tNear;

    for (int i = 0; i < 512 && t < tFar; i++) {
        float density = texture(uVolume, pos).r;
        vec4 sample = texture(uTransferFunction, density);

        // Front-to-back compositing
        color.rgb += (1.0 - color.a) * sample.a * sample.rgb;
        color.a += (1.0 - color.a) * sample.a;

        if (color.a > 0.99) break;

        pos += rayDir * uStepSize;
        t += uStepSize;
    }

    FragColor = color;
}";

        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexSource);
        GL.CompileShader(vertexShader);
        CheckShaderError(vertexShader, "VERTEX");

        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentSource);
        GL.CompileShader(fragmentShader);
        CheckShaderError(fragmentShader, "FRAGMENT");

        _shaderProgram = GL.CreateProgram();
        GL.AttachShader(_shaderProgram, vertexShader);
        GL.AttachShader(_shaderProgram, fragmentShader);
        GL.LinkProgram(_shaderProgram);
        CheckProgramError(_shaderProgram);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    private void CreateQuad()
    {
        float[] quadVertices = {
            -1f, -1f,
             1f, -1f,
            -1f,  1f,
             1f,  1f,
        };

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);

        GL.BindVertexArray(0);
    }

    private void CreateFBO()
    {
        _fbo = GL.GenFramebuffer();
        _fboTexture = GL.GenTexture();

        ResizeFBO(_width, _height);
    }

    private void ResizeFBO(int width, int height)
    {
        GL.BindTexture(TextureTarget.Texture2D, _fboTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb8,
            width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, _fboTexture, 0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void SetMatrix4(string name, Matrix4 value)
    {
        var loc = GL.GetUniformLocation(_shaderProgram, name);
        GL.UniformMatrix4(loc, false, ref value);
    }

    private void SetFloat(string name, float value)
    {
        var loc = GL.GetUniformLocation(_shaderProgram, name);
        GL.Uniform1(loc, value);
    }

    private static void CheckShaderError(int shader, string stage)
    {
        GL.GetShader(shader, ShaderParameter.CompileStatus, out var status);
        if (status == 0)
        {
            var log = GL.GetShaderInfoLog(shader);
            throw new InvalidOperationException($"{stage} shader compile error: {log}");
        }
    }

    private static void CheckProgramError(int program)
    {
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var status);
        if (status == 0)
        {
            var log = GL.GetProgramInfoLog(program);
            throw new InvalidOperationException($"Program link error: {log}");
        }
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("RenderEngine not initialized. Call Initialize() first.");
    }

    public void Dispose()
    {
        if (_initialized)
        {
            GL.DeleteTexture(_volumeTexture);
            GL.DeleteTexture(_transferTexture);
            GL.DeleteTexture(_fboTexture);
            GL.DeleteFramebuffer(_fbo);
            GL.DeleteProgram(_shaderProgram);
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            _window?.Dispose();
        }
    }
}
