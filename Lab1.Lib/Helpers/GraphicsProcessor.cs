﻿using System.Numerics;

namespace Lab1.Lib.Helpers;

public class GraphicsProcessor
{
    public static Matrix4x4 CreateModelMatrix(Vector3 position, Vector3 forward, Vector3 up)
    {
        Vector3 zAxis = Vector3.Normalize(-forward);
        Vector3 xAxis = Vector3.Normalize(Vector3.Cross(up, zAxis));
        Vector3 yAxis = up;

        Matrix4x4 result = Matrix4x4.Identity;

        result.M11 = xAxis.X;
        result.M12 = xAxis.Y;
        result.M13 = xAxis.Z;

        result.M21 = yAxis.X;
        result.M22 = yAxis.Y;
        result.M23 = yAxis.Z;

        result.M31 = zAxis.X;
        result.M32 = zAxis.Y;
        result.M33 = zAxis.Z;

        result.M41 = position.X;
        result.M42 = position.Y;
        result.M43 = position.Z;

        return result;
    }

    public static Matrix4x4 CreateViewMatrix(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUp)
    {
        Vector3 zAxis = Vector3.Normalize(cameraPosition - cameraTarget);
        Vector3 xAxis = Vector3.Normalize(Vector3.Cross(cameraUp, zAxis));
        Vector3 yAxis = cameraUp;

        Matrix4x4 result = Matrix4x4.Identity;

        result.M11 = xAxis.X;
        result.M12 = yAxis.X;
        result.M13 = zAxis.X;

        result.M21 = xAxis.Y;
        result.M22 = yAxis.Y;
        result.M23 = zAxis.Y;

        result.M31 = xAxis.Z;
        result.M32 = yAxis.Z;
        result.M33 = zAxis.Z;

        result.M41 = -Vector3.Dot(xAxis, cameraPosition);
        result.M42 = -Vector3.Dot(yAxis, cameraPosition);
        result.M43 = -Vector3.Dot(zAxis, cameraPosition);

        return result;
    }

    public static Matrix4x4 CreatePerspectiveFieldOfViewMatrix(float aspect, float fov, float zNear, float zFar)
    {
        var yScale = 1.0f / MathF.Tan(fov * 0.5f);
        var xScale = yScale / aspect;
        var negZFar = zFar / (zNear - zFar);

        Matrix4x4 result = Matrix4x4.Identity;

        result.M11 = xScale;
        result.M22 = yScale;
        result.M33 = negZFar;
        result.M34 = -1.0f;
        result.M43 = zNear * negZFar;
        result.M44 = 0.0f;

        return result;
    }

    public static Matrix4x4 CreateViewportMatrix(float width, float height, float xMin, float yMin)
    {
        Matrix4x4 result = Matrix4x4.Identity;

        result.M11 = width * 0.5f;
        result.M22 = -height * 0.5f;
        result.M41 = xMin + width * 0.5f;
        result.M42 = yMin + height * 0.5f;

        return result;
    }

    public static float AngleBetween(Vector3 v1, Vector3 v2) =>
        //(float)Math.Acos(Vector3.Dot(v1, v2) / (v1.Length() * v2.Length()));
        (float)Math.Atan2(Vector3.Cross(v1, v2).Length(), Vector3.Dot(v1, v2));

    public static float ConvertDegreesToRadians(float degrees) => MathF.PI * degrees / 180.0f;

    public static float ConvertRadiansToDegrees(float radians) => radians * 180.0f / MathF.PI;

    public static List<Vector2> Bresenham(Vector2 p1, Vector2 p2)
    {
        List<Vector2> result = new();

        var x1 = (int)Math.Floor(p1.X);
        var x2 = (int)Math.Floor(p2.X);
        var y1 = (int)Math.Floor(p1.Y);
        var y2 = (int)Math.Floor(p2.Y);

        var dX = Math.Abs(x2 - x1);
        var dY = Math.Abs(y2 - y1);

        var signX = x1 < x2 ? 1 : -1;
        var signY = y1 < y2 ? 1 : -1;
        var error = dX - dY;

        result.Add(new Vector2(x2, y2));

        while (x1 != x2 || y1 != y2)
        {
            result.Add(new Vector2(x1, y1));
            if (error * 2 > -dY)
            {
                error -= dY;
                x1 += signX;
            }

            if (error * 2 < dX)
            {
                error += dX;
                y1 += signY;
            }
        }

        return result;
    }
}
