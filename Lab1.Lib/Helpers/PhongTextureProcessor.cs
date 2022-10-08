// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Lab1.Lib.Types;

namespace Lab1.Lib.Helpers;

public class PhongTextureProcessor
{
    public float Shininess { get; set; }

    private Color MakeDiffuseColor(Color color, Vector3 normal, Vector3 light)
    {
        var dot = Math.Clamp(Vector3.Dot(normal, light), 0, 1);

        color.Red = (byte)(color.Red * dot);
        color.Green = (byte)(color.Green * dot);
        color.Blue = (byte)(color.Blue * dot);

        return color;
    }

    private Color MakeSpecularLight(Color color, Vector3 normal, Vector3 light, Vector3 view)
    {
        Vector3 reflected = Vector3.Reflect(light, normal);
        var dot = Math.Clamp(Vector3.Dot(reflected, view), 0, 1);
        var pow = Math.Pow(dot, Shininess);

        color.Red = (byte)(color.Red * pow);
        color.Green = (byte)(color.Green * pow);
        color.Blue = (byte)(color.Blue * pow);

        return color;
    }

    public Color MakeColor(Vector3 normal, Vector3 light, Vector3 view, Color diffuseColor, Color specularColor) =>
        MakeDiffuseColor(diffuseColor, normal, light) + MakeSpecularLight(specularColor, normal, light, view);
}
