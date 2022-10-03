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

        SceneManager.Init(1, 1);

        SceneManager.ChangeEvent += () =>
        {
            CoordsLabel.Content =
                $"Polar: {GraphicsProcessor.ConvertRadiansToDegrees(SceneManager.MainCamera.PolarAngle)}°; " +
                $"Azimuthal: {GraphicsProcessor.ConvertRadiansToDegrees(SceneManager.MainCamera.AzimuthalAngle)}°; " +
                $"Eye: {SceneManager.MainCamera.Pivot.Position}; " +
                $"Distance: {SceneManager.MainCamera.Distance}; " +
                $"Target: {SceneManager.MainCamera.Target};";
            ModelImage.Source = SceneManager.WriteableBitmap;
        };
    }

    public SceneManager SceneManager { get; } = SceneManager.Instance;

    public Vector2 TempPoint { get; set; }
    public bool IsMoving { get; set; } = false;

    private void ModelCanvas_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (IsMoving)
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
                            SceneManager.MainCamera.Rotate(TempPoint, point with { Y = TempPoint.Y });
                        }
                        else
                        {
                            SceneManager.MainCamera.Rotate(TempPoint, point with { X = TempPoint.X });
                        }
                    }
                    else
                    {
                        SceneManager.MainCamera.Rotate(TempPoint, point);
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
                    if (Keyboard.IsKeyDown(Key.LeftShift))
                    {
                        if (dx > dy)
                        {
                            SceneManager.MainCamera.Move(TempPoint, point with { Y = TempPoint.Y });
                        }
                        else
                        {
                            SceneManager.MainCamera.Move(TempPoint, point with { X = TempPoint.X });
                        }
                    }
                    else
                    {
                        SceneManager.MainCamera.Move(TempPoint, point);
                    }

                    TempPoint = point;
                }
            }
        }
    }

    private void ModelCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e) =>
        SceneManager.Resize((int)e.NewSize.Width, (int)e.NewSize.Height);


    private void ModelCanvas_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsMoving)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                IsMoving = true;
                Point position = e.GetPosition(ModelCanvas);
                TempPoint = new Vector2((float)position.X, (float)position.Y);
            }
            else if (e.MiddleButton == MouseButtonState.Pressed)
            {
                IsMoving = true;
                Point position = e.GetPosition(ModelCanvas);
                TempPoint = new Vector2((float)position.X, (float)position.Y);
            }
        }
    }

    private void OpenFileMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new() { Filter = "Obj files (*.obj)|*.obj" };
        if (openFileDialog.ShowDialog() == true)
        {
            SceneManager.Model = ObjParser.FromObjFile(File.ReadAllLines(openFileDialog.FileName));
            SceneManager.MainCamera.Reset();
        }
    }

    private void ModelCanvas_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta > 0)
        {
            SceneManager.MainCamera.ChangeDistance(-1f);
        }
        else if (e.Delta < 0)
        {
            SceneManager.MainCamera.ChangeDistance(1f);
        }
    }

    private void ResetCameraMenuItem_OnClick(object sender, RoutedEventArgs e) => SceneManager.MainCamera.Reset();

    private void CameraSpeedSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
        SceneManager.MainCamera.Speed = (float)e.NewValue;

    private void ModelCanvas_OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Middle)
        {
            IsMoving = false;
        }
    }
}
