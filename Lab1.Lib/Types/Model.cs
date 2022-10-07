using System.Numerics;
using Lab1.Lib.Helpers;

namespace Lab1.Lib.Types;

public class Model
{
    public Model(IEnumerable<Vector3> vertices, IEnumerable<Vector3> texturesVertices, IEnumerable<Vector3> normals,
        IEnumerable<Polygon> polygons)
    {
        Pivot = Pivot.CreateBasePivot(Vector3.Zero);

        LocalVertices = vertices.ToArray();
        WorldVertices = LocalVertices.Select(v => Pivot.ToWorldCoords(v)).ToArray();
        TexturesVertices = texturesVertices.ToArray();
        Normals = normals.ToArray();
        Polygons = polygons.ToArray();
    }

    public Pivot Pivot { get; set; }

    public Vector3[] LocalVertices { get; }
    public Vector3[] WorldVertices { get; }
    public Vector3[] TexturesVertices { get; }
    public Vector3[] Normals { get; }
    public Polygon[] Polygons { get; }
}
