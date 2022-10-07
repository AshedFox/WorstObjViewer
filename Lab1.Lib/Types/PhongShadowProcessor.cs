// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Lab1.Lib.Interfaces;

namespace Lab1.Lib.Types;

public class PhongShadowProcessor : IShadowProcessor
{
    public float Intensity { get; set; }

    public void ChangeIntensity(float xStart, float xEnd, float x, Vector3 nStart, Vector3 nEnd, Vector3 light)
    {
        Vector3 n = -Vector3.Normalize(nStart + (nEnd - nStart) * (x - xStart) / (xEnd - xStart));
        Intensity = Math.Clamp(Vector3.Dot(n, light), 0, 1);
    }

    public Color TransformColor(Color baseColor) => baseColor * Intensity;
}
