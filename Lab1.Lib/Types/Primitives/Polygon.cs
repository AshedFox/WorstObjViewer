// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

namespace Lab1.Lib.Types.Primitives;

public class Polygon(IEnumerable<Polygon.Point> points)
{
    public Point[] Points { get; } = [.. points];

    public class Point
    {
        public int VertexIndex { get; set; }
        public int TextureIndex { get; set; }
        public int NormalIndex { get; set; }
    }
}
