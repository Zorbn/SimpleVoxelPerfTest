using UnityEngine;

public class Tesselator
{
    // NOTE: When expanding to more chunks, only one buffer of this size
    // should be used for meshing all the chunks, rather than having tons
    // of extra buffers for each chunk!
    private const int MaxFaces = VoxelChunk.VoxelCount / 2 * 6;
    private const int MaxVertices = MaxFaces * 4;
    private const int MaxIndices = MaxFaces * 6;

    public static readonly Color XPlusShade = new(0.9f, 0.9f, 0.9f, 1.0f);
    public static readonly Color XMinusShade = new(0.8f, 0.8f, 0.8f, 1.0f);
    public static readonly Color YPlusShade = new(1.0f, 1.0f, 1.0f, 1.0f);
    public static readonly Color YMinusShade = new(0.6f, 0.6f, 0.6f, 1.0f);
    public static readonly Color ZPlusShade = new(0.8f, 0.8f, 0.8f, 1.0f);
    public static readonly Color ZMinusShade = new(0.7f, 0.7f, 0.7f, 1.0f);
    
    public readonly Vector3[] Vertices = new Vector3[MaxVertices];
    public readonly int[] Indices = new int[MaxIndices];
    public readonly Color[] Colors = new Color[MaxVertices];

    public int VertexCount = 0;
    public int IndexCount = 0;
}