// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace Lab1.Lib.Types.Textures;

public class NormalTexture
{
    private readonly int _bytesPerPixel;
    private readonly byte[] _colors;
    private readonly int _height;
    private readonly int _width;

    public NormalTexture(byte[] colors, int width, int height, int bytesPerPixel)
    {
        _colors = colors;
        _width = width;
        _height = height;
        _bytesPerPixel = bytesPerPixel;
    }

    public Vector3 MakeNormal(Vector2 texture)
    {
        var u = texture.X;
        var v = texture.Y;

        var x = (int)(u * _width);
        var y = (int)((1 - v) * _height);

        var offset = (x + y * _width) * _bytesPerPixel;

        if (offset >= 0 && offset + _bytesPerPixel < _colors.Length && (_bytesPerPixel == 4 || _bytesPerPixel == 3))
        {
            float b = _colors[offset];
            float g = _colors[offset + 1];
            float r = _colors[offset + 2];

            return new Vector3(r / 255f * 2f - 1f, g / 255f * 2f - 1f, b / 255f * 2f - 1f);
        }

        return new Vector3(0, 0, 0);
    }
}
