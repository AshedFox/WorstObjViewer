// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using Lab1.Lib.Helpers;

namespace Lab1.Lib.Types;

public class Camera
{
    private readonly float _minDistance = 5f;
    private readonly float _maxDistance = 500f;

    public delegate void ChangeHandler();

    public event ChangeHandler? Change;

    private float _distance;
    public Pivot Pivot { get; set; }
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }

    public float Distance
    {
        get => _distance;
        set
        {
            _distance = Math.Clamp(value, _minDistance, _maxDistance);
            RecountPosition();
        }
    }

    public float Aspect => (float)ViewportWidth / ViewportHeight;
    public float FieldOfView { get; set; }
    public float NearPlane { get; set; }
    public float FarPlane { get; set; }
    public float PolarAngle { get; set; }
    public float AzimuthalAngle { get; set; }
    public Vector3 Target { get; set; } = Vector3.Zero;

    public Matrix4x4 View =>
        Matrix4x4.CreateLookAt(Pivot.Position, Target, Vector3.UnitY);
    //GraphicsProcessor.CreateViewMatrix(Pivot.Position, Target, Vector3.UnitY);

    public Matrix4x4 Projection =>
        Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, Aspect, NearPlane, FarPlane);
    //GraphicsProcessor.CreatePerspectiveFieldOfViewMatrix(Aspect, FieldOfView, NearPlane, FarPlane);

    public Matrix4x4 Viewport =>
        GraphicsProcessor.CreateViewportMatrix(ViewportWidth, ViewportHeight, 0, 0);

    public Camera(Vector3 position, int viewportWidth, int viewportHeight, float distance,
        float fieldOfView, float nearPlane, float farPlane)
    {
        Pivot = Pivot.CreateBasePivot(position);
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        Distance = distance;
        FieldOfView = fieldOfView;
        NearPlane = nearPlane;
        FarPlane = farPlane;
    }

    public void Rotate(Vector2 startPoint, Vector2 endPoint)
    {
        var dX = (endPoint.X - startPoint.X) / ViewportWidth;
        var dY = (endPoint.Y - startPoint.Y) / ViewportHeight;

        var twoPI = 2 * MathF.PI;
        var halfPI = MathF.PI / 2 - 0.000001f;

        PolarAngle = ((PolarAngle + dX) % twoPI + twoPI) % twoPI;
        //AzimuthalAngle = ((AzimuthalAngle + dY) % halfPI + halfPI) % halfPI;
        //PolarAngle = Math.Clamp(PolarAngle + dX, 0, twoPI);
        AzimuthalAngle = Math.Clamp(AzimuthalAngle + dY, -halfPI, halfPI) ;

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
        Vector3 result = Vector3.Transform(
            Vector3.Transform(
                world,
                View
            ),
            Projection
        );
        result /= result.Z;
        result = Vector3.Transform(result, Viewport);
        return new Vector2(result.X, result.Y);
    }

    public bool IsInView(Vector3 local) =>
        /*if (local.Z <= Distance)
        {
            return false;
        }
        var angle = GraphicsProcessor.AngleBetween(Vector3.UnitZ, local);
        if (Math.Abs(angle) > FarPlane / 2)
        {
            return false;
        }
        return true;*/
        true;

    public virtual void OnChange() => Change?.Invoke();
}
