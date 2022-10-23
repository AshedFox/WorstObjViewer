// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

namespace Lab1.Lib.Types;

public struct Color
{
    public Color(float value)
    {
        Red = value;
        Green = value;
        Blue = value;
        Alpha = 1;
    }

    public Color(float red, float green, float blue, float alpha = 1)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }

    public float Red { get; set; }
    public float Green { get; set; }
    public float Blue { get; set; }
    public float Alpha { get; set; }

    public static Color operator *(Color color, float multiplier)
    {
        color.Red = color.Red * multiplier;
        color.Green = color.Green * multiplier;
        color.Blue = color.Blue * multiplier;

        return color;
    }

    public static Color operator /(Color color, float divider)
    {
        color.Red = color.Red / divider;
        color.Green = color.Green / divider;
        color.Blue = color.Blue / divider;

        return color;
    }

    public static Color operator +(Color color1, Color color2)
    {
        color1.Red = color1.Red + color2.Red;
        color1.Green = color1.Green + color2.Green;
        color1.Blue = color1.Blue + color2.Blue;

        return color1;
    }

    public static Color operator -(Color color1, Color color2)
    {
        color1.Red = color1.Red - color2.Red;
        color1.Green = color1.Green - color2.Green;
        color1.Blue = color1.Blue - color2.Blue;

        return color1;
    }
}
