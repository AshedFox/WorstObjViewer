using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lab1.Lib.Enums;
using Lab1.Lib.Helpers;
using Lab1.Lib.Helpers.Shadow;
using Lab1.Lib.Types;
using Color = Lab1.Lib.Types.Color;

namespace Lab1.App;

public class SceneManager
{
    public delegate void ChangeHandler();

    private readonly Vector3 _lightVector = Vector3.Normalize(new Vector3(-1f, -1f, -1f));

    private Color[] _colorsBuffer = [];
    private Color[] _brightColorsBuffer = [];
    private Color[] _bloomTempbuffer = [];

    private byte[] _pixels = [];

    private int[] _zBuffer = [];

    private bool _isDirty = false;
    private bool _isRendering = false;

    private SceneManager()
    {
        MainCamera = new Camera(0, 0, 0, 0, 0, 0);
    }

    public static SceneManager Instance { get; } = new();

    public Camera MainCamera { get; private set; }
    public WriteableBitmap? WriteableBitmap { get; private set; }
    public Model? Model { get; set; }
    public int ViewportWidth { get; private set; }
    public int ViewportHeight { get; private set; }
    public ShadowType ShadowType { get; private set; } = ShadowType.PhongLight;
    public bool WithBloom { get; private set; }

    public event ChangeHandler? ChangeEvent;

    public void Init(int width, int height)
    {
        MainCamera = new Camera(width, height, 80.0f, GraphicsProcessor.ConvertDegreesToRadians(45), 1f, 2000.0f);
        MainCamera.Change += RequestRender;
        Resize(width, height);
    }

    public void ChangeShadow(ShadowType shadowType)
    {
        ShadowType = shadowType;
        RequestRender();
    }

    public void ChangeBloom(bool withBloom)
    {
        WithBloom = withBloom;
        RequestRender();
    }

    public void ChangeModel(Model model)
    {
        Model = model;
        Model.Change += RequestRender;
        MainCamera?.Reset();
        RequestRender();
    }

    public void Resize(int width, int height)
    {
        ViewportWidth = width;
        ViewportHeight = height;

        var size = width * height;
        _colorsBuffer = new Color[size];
        _brightColorsBuffer = new Color[size];
        _bloomTempbuffer = new Color[size];
        _zBuffer = new int[size];
        _pixels = new byte[size * 3];

        WriteableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);

        if (MainCamera != null)
        {
            MainCamera.ViewportWidth = width;
            MainCamera.ViewportHeight = height;
        }

        RequestRender();
    }

    public void RequestRender()
    {
        _isDirty = true;
        if (!_isRendering)
        {
            _ = RenderLoop();
        }
    }

    private async Task RenderLoop()
    {
        _isRendering = true;
        while (_isDirty)
        {
            _isDirty = false;
            await DrawFrame();
        }
        _isRendering = false;
        OnChange();
    }

    private async Task DrawFrame()
    {
        if (Model is not { } model || MainCamera is not { } camera || WriteableBitmap is not { } bitmap)
        {
            return;
        }

        Array.Clear(_colorsBuffer, 0, _colorsBuffer.Length);
        Array.Fill(_zBuffer, int.MaxValue);

        var width = ViewportWidth;

        await Task.Run(() =>
        {
            var screenVertices = model.WorldVertices.Select(v => camera.ProjectToScreen(v)).ToArray();

            Parallel.ForEach(Partitioner.Create(model.Polygons), polygon =>
            {
                IShadowProcessor? shadowProcessor = ShadowType switch
                {
                    ShadowType.None => null,
                    ShadowType.Lambert => new LambertShadowProcessor(),
                    ShadowType.Gouraud => new GouraudShadowProcessor(),
                    ShadowType.PhongShadow => new PhongShadowProcessor(),
                    ShadowType.PhongLight => new PhongLightProcessor(0.05f, 0.7f, 0.5f, 20),
                    _ => null
                };

                GraphicsProcessor.FillPolygonColors(_colorsBuffer, _zBuffer, polygon,
                    screenVertices, model, camera, shadowProcessor, _lightVector
                );
            });

            if (WithBloom && model.EmissionTexture != null)
            {
                ApplyBloom();
            }

            Parallel.For(0, _colorsBuffer.Length, i =>
            {
                var color = GraphicsProcessor.ACESMapTone(_colorsBuffer[i]) * 255 * _colorsBuffer[i].Alpha;
                var idx = i * 3;
                _pixels[idx] = (byte)color.Red;
                _pixels[idx + 1] = (byte)color.Green;
                _pixels[idx + 2] = (byte)color.Blue;
            });
        });

        Int32Rect rect = new(0, 0, ViewportWidth, ViewportHeight);
        bitmap.WritePixels(rect, _pixels, ViewportWidth * 3, 0);
    }

    private void ApplyBloom()
    {
        Parallel.For(0, _colorsBuffer.Length, i =>
        {
            var c = _colorsBuffer[i];
            if (0.2126 * c.Red + 0.7152 * c.Green + 0.0722 * c.Blue > 1)
                _brightColorsBuffer[i] = c;
            else
                _brightColorsBuffer[i] = Color.Zero;
        });

        Parallel.For(0, _colorsBuffer.Length, i =>
        {
            _colorsBuffer[i] += _brightColorsBuffer[i];
        });
    }

    protected virtual void OnChange() => ChangeEvent?.Invoke();
}
