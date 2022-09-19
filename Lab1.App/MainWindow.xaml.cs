using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lab1.Lib.Helpers;
using Lab1.Lib.Types;
using Microsoft.Win32;

namespace Lab1.App;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Camera = new Camera(0,
            0, 80.0f,
            GraphicsProcessor.ConvertDegreesToRadians(45), .1f, 100.0f
        );
        AutoScaleMenuItem.IsEnabled = false;
        Camera.Change += async () =>
        {
            CoordsLabel.Content =
                $"Polar: {GraphicsProcessor.ConvertRadiansToDegrees(Camera.PolarAngle)}; " +
                $"Azimuthal: {GraphicsProcessor.ConvertRadiansToDegrees(Camera.AzimuthalAngle)}; " +
                $"Eye: {Camera.Pivot.Position};";

            if (Model is { } model && Camera is { ViewportWidth: > 0, ViewportHeight: > 0 })
            {
                WriteableBitmap bitmap = new(Camera.ViewportWidth, Camera.ViewportHeight, 96, 96,
                    PixelFormats.Gray8, null
                );
                var width = bitmap.PixelWidth;
                var bytesPerPixel = bitmap.Format.BitsPerPixel / 8;
                Int32Rect rect = new(0, 0, Camera.ViewportWidth, Camera.ViewportHeight);
                var size = Camera.ViewportWidth * Camera.ViewportHeight * bytesPerPixel;
                var pixels = new byte[size];

                Array.Fill(pixels, (byte)0xc1);

                await Task.Run(() =>
                {
                    Parallel.ForEach(model.Polygons, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, (polygon) =>
                    {
                        for (var i = 0; i < polygon.Count; i++)
                        {
                            var vertexIndex1 = polygon[i].VertexIndex - 1;
                            var vertexIndex2 = polygon[(i + 1) % polygon.Count].VertexIndex - 1;

                            if (Camera.IsInView(model.WorldVertices[vertexIndex1]) ||
                                Camera.IsInView(model.WorldVertices[vertexIndex2]))
                            {
                                Vector2 v1 = Camera.ProjectToScreen(model.WorldVertices[vertexIndex1]);
                                Vector2 v2 = Camera.ProjectToScreen(model.WorldVertices[vertexIndex2]);

                                var x1 = (int)Math.Floor(v1.X);
                                var x2 = (int)Math.Floor(v2.X);
                                var y1 = (int)Math.Floor(v1.Y);
                                var y2 = (int)Math.Floor(v2.Y);
                                var dx = Math.Abs(x2 - x1);
                                var dy = Math.Abs(y2 - y1);

                                var error = 2 * dy - dx;
                                for (var j = 0; j <= dx; j++)
                                {
                                    var offset = (x1 + y1 * width) * bytesPerPixel;
                                    if (offset >= 0 && offset < pixels.Length)
                                    {
                                        pixels[offset] = 0x1c;
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
                    });
                });

                bitmap.WritePixels(rect, pixels, width * bytesPerPixel, 0);

                ModelImage.Source = bitmap;
            }
        };
    }

    public Camera Camera { get; set; }
    public Model? Model { get; set; }
    public Vector2 TempPoint { get; set; }

    private void ModelCanvas_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            Point endPoint = e.GetPosition(ModelCanvas);
            Vector2 point = new((float)endPoint.X, (float)endPoint.Y);
            var dx = Math.Abs(point.X - TempPoint.X);
            var dy = Math.Abs(point.Y - TempPoint.Y);

            if (dx > SystemParameters.MinimumHorizontalDragDistance ||
                dy > SystemParameters.MinimumVerticalDragDistance)
            {
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    if (dx > dy)
                    {
                        Camera.Rotate(TempPoint, point with { Y = TempPoint.Y });
                    }
                    else
                    {
                        Camera.Rotate(TempPoint, point with { X = TempPoint.X });
                    }
                }
                else
                {
                    Camera.Rotate(TempPoint, point);
                }

                TempPoint = point;
            }
        }
        else if (e.MiddleButton == MouseButtonState.Pressed)
        {
            Point endPoint = e.GetPosition(ModelCanvas);
            Vector2 point = new((float)endPoint.X, (float)endPoint.Y);
            var dx = Math.Abs(point.X - TempPoint.X);
            var dy = Math.Abs(point.Y - TempPoint.Y);

            if (dx > SystemParameters.MinimumHorizontalDragDistance ||
                dy > SystemParameters.MinimumVerticalDragDistance)
            {
                Camera.Target = new Vector3(
                    Camera.Target.X + (point.X - TempPoint.X) / Camera.ViewportWidth,
                    Camera.Target.Y + (point.Y - TempPoint.Y) / Camera.ViewportHeight,
                    Camera.Target.Z
                );
            }
        }
    }

    private void ModelCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Camera.ViewportHeight = (int)e.NewSize.Height;
        Camera.ViewportWidth = (int)e.NewSize.Width;
    }


    private void ModelCanvas_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            Point position = e.GetPosition(ModelCanvas);
            TempPoint = new Vector2((float)position.X, (float)position.Y);
        }
        else if (e.MiddleButton == MouseButtonState.Pressed)
        {
            Point position = e.GetPosition(ModelCanvas);
            TempPoint = new Vector2((float)position.X, (float)position.Y);
        }
    }

    private void OpenFileMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new() { Filter = "Obj files (*.obj)|*.obj" };
        if (openFileDialog.ShowDialog() == true)
        {
            Model = ObjParser.FromObjFile(File.ReadAllLines(openFileDialog.FileName));

            Camera.OnChange();

            AutoScaleMenuItem.IsEnabled = true;
        }
    }

    private void ModelCanvas_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta > 0)
        {
            Camera.Distance -= 1f;
        }
        else if (e.Delta < 0)
        {
            Camera.Distance += 1f;
        }
    }

    private void AutoScaleMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
    }

    private void CenterCameraMenuItem_OnClick(object sender, RoutedEventArgs e) => Camera.Target = Vector3.Zero;
}
