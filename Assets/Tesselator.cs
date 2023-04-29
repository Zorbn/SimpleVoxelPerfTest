using System.Collections.Generic;
using UnityEngine;

public class Tesselator
{
    public static readonly Color XPlusShade = new(0.9f, 0.9f, 0.9f, 1.0f);
    public static readonly Color XMinusShade = new(0.8f, 0.8f, 0.8f, 1.0f);
    public static readonly Color YPlusShade = new(1.0f, 1.0f, 1.0f, 1.0f);
    public static readonly Color YMinusShade = new(0.6f, 0.6f, 0.6f, 1.0f);
    public static readonly Color ZPlusShade = new(0.8f, 0.8f, 0.8f, 1.0f);
    public static readonly Color ZMinusShade = new(0.7f, 0.7f, 0.7f, 1.0f);
    
    public readonly List<Vector3> Vertices = new();
    public readonly List<int> Indices = new();
    public readonly List<Color> Colors = new();

    public int VertexCount = 0;
    public int IndexCount = 0;
}