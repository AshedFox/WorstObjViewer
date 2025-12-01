using System.Globalization;
using System.Numerics;
using Lab1.Lib.Types;
using Lab1.Lib.Types.Primitives;

namespace Lab1.Lib.Helpers;

public static class ObjParser
{
    public static Model FromObjFile(string[] lines)
    {
        List<Vector3> vertices = [];
        List<Vector2> texturesVertices = [];
        List<Vector3> normals = [];
        List<Polygon> polygons = [];

        foreach (var lineStr in lines)
        {
            var line = lineStr.AsSpan().Trim();
            if (line.IsEmpty || line.IsWhiteSpace())
            {
                continue;
            }

            if (line.StartsWith("vt "))
            {
                var firstSpace = line.IndexOf(' ');
                var content = line.Slice(firstSpace + 1).Trim();

                var values = ParseSpans(content);
                texturesVertices.Add(new Vector2(values[0], values.Length > 1 ? values[1] : 0));
            }
            else if (line.StartsWith("vn "))
            {
                var firstSpace = line.IndexOf(' ');
                var values = ParseSpans(line.Slice(firstSpace + 1));
                normals.Add(new Vector3(values[0], values[1], values[2]));
            }
            else if (line.StartsWith("v "))
            {
                var firstSpace = line.IndexOf(' ');
                var values = ParseSpans(line.Slice(firstSpace + 1));
                vertices.Add(new Vector3(values[0], values[1], values[2]));
            }
            else if (line.StartsWith("f "))
            {
                polygons.Add(ParseFace(line));
            }
        }

        return new Model(vertices, texturesVertices, normals, polygons);
    }

    private static float[] ParseSpans(ReadOnlySpan<char> span)
    {
        var parts = span.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var res = new float[parts.Length];
        for (var i = 0; i < parts.Length; i++) res[i] = float.Parse(parts[i], CultureInfo.InvariantCulture);
        return res;
    }

    private static Polygon ParseFace(ReadOnlySpan<char> line)
    {
        List<Polygon.Point> points = [];
        var firstSpace = line.IndexOf(' ');
        var parts = line.Slice(firstSpace + 1).ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var indices = part.Split('/');
            points.Add(new Polygon.Point
            {
                VertexIndex = int.Parse(indices[0]),
                TextureIndex = indices.Length > 1 && indices[1].Length > 0 ? int.Parse(indices[1]) : 0,
                NormalIndex = indices.Length > 2 ? int.Parse(indices[2]) : 0
            });
        }
        return new Polygon(points);
    }
}
