// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace Lab1.Lib.Types.Textures;

public class Texture(byte[] colors, int width, int height, int bytesPerPixel)
{
    private readonly int _bytesPerPixel = bytesPerPixel;
    private readonly byte[] _colors = colors;
    private readonly int _height = height;
    private readonly int _width = width;

    public Color MakeColor(Vector2 uv)
    {
        var u = uv.X;
        var v = uv.Y;

        var x = (int)(u * _width);
        var y = (int)((1 - v) * _height);

        var offset = (x + y * _width) * _bytesPerPixel;

        if (offset >= 0 && offset + _bytesPerPixel < _colors.Length)
        {
            if (_bytesPerPixel == 4)
            {
                float b = _colors[offset];
                float g = _colors[offset + 1];
                float r = _colors[offset + 2];
                float a = _colors[offset + 3];

                return new Color(r / 255, g / 255, b / 255, a / 255);
            }

            if (_bytesPerPixel == 3)
            {
                float b = _colors[offset];
                float g = _colors[offset + 1];
                float r = _colors[offset + 2];

                return new Color(r / 255, g / 255, b / 255);
            }
        }

        return new Color(1);
    }
}
