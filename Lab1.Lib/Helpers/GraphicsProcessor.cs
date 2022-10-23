using System.Numerics;
using Lab1.Lib.Helpers.Shadow;
using Lab1.Lib.Types;
using Lab1.Lib.Types.Primitives;

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

    private static void FillColorWithScanline(ref Color[] colorsBuffer, ref float[] zBuffer, ref SpinLock[] locks,
        ref FullVertex[] scanline, float x, float y, int width, Color color)
    {
        Vector4 v = Interpolate(scanline[0].Vertex, scanline[1].Vertex,
            scanline[0].Vertex.X, scanline[1].Vertex.X, x
        );
        var offset = (int)(x + y * width);

        if (offset >= 0 && offset < zBuffer.Length && v.Z is > 0 and < 1)
        {
            var lockTaken = false;
            try
            {
                locks[offset].Enter(ref lockTaken);
                if (v.Z < zBuffer[offset] || zBuffer[offset] == 0)
                {
                    zBuffer[offset] = v.Z;
                    colorsBuffer[offset] = color;
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

    public static Vector2 InterpolateUV(Vector2 startV, Vector2 endV, float startW, float endW,
        float interpolationStart, float interpolationEnd, float interpolationValue)
    {
        var phi = (interpolationValue - interpolationStart) / (interpolationEnd - interpolationStart);

        return ((1 - phi) * startV * startW + phi * endV * endW) / ((1 - phi) * startW + phi * endW);
    }

    public static Vector4 Interpolate(Vector4 startV, Vector4 endV,
        float interpolationStart, float interpolationEnd, float interpolationValue)
    {
        var phi = (interpolationValue - interpolationStart) / (interpolationEnd - interpolationStart);

        return startV + phi * (endV - startV);
    }

    public static Vector3 InterpolateNormal(Vector3 startV, Vector3 endV, float startW, float endW,
        float interpolationStart, float interpolationEnd, float interpolationValue)
    {
        var phi = (interpolationValue - interpolationStart) / (interpolationEnd - interpolationStart);

        return Vector3.Normalize(((1 - phi) * startV * startW + phi * endV * endW) / ((1 - phi) * startW + phi * endW));
    }

    public static Color ACESMapTone(Color color)
    {
        const float a = 2.51f;
        const float b = 0.03f;
        const float c = 2.43f;
        const float d = 0.59f;
        const float e = 0.14f;

        color.Red = Math.Min(color.Red * (a * color.Red + b) / (color.Red * (c * color.Red + d) + e), 1);
        color.Green = Math.Min(color.Green * (a * color.Green + b) / (color.Green * (c * color.Green + d) + e), 1);
        color.Blue = Math.Min(color.Blue * (a * color.Blue + b) / (color.Blue * (c * color.Blue + d) + e), 1);
        return color;
    }

    public static void FillPolygonColors(ref Color[] colorsBuffer, ref float[] zBuffer, ref SpinLock[] locks,
        ref Polygon polygon,
        ref Vector4[] screenVertices, ref Model model, ref Camera camera, IShadowProcessor? shadowProcessor,
        Vector3 light)
    {
        if (polygon.Points.Length <= 2)
        {
            return;
        }

        Vector3 normal = FindPolygonNormal(ref polygon, ref model);
        Vector3 view = Vector3.Normalize(
            model.WorldVertices[polygon.Points[0].VertexIndex - 1] - camera.Pivot.Position
        );

        if (Vector3.Dot(normal, view) <= 0)
        {
            return;
        }

        var minY = (int)Math.Round(screenVertices[polygon.Points[0].VertexIndex - 1].Y);
        var maxY = (int)Math.Round(screenVertices[polygon.Points[0].VertexIndex - 1].Y);

        Edge[] edges = new Edge[polygon.Points.Length];

        for (var i = 0; i < polygon.Points.Length; i++)
        {
            Vector4 vertex1 = screenVertices[polygon.Points[i].VertexIndex - 1];
            Vector4 vertex2 = screenVertices[polygon.Points[(i + 1) % polygon.Points.Length].VertexIndex - 1];
            Vector3 normal1 = model.Normals[polygon.Points[i].NormalIndex - 1];
            Vector3 normal2 = model.Normals[polygon.Points[(i + 1) % polygon.Points.Length].NormalIndex - 1];
            Vector2 texture1 = model.TexturesVertices[polygon.Points[i].TextureIndex - 1];
            Vector2 texture2 = model.TexturesVertices[polygon.Points[(i + 1) % polygon.Points.Length].TextureIndex - 1];

            if (vertex1.Y < minY)
            {
                minY = (int)Math.Round(vertex1.Y);
            }

            if (vertex1.Y > maxY)
            {
                maxY = (int)Math.Round(vertex1.Y);
            }

            edges[i] = new Edge
            {
                FullVertex1 =
                    new FullVertex
                    {
                        Vertex = vertex1,
                        Normal = normal1,
                        Texture = texture1,
                        Color = new Color(255) * Math.Clamp(Vector3.Dot(-normal1, light), 0, 1)
                    },
                FullVertex2 = new FullVertex
                {
                    Vertex = vertex2,
                    Normal = normal2,
                    Texture = texture2,
                    Color = new Color(255) * Math.Clamp(Vector3.Dot(-normal2, light), 0, 1)
                }
            };
        }

        FullVertex[] ends = new FullVertex[2];

        PhongTextureProcessor phongTextureProcessor = new();

        for (var y = Math.Max(minY, 0); y <= Math.Min(maxY, camera.ViewportHeight); y++)
        {
            Array.Clear(ends);
            var index = 0;

            foreach (Edge edge in edges)
            {
                if (index == 2)
                {
                    break;
                }

                if (IsIntersect(y, edge.FullVertex1.Vertex.Y, edge.FullVertex2.Vertex.Y))
                {
                    var phi = (y - edge.FullVertex1.Vertex.Y) / (edge.FullVertex2.Vertex.Y - edge.FullVertex1.Vertex.Y);
                    Vector4 v = Interpolate(edge.FullVertex1.Vertex, edge.FullVertex2.Vertex,
                        edge.FullVertex1.Vertex.Y, edge.FullVertex2.Vertex.Y, y
                    );
                    Vector3 n = InterpolateNormal(edge.FullVertex1.Normal, edge.FullVertex2.Normal,
                        edge.FullVertex1.Vertex.W, edge.FullVertex2.Vertex.W,
                        edge.FullVertex1.Vertex.Y, edge.FullVertex2.Vertex.Y, y
                    );
                    Vector2 t = InterpolateUV(edge.FullVertex1.Texture, edge.FullVertex2.Texture,
                        edge.FullVertex1.Vertex.W, edge.FullVertex2.Vertex.W,
                        edge.FullVertex1.Vertex.Y, edge.FullVertex2.Vertex.Y, y
                    );
                    Color c = edge.FullVertex1.Color + (edge.FullVertex2.Color - edge.FullVertex1.Color) * phi;
                    c.Alpha = edge.FullVertex1.Color.Alpha +
                              (edge.FullVertex2.Color.Alpha - edge.FullVertex1.Color.Alpha) * phi;

                    ends[index].Vertex = v;
                    ends[index].Normal = n;
                    ends[index].Texture = t;
                    ends[index].Color = c;
                    index++;
                }
            }

            if (ends[0].Vertex.X > ends[1].Vertex.X)
            {
                (ends[0], ends[1]) = (ends[1], ends[0]);
            }

            Color baseColor = new(1);

            switch (shadowProcessor)
            {
                case null:
                    for (var x = ends[0].Vertex.X; x < ends[1].Vertex.X; x++)
                    {
                        if (x > 0 && x < camera.ViewportWidth && y > 0 && y < camera.ViewportHeight)
                        {
                            Vector2 texture = InterpolateUV(ends[0].Texture, ends[1].Texture, ends[0].Vertex.W,
                                ends[1].Vertex.W, ends[0].Vertex.X, ends[1].Vertex.X, x
                            );

                            if (model.DiffuseTexture is not null)
                            {
                                baseColor = model.DiffuseTexture.MakeColor(texture);
                            }

                            FillColorWithScanline(ref colorsBuffer, ref zBuffer, ref locks, ref ends, x, y,
                                camera.ViewportWidth, baseColor);
                        }
                    }

                    break;
                case LambertShadowProcessor lambertShadowProcessor:
                    lambertShadowProcessor.ChangeIntensity(light, normal);

                    for (var x = ends[0].Vertex.X; x < ends[1].Vertex.X; x++)
                    {
                        if (x > 0 && x < camera.ViewportWidth && y > 0 && y < camera.ViewportHeight)
                        {
                            Vector2 texture = InterpolateUV(ends[0].Texture, ends[1].Texture, ends[0].Vertex.W,
                                ends[1].Vertex.W, ends[0].Vertex.X, ends[1].Vertex.X, x
                            );

                            if (model.DiffuseTexture is not null)
                            {
                                if (model.DiffuseTexture is not null)
                                {
                                    baseColor = model.DiffuseTexture.MakeColor(texture);
                                }
                            }

                            Color color = lambertShadowProcessor.TransformColor(baseColor);

                            if (model.EmissionTexture is not null)
                            {
                                color += model.EmissionTexture.MakeColor(texture);
                            }

                            FillColorWithScanline(ref colorsBuffer, ref zBuffer, ref locks, ref ends, x, y,
                                camera.ViewportWidth, color
                            );
                        }
                    }

                    break;
                case GouraudShadowProcessor:
                    for (var x = ends[0].Vertex.X; x < ends[1].Vertex.X; x++)
                    {
                        if (x > 0 && x < camera.ViewportWidth && y > 0 && y < camera.ViewportHeight)
                        {
                            var phi = (x - ends[0].Vertex.X) / (ends[1].Vertex.X - ends[0].Vertex.X);
                            Color color = ends[0].Color + (ends[1].Color - ends[0].Color) * phi;
                            color.Alpha = ends[0].Color.Alpha + (ends[1].Color.Alpha - ends[0].Color.Alpha) * phi;

                            FillColorWithScanline(ref colorsBuffer, ref zBuffer, ref locks, ref ends, x, y,
                                camera.ViewportWidth, color
                            );
                        }
                    }

                    break;
                case PhongShadowProcessor phongShadowProcessor:
                    for (var x = ends[0].Vertex.X; x < ends[1].Vertex.X; x++)
                    {
                        if (x > 0 && x < camera.ViewportWidth && y > 0 && y < camera.ViewportHeight)
                        {
                            Vector3 n = InterpolateNormal(ends[0].Normal, ends[1].Normal,
                                ends[0].Vertex.W, ends[1].Vertex.W,
                                ends[0].Vertex.X, ends[1].Vertex.X, x
                            );
                            Vector2 texture = InterpolateUV(ends[0].Texture, ends[1].Texture,
                                ends[0].Vertex.W, ends[1].Vertex.W, ends[0].Vertex.X,
                                ends[1].Vertex.X, x
                            );

                            if (model.DiffuseTexture is not null || model.NormalTexture is not null)
                            {
                                if (model.DiffuseTexture is not null)
                                {
                                    baseColor = model.DiffuseTexture.MakeColor(texture);
                                }

                                if (model.NormalTexture is not null)
                                {
                                    n = model.NormalTexture.MakeNormal(texture);
                                }
                            }

                            phongShadowProcessor.ChangeIntensity(-n, light);

                            Color color = phongShadowProcessor.TransformColor(baseColor);

                            if (model.EmissionTexture is not null)
                            {
                                color += model.EmissionTexture.MakeColor(texture);
                            }

                            FillColorWithScanline(ref colorsBuffer, ref zBuffer, ref locks, ref ends, x, y,
                                camera.ViewportWidth, color
                            );
                        }
                    }

                    break;
                case PhongLightProcessor phongLightProcessor:
                    for (var x = ends[0].Vertex.X; x < ends[1].Vertex.X; x++)
                    {
                        if (x > 0 && x < camera.ViewportWidth && y > 0 && y < camera.ViewportHeight)
                        {
                            Vector3 n = InterpolateNormal(ends[0].Normal, ends[1].Normal,
                                ends[0].Vertex.W, ends[1].Vertex.W,
                                ends[0].Vertex.X, ends[1].Vertex.X, x
                            );
                            Vector2 texture = InterpolateUV(ends[0].Texture, ends[1].Texture,
                                ends[0].Vertex.W, ends[1].Vertex.W, ends[0].Vertex.X,
                                ends[1].Vertex.X, x
                            );

                            view = Vector3.Normalize(
                                camera.ProjectFromScreen(new Vector3(x, y, 1)) - camera.Pivot.Position
                            );

                            Color color;

                            if (model.DiffuseTexture is not null || model.NormalTexture is not null ||
                                model.MRAOTexture is not null)
                            {
                                Color diffuseColor = new(1);
                                Color mraoColor = new(0);

                                if (model.NormalTexture is not null)
                                {
                                    n = model.NormalTexture.MakeNormal(texture);
                                }

                                if (model.DiffuseTexture is not null)
                                {
                                    diffuseColor = model.DiffuseTexture.MakeColor(texture);
                                }

                                if (model.MRAOTexture is not null)
                                {
                                    mraoColor = model.MRAOTexture.MakeColor(texture);
                                }

                                color = phongTextureProcessor.MakeColor(-n, light, -view,
                                    diffuseColor, mraoColor
                                );
                            }
                            else
                            {
                                phongLightProcessor.Change(-n, light, -view);

                                color = phongLightProcessor.TransformColor(baseColor);
                            }

                            if (model.EmissionTexture is not null)
                            {
                                color += model.EmissionTexture.MakeColor(texture);
                            }

                            FillColorWithScanline(ref colorsBuffer, ref zBuffer, ref locks, ref ends, x, y,
                                camera.ViewportWidth, color
                            );
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shadowProcessor));
            }
        }
    }

    public static void GaussianBlur(ref Color[] colorsBuffer)
    {
    }
}
