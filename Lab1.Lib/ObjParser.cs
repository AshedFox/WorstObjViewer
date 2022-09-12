using System.Globalization;
using System.Numerics;
using System.Text;

namespace Lab1.Lib;

public class ObjParser
{
    private static Vector4 ParseV(string line)
    {
        var values = line.Split(' ').Skip(1).Take(4).ToArray();
        return new Vector4
        {
            X = float.Parse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture),
            Y = float.Parse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture),
            Z = float.Parse(values[2], NumberStyles.Any, CultureInfo.InvariantCulture),
            W = float.Parse(values.ElementAtOrDefault(3) ?? "1", NumberStyles.Any, CultureInfo.InvariantCulture)
        };
    }

    private static Vector3 ParseVt(string line)
    {
        var values = line.Split(' ').Skip(1).Take(3).ToArray();
        return new Vector3
        {
            X = float.Parse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture),
            Y = float.Parse(values.ElementAtOrDefault(1) ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture),
            Z = float.Parse(values.ElementAtOrDefault(2) ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture)
        };
    }

    private static Vector3 ParseVn(string line)
    {
        var values = line.Split(' ').Skip(1).Take(3).ToArray();
        return new Vector3
        {
            X = float.Parse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture),
            Y = float.Parse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture),
            Z = float.Parse(values[2], NumberStyles.Any, CultureInfo.InvariantCulture)
        };
    }

    private static List<int[]> ParseF(string line)
    {
        List<int[]> result = new();
        var values = line.Split(' ').Skip(1).ToArray();
        foreach (var value in values)
        {
            var subValues = value.Split('/').Take(3).ToArray();
            result.Add(new[]
            {
                int.Parse(subValues[0]), int.Parse(subValues.ElementAtOrDefault(1) ?? "-1"),
                int.Parse(subValues.ElementAtOrDefault(2) ?? "-1")
            });
        }

        return result;
    }

    public static ParsedObj ParseFromObjFile(IEnumerable<string> lines)
    {
        ParsedObj result = new();

        foreach (var line in lines)
        {
            if (line.StartsWith("vt "))
            {
                result.Vt.Add(ParseVt(line));
            }
            else if (line.StartsWith("vn "))
            {
                result.Vn.Add(ParseVn(line));
            }
            else if (line.StartsWith("v "))
            {
                result.V.Add(ParseV(line));
            }
            else if (line.StartsWith("f "))
            {
                result.F.Add(ParseF(line));
            }
        }

        return result;
    }

    public static string ParseToWiredBresenham(ParsedObj parsedObj)
    {
        StringBuilder lBuilder = new();
        StringBuilder vBuilder = new();

        var index = 1;
        foreach (List<int[]>? f in parsedObj.F)
        {
            lBuilder.Append("l");

            for (var i = 0; i < f.Count - 1; i++)
            {
                GraphicsProcessor.Bresenham(
                    new Vector2(parsedObj.V[f[i][0] - 1].Y, parsedObj.V[f[i][0] - 1].Z),
                    new Vector2(parsedObj.V[f[i + 1][0] - 1].Y, parsedObj.V[f[i + 1][0] - 1].Z)
                ).ForEach(yzp =>
                {
                    GraphicsProcessor.Bresenham(
                        new Vector2(parsedObj.V[f[i][0] - 1].X, parsedObj.V[f[i][0] - 1].Y),
                        new Vector2(parsedObj.V[f[i + 1][0] - 1].X, parsedObj.V[f[i + 1][0] - 1].Y)
                    ).ForEach(xyp =>
                    {
                        vBuilder.AppendLine($"v {xyp.X} {xyp.Y} {yzp.Y}");
                        lBuilder.Append($" {index++}");
                    });
                });
            }

            lBuilder.AppendLine();
        }

        return string.Join("\n", vBuilder.ToString(), lBuilder.ToString());
    }

    public static string ParseToWired(ParsedObj parsedObj) =>
        string.Join(
            "\n",
            string.Join("\n", parsedObj.V.Select(v => $"v {v.X} {v.Y} {v.Z} {v.W}")),
            string.Join("\n", parsedObj.F.Select(f => $"l {string.Join(" ", f.Select(p => $"{p[0]}"))}"))
        );
}
