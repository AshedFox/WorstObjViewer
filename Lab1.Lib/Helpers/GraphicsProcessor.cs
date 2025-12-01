using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;
using Lab1.Lib.Helpers.Shadow;
using Lab1.Lib.Types;
using Lab1.Lib.Types.Primitives;

namespace Lab1.Lib.Helpers;

public static class GraphicsProcessor
{
    private const float ZBufferScale = int.MaxValue;

    public static Matrix4x4 CreateViewportMatrix(float width, float height, float xMin, float yMin)
    {
        var result = Matrix4x4.Identity;
        result.M11 = width * 0.5f;
        result.M22 = -height * 0.5f;
        result.M41 = xMin + width * 0.5f;
        result.M42 = yMin + height * 0.5f;
        return result;
    }

    public static float ConvertDegreesToRadians(float degrees) => MathF.PI * degrees / 180.0f;
    public static float ConvertRadiansToDegrees(float radians) => radians * 180.0f / MathF.PI;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsIntersect(float y, float startY, float endY)
    {
        return (y >= startY && y <= endY) || (y >= endY && y <= startY);
    }

    private static void FillColorWithScanline(Color[] colorsBuffer, int[] zBuffer,
        ref FullVertex v1, ref FullVertex v2, float x, float y, int width, Color color)
    {
        var z = InterpolateFloat(v1.Vertex.Z, v2.Vertex.Z, v1.Vertex.X, v2.Vertex.X, x);

        if (z is <= 0 or >= 1)
        {
            return;
        }

        var offset = (int)(x + y * width);
        if (offset < 0 || offset >= zBuffer.Length)
        {
            return;
        }

        var newZ = (int)(z * ZBufferScale);

        var oldZ = zBuffer[offset];
        while (newZ < oldZ)
        {
            var actualOldZ = Interlocked.CompareExchange(ref zBuffer[offset], newZ, oldZ);

            if (actualOldZ == oldZ)
            {
                colorsBuffer[offset] = color;
                return;
            }
            oldZ = actualOldZ;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float InterpolateFloat(float start, float end, float startW, float endW, float val)
    {
        return start + (val - startW) / (endW - startW) * (end - start);
    }

    public static Vector3 FindPolygonNormal(Polygon polygon, Model model)
    {
        var v1 = model.WorldVertices[polygon.Points[0].VertexIndex - 1];
        var v2 = model.WorldVertices[polygon.Points[1].VertexIndex - 1];
        var v3 = model.WorldVertices[polygon.Points[2].VertexIndex - 1];
        return Vector3.Normalize(Vector3.Cross(v1 - v2, v3 - v2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 InterpolateUV(Vector2 startV, Vector2 endV, float startW, float endW,
        float interpolationStart, float interpolationEnd, float interpolationValue)
    {
        var phi = (interpolationValue - interpolationStart) / (interpolationEnd - interpolationStart);
        return ((1 - phi) * startV * startW + phi * endV * endW) / ((1 - phi) * startW + phi * endW);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Interpolate(Vector4 startV, Vector4 endV,
        float interpolationStart, float interpolationEnd, float interpolationValue)
    {
        var phi = (interpolationValue - interpolationStart) / (interpolationEnd - interpolationStart);
        return startV + phi * (endV - startV);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    public static void FillPolygonColors(Color[] colorsBuffer, int[] zBuffer,
        Polygon polygon,
        Vector4[] screenVertices, Model model, Camera camera, IShadowProcessor? shadowProcessor,
        Vector3 light)
    {
        if (polygon.Points.Length < 3)
        {
            return;
        }

        var normal = FindPolygonNormal(polygon, model);
        Vector3 view = Vector3.Normalize(
            model.WorldVertices[polygon.Points[0].VertexIndex - 1] - camera.Pivot.Position
        );

        if (Vector3.Dot(normal, view) <= 0)
        {
            return;
        }

        if (shadowProcessor is LambertShadowProcessor lambertProcessor)
        {
            lambertProcessor.ChangeIntensity(light, normal);
        }

        Span<Edge> edges = stackalloc Edge[polygon.Points.Length];

        var minY = float.MaxValue;
        var maxY = float.MinValue;

        for (var i = 0; i < polygon.Points.Length; i++)
        {
            var p1Idx = polygon.Points[i].VertexIndex - 1;
            var p2Idx = polygon.Points[(i + 1) % polygon.Points.Length].VertexIndex - 1;

            var vertex1 = screenVertices[p1Idx];
            var vertex2 = screenVertices[p2Idx];

            if (vertex1.W <= 0 || float.IsNaN(vertex1.X) || float.IsNaN(vertex1.Y) || float.IsInfinity(vertex1.X) || float.IsInfinity(vertex1.Y))
            {
                return;
            }

            if (vertex1.Y < minY)
            {
                minY = vertex1.Y;
            }
            if (vertex1.Y > maxY)
            {
                maxY = vertex1.Y;
            }

            var normal1 = model.Normals[polygon.Points[i].NormalIndex - 1];
            var normal2 = model.Normals[polygon.Points[(i + 1) % polygon.Points.Length].NormalIndex - 1];

            edges[i] = new Edge
            {
                FullVertex1 = new FullVertex
                {
                    Vertex = vertex1,
                    Normal = normal1,
                    Texture = model.TexturesVertices[polygon.Points[i].TextureIndex - 1],
                    Color = new Color(255) * Math.Clamp(Vector3.Dot(-normal1, light), 0, 1)
                },
                FullVertex2 = new FullVertex
                {
                    Vertex = vertex2,
                    Normal = normal2,
                    Texture = model.TexturesVertices[polygon.Points[(i + 1) % polygon.Points.Length].TextureIndex - 1],
                    Color = new Color(255) * Math.Clamp(Vector3.Dot(-normal2, light), 0, 1)
                }
            };
        }

        var startY = Math.Max((int)Math.Round(minY), 0);
        var endY = Math.Min((int)Math.Round(maxY), camera.ViewportHeight - 1);

        PhongTextureProcessor phongTextureProcessor = new();
        Span<FullVertex> ends = stackalloc FullVertex[2];

        for (var y = startY; y <= endY; y++)
        {
            var index = 0;

            for (var i = 0; i < edges.Length; i++)
            {
                if (index >= 2) break;
                ref var edge = ref edges[i];

                if (IsIntersect(y, edge.FullVertex1.Vertex.Y, edge.FullVertex2.Vertex.Y))
                {
                    var phi = (y - edge.FullVertex1.Vertex.Y) / (edge.FullVertex2.Vertex.Y - edge.FullVertex1.Vertex.Y);

                    ends[index] = new FullVertex
                    {
                        Vertex = Interpolate(edge.FullVertex1.Vertex, edge.FullVertex2.Vertex, edge.FullVertex1.Vertex.Y, edge.FullVertex2.Vertex.Y, y),
                        Normal = InterpolateNormal(edge.FullVertex1.Normal, edge.FullVertex2.Normal, edge.FullVertex1.Vertex.W, edge.FullVertex2.Vertex.W, edge.FullVertex1.Vertex.Y, edge.FullVertex2.Vertex.Y, y),
                        Texture = InterpolateUV(edge.FullVertex1.Texture, edge.FullVertex2.Texture, edge.FullVertex1.Vertex.W, edge.FullVertex2.Vertex.W, edge.FullVertex1.Vertex.Y, edge.FullVertex2.Vertex.Y, y),
                        Color = edge.FullVertex1.Color + (edge.FullVertex2.Color - edge.FullVertex1.Color) * phi
                    };

                    index++;
                }
            }

            if (index < 2)
            {
                continue;
            }

            if (ends[0].Vertex.X > ends[1].Vertex.X)
            {
                (ends[0], ends[1]) = (ends[1], ends[0]);
            }

            var startX = Math.Max((int)Math.Ceiling(ends[0].Vertex.X), 0);
            var endX = Math.Min((int)Math.Ceiling(ends[1].Vertex.X), camera.ViewportWidth);

            for (var x = startX; x < endX; x++)
            {
                var z = InterpolateFloat(ends[0].Vertex.Z, ends[1].Vertex.Z, ends[0].Vertex.X, ends[1].Vertex.X, x);

                Color finalColor = new(1);

                if (shadowProcessor is GouraudShadowProcessor)
                {
                    var phi = (x - ends[0].Vertex.X) / (ends[1].Vertex.X - ends[0].Vertex.X);
                    finalColor = ends[0].Color + (ends[1].Color - ends[0].Color) * phi;
                    finalColor.Alpha = ends[0].Color.Alpha + (ends[1].Color.Alpha - ends[0].Color.Alpha) * phi;
                }
                else
                {
                    var texture = InterpolateUV(ends[0].Texture, ends[1].Texture, ends[0].Vertex.W, ends[1].Vertex.W, ends[0].Vertex.X, ends[1].Vertex.X, x);

                    if (model.DiffuseTexture != null)
                    {
                        finalColor = model.DiffuseTexture.MakeColor(texture);
                    }

                    if (shadowProcessor is LambertShadowProcessor lambert)
                    {
                        finalColor = lambert.TransformColor(finalColor);
                    }
                    else if (shadowProcessor is PhongShadowProcessor phongShadow)
                    {
                        var n = InterpolateNormal(ends[0].Normal, ends[1].Normal, ends[0].Vertex.W, ends[1].Vertex.W, ends[0].Vertex.X, ends[1].Vertex.X, x);

                        if (model.NormalTexture != null)
                        {
                            n = model.NormalTexture.MakeNormal(texture);
                        }

                        phongShadow.ChangeIntensity(-n, light);
                        finalColor = phongShadow.TransformColor(finalColor);
                    }
                    else if (shadowProcessor is PhongLightProcessor phongLight)
                    {
                        var n = InterpolateNormal(ends[0].Normal, ends[1].Normal, ends[0].Vertex.W, ends[1].Vertex.W, ends[0].Vertex.X, ends[1].Vertex.X, x);
                        Vector3 pixelView = Vector3.Normalize(camera.ProjectFromScreen(new Vector3(x, y, 1)) - camera.Pivot.Position);

                        if (model.DiffuseTexture != null || model.NormalTexture != null || model.MRAOTexture != null)
                        {
                            var diffuseColor = model.DiffuseTexture != null ? model.DiffuseTexture.MakeColor(texture) : new Color(1);
                            var mraoColor = model.MRAOTexture != null ? model.MRAOTexture.MakeColor(texture) : new Color(0);

                            if (model.NormalTexture != null)
                            {
                                n = model.NormalTexture.MakeNormal(texture);
                            }

                            finalColor = PhongTextureProcessor.MakeColor(-n, light, -pixelView, diffuseColor, mraoColor);
                        }
                        else
                        {
                            phongLight.Change(-n, light, -pixelView);
                            finalColor = phongLight.TransformColor(finalColor);
                        }
                    }

                    if (model.EmissionTexture != null)
                    {
                        var emission = model.EmissionTexture.MakeColor(texture);
                        if (shadowProcessor is PhongLightProcessor) emission = emission * 10;
                        finalColor += emission;
                    }
                }

                FillColorWithScanline(colorsBuffer, zBuffer, ref ends[0], ref ends[1], x, y, camera.ViewportWidth, finalColor);
            }
        }
    }
}
