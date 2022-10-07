// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Lab1.Lib.Interfaces;

namespace Lab1.Lib.Types;

public class LambertShadowProcessor : IShadowProcessor
{
    public float Intensity { get; set; }

    public void ChangeIntensity(Vector3 light, Vector3 normal) =>
        Intensity = Math.Clamp(Vector3.Dot(normal, light), 0, 1);

    public Color TransformColor(Color baseColor) => baseColor * Intensity;
}
