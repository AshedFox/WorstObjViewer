// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Lab1.Lib.Interfaces;

namespace Lab1.Lib.Types;

public class PhongLightProcessor : IShadowProcessor
{
    private readonly float _ambientFactor;
    private readonly float _diffuseFactor;
    private readonly float _specularFactor;
    private readonly float _shininess;

    public Vector3 Normal { get; set; } = Vector3.Zero;
    public Vector3 Light { get; set; } = Vector3.Zero;
    public Vector3 View { get; set; } = Vector3.Zero;

    public PhongLightProcessor(float ambientFactor, float diffuseFactor, float specularFactor, float shininess)
    {
        _ambientFactor = Math.Clamp(ambientFactor, 0, 1);
        _diffuseFactor = Math.Clamp(diffuseFactor, 0, 1);
        _specularFactor = Math.Clamp(specularFactor, 0, 1);
        _shininess = shininess;
    }

    public void Change(float xStart, float xEnd, float x, Vector3 nStart, Vector3 nEnd, Vector3 light, Vector3 view)
    {
        Normal = -Vector3.Normalize(nStart + (nEnd - nStart) * (x - xStart) / (xEnd - xStart));
        Light = light;
        View = -view;
    }

    private Color MakeAmbientLight(Color color)
    {
        color.Red = (byte)(color.Red * _ambientFactor);
        color.Green = (byte)(color.Green * _ambientFactor);
        color.Blue = (byte)(color.Blue * _ambientFactor);

        return color;
    }

    private Color MakeDiffuseColor(Color color, Vector3 normal, Vector3 light)
    {
        var dot = Math.Clamp(Vector3.Dot(normal, light), 0, 1);

        color.Red = (byte)(color.Red * _diffuseFactor * dot);
        color.Green = (byte)(color.Green * _diffuseFactor * dot);
        color.Blue = (byte)(color.Blue * _diffuseFactor * dot);

        return color;
    }

    private Color MakeSpecularLight(Color color, Vector3 normal, Vector3 light, Vector3 view)
    {
        Vector3 reflected = Vector3.Reflect(light, normal);
        var dot = Math.Clamp(Vector3.Dot(reflected, view), 0, 1);
        var pow = Math.Pow(dot, _shininess);

        color.Red = (byte)(color.Red * _specularFactor * pow);
        color.Green = (byte)(color.Green * _specularFactor * pow);
        color.Blue = (byte)(color.Blue * _specularFactor * pow);

        return color;
    }

    public Color TransformColor(Color baseColor) =>
        MakeAmbientLight(baseColor) + MakeDiffuseColor(baseColor, Normal, Light) +
        MakeSpecularLight(baseColor, Normal, Light, View);
}
