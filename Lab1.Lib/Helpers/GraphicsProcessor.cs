using System.Numerics;
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


    public static void DrawPolygon(ref byte[] pixels, ref float[] zBuffer, ref List<Model.PolygonPoint> polygonPoints,
        ref Vector3[] vertices, int width, int height, float intensity)
    {
        if (polygonPoints.Count <= 2)
        {
            return;
        }

        var minY = (int)Math.Round(vertices[polygonPoints[0].VertexIndex - 1].Y);
        var maxY = (int)Math.Round(vertices[polygonPoints[0].VertexIndex - 1].Y);

        List<(Vector3 v1, Vector3 v2)> edges = new();

        for (var i = 0; i < polygonPoints.Count; i++)
        {
            Vector3 vertex1 = vertices[polygonPoints[i].VertexIndex - 1];
            Vector3 vertex2 = vertices[polygonPoints[(i + 1) % polygonPoints.Count].VertexIndex - 1];

            if (vertex1.Y < minY)
            {
                minY = (int)Math.Round(vertex1.Y);
            }

            if (vertex1.Y > maxY)
            {
                maxY = (int)Math.Round(vertex1.Y);
            }

            edges.Add((vertex1, vertex2));
        }

        for (var y = minY; y <= maxY; y++)
        {
            List<Vector3> ends = new();

            foreach ((Vector3 v1, Vector3 v2) edge in edges)
            {
                if (ends.Count >= 2)
                {
                    break;
                }

                if (IsIntersect(y, edge.v1.Y, edge.v2.Y))
                {
                    var phi = (y - edge.v1.Y) / (edge.v2.Y - edge.v1.Y);
                    Vector3 v = edge.v1 + (edge.v2 - edge.v1) * phi;
                    ends.Add(v with { Y = y });
                }
            }

            if (ends.Count >= 2)
            {
                if (ends[0].X > ends[1].X)
                {
                    (ends[0], ends[1]) = (ends[1], ends[0]);
                }

                for (var x = ends[0].X; x < ends[1].X; x++)
                {
                    if (x >= 0 && x < width && y > 0 && y < height)
                    {
                        var phi = (x - ends[0].X) / (ends[1].X - ends[0].X);
                        Vector3 v = ends[0] + (ends[1] - ends[0]) * phi;
                        var offset = (int)(x + ends[0].Y * width);

                        if (offset > 0 && offset < zBuffer.Length && (v.Z < zBuffer[offset] || zBuffer[offset] == 0))
                        {
                            zBuffer[offset] = v.Z;
                            pixels[offset] = (byte)(intensity * 255);
                            //var index = 3 * offset;

                            //pixels[index] = (byte)(intensity * 255);
                            //pixels[index + 1] = (byte)(intensity * 255);
                            //pixels[index + 2] = (byte)(intensity * 255);
                        }
                    }
                }
            }
        }
    }
}
