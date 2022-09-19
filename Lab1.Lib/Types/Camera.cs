// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using Lab1.Lib.Helpers;

namespace Lab1.Lib.Types;

public class Camera
{
    private readonly float _minDistance = 5f;
    private readonly float _maxDistance = 5000f;

    public delegate void ChangeHandler();

    public event ChangeHandler? Change;

    private bool _changing;

    private float _distance;
    private Vector3 _target = Vector3.Zero;
    public Pivot Pivot { get; set; }
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }

    public float Distance
    {
        get => _distance;
        set
        {
            var newDistance = Math.Clamp(value, _minDistance, _maxDistance);
            if (Math.Abs(_distance - newDistance) > 0.1)
            {
                _distance = newDistance;
                RecountPosition();
            }
        }
    }

    public float Aspect => (float)ViewportWidth / ViewportHeight;
    public float FieldOfView { get; set; }
    public float NearPlane { get; set; }
    public float FarPlane { get; set; }
    public float PolarAngle { get; set; }
    public float AzimuthalAngle { get; set; }

    public Vector3 Target
    {
        get => _target;
        set
        {
            if (value != _target)
            {
                _target = value;
                OnChange();
            }
        }
    }

    public Matrix4x4 View =>
        Matrix4x4.CreateLookAt(Pivot.Position, Target, Vector3.UnitY);
    //GraphicsProcessor.CreateViewMatrix(Pivot.Position, Target, Vector3.UnitY);

    public Matrix4x4 Projection =>
        Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, Aspect, NearPlane, FarPlane);
    //GraphicsProcessor.CreatePerspectiveFieldOfViewMatrix(Aspect, FieldOfView, NearPlane, FarPlane);

    public Matrix4x4 Viewport =>
        GraphicsProcessor.CreateViewportMatrix(ViewportWidth, ViewportHeight, 0, 0);

    public Camera(int viewportWidth, int viewportHeight, float distance,
        float fieldOfView, float nearPlane, float farPlane)
    {
        Pivot = Pivot.CreateBasePivot(new Vector3(0, 0, -distance));
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        Distance = distance;
        FieldOfView = fieldOfView;
        NearPlane = nearPlane;
        FarPlane = farPlane;
    }

    public void Rotate(Vector2 startPoint, Vector2 endPoint)
    {
        var dX = -(endPoint.X - startPoint.X) / ViewportWidth;
        var dY = (endPoint.Y - startPoint.Y) / ViewportHeight;

        var twoPI = 2 * MathF.PI;
        var halfPI = MathF.PI / 2 - 0.000001f;

        PolarAngle = ((PolarAngle + dX) % twoPI + twoPI) % twoPI;
        //AzimuthalAngle = ((AzimuthalAngle + dY) % halfPI + halfPI) % halfPI;
        //PolarAngle = Math.Clamp(PolarAngle + dX, 0, twoPI);
        AzimuthalAngle = Math.Clamp(AzimuthalAngle + dY, -halfPI, halfPI);

        RecountPosition();
    }

    private void RecountPosition()
    {
        Pivot.Position = new Vector3(
            Distance * MathF.Cos(AzimuthalAngle) * MathF.Cos(PolarAngle),
            Distance * MathF.Sin(AzimuthalAngle),
            -Distance * MathF.Cos(AzimuthalAngle) * MathF.Sin(PolarAngle)
        );
        OnChange();
    }

    public Vector2 ProjectToScreen(Vector3 world)
    {
        //Vector4 vector4 = new(world, 1);
        Vector4 result = Vector4.Transform(
            Vector4.Transform(
                new Vector4(world, 1),
                View
            ),
            Projection
        );
        result /= result.W;
        result = Vector4.Transform(result, Viewport);
        return new Vector2(result.X, result.Y);
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
        return Math.Abs(result.X) <= 1 && Math.Abs(result.Y) <= 1 && Math.Abs(result.Z) <= 1;
    }

    public virtual void OnChange()
    {
        if (!_changing)
        {
            _changing = true;
            Change?.Invoke();
            _changing = false;
        }
    }
}
