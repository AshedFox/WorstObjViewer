using System.Numerics;
using Lab1.Lib.Helpers;

namespace Lab1.Lib.Types;

public class Model
{
    public class PolygonPoint
    {
        public int VertexIndex { get; set; }
        public int? TextureIndex { get; set; }
        public int? NormalIndex { get; set; }
    }

    public Model(List<Vector3> vertices, List<Vector3> texturesVertices, List<Vector3> normals,
        List<List<PolygonPoint>> polygons)
    {
        Pivot = Pivot.CreateBasePivot(Vector3.Zero);
        Pivot.Scale(new Vector3(0.2f, 0.2f, 0.2f));
        Pivot.RotateY(GraphicsProcessor.ConvertDegreesToRadians(90));
        //Pivot.RotateZ(GraphicsProcessor.ConvertDegreesToRadians(180));
        LocalVertices = vertices;
        WorldVertices = vertices.Select(v => Pivot.ToWorldCoords(v)).ToList();
        TexturesVertices = texturesVertices;
        Normals = normals;
        Polygons = polygons;
    }

    public Pivot Pivot { get; set; }

    public List<Vector3> LocalVertices { get; }
    public List<Vector3> WorldVertices { get; }
    public List<Vector3> TexturesVertices { get; }
    public List<Vector3> Normals { get; }
    public List<List<PolygonPoint>> Polygons { get; }
}
