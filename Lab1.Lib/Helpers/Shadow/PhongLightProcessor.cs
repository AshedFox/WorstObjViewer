// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Lab1.Lib.Types;

namespace Lab1.Lib.Helpers.Shadow;

public class PhongLightProcessor : IShadowProcessor
{
    public readonly float AmbientFactor;
    public readonly float DiffuseFactor;
    public readonly float Shininess;
    public readonly float SpecularFactor;

    public PhongLightProcessor(float ambientFactor, float diffuseFactor, float specularFactor, float shininess)
    {
        AmbientFactor = Math.Clamp(ambientFactor, 0, 1);
        DiffuseFactor = Math.Clamp(diffuseFactor, 0, 1);
        SpecularFactor = Math.Clamp(specularFactor, 0, 1);
        Shininess = shininess;
    }

    public Vector3 Normal { get; set; } = Vector3.Zero;
    public Vector3 Light { get; set; } = Vector3.Zero;
    public Vector3 View { get; set; } = Vector3.Zero;

    public Color TransformColor(Color baseColor) =>
        MakeAmbientLight(baseColor) + MakeDiffuseColor(baseColor, Normal, Light) +
        MakeSpecularLight(baseColor, Normal, Light, View);

    public void Change(Vector3 normal, Vector3 light, Vector3 view)
    {
        Normal = normal;
        Light = light;
        View = view;
    }

    private Color MakeAmbientLight(Color color)
    {
        color.Red = (byte)(color.Red * AmbientFactor);
        color.Green = (byte)(color.Green * AmbientFactor);
        color.Blue = (byte)(color.Blue * AmbientFactor);

        return color;
    }

    private Color MakeDiffuseColor(Color color, Vector3 normal, Vector3 light)
    {
        var dot = Math.Clamp(Vector3.Dot(normal, light), 0, 1);

        color.Red = (byte)(color.Red * DiffuseFactor * dot);
        color.Green = (byte)(color.Green * DiffuseFactor * dot);
        color.Blue = (byte)(color.Blue * DiffuseFactor * dot);

        return color;
    }

    private Color MakeSpecularLight(Color color, Vector3 normal, Vector3 light, Vector3 view)
    {
        Vector3 reflected = Vector3.Reflect(light, normal);
        var dot = Math.Clamp(Vector3.Dot(reflected, view), 0, 1);
        var pow = Math.Pow(dot, Shininess);

        color.Red = (byte)(color.Red * SpecularFactor * pow);
        color.Green = (byte)(color.Green * SpecularFactor * pow);
        color.Blue = (byte)(color.Blue * SpecularFactor * pow);

        return color;
    }
}
