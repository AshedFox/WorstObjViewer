// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace Lab1.Lib.Types.Primitives;

public class Pivot(Vector3 position, Vector3 xAxis, Vector3 yAxis, Vector3 zAxis)
{
    public Vector3 Position { get; set; } = position;
    public Vector3 XAxis { get; set; } = xAxis;
    public Vector3 YAxis { get; set; } = yAxis;
    public Vector3 ZAxis { get; set; } = zAxis;

    public Matrix4x4 LocalMatrix => new(
        XAxis.X, YAxis.X, ZAxis.X, 0,
        XAxis.Y, YAxis.Y, ZAxis.Y, 0,
        XAxis.Z, YAxis.Z, ZAxis.Z, 0,
        0, 0, 0, 1
    );

    public Matrix4x4 WorldMatrix => new(
        XAxis.X, XAxis.Y, XAxis.Z, 0,
        YAxis.X, YAxis.Y, YAxis.Z, 0,
        ZAxis.X, ZAxis.Y, ZAxis.Z, 0,
        0, 0, 0, 1
    );

    public static Pivot CreateBasePivot(Vector3 center) => new(center, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);

    public void RotateX(float radians)
    {
        Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationX(radians);
        XAxis = Vector3.Transform(XAxis, rotationMatrix);
        YAxis = Vector3.Transform(YAxis, rotationMatrix);
        ZAxis = Vector3.Transform(ZAxis, rotationMatrix);
    }

    public void RotateY(float radians)
    {
        Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationY(radians);
        XAxis = Vector3.Transform(XAxis, rotationMatrix);
        YAxis = Vector3.Transform(YAxis, rotationMatrix);
        ZAxis = Vector3.Transform(ZAxis, rotationMatrix);
    }

    public void RotateZ(float radians)
    {
        Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationZ(radians);
        XAxis = Vector3.Transform(XAxis, rotationMatrix);
        YAxis = Vector3.Transform(YAxis, rotationMatrix);
        ZAxis = Vector3.Transform(ZAxis, rotationMatrix);
    }

    public void Scale(Vector3 scale)
    {
        Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(scale);
        XAxis = Vector3.Transform(XAxis, scaleMatrix);
        YAxis = Vector3.Transform(YAxis, scaleMatrix);
        ZAxis = Vector3.Transform(ZAxis, scaleMatrix);
    }

    public void Translate(Vector3 translation)
    {
        Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(translation);
        XAxis = Vector3.Transform(XAxis, translationMatrix);
        YAxis = Vector3.Transform(YAxis, translationMatrix);
        ZAxis = Vector3.Transform(ZAxis, translationMatrix);
    }

    public Vector3 ToWorldCoords(Vector3 local) => Vector3.Transform(local, WorldMatrix) + Position;

    public Vector3 ToLocalCoords(Vector3 world) => Vector3.Transform(world - Position, LocalMatrix);
}
