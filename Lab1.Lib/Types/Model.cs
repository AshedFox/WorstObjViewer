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

        var minX = float.MaxValue;
        var maxX = float.MinValue;
        var minY = float.MaxValue;
        var maxY = float.MinValue;
        var minZ = float.MaxValue;
        var maxZ = float.MinValue;

        foreach (Vector3 localVertex in LocalVertices)
        {
            if (localVertex.X < minX)
            {
                minX = localVertex.X;
            }

            if (localVertex.X > maxX)
            {
                maxX = localVertex.X;
            }

            if (localVertex.Y < minY)
            {
                minY = localVertex.Y;
            }

            if (localVertex.Y > maxY)
            {
                maxY = localVertex.Y;
            }

            if (localVertex.Z < minZ)
            {
                minZ = localVertex.Z;
            }

            if (localVertex.Z > maxZ)
            {
                maxZ = localVertex.Z;
            }
        }

        Vector3 scale = new(40f / Math.Max(Math.Max(maxX - minX, maxY - minY), maxZ - minZ));

        Pivot.Scale(scale);

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
    public Texture? MRAOTexture { get; private set; }
    public Texture? EmissionTexture { get; private set; }


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

    public void ChangeMRAOTexture(Texture mraoTexture)
    {
        MRAOTexture = mraoTexture;
        OnChange();
    }

    public void ChangeEmissionTexture(Texture emissionTexture)
    {
        EmissionTexture = emissionTexture;
        OnChange();
    }

    protected virtual void OnChange() => Change?.Invoke();
}
