using System.Numerics;

namespace Lab1.Lib;

public class ParsedObj
{
    public List<Vector4> V { get; } = new();
    public List<Vector3> Vt { get; } = new();
    public List<Vector3> Vn { get; } = new();
    public List<List<int[]>> F { get; } = new();

    public void TransformVertices(Matrix4x4 transformMatrix)
    {
        for (var i = 0; i < V.Count; i++)
        {
            V[i] = Vector4.Transform(V[i], transformMatrix);
        }
    }

    public override string ToString() =>
        string.Join(
            "\n",
            string.Join("\n", V.Select(v => $"v {v.X} {v.Y} {v.Z} {v.W}")),
            //string.Join("\n", Vt.Select(vt => $"vt {vt.X} {vt.Y} {vt.Z}")),
            //string.Join("\n", Vn.Select(vn => $"vn {vn.X} {vn.Y} {vn.Z}")),
            string.Join("\n", F.Select(f => $"f {string.Join(" ", f.Select(p => $"{p[0]}/{p[1]}/{p[2]}"))}"))
        ).Replace(',', '.');
}
