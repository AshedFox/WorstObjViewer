// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace Lab1.Lib.Types;

public class Polygon
{
    public class Point
    {
        public int VertexIndex { get; set; }
        public int? TextureIndex { get; set; }
        public int? NormalIndex { get; set; }
    }

    public Point[] Points { get; }

    public Polygon(IEnumerable<Point> points) => Points = points.ToArray();
}
