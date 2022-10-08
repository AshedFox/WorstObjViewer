using System.Globalization;
using System.Numerics;
using Lab1.Lib.Types;
using Lab1.Lib.Types.Primitives;

namespace Lab1.Lib.Helpers;

public static class ObjParser
{
    private static Vector3 ParseV(string line)
    {
        var values = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).Take(3).ToArray();
        return new Vector3
        {
            X = float.Parse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture),
            Y = float.Parse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture),
            Z = float.Parse(values[2], NumberStyles.Any, CultureInfo.InvariantCulture)
        };
    }

    private static Vector2 ParseVt(string line)
    {
        var values = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).Take(3).ToArray();
        return new Vector2
        {
            X = float.Parse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture),
            Y = float.Parse(values.ElementAtOrDefault(1) ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture)
        };
    }

    private static Vector3 ParseVn(string line)
    {
        var values = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).Take(3).ToArray();
        return new Vector3
        {
            X = float.Parse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture),
            Y = float.Parse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture),
            Z = float.Parse(values[2], NumberStyles.Any, CultureInfo.InvariantCulture)
        };
    }

    private static List<Polygon.Point> ParseF(string line)
    {
        List<Polygon.Point> result = new();
        var values = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
        foreach (var value in values)
        {
            var subValues = value.Split('/', StringSplitOptions.RemoveEmptyEntries).Take(3).ToArray();
            Polygon.Point polyPoint = new()
            {
                VertexIndex = int.Parse(subValues[0]),
                TextureIndex = int.Parse(subValues[1]),
                NormalIndex = int.Parse(subValues[2])
            };

            result.Add(polyPoint);
        }

        return result;
    }

    public static Model FromObjFile(IEnumerable<string> lines)
    {
        List<Vector3> vertices = new();
        List<Vector2> texturesVertices = new();
        List<Vector3> normals = new();
        List<Polygon> polygons = new();

        foreach (var line in lines)
        {
            if (line.StartsWith("vt "))
            {
                texturesVertices.Add(ParseVt(line));
            }
            else if (line.StartsWith("vn "))
            {
                normals.Add(ParseVn(line));
            }
            else if (line.StartsWith("v "))
            {
                vertices.Add(ParseV(line));
            }
            else if (line.StartsWith("f "))
            {
                polygons.Add(new Polygon(ParseF(line)));
            }
        }

        return new Model(vertices, texturesVertices, normals, polygons);
    }
}
