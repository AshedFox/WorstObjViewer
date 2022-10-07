// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Lab1.Lib.Helpers;

namespace Lab1.Lib.Types;

public class Camera
{
    private readonly float _minDistance = 5f;
    private readonly float _maxDistance = 500f;

    public delegate void ChangeHandler();

    public event ChangeHandler? Change;

    private float _speed = 1.0f;
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
