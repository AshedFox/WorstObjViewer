// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Lab1.Lib.Helpers;
using Lab1.Lib.Types.Primitives;

namespace Lab1.Lib.Types;

public class Camera
{
    public delegate void ChangeHandler();

    private readonly float _maxDistance = 500f;
    private readonly float _minDistance = 5f;

    private float _speed = 1.0f;

    public Camera(int viewportWidth, int viewportHeight, float distance,
        float fieldOfView, float nearPlane, float farPlane)
    {
        Pivot = Pivot.CreateBasePivot(new Vector3(0, 0, distance));
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        Distance = distance;
        FieldOfView = fieldOfView;
        NearPlane = nearPlane;
        FarPlane = farPlane;
    }

    public Pivot Pivot { get; set; }
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }

    public float Speed
    {
        get => _speed;
        set => _speed = Math.Clamp(value, 1, 20);
    }

    public float Distance { get; set; }

    public float Aspect => (float)ViewportWidth / ViewportHeight;
    public float FieldOfView { get; set; }
    public float NearPlane { get; set; }
    public float FarPlane { get; set; }
    public float PolarAngle { get; set; }
    public float AzimuthalAngle { get; set; }

    public Vector3 Target { get; set; } = Vector3.Zero;

    public Matrix4x4 View =>
        Matrix4x4.CreateLookAt(Pivot.Position, Target, Vector3.UnitY);

    public Matrix4x4 Projection =>
        Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, Aspect, NearPlane, FarPlane);

    public Matrix4x4 Viewport =>
        GraphicsProcessor.CreateViewportMatrix(ViewportWidth, ViewportHeight, 0, 0);

    public event ChangeHandler? Change;

    public void Move(Vector2 startPoint, Vector2 endPoint)
    {
        var dX = -30 * Speed * (endPoint.X - startPoint.X) / ViewportWidth;
        var dY = 30 * Speed * (endPoint.Y - startPoint.Y) / ViewportHeight;

        Target = new Vector3(
            Target.X + MathF.Cos(PolarAngle) * dX,
            Target.Y + dY,
            Target.Z - MathF.Sin(PolarAngle) * dX
        );

        RecountPosition();
    }

    public void Rotate(Vector2 startPoint, Vector2 endPoint)
    {
        var dX = -Speed * (endPoint.X - startPoint.X) / ViewportWidth;
        var dY = Speed * (endPoint.Y - startPoint.Y) / ViewportHeight;

        var twoPi = 2 * MathF.PI;
        var halfPi = MathF.PI / 2 - 0.1f;

        PolarAngle = ((PolarAngle + dX) % twoPi + twoPi) % twoPi;
        AzimuthalAngle = Math.Clamp(AzimuthalAngle + dY, -halfPi, halfPi);

        RecountPosition();
    }

    public void ChangeDistance(float delta)
    {
        var newDistance = Math.Clamp(Distance + Speed * delta, _minDistance, _maxDistance);
        if (Math.Abs(newDistance - Distance) > 0.1)
        {
            Distance = newDistance;
            RecountPosition();
        }
    }

    private void RecountPosition()
    {
        Pivot.Position = new Vector3(
            Distance * MathF.Cos(AzimuthalAngle) * MathF.Sin(PolarAngle),
            Distance * MathF.Sin(AzimuthalAngle),
            Distance * MathF.Cos(AzimuthalAngle) * MathF.Cos(PolarAngle)
        ) + Target;

        OnChange();
    }

    public Vector3 ProjectToScreen(Vector3 world)
    {
        Vector4 result = Vector4.Transform(
            Vector4.Transform(
                new Vector4(world, 1),
                View
            ),
            Projection
        );
        result /= result.W;
        result = Vector4.Transform(result, Viewport);
        return new Vector3(result.X, result.Y, result.Z);
    }

    public Matrix4x4 Rotate(Matrix4x4 matrix)
    {
        matrix.M11 = matrix.M11 != 0 ? 1f / matrix.M11 : 0;
        matrix.M12 = matrix.M12 != 0 ? 1f / matrix.M12 : 0;
        matrix.M13 = matrix.M13 != 0 ? 1f / matrix.M13 : 0;
        matrix.M14 = matrix.M14 != 0 ? 1f / matrix.M14 : 0;

        matrix.M21 = matrix.M21 != 0 ? 1f / matrix.M21 : 0;
        matrix.M22 = matrix.M22 != 0 ? 1f / matrix.M22 : 0;
        matrix.M23 = matrix.M23 != 0 ? 1f / matrix.M23 : 0;
        matrix.M24 = matrix.M24 != 0 ? 1f / matrix.M24 : 0;

        matrix.M31 = matrix.M31 != 0 ? 1f / matrix.M31 : 0;
        matrix.M32 = matrix.M32 != 0 ? 1f / matrix.M32 : 0;
        matrix.M33 = matrix.M33 != 0 ? 1f / matrix.M33 : 0;
        matrix.M34 = matrix.M34 != 0 ? 1f / matrix.M34 : 0;

        matrix.M41 = matrix.M41 != 0 ? 1f / matrix.M41 : 0;
        matrix.M42 = matrix.M42 != 0 ? 1f / matrix.M42 : 0;
        matrix.M43 = matrix.M43 != 0 ? 1f / matrix.M43 : 0;
        matrix.M44 = matrix.M44 != 0 ? 1f / matrix.M44 : 0;
        return matrix;
    }

    public Vector3 ProjectFromScreen(Vector3 screen)
    {
        Matrix4x4.Invert(Viewport, out Matrix4x4 viewport);
        Matrix4x4.Invert(Projection, out Matrix4x4 projection);
        Matrix4x4.Invert(View, out Matrix4x4 view);

        Vector4 result = Vector4.Transform(new Vector4(screen, 1), viewport);
        result = Vector4.Transform(result, projection);
        result = Vector4.Transform(result, view);
        result /= result.W;

        return new Vector3(result.X, result.Y, result.Z);
    }

    public bool IsInView(Vector3 world)
    {
        Vector4 result = Vector4.Transform(
            Vector4.Transform(
                new Vector4(world, 1),
                View
            ),
            Projection
        );
        result /= result.W;
        return Math.Abs(result.X) <= 1 && Math.Abs(result.Y) <= 1 && result.Z is > 0 and < 1;
    }

    public void Reset()
    {
        PolarAngle = 0;
        AzimuthalAngle = 0;
        Target = Vector3.Zero;
        Pivot = Pivot.CreateBasePivot(new Vector3(0, 0, Distance));

        RecountPosition();
    }

    public virtual void OnChange() => Change?.Invoke();
}
