// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

namespace Lab1.Lib.Types;

public struct Color
{
    public Color()
    {
        Red = 0;
        Green = 0;
        Blue = 0;
    }

    public Color(byte value)
    {
        Red = value;
        Green = value;
        Blue = value;
    }

    public byte Red { get; set; }
    public byte Green { get; set; }
    public byte Blue { get; set; }

    public static Color operator *(Color color, float multiplier)
    {
        color.Red = (byte)Math.Clamp(color.Red * multiplier, 0, 255);
        color.Green = (byte)Math.Clamp(color.Green * multiplier, 0, 255);
        color.Blue = (byte)Math.Clamp(color.Blue * multiplier, 0, 255);

        return color;
    }

    public static Color operator +(Color color1, Color color2)
    {
        color1.Red = (byte)Math.Clamp(color1.Red + color2.Red, 0, 255);
        color1.Green = (byte)Math.Clamp(color1.Green + color2.Green, 0, 255);
        color1.Blue = (byte)Math.Clamp(color1.Blue + color2.Blue, 0, 255);

        return color1;
    }
}
