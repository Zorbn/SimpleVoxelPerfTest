using UnityEngine;

public class Tesselator
{
    // NOTE: When expanding to more chunks, only one buffer of this size
    // should be used for meshing all the chunks, rather than having tons
    // of extra buffers for each chunk!
    private const int MaxFaces = VoxelChunk.VoxelCount / 2 * 6;
    private const int MaxVertices = MaxFaces * 4;
    private const int MaxIndices = MaxFaces * 6;

    public static readonly Color XPlusShade = new Color(0.9f, 0.9f, 0.9f, 1.0f);
    public static readonly Color XMinusShade = new Color(0.8f, 0.8f, 0.8f, 1.0f);
    public static readonly Color YPlusShade = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    public static readonly Color YMinusShade = new Color(0.6f, 0.6f, 0.6f, 1.0f);
    public static readonly Color ZPlusShade = new Color(0.8f, 0.8f, 0.8f, 1.0f);
    public static readonly Color ZMinusShade = new Color(0.7f, 0.7f, 0.7f, 1.0f);
    
    public Vector3[] Vertices = new Vector3[MaxVertices];
    public int[] Indices = new int[MaxIndices];
    public Color[] Colors = new Color[MaxVertices];
}