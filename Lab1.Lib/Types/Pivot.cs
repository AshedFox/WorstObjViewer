// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Lab1.Lib.Helpers;

namespace Lab1.Lib.Types;

public class Pivot
{
    public Vector3 Position { get; set; }
    public Vector3 XAxis { get; set; }
    public Vector3 YAxis { get; set; }
    public Vector3 ZAxis { get; set; }

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

    public Pivot(Vector3 position, Vector3 xAxis, Vector3 yAxis, Vector3 zAxis)
    {
        Position = position;
        XAxis = xAxis;
        YAxis = yAxis;
        ZAxis = zAxis;
    }

    public static Pivot CreateBasePivot(Vector3 center) => new(center, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);

    public void RotateX(float angle)
    {
        Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationX(angle);
        XAxis = Vector3.Transform(XAxis, rotationMatrix);
        YAxis = Vector3.Transform(YAxis, rotationMatrix);
        ZAxis = Vector3.Transform(ZAxis, rotationMatrix);
    }

    public void RotateY(float angle)
    {
        Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationY(angle);
        XAxis = Vector3.Transform(XAxis, rotationMatrix);
        YAxis = Vector3.Transform(YAxis, rotationMatrix);
        ZAxis = Vector3.Transform(ZAxis, rotationMatrix);
    }

    public void RotateZ(float angle)
    {
        Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationZ(angle);
        XAxis = Vector3.Transform(XAxis, rotationMatrix);
        YAxis = Vector3.Transform(YAxis, rotationMatrix);
        ZAxis = Vector3.Transform(ZAxis, rotationMatrix);
    }

    public void Move(Vector3 moveVector) => Position += moveVector;

    public Vector3 ToWorldCoords(Vector3 local) => Vector3.Transform(local, WorldMatrix) + Position;

    public Vector3 ToLocalCoords(Vector3 world) => Vector3.Transform(world - Position, LocalMatrix);
}
