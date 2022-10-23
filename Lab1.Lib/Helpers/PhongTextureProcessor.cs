// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Lab1.Lib.Types;

namespace Lab1.Lib.Helpers;

public class PhongTextureProcessor
{
    private Color MakeAmbientLight(Color color, float ambientFactor) => color * ambientFactor * 0.05f;

    private Color MakeDiffuseColor(Color color, Vector3 normal, Vector3 light) =>
        color * Math.Max(Vector3.Dot(normal, light), 0);

    private Color MakeSpecularLight(Color color, Vector3 normal, Vector3 light, Vector3 view)
    {
        Vector3 reflected = Vector3.Normalize(Vector3.Reflect(light, normal));
        var dot = Math.Max(Vector3.Dot(reflected, view), 0);
        var pow = MathF.Pow(dot, MathF.Pow(1 - color.Green, 8) * 127 + 1);

        return new Color((0.05f + 0.95f * MathF.Pow(1f - color.Green, 4)) * pow);
    }

    public Color MakeColor(Vector3 normal, Vector3 light, Vector3 view, Color diffuseColor, Color mraoColor) =>
        MakeAmbientLight(diffuseColor, mraoColor.Blue) +
        (MakeDiffuseColor(diffuseColor, normal, light) + MakeSpecularLight(mraoColor, normal, light, view)) * 1;
}
