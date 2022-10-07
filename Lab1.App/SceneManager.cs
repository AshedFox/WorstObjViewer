// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lab1.Lib.Enums;
using Lab1.Lib.Helpers;
using Lab1.Lib.Types;

namespace Lab1.App;

public class SceneManager
{
    public static SceneManager Instance { get; } = new();

    private byte[] _pixels = Array.Empty<byte>();
    private float[] _zBuffer = Array.Empty<float>();
    private SpinLock[] _locks = Array.Empty<SpinLock>();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Vector3 _lightVector = Vector3.Normalize(new Vector3(-1f, -1f, -1f));
    private Dictionary<long, long> _renders = new();

    public delegate void ChangeHandler();

    public event ChangeHandler? ChangeEvent;

    public Camera MainCamera { get; private set; } = new(0, 0, 80.0f,
        GraphicsProcessor.ConvertDegreesToRadians(45), .1f, 1000.0f
    );

    public WriteableBitmap? WriteableBitmap { get; private set; }

    public Model? Model { get; set; }

    public int ViewportWidth { get; private set; }
    public int ViewportHeight { get; private set; }
    public ShadowType ShadowType { get; private set; }

    private SceneManager()
    {
    }

    public void Init(int width, int height)
    {
        _renders = new Dictionary<long, long>();
        _pixels = Array.Empty<byte>();
        _zBuffer = Array.Empty<float>();
        _locks = Array.Empty<SpinLock>();

        ViewportWidth = width;
        ViewportHeight = height;
        WriteableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
        MainCamera = new Camera(width, height, 80.0f, GraphicsProcessor.ConvertDegreesToRadians(45),
            .1f, 200.0f
        );
        ShadowType = ShadowType.Lambert;
        MainCamera.Change += Redraw;

        Redraw();
    }

    public void ChangeShadow(ShadowType shadowType)
    {
        ShadowType = shadowType;

        Redraw();
    }

    public void ChangeModel(Model model)
    {
        _renders = new Dictionary<long, long>();
        _pixels = Array.Empty<byte>();
        _zBuffer = Array.Empty<float>();
        _locks = Array.Empty<SpinLock>();

        Model = model;
        MainCamera.Reset();

        Redraw();
    }

    public void Resize(int width, int height)
    {
        ViewportWidth = width;
        ViewportHeight = height;
        WriteableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
        MainCamera.ViewportWidth = width;
        MainCamera.ViewportHeight = height;

        Redraw();
    }

    private async void Redraw()
    {
        if (WriteableBitmap is not null)
        {
            if (Model is { } model && MainCamera is { } camera && WriteableBitmap is { } bitmap)
            {
                var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                if (!_renders.TryAdd(time, time))
                {
                    return;
                }

                await _semaphore.WaitAsync();

                if (!_renders.ContainsKey(time))
                {
                    _semaphore.Release();
                    return;
                }

                CancellationTokenSource cancelSource = new();

                var bytesPerPixel = bitmap.Format.BitsPerPixel / 8;
                var size = ViewportHeight * ViewportWidth * bytesPerPixel;

                Int32Rect rect = new(0, 0, ViewportWidth, ViewportHeight);

                if (_pixels.Length != size)
                {
                    Array.Resize(ref _pixels, size);
                }

                Array.Fill(_pixels, (byte)0);

                if (_zBuffer.Length != ViewportHeight * ViewportWidth)
                {
                    Array.Resize(ref _zBuffer, ViewportHeight * ViewportWidth);
                }

                Array.Fill(_zBuffer, 0);

                if (_locks.Length != ViewportHeight * ViewportWidth)
                {
                    Array.Resize(ref _locks, ViewportHeight * ViewportWidth);
                }

                cancelSource.CancelAfter(1000);

                await Task.Run(() =>
                {
                    Vector3[] screenVertices = model.WorldVertices.Select((v) => camera.ProjectToScreen(v)).ToArray();

                    Parallel.ForEach(model.Polygons,
                        new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount - 1 },
                        (polygon) =>
                        {
                            if (cancelSource.IsCancellationRequested)
                            {
                                return;
                            }

                            GraphicsProcessor.DrawPolygon(ref _pixels, ref _zBuffer, ref _locks, ref polygon,
                                ref screenVertices, ref model, ref camera, _lightVector, ShadowType
                            );
                        }
                    );
                }, cancelSource.Token);

                foreach (KeyValuePair<long, long> render in _renders)
                {
                    if (render.Value < DateTimeOffset.Now.ToUnixTimeMilliseconds() - 5)
                    {
                        _renders.Remove(render.Key);
                    }
                }

                bitmap.WritePixels(rect, _pixels, rect.Width * bytesPerPixel, 0);

                _semaphore.Release();
            }

            OnChange();
        }
    }

    protected virtual void OnChange() => ChangeEvent?.Invoke();
}
