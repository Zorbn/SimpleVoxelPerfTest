using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VoxelChunk : MonoBehaviour
{
    public const int Size = 32;
    public const int Height = 256;
    // For bitwise shifting as opposed to multiply/divide.
    public const int SizeShifts = 5;
    public const int HeightShifts = 8;
    // For logical &ing as an alternative to modulo.
    public const int SizeAnd = Size - 1;
    public const int HeightAnd = Height - 1;
    public const int VoxelCount = Size * Height * Size;

    private const int BaseY = 50;
    private const float AmplitudeY = Height - BaseY;
    private const float NoiseScale = 0.003f;

    public int ChunkX, ChunkY, ChunkZ;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;
    private byte[] _voxels = new byte[VoxelCount];
    private Task _task;
    // Used to reduce the amount of meshing required, ie:
    // don't mesh too far beneath the surface and don't
    // mesh into the sky if the sky is full of empty space.
    public int MinVisibleY { get; private set; }
    public int MaxVisibleY { get; private set; }
    
    private void Start()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _mesh = new Mesh();
        _mesh.indexFormat = IndexFormat.UInt32;
        _meshFilter.sharedMesh = _mesh;

        Generate();
    }

    private void Generate()
    {
        MinVisibleY = Height;
        MaxVisibleY = 0;
        
        for (var x = -1; x < Size + 1; ++x)
        {
            for (var z = -1; z < Size + 1; ++z)
            {
                var globalX = ChunkX + x;
                var globalZ = ChunkZ + z;
                
                var height = (int)(BaseY + Mathf.PerlinNoise(globalX * NoiseScale, globalZ * NoiseScale) * AmplitudeY);
                height = Mathf.Min(height, Height);

                // The edges are only used for making sure Min/MaxVisibleY
                // line up between chunks and don't leave any gaps.
                if (x > -1 && x < Size && z > -1 && z < Size)
                {
                    for (var y = 0; y < height; y++)
                    {
                        _voxels[x + y * Size + z * Size * Height] = 1;
                    }
                }

                MinVisibleY = Math.Min(MinVisibleY, height);
                MaxVisibleY = Math.Max(MaxVisibleY, height);
            }
        }

        MinVisibleY = Math.Max(MinVisibleY - 1, 0);
        MaxVisibleY = Math.Min(MaxVisibleY + 1, Height);
    }

    public byte GetVoxel(int x, int y, int z)
    {
        if (x < 0 || y < 0 | z < 0 || x >= Size || y >= Height || z >= Size) return 0;
        
        return _voxels[x + y * Size + z * Size * Height];
    }

    // NOTE: Only one tesselator is needed for all chunks.
    public void Mesh(VoxelWorld voxelWorld, Tesselator tesselator)
    {
        var vertexCount = 0;
        var indexCount = 0;
        
        for (var localZ = 0; localZ < Size; ++localZ)
        {
            for (var localY = MinVisibleY; localY < MaxVisibleY; ++localY)
            {
                for (var localX = 0; localX < Size; ++localX)
                {
                    var voxel = _voxels[localX + localY * Size + localZ * Size * Height];
                    var x = localX + ChunkX;
                    var y = localY + ChunkY;
                    var z = localZ + ChunkZ;
                    
                    if (voxel == 0) continue;

                    // X
                    bool needsXMinus = voxelWorld.GetVoxel(x - 1, y, z) == 0;
                    if (needsXMinus)
                    {
                        tesselator.Vertices[vertexCount] = new Vector3(x, y, z);
                        tesselator.Vertices[vertexCount + 1] = new Vector3(x, y, z + 1);
                        tesselator.Vertices[vertexCount + 2] = new Vector3(x, y + 1, z + 1);
                        tesselator.Vertices[vertexCount + 3] = new Vector3(x, y + 1, z);

                        tesselator.Indices[indexCount] = vertexCount;
                        tesselator.Indices[indexCount + 1] = vertexCount + 1;
                        tesselator.Indices[indexCount + 2] = vertexCount + 2;
                        tesselator.Indices[indexCount + 3] = vertexCount;
                        tesselator.Indices[indexCount + 4] = vertexCount + 2;
                        tesselator.Indices[indexCount + 5] = vertexCount + 3;

                        tesselator.Colors[vertexCount] = Tesselator.XMinusShade;
                        tesselator.Colors[vertexCount + 1] = Tesselator.XMinusShade;
                        tesselator.Colors[vertexCount + 2] = Tesselator.XMinusShade;
                        tesselator.Colors[vertexCount + 3] = Tesselator.XMinusShade;
                        
                        vertexCount += 4;
                        indexCount += 6;
                    }
                    
                    bool needsXPlus = voxelWorld.GetVoxel(x + 1, y, z) == 0;
                    if (needsXPlus)
                    {
                        tesselator.Vertices[vertexCount] = new Vector3(x + 1, y, z);
                        tesselator.Vertices[vertexCount + 1] = new Vector3(x + 1, y, z + 1);
                        tesselator.Vertices[vertexCount + 2] = new Vector3(x + 1, y + 1, z + 1);
                        tesselator.Vertices[vertexCount + 3] = new Vector3(x + 1, y + 1, z);

                        tesselator.Indices[indexCount] = vertexCount;
                        tesselator.Indices[indexCount + 1] = vertexCount + 2;
                        tesselator.Indices[indexCount + 2] = vertexCount + 1;
                        tesselator.Indices[indexCount + 3] = vertexCount;
                        tesselator.Indices[indexCount + 4] = vertexCount + 3;
                        tesselator.Indices[indexCount + 5] = vertexCount + 2;
                        
                        tesselator.Colors[vertexCount] = Tesselator.XPlusShade;
                        tesselator.Colors[vertexCount + 1] = Tesselator.XPlusShade;
                        tesselator.Colors[vertexCount + 2] = Tesselator.XPlusShade;
                        tesselator.Colors[vertexCount + 3] = Tesselator.XPlusShade;
                        
                        vertexCount += 4;
                        indexCount += 6;
                    }
                    
                    // Y
                    bool needsYMinus = voxelWorld.GetVoxel(x, y - 1, z) == 0;
                    if (needsYMinus)
                    {
                        tesselator.Vertices[vertexCount] = new Vector3(x, y, z);
                        tesselator.Vertices[vertexCount + 1] = new Vector3(x, y, z + 1);
                        tesselator.Vertices[vertexCount + 2] = new Vector3(x + 1, y, z + 1);
                        tesselator.Vertices[vertexCount + 3] = new Vector3(x + 1, y, z);

                        tesselator.Indices[indexCount] = vertexCount;
                        tesselator.Indices[indexCount + 1] = vertexCount + 2;
                        tesselator.Indices[indexCount + 2] = vertexCount + 1;
                        tesselator.Indices[indexCount + 3] = vertexCount;
                        tesselator.Indices[indexCount + 4] = vertexCount + 3;
                        tesselator.Indices[indexCount + 5] = vertexCount + 2;
                        
                        tesselator.Colors[vertexCount] = Tesselator.YMinusShade;
                        tesselator.Colors[vertexCount + 1] = Tesselator.YMinusShade;
                        tesselator.Colors[vertexCount + 2] = Tesselator.YMinusShade;
                        tesselator.Colors[vertexCount + 3] = Tesselator.YMinusShade;
                        
                        vertexCount += 4;
                        indexCount += 6;
                    }
                    
                    bool needsYPlus = voxelWorld.GetVoxel(x, y + 1, z) == 0;
                    if (needsYPlus)
                    {
                        tesselator.Vertices[vertexCount] = new Vector3(x, y + 1, z);
                        tesselator.Vertices[vertexCount + 1] = new Vector3(x, y + 1, z + 1);
                        tesselator.Vertices[vertexCount + 2] = new Vector3(x + 1, y + 1, z + 1);
                        tesselator.Vertices[vertexCount + 3] = new Vector3(x + 1, y + 1, z);

                        tesselator.Indices[indexCount] = vertexCount;
                        tesselator.Indices[indexCount + 1] = vertexCount + 1;
                        tesselator.Indices[indexCount + 2] = vertexCount + 2;
                        tesselator.Indices[indexCount + 3] = vertexCount;
                        tesselator.Indices[indexCount + 4] = vertexCount + 2;
                        tesselator.Indices[indexCount + 5] = vertexCount + 3;
                        
                        tesselator.Colors[vertexCount] = Tesselator.YPlusShade;
                        tesselator.Colors[vertexCount + 1] = Tesselator.YPlusShade;
                        tesselator.Colors[vertexCount + 2] = Tesselator.YPlusShade;
                        tesselator.Colors[vertexCount + 3] = Tesselator.YPlusShade;
                        
                        vertexCount += 4;
                        indexCount += 6;
                    }
                    
                    // Z
                    bool needsZMinus = voxelWorld.GetVoxel(x, y, z - 1) == 0;
                    if (needsZMinus)
                    {
                        tesselator.Vertices[vertexCount] = new Vector3(x, y, z);
                        tesselator.Vertices[vertexCount + 1] = new Vector3(x + 1, y, z);
                        tesselator.Vertices[vertexCount + 2] = new Vector3(x + 1, y + 1, z);
                        tesselator.Vertices[vertexCount + 3] = new Vector3(x, y + 1, z);

                        tesselator.Indices[indexCount] = vertexCount;
                        tesselator.Indices[indexCount + 1] = vertexCount + 2;
                        tesselator.Indices[indexCount + 2] = vertexCount + 1;
                        tesselator.Indices[indexCount + 3] = vertexCount;
                        tesselator.Indices[indexCount + 4] = vertexCount + 3;
                        tesselator.Indices[indexCount + 5] = vertexCount + 2;
                        
                        tesselator.Colors[vertexCount] = Tesselator.ZMinusShade;
                        tesselator.Colors[vertexCount + 1] = Tesselator.ZMinusShade;
                        tesselator.Colors[vertexCount + 2] = Tesselator.ZMinusShade;
                        tesselator.Colors[vertexCount + 3] = Tesselator.ZMinusShade;
                        
                        vertexCount += 4;
                        indexCount += 6;
                    }
                    
                    bool needsZPlus = voxelWorld.GetVoxel(x, y, z + 1) == 0;
                    if (needsZPlus)
                    {
                        tesselator.Vertices[vertexCount] = new Vector3(x, y, z + 1);
                        tesselator.Vertices[vertexCount + 1] = new Vector3(x + 1, y, z + 1);
                        tesselator.Vertices[vertexCount + 2] = new Vector3(x + 1, y + 1, z + 1);
                        tesselator.Vertices[vertexCount + 3] = new Vector3(x, y + 1, z + 1);

                        tesselator.Indices[indexCount] = vertexCount;
                        tesselator.Indices[indexCount + 1] = vertexCount + 1;
                        tesselator.Indices[indexCount + 2] = vertexCount + 2;
                        tesselator.Indices[indexCount + 3] = vertexCount;
                        tesselator.Indices[indexCount + 4] = vertexCount + 2;
                        tesselator.Indices[indexCount + 5] = vertexCount + 3;
                        
                        tesselator.Colors[vertexCount] = Tesselator.ZPlusShade;
                        tesselator.Colors[vertexCount + 1] = Tesselator.ZPlusShade;
                        tesselator.Colors[vertexCount + 2] = Tesselator.ZPlusShade;
                        tesselator.Colors[vertexCount + 3] = Tesselator.ZPlusShade;
                        
                        vertexCount += 4;
                        indexCount += 6;
                    }
                    
                }
            }
        }
        
        _mesh.Clear();
        var bounds = new Bounds();
        bounds.min = new Vector3(0, MinVisibleY, 0);
        bounds.max = new Vector3(Size, MaxVisibleY, Size);
        _mesh.bounds = bounds;
        _mesh.SetVertices(tesselator.Vertices, 0, vertexCount, MeshUpdateFlags.DontValidateIndices);
        _mesh.SetColors(tesselator.Colors, 0, vertexCount);
        _mesh.SetIndices(tesselator.Indices, 0, indexCount, MeshTopology.Triangles, 0, false);
    }
}
