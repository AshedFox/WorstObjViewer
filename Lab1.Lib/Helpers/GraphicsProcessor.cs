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
        Vector4 v = Interpolate(scanline[0].Vertex, scanline[1].Vertex,
            scanline[0].Vertex.W, scanline[1].Vertex.W,
            scanline[0].Vertex.X, scanline[1].Vertex.X, x
        );
        var offset = (int)(x + y * width);

        if (offset >= 0 && offset < zBuffer.Length && offset < pixels.Length && offset < locks.Length &&
            v.Z is > 0 and < 1)
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

    public static Vector2 InterpolateUV(Vector2 startV, Vector2 endV, float startW, float endW,
        float interpolationStart, float interpolationEnd, float interpolationValue)
    {
        var phi = (interpolationValue - interpolationStart) / (interpolationEnd - interpolationStart);

        return ((1 - phi) * startV * startW + phi * endV * endW) / ((1 - phi) * startW + phi * endW);
    }

    public static Vector4 Interpolate(Vector4 startV, Vector4 endV, float startW, float endW,
        float interpolationStart, float interpolationEnd, float interpolationValue)
    {
        var phi = (interpolationValue - interpolationStart) / (interpolationEnd - interpolationStart);

        return ((1 - phi) * startV * startW + phi * endV * endW) / ((1 - phi) * startW + phi * endW);
    }

    public static Vector3 InterpolateNormal(Vector3 startV, Vector3 endV, float startW, float endW,
        float interpolationStart, float interpolationEnd, float interpolationValue)
    {
        var phi = (interpolationValue - interpolationStart) / (interpolationEnd - interpolationStart);

        return Vector3.Normalize(((1 - phi) * startV * startW + phi * endV * endW) / ((1 - phi) * startW + phi * endW));
    }

    public static void DrawPolygon(ref byte[] pixels, ref float[] zBuffer, ref SpinLock[] locks, ref Polygon polygon,
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
                        edge.FullVertex1.Vertex.W, edge.FullVertex2.Vertex.W,
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
                    Color c = edge.FullVertex1.Color;
                    c.Red = (byte)Math.Clamp(c.Red + (edge.FullVertex2.Color.Red - edge.FullVertex1.Color.Red) * phi, 0,
                        255);
                    c.Green = (byte)Math.Clamp(
                        c.Green + (edge.FullVertex2.Color.Green - edge.FullVertex1.Color.Green) * phi, 0, 255);
                    c.Blue = (byte)Math.Clamp(
                        c.Blue + (edge.FullVertex2.Color.Blue - edge.FullVertex1.Color.Blue) * phi, 0, 255);
                    c.Alpha = (byte)Math.Clamp(
                        c.Alpha + (edge.FullVertex2.Color.Alpha - edge.FullVertex1.Color.Alpha) * phi, 0, 255);

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

            Color baseColor = new(255);

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

                            FillPixelWithScanline(ref pixels, ref zBuffer, ref locks, ref ends, x, y,
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
                            Color color = ends[0].Color;
                            color.Red = (byte)Math.Clamp(color.Red + (ends[1].Color.Red - ends[0].Color.Red) * phi, 0,
                                255);
                            color.Green =
                                (byte)Math.Clamp(color.Green + (ends[1].Color.Green - ends[0].Color.Green) * phi, 0,
                                    255);
                            color.Blue = (byte)Math.Clamp(color.Blue + (ends[1].Color.Blue - ends[0].Color.Blue) * phi,
                                0, 255);
                            color.Alpha =
                                (byte)Math.Clamp(color.Alpha + (ends[1].Color.Alpha - ends[0].Color.Alpha) * phi, 0,
                                    255);

                            FillPixelWithScanline(ref pixels, ref zBuffer, ref locks, ref ends, x, y,
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

                            FillPixelWithScanline(ref pixels, ref zBuffer, ref locks, ref ends, x, y,
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
                                model.SpecularTexture is not null)
                            {
                                phongTextureProcessor.Shininess = phongLightProcessor.Shininess;

                                Color diffuseColor = new(255);
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

                                color = phongTextureProcessor.MakeColor(-n, light, -view,
                                    diffuseColor, specularColor
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

                            FillPixelWithScanline(ref pixels, ref zBuffer, ref locks, ref ends, x, y,
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
}
