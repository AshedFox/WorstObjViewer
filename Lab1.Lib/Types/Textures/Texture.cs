// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace Lab1.Lib.Types.Textures;

public class Texture
{
    private readonly int _bytesPerPixel;
    private readonly byte[] _colors;
    private readonly int _height;
    private readonly int _width;

    public Texture(byte[] colors, int width, int height, int bytesPerPixel)
    {
        _colors = colors;
        _width = width;
        _height = height;
        _bytesPerPixel = bytesPerPixel;
    }

    public Color MakeColor(Vector2 texture)
    {
        var u = texture.X;
        var v = texture.Y;

        var x = (int)(u * _width);
        var y = (int)((1 - v) * _height);

        var offset = (x + y * _width) * _bytesPerPixel;

        if (_bytesPerPixel == 4)
        {
            var b = _colors[offset];
            var g = _colors[offset + 1];
            var r = _colors[offset + 2];
            var a = _colors[offset + 3];

            return new Color(r, g, b, a);
        }

        if (_bytesPerPixel == 3)
        {
            var b = _colors[offset];
            var g = _colors[offset + 1];
            var r = _colors[offset + 2];

            return new Color(r, g, b);
        }

        return new Color(255);
    }
}
