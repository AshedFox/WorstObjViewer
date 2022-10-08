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

    private static void FillPixelWithScanline(ref byte[] pixels, ref float[] zBuffer, ref SpinLock[] locks,
        ref FullVertex[] scanline, float x, float y, int width, Color color)
    {
        var phi = (x - scanline[0].Vertex.X) / (scanline[1].Vertex.X - scanline[0].Vertex.X);
        Vector3 v = scanline[0].Vertex + (scanline[1].Vertex - scanline[0].Vertex) * phi;
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

                    var transparency = (float)color.Alpha / 255;

                    pixels[3 * offset] = (byte)(color.Red * transparency);
                    pixels[3 * offset + 1] = (byte)(color.Green * transparency);
                    pixels[3 * offset + 2] = (byte)(color.Blue * transparency);
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

    public static Vector2 InterpolateUV(Vector2 startV, Vector2 endV, float startZ, float endZ,
        float interpolationStart, float interpolationEnd, float interpolationValue)
    {
        var phi = (interpolationValue - interpolationStart) / (interpolationEnd - interpolationStart);

        return ((1 - phi) * startV / startZ + phi * endV / endZ) /
               ((1 - phi) * 1 / startZ + phi * 1 / endZ);
    }

    public static Vector3 Interpolate(Vector3 startV, Vector3 endV, float interpolationStart, float interpolationEnd,
        float interpolationValue) =>
        startV + (endV - startV) * (interpolationValue - interpolationStart) / (interpolationEnd - interpolationStart);

    public static Vector3 InterpolateNormal(Vector3 startV, Vector3 endV, float interpolationStart,
        float interpolationEnd, float interpolationValue) =>
        Vector3.Normalize(Interpolate(startV, endV, interpolationStart, interpolationEnd, interpolationValue));

    public static void DrawPolygon(ref byte[] pixels, ref float[] zBuffer, ref SpinLock[] locks, ref Polygon polygon,
        ref Vector3[] screenVertices, ref Model model, ref Camera camera, IShadowProcessor? shadowProcessor,
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
            Vector3 vertex1 = screenVertices[polygon.Points[i].VertexIndex - 1];
            Vector3 vertex2 = screenVertices[polygon.Points[(i + 1) % polygon.Points.Length].VertexIndex - 1];
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
                FullVertex1 = new FullVertex { Vertex = vertex1, Normal = normal1, Texture = texture1 },
                FullVertex2 = new FullVertex { Vertex = vertex2, Normal = normal2, Texture = texture2 }
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
                    Vector3 v = edge.FullVertex1.Vertex + (edge.FullVertex2.Vertex - edge.FullVertex1.Vertex) * phi;
                    Vector3 n = Vector3.Normalize(
                        edge.FullVertex1.Normal + phi * (edge.FullVertex2.Normal - edge.FullVertex1.Normal)
                    );
                    Vector2 t = InterpolateUV(edge.FullVertex1.Texture, edge.FullVertex2.Texture,
                        edge.FullVertex1.Vertex.Z, edge.FullVertex2.Vertex.Z,
                        edge.FullVertex1.Vertex.Y, edge.FullVertex2.Vertex.Y, y
                    );

                    ends[index].Vertex = v;
                    ends[index].Normal = n;
                    ends[index].Texture = t;
                    index++;
                }
            }

            if (ends[0].Vertex.X > ends[1].Vertex.X)
            {
                (ends[0], ends[1]) = (ends[1], ends[0]);
            }

            Color baseColor = new(255);

            switch (shadowProcessor)
            {
                case null:
                    for (var x = ends[0].Vertex.X; x < ends[1].Vertex.X; x++)
                    {
                        if (x > 0 && x < camera.ViewportWidth && y > 0 && y < camera.ViewportHeight)
                        {
                            Vector2 texture = InterpolateUV(ends[0].Texture, ends[1].Texture, ends[0].Vertex.Z,
                                ends[1].Vertex.Z, ends[0].Vertex.X, ends[1].Vertex.X, x
                            );

                            if (model.DiffuseTexture is not null)
                            {
                                baseColor = model.DiffuseTexture.MakeColor(texture);
                            }

                            FillPixelWithScanline(ref pixels, ref zBuffer, ref locks, ref ends, x, y,
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
                            if (model.DiffuseTexture is not null)
                            {
                                Vector2 texture = InterpolateUV(ends[0].Texture, ends[1].Texture, ends[0].Vertex.Z,
                                    ends[1].Vertex.Z, ends[0].Vertex.X, ends[1].Vertex.X, x
                                );

                                if (model.DiffuseTexture is not null)
                                {
                                    baseColor = model.DiffuseTexture.MakeColor(texture);
                                }
                            }

                            FillPixelWithScanline(ref pixels, ref zBuffer, ref locks, ref ends, x, y,
                                camera.ViewportWidth, lambertShadowProcessor.TransformColor(baseColor)
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
                                ends[0].Vertex.X, ends[1].Vertex.X, x
                            );

                            if (model.DiffuseTexture is not null || model.NormalTexture is not null)
                            {
                                Vector2 texture = InterpolateUV(ends[0].Texture, ends[1].Texture,
                                    ends[0].Vertex.Z, ends[1].Vertex.Z, ends[0].Vertex.X,
                                    ends[1].Vertex.X, x
                                );

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

                            FillPixelWithScanline(ref pixels, ref zBuffer, ref locks, ref ends, x, y,
                                camera.ViewportWidth, phongShadowProcessor.TransformColor(baseColor)
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
                                ends[0].Vertex.X, ends[1].Vertex.X, x
                            );

                            if (model.DiffuseTexture is not null || model.NormalTexture is not null ||
                                model.SpecularTexture is not null)
                            {
                                Vector2 texture = InterpolateUV(ends[0].Texture, ends[1].Texture,
                                    ends[0].Vertex.Z, ends[1].Vertex.Z, ends[0].Vertex.X,
                                    ends[1].Vertex.X, x
                                );
                                phongTextureProcessor.Shininess = phongLightProcessor.Shininess;

                                Color diffuseColor = new(0);
                                Color specularColor = baseColor * phongLightProcessor.SpecularFactor;

                                if (model.NormalTexture is not null)
                                {
                                    n = model.NormalTexture.MakeNormal(texture);
                                }

                                if (model.DiffuseTexture is not null)
                                {
                                    diffuseColor = model.DiffuseTexture.MakeColor(texture);
                                }

                                if (model.SpecularTexture is not null)
                                {
                                    specularColor = model.SpecularTexture.MakeColor(texture);
                                }

                                Color color = phongTextureProcessor.MakeColor(-n, light, -view,
                                    diffuseColor, specularColor
                                );

                                FillPixelWithScanline(ref pixels, ref zBuffer, ref locks, ref ends, x, y,
                                    camera.ViewportWidth, color
                                );
                            }
                            else
                            {
                                phongLightProcessor.Change(-n, light, -view);

                                FillPixelWithScanline(ref pixels, ref zBuffer, ref locks, ref ends, x, y,
                                    camera.ViewportWidth, phongLightProcessor.TransformColor(baseColor)
                                );
                            }
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shadowProcessor));
            }
        }
    }
}
