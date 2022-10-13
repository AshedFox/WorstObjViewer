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

namespace Lab1.App;

public class SceneManager
{
    public delegate void ChangeHandler();

    private readonly Vector3 _lightVector = Vector3.Normalize(new Vector3(-1f, -1f, -1f));
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private SpinLock[] _locks = Array.Empty<SpinLock>();
    private readonly PeriodicTimer _timer;
    private CancellationTokenSource? _cts;
    private const int FramesPerSecond = 60;
    private const double FramePeriod = 1000d / FramesPerSecond;

    private byte[] _pixels = Array.Empty<byte>();
    private List<long> _renders = new();
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

    public event ChangeHandler? ChangeEvent;

    public void Init(int width, int height)
    {
        if (_cts is not null)
        {
            _cts.Cancel();
        }

        _cts = new CancellationTokenSource();

        _renders = new List<long>();
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

    public void ChangeModel(Model model)
    {
        if (_cts is not null)
        {
            _cts.Cancel();
        }

        _cts = new CancellationTokenSource();

        _renders = new List<long>();
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

    public void AddFrame()
    {
        _renders.Add(DateTimeOffset.Now.ToUnixTimeMilliseconds());
    }

    private async void Draw()
    {
        while (await _timer.WaitForNextTickAsync())
        {
            if (_renders.Count > 0)
            {
                await _semaphore.WaitAsync();

                var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                _cts = new CancellationTokenSource();

                await Redraw();

                _renders = _renders.Where((t) => t >= startTime).ToList();

                _cts.Dispose();
                _cts = null;

                _semaphore.Release();

                OnChange();
            }
        }
    }

    private async Task Redraw()
    {
        if (Model is { } model && MainCamera is { } camera && WriteableBitmap is { } bitmap)
        {
            var bytesPerPixel = bitmap.Format.BitsPerPixel / 8;
            var size = ViewportHeight * ViewportWidth;

            Int32Rect rect = new(0, 0, ViewportWidth, ViewportHeight);

            if (_pixels.Length != size * bytesPerPixel)
            {
                Array.Resize(ref _pixels, size * bytesPerPixel);
            }

            Array.Fill(_pixels, (byte)0);

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
                                shadowProcessor = new PhongLightProcessor(0.01f, 0.7f, 0.5f, 20);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        GraphicsProcessor.DrawPolygon(ref _pixels, ref _zBuffer, ref _locks, ref polygon,
                            ref screenVertices, ref model, ref camera, shadowProcessor, _lightVector
                        );
                    }
                );
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
