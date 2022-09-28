﻿// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lab1.Lib.Helpers;
using Lab1.Lib.Types;

namespace Lab1.App;

public class SceneManager
{
    public static SceneManager Instance { get; } = new();

    private Model? _model;

    private byte[] _pixels = Array.Empty<byte>();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public delegate void ChangeHandler();

    public event ChangeHandler? ChangeEvent;

    public Camera MainCamera { get; private set; } = new(0, 0, 80.0f,
        GraphicsProcessor.ConvertDegreesToRadians(45), .1f, 1000.0f
    );

    public WriteableBitmap? WriteableBitmap { get; private set; }

    public Model? Model
    {
        get => _model;
        set
        {
            _model = value;
            Redraw();
        }
    }

    public int ViewportWidth { get; private set; }
    public int ViewportHeight { get; private set; }

    private SceneManager()
    {
    }

    public void Init(int width, int height)
    {
        ViewportWidth = width;
        ViewportHeight = height;
        WriteableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
        MainCamera = new Camera(width, height, 80.0f, GraphicsProcessor.ConvertDegreesToRadians(45),
            .1f, 1000.0f
        );
        MainCamera.Change += Redraw;

        OnChange();
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
                try
                {
                    await _semaphore.WaitAsync();

                    var bytesPerPixel = bitmap.Format.BitsPerPixel / 8;
                    var size = ViewportHeight * ViewportWidth * bytesPerPixel;

                    Int32Rect rect = new(0, 0, ViewportWidth, ViewportHeight);

                    if (_pixels.Length != size)
                    {
                        Array.Resize(ref _pixels, size);
                    }

                    Array.Fill(_pixels, (byte)0);

                    await Task.Run(() =>
                    {
                        Parallel.ForEach(model.Polygons,
                            new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount - 1 },
                            (polygon) =>
                            {
                                for (var i = 0; i < polygon.Count; i++)
                                {
                                    var vertexIndex1 = polygon[i].VertexIndex - 1;
                                    var vertexIndex2 = polygon[(i + 1) % polygon.Count].VertexIndex - 1;

                                    if (camera.IsInView(model.WorldVertices[vertexIndex1]) ||
                                        camera.IsInView(model.WorldVertices[vertexIndex2]))
                                    {
                                        Vector2 v1 = camera.ProjectToScreen(model.WorldVertices[vertexIndex1]);
                                        Vector2 v2 = camera.ProjectToScreen(model.WorldVertices[vertexIndex2]);

                                        var x1 = (int)Math.Floor(v1.X);
                                        var x2 = (int)Math.Floor(v2.X);
                                        var y1 = (int)Math.Floor(v1.Y);
                                        var y2 = (int)Math.Floor(v2.Y);
                                        var dx = Math.Abs(x2 - x1);
                                        var dy = Math.Abs(y2 - y1);

                                        var signX = x1 < x2 ? 1 : -1;
                                        var signY = y1 < y2 ? 1 : -1;
                                        var error = dx - dy;

                                        var offset = (x2 + y2 * rect.Width) * bytesPerPixel;
                                        if (offset >= 0 && offset < _pixels.Length)
                                        {
                                            _pixels[offset] = 255;
                                        }


                                        while (x1 != x2 || y1 != y2)
                                        {
                                            offset = (x1 + y1 * rect.Width) * bytesPerPixel;
                                            if (offset >= 0 && offset < _pixels.Length)
                                            {
                                                _pixels[offset] = 255;
                                            }

                                            if (error * 2 > -dy)
                                            {
                                                error -= dy;
                                                x1 += signX;
                                            }
                                            else if (error * 2 < dx)
                                            {
                                                error += dx;
                                                y1 += signY;
                                            }
                                        }
                                    }
                                }
                            }
                        );
                    });

                    bitmap.WritePixels(rect, _pixels, rect.Width * bytesPerPixel, 0);

                    _semaphore.Release();
                }
                catch
                {
                    // ignored
                }
            }

            OnChange();
        }
    }

    protected virtual void OnChange() => ChangeEvent?.Invoke();
}
