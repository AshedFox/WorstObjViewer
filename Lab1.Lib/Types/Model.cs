using System.Numerics;
using Lab1.Lib.Types.Primitives;
using Lab1.Lib.Types.Textures;

namespace Lab1.Lib.Types;

public class Model
{
    public delegate void ChangeHandler();

    public Model(IEnumerable<Vector3> vertices, IEnumerable<Vector2> texturesVertices, IEnumerable<Vector3> normals,
        IEnumerable<Polygon> polygons)
    {
        Pivot = Pivot.CreateBasePivot(Vector3.Zero);

        LocalVertices = vertices.ToArray();
        WorldVertices = LocalVertices.Select(v => Pivot.ToWorldCoords(v)).ToArray();
        TexturesVertices = texturesVertices.ToArray();
        Normals = normals.ToArray();
        Polygons = polygons.ToArray();
    }

    public Pivot Pivot { get; }

    public Vector3[] LocalVertices { get; }
    public Vector3[] WorldVertices { get; }
    public Vector2[] TexturesVertices { get; }
    public Vector3[] Normals { get; }
    public Polygon[] Polygons { get; }

    public Texture? DiffuseTexture { get; private set; }
    public NormalTexture? NormalTexture { get; private set; }
    public Texture? SpecularTexture { get; private set; }

    public event ChangeHandler? Change;

    public void ChangeDiffuseTexture(Texture diffuseDiffuseTexture)
    {
        DiffuseTexture = diffuseDiffuseTexture;
        OnChange();
    }

    public void ChangeNormalTexture(NormalTexture normalTexture)
    {
        NormalTexture = normalTexture;
        OnChange();
    }

    public void ChangeSpecularTexture(Texture specularTexture)
    {
        SpecularTexture = specularTexture;
        OnChange();
    }

    protected virtual void OnChange() => Change?.Invoke();
}
