// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

    private const int FramesPerSecond = 60;
    private const double FramePeriod = 1000d / FramesPerSecond;

    private static readonly float[] s_gaussianWeights =
    {
        0.2270270270f, 0.1945945946f, 0.1216216216f, 0.0540540541f, 0.0162162162f
    };

    private readonly Vector3 _lightVector = Vector3.Normalize(new Vector3(-1f, -1f, -1f));
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly PeriodicTimer _timer;
    private Color[] _brightColorsBuffer = Array.Empty<Color>();

    private Color[] _colorsBuffer = Array.Empty<Color>();
    private CancellationTokenSource? _cts;
    private long _currentFrameStartAt;
    private SpinLock[] _locks = Array.Empty<SpinLock>();
    private byte[] _pixels = Array.Empty<byte>();
    private Queue<long> _renders = new();
    private float[] _zBuffer = Array.Empty<float>();

    private SceneManager()
    {
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(FramePeriod));
        Draw();
    }

    public static SceneManager Instance { get; } = new();

    public Camera MainCamera { get; private set; } = new(0, 0, 80.0f,
        GraphicsProcessor.ConvertDegreesToRadians(45), 1f, 1000.0f
    );

    public WriteableBitmap? WriteableBitmap { get; private set; }

    public Model? Model { get; set; }

    public int ViewportWidth { get; private set; }
    public int ViewportHeight { get; private set; }
    public ShadowType ShadowType { get; private set; }
    public bool WithBloom { get; private set; }

    public event ChangeHandler? ChangeEvent;

    public void Init(int width, int height)
    {
        _renders = new Queue<long>();

        if (_cts is not null)
        {
            _cts.Cancel();
        }

        _cts = null;
        _currentFrameStartAt = 0;

        _pixels = Array.Empty<byte>();
        _zBuffer = Array.Empty<float>();
        _locks = Array.Empty<SpinLock>();

        ViewportWidth = width;
        ViewportHeight = height;
        WriteableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
        MainCamera = new Camera(width, height, 80.0f, GraphicsProcessor.ConvertDegreesToRadians(45),
            1f, 2000.0f
        );
        ShadowType = ShadowType.Lambert;
        MainCamera.Change += AddFrame;

        AddFrame();
    }

    public void ChangeShadow(ShadowType shadowType)
    {
        ShadowType = shadowType;

        AddFrame();
    }

    public void ChangeBloom(bool withBloom)
    {
        WithBloom = withBloom;

        AddFrame();
    }

    public void ChangeModel(Model model)
    {
        _renders = new Queue<long>();

        if (_cts is not null)
        {
            _cts.Cancel();
        }

        _cts = null;

        _currentFrameStartAt = 0;

        _pixels = Array.Empty<byte>();
        _zBuffer = Array.Empty<float>();
        _locks = Array.Empty<SpinLock>();

        Model = model;
        Model.Change += AddFrame;
        MainCamera.Reset();

        AddFrame();
    }

    public void Resize(int width, int height)
    {
        ViewportWidth = width;
        ViewportHeight = height;
        WriteableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
        MainCamera.ViewportWidth = width;
        MainCamera.ViewportHeight = height;

        AddFrame();
    }

    public void AddFrame() => _renders.Enqueue(DateTimeOffset.Now.ToUnixTimeMilliseconds());

    private async void Draw()
    {
        while (await _timer.WaitForNextTickAsync())
        {
            ProcessFrame();
        }
    }

    private async void ProcessFrame()
    {
        if (_renders.Count > 0)
        {
            if (_semaphore.CurrentCount == 0)
            {
                if (_renders.Count > 1)
                {
                    _renders.TryDequeue(out _);
                }

                return;
            }

            if (await _semaphore.WaitAsync((int)(10 * FramePeriod)))
            {
                _renders.TryDequeue(out _);

                _cts = new CancellationTokenSource();

                _currentFrameStartAt = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                await DrawFrame();

                while (_renders.TryPeek(out var time) && time < _currentFrameStartAt)
                {
                    _renders.TryDequeue(out _);
                }

                OnChange();

                if (_cts is not null)
                {
                    _cts.Dispose();
                    _cts = null;
                }

                if (_semaphore.CurrentCount == 0)
                {
                    _semaphore.Release();
                }
            }
        }
    }

    private async Task DrawFrame()
    {
        if (Model is { } model && MainCamera is { } camera && WriteableBitmap is { } bitmap)
        {
            var bytesPerPixel = bitmap.Format.BitsPerPixel / 8;
            var size = ViewportHeight * ViewportWidth;

            Int32Rect rect = new(0, 0, ViewportWidth, ViewportHeight);

            if (_colorsBuffer.Length != size)
            {
                Array.Resize(ref _colorsBuffer, size);
            }

            Array.Fill(_colorsBuffer, new Color(0));

            if (_pixels.Length != size * bytesPerPixel)
            {
                Array.Resize(ref _pixels, size * bytesPerPixel);
            }

            if (_zBuffer.Length != size)
            {
                Array.Resize(ref _zBuffer, size);
            }

            Array.Fill(_zBuffer, 0);

            if (_locks.Length != size)
            {
                Array.Resize(ref _locks, size);
            }

            await Task.Run(() =>
            {
                Vector4[] screenVertices = model.WorldVertices.Select(v => camera.ProjectToScreen(v)).ToArray();

                Parallel.ForEach(Partitioner.Create(model.Polygons),
                    polygon =>
                    {
                        if (_cts is { IsCancellationRequested: true })
                        {
                            return;
                        }

                        IShadowProcessor? shadowProcessor;

                        switch (ShadowType)
                        {
                            case ShadowType.None:
                                shadowProcessor = null;
                                break;
                            case ShadowType.Lambert:
                                shadowProcessor = new LambertShadowProcessor();
                                break;
                            case ShadowType.Gouraud:
                                shadowProcessor = new GouraudShadowProcessor();
                                break;
                            case ShadowType.PhongShadow:
                                shadowProcessor = new PhongShadowProcessor();
                                break;
                            case ShadowType.PhongLight:
                                shadowProcessor = new PhongLightProcessor(0.05f, 0.7f, 0.5f, 20);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        GraphicsProcessor.FillPolygonColors(ref _colorsBuffer, ref _zBuffer, ref _locks, ref polygon,
                            ref screenVertices, ref model, ref camera, shadowProcessor, _lightVector
                        );
                    }
                );

                if (!WithBloom)
                {
                    Parallel.For(0, _colorsBuffer.Length, i =>
                    {
                        if (_cts is { IsCancellationRequested: true })
                        {
                            return;
                        }

                        Color color = GraphicsProcessor.ACESMapTone(_colorsBuffer[i]) * 255 * _colorsBuffer[i].Alpha;

                        _pixels[bytesPerPixel * i] = (byte)Math.Min(color.Red, 255);
                        _pixels[bytesPerPixel * i + 1] = (byte)Math.Min(color.Green, 255);
                        _pixels[bytesPerPixel * i + 2] = (byte)Math.Min(color.Blue, 255);
                    });
                }
                else
                {
                    if (_brightColorsBuffer.Length != size)
                    {
                        Array.Resize(ref _brightColorsBuffer, size);
                    }

                    Parallel.For(0, _colorsBuffer.Length, i =>
                    {
                        /*if (0.2126 * _colorsBuffer[i].Red + 0.7152 * _colorsBuffer[i].Green +
                            0.0722 * _colorsBuffer[i].Blue > 1)*/
                        if (0.299 * _colorsBuffer[i].Red + 0.587 * _colorsBuffer[i].Green +
                            0.114 * _colorsBuffer[i].Blue > 1)
                        {
                            _brightColorsBuffer[i] = _colorsBuffer[i];
                        }
                        else
                        {
                            _brightColorsBuffer[i] = new Color(0);
                        }
                    });

                    var m = _colorsBuffer.Average(c => c.Red + c.Green + c.Blue);
                    var sum = _colorsBuffer.Sum(c => MathF.Pow(c.Red + c.Green + c.Blue - m, 2));
                    var sigma = MathF.Sqrt(sum / (_colorsBuffer.Length - 1));

                    var weights = new float[(int)Math.Round(sigma * 3)];
                    for (var i = 0; i < weights.Length; i++)
                    {
                        weights[i] = 1 / MathF.Sqrt(2 * MathF.PI * sigma * sigma) *
                                     MathF.Exp(-i * i / (2 * sigma * sigma));
                    }

                    Color[] buffer = new Color[_brightColorsBuffer.Length];

                    _brightColorsBuffer.CopyTo(buffer, 0);

                    Parallel.For(0, buffer.Length, offset =>
                    {
                        if (_cts is { IsCancellationRequested: true })
                        {
                            return;
                        }

                        buffer[offset] += _brightColorsBuffer[offset] * s_gaussianWeights[0];

                        for (var j = 1; j < s_gaussianWeights.Length; j++)
                        {
                            var index = offset + j;

                            if (index / ViewportWidth == offset / ViewportWidth &&
                                index < _brightColorsBuffer.Length && index >= 0)
                            {
                                buffer[offset] += _brightColorsBuffer[index] * s_gaussianWeights[j];
                            }

                            index = offset - j;

                            if (index / ViewportWidth == offset / ViewportWidth &&
                                index < _brightColorsBuffer.Length && index >= 0)
                            {
                                buffer[offset] += _brightColorsBuffer[index] * s_gaussianWeights[j];
                            }

                            index = offset + j * ViewportWidth;

                            if (index < _brightColorsBuffer.Length && index >= 0)
                            {
                                buffer[offset] += _brightColorsBuffer[index] * s_gaussianWeights[j];
                            }

                            index = offset - j * ViewportWidth;

                            if (index < _brightColorsBuffer.Length && index >= 0)
                            {
                                buffer[offset] += _brightColorsBuffer[index] * s_gaussianWeights[j];
                            }
                        }
                    });

                    buffer.CopyTo(_brightColorsBuffer, 0);

                    Parallel.For(0, _colorsBuffer.Length, i =>
                    {
                        if (_cts is { IsCancellationRequested: true })
                        {
                            return;
                        }

                        Color color = GraphicsProcessor.ACESMapTone(_colorsBuffer[i] + _brightColorsBuffer[i]) *
                                      255 * _colorsBuffer[i].Alpha;

                        _pixels[bytesPerPixel * i] = (byte)Math.Min(color.Red, 255);
                        _pixels[bytesPerPixel * i + 1] = (byte)Math.Min(color.Green, 255);
                        _pixels[bytesPerPixel * i + 2] = (byte)Math.Min(color.Blue, 255);
                    });
                }
            });

            if (_cts is { IsCancellationRequested: true })
            {
                return;
            }

            bitmap.WritePixels(rect, _pixels, rect.Width * bytesPerPixel, 0);
        }
    }

    protected virtual void OnChange() => ChangeEvent?.Invoke();
}
