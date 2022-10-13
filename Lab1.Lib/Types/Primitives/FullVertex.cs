// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace Lab1.Lib.Types.Primitives;

public struct FullVertex
{
    public Vector4 Vertex { get; set; }
    public Vector3 Normal { get; set; }
    public Vector2 Texture { get; set; }
    public Color Color { get; set; }
}
