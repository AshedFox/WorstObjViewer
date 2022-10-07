using System.Numerics;
using Lab1.Lib.Enums;
using Lab1.Lib.Types;

namespace Lab1.Lib.Helpers;

public static class GraphicsProcessor
{
    public static Matrix4x4 CreateViewportMatrix(float width, float height, float xMin, float yMin)
    {
        Matrix4x4 result = Matrix4x4.Identity;

        result.M11 = width * 0.5f;
        result.M22 = -height * 0.5f;
        result.M41 = xMin + width * 0.5f;
        result.M42 = yMin + height * 0.5f;

        return result;
    }

    public static float ConvertDegreesToRadians(float degrees) => MathF.PI * degrees / 180.0f;

    public static float ConvertRadiansToDegrees(float radians) => radians * 180.0f / MathF.PI;

    private static bool IsIntersect(float y, float startY, float endY)
    {
        if (startY <= endY)
        {
            if (y >= startY && y <= endY)
            {
                return true;
            }
        }
        else
        {
            if (y >= endY && y <= startY)
            {
                return true;
            }
        }

        return false;
    }

    private static void FillPixelWithScanline(ref byte[] pixels, ref float[] zBuffer, ref SpinLock[] locks,
        ref (Vector3 v, Vector3 n)[] scanline, float x, float y, int width, byte color, bool invisible = false)
    {
        var phi = (x - scanline[0].v.X) / (scanline[1].v.X - scanline[0].v.X);
        Vector3 v = scanline[0].v + (scanline[1].v - scanline[0].v) * phi;
        var offset = (int)(x + y * width);

        if (offset >= 0 && offset < zBuffer.Length && offset < pixels.Length && offset < locks.Length)
        {
            var lockTaken = false;
            try
            {
                locks[offset].Enter(ref lockTaken);
                if (v.Z < zBuffer[offset] || zBuffer[offset] == 0)
                {
                    zBuffer[offset] = v.Z;
                    if (!invisible)
                    {
                        pixels[offset] = color;
                    }
                }
            }
            finally
            {
                if (lockTaken)
                {
                    locks[offset].Exit();
                }
            }
        }
    }

    public static Vector3 FindPolygonNormal(ref Polygon polygon, ref Model model)
    {
        Vector3 v1 = model.WorldVertices[polygon.Points[0].VertexIndex - 1];
        Vector3 v2 = model.WorldVertices[polygon.Points[1].VertexIndex - 1];
        Vector3 v3 = model.WorldVertices[polygon.Points[2].VertexIndex - 1];

        return Vector3.Normalize(Vector3.Cross(v1 - v2, v3 - v2));
    }

    public static void DrawPolygon(ref byte[] pixels, ref float[] zBuffer, ref SpinLock[] locks, ref Polygon polygon,
        ref Vector3[] screenVertices, ref Model model, ref Camera camera, Vector3 light = default,
        ShadowType shadowType = ShadowType.None)
    {
        if (polygon.Points.Length <= 2)
        {
            return;
        }

        Vector3 normal = FindPolygonNormal(ref polygon, ref model);

        if (Vector3.Dot(normal, model.WorldVertices[polygon.Points[0].VertexIndex - 1] - camera.Pivot.Position) <= 0)
        {
            return;
        }

        var minY = (int)Math.Round(screenVertices[polygon.Points[0].VertexIndex - 1].Y);
        var maxY = (int)Math.Round(screenVertices[polygon.Points[0].VertexIndex - 1].Y);

        List<(Vector3 v1, Vector3 v2, Vector3 n1, Vector3 n2)> edges = new();

        for (var i = 0; i < polygon.Points.Length; i++)
        {
            Vector3 vertex1 = screenVertices[polygon.Points[i].VertexIndex - 1];
            Vector3 vertex2 = screenVertices[polygon.Points[(i + 1) % polygon.Points.Length].VertexIndex - 1];
            Vector3 normal1 = model.Normals[(int)(polygon.Points[i].NormalIndex - 1)];
            Vector3 normal2 = model.Normals[(int)(polygon.Points[(i + 1) % polygon.Points.Length].NormalIndex - 1)];

            if (vertex1.Y < minY)
            {
                minY = (int)Math.Round(vertex1.Y);
            }

            if (vertex1.Y > maxY)
            {
                maxY = (int)Math.Round(vertex1.Y);
            }

            edges.Add((vertex1, vertex2, normal1, normal2));
        }

        (Vector3 v, Vector3 n)[] ends = new (Vector3 v, Vector3 n)[2];

        for (var y = Math.Max(minY, 0); y <= Math.Min(maxY, camera.ViewportHeight); y++)
        {
            Array.Clear(ends);
            var index = 0;

            foreach ((Vector3 v1, Vector3 v2, Vector3 n1, Vector3 n2) edge in edges)
            {
                if (index == 2)
                {
                    break;
                }

                if (IsIntersect(y, edge.v1.Y, edge.v2.Y))
                {
                    var phi = (y - edge.v1.Y) / (edge.v2.Y - edge.v1.Y);
                    Vector3 v = edge.v1 + (edge.v2 - edge.v1) * phi;
                    Vector3 n = Vector3.Normalize(edge.n1 + phi * (edge.n2 - edge.n1));

                    ends[index++] = (v with { Y = y }, n);
                }
            }

            if (ends[0].v.X > ends[1].v.X)
            {
                (ends[0], ends[1]) = (ends[1], ends[0]);
            }

            float intensity;
            switch (shadowType)
            {
                case ShadowType.None:
                    for (var x = ends[0].v.X; x < ends[1].v.X; x++)
                    {
                        if (x > 0 && x < camera.ViewportWidth && y > 0 && y < camera.ViewportHeight)
                        {
                            FillPixelWithScanline(ref pixels, ref zBuffer, ref locks, ref ends, x, y,
                                camera.ViewportWidth, 255);
                        }
                    }

                    break;
                case ShadowType.Lambert:
                    intensity = Math.Clamp(Vector3.Dot(normal, light), 0, 1);

                    for (var x = ends[0].v.X; x < ends[1].v.X; x++)
                    {
                        if (x > 0 && x < camera.ViewportWidth && y > 0 && y < camera.ViewportHeight)
                        {
                            FillPixelWithScanline(ref pixels, ref zBuffer, ref locks, ref ends, x, y,
                                camera.ViewportWidth, (byte)(255 * intensity));
                        }
                    }

                    break;
                case ShadowType.Phong:
                    for (var x = ends[0].v.X; x < ends[1].v.X; x++)
                    {
                        if (x > 0 && x < camera.ViewportWidth && y > 0 && y < camera.ViewportHeight)
                        {
                            var phi = (x - ends[0].v.X) / (ends[1].v.X - ends[0].v.X);
                            Vector3 n = -Vector3.Normalize(ends[0].n + (ends[1].n - ends[0].n) * phi);
                            intensity = Math.Clamp(Vector3.Dot(n, light), 0, 1);

                            FillPixelWithScanline(ref pixels, ref zBuffer, ref locks, ref ends, x, y,
                                camera.ViewportWidth, (byte)(255 * intensity));
                        }
                    }


                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shadowType), shadowType, null);
            }
        }
    }
}
