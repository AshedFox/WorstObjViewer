// Licensed to the.NET Foundation under one or more agreements.
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
    // SINGLETON INSTANCE
    public static SceneManager Instance { get; } = new();

    private Model? _model;

    private readonly List<CancellationTokenSource> _list = new();

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
            if (_list.Count > 6)
            {
                for (var i = 0; i < _list.Count / 2; i++)
                {
                    if (!_list[i].IsCancellationRequested)
                    {
                        _list[i].Cancel();
                    }
                }
            }

            WriteableBitmap.Lock();

            if (Model is { } model && MainCamera is { } camera && WriteableBitmap is { } bitmap)
            {
                Vector2[] projected = model.WorldVertices.Select(v => camera.ProjectToScreen(v)).ToArray();

                var bytesPerPixel = bitmap.Format.BitsPerPixel / 8;
                var size = ViewportHeight * ViewportWidth * bytesPerPixel;

                Int32Rect rect = new(0, 0, ViewportWidth, ViewportHeight);

                var pixels = new byte[size];

                CancellationTokenSource cts = new();
                _list.Add(cts);

                try
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            Parallel.ForEach(model.Polygons,
                                new ParallelOptions()
                                {
                                    MaxDegreeOfParallelism = Environment.ProcessorCount - 1,
                                    CancellationToken = cts.Token
                                },
                                (polygon) =>
                                {
                                    for (var i = 0; i < polygon.Count; i++)
                                    {
                                        var vertexIndex1 = polygon[i].VertexIndex - 1;
                                        var vertexIndex2 = polygon[(i + 1) % polygon.Count].VertexIndex - 1;

                                        if (camera.IsInView(model.WorldVertices[vertexIndex1]) ||
                                            camera.IsInView(model.WorldVertices[vertexIndex2]))
                                        {
                                            //Vector2 v1 = camera.ProjectToScreen(model.WorldVertices[vertexIndex1]);
                                            //Vector2 v2 = camera.ProjectToScreen(model.WorldVertices[vertexIndex2]);
                                            Vector2 v1 = projected[vertexIndex1];
                                            Vector2 v2 = projected[vertexIndex2];

                                            var x1 = (int)Math.Floor(v1.X);
                                            var x2 = (int)Math.Floor(v2.X);
                                            var y1 = (int)Math.Floor(v1.Y);
                                            var y2 = (int)Math.Floor(v2.Y);
                                            var dx = Math.Abs(x2 - x1);
                                            var dy = Math.Abs(y2 - y1);

                                            var error = 2 * dy - dx;
                                            for (var j = 0; j <= dx; j++)
                                            {
                                                var offset = (x1 + y1 * rect.Width) * bytesPerPixel;
                                                if (offset >= 0 && offset < pixels.Length)
                                                {
                                                    pixels[offset] = 255;
                                                }

                                                x1 = x1 < x2 ? x1 + 1 : x1 - 1;

                                                if (error < 0)
                                                {
                                                    error = error + 2 * dy;
                                                }
                                                else
                                                {
                                                    y1 = y1 < y2 ? y1 + 1 : y1 - 1;
                                                    error = error + 2 * dy - 2 * dx;
                                                }
                                            }
                                        }
                                    }
                                }
                            );
                        }
                        catch
                        {
                            cts.Token.ThrowIfCancellationRequested();
                        }
                    }, cts.Token);

                    bitmap.WritePixels(rect, pixels, rect.Width * bytesPerPixel, 0);
                }
                catch
                {
                    // ignored
                }
                finally
                {
                    _list.Remove(cts);
                    cts.Dispose();
                }
            }

            WriteableBitmap.Unlock();

            OnChange();
        }
    }

    protected virtual void OnChange() => ChangeEvent?.Invoke();
}
