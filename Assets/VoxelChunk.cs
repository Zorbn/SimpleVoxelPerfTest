using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VoxelChunk : MonoBehaviour
{
    public enum Direction
    {
        XPos,
        XNeg,
        YPos,
        YNeg,
        ZPos,
        ZNeg
    }

    private static readonly Color[] FaceColors =
    {
        Tesselator.XPlusShade,
        Tesselator.XMinusShade,
        Tesselator.YPlusShade,
        Tesselator.YMinusShade,
        Tesselator.ZPlusShade,
        Tesselator.ZMinusShade
    };

    private static readonly Vector3[][] FaceVertices =
    {
        new Vector3[]
        {
            new(1, 0, 0), new(1, 0, 1),
            new(1, 1, 1), new(1, 1, 0),
        },
        new Vector3[]
        {
            new(0, 0, 0), new(0, 0, 1),
            new(0, 1, 1), new(0, 1, 0),
        },
        new Vector3[]
        {
            new(0, 1, 0), new(0, 1, 1),
            new(1, 1, 1), new(1, 1, 0),
        },
        new Vector3[]
        {
            new(0, 0, 0), new(0, 0, 1),
            new(1, 0, 1), new(1, 0, 0),
        },
        new Vector3[]
        {
            new(0, 0, 1), new(0, 1, 1),
            new(1, 1, 1), new(1, 0, 1),
        },
        new Vector3[]
        {
            new(0, 0, 0), new(0, 1, 0),
            new(1, 1, 0), new(1, 0, 0),
        },
    };

    private static readonly int[][] FaceIndices =
    {
        new[] { 0, 2, 1, 0, 3, 2 },
        new[] { 0, 1, 2, 0, 2, 3 },
        new[] { 0, 1, 2, 0, 2, 3 },
        new[] { 0, 2, 1, 0, 3, 2 },
        new[] { 0, 2, 1, 0, 3, 2 },
        new[] { 0, 1, 2, 0, 2, 3 },
    };

    private static readonly Vector3Int[] FaceDirections =
    {
        new(1, 0, 0),
        new(-1, 0, 0),
        new(0, 1, 0),
        new(0, -1, 0),
        new(0, 0, 1),
        new(0, 0, -1),
    };

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

    private Tesselator _tesselator = new();

    // Used to reduce the amount of meshing required, ie:
    // don't mesh into the sky if the sky is full of empty space.
    public int MaxOccupiedY { get; private set; }

    public void Init()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _mesh = new Mesh
        {
            indexFormat = IndexFormat.UInt32
        };
        _meshFilter.sharedMesh = _mesh;

        var bounds = new Bounds
        {
            min = new Vector3(0, 0, 0),
            max = new Vector3(Size, Height, Size)
        };
        _mesh.bounds = bounds;
        
        Generate();
    }

    private void Generate()
    {
        MaxOccupiedY = 0;

        for (var x = 0; x < Size; ++x)
        {
            for (var z = 0; z < Size; ++z)
            {
                var globalX = ChunkX + x;
                var globalZ = ChunkZ + z;
                
                var height = (int)(BaseY + Mathf.PerlinNoise(globalX * NoiseScale, globalZ * NoiseScale) * AmplitudeY);
                height = Mathf.Min(height, Height);
                
                for (var y = 0; y < height; y++)
                {
                    _voxels[x + y * Size + z * Size * Height] = 1;
                }

                MaxOccupiedY = Math.Max(MaxOccupiedY, height);
            }
        }

        MaxOccupiedY = Math.Min(MaxOccupiedY + 1, Height);
    }

    public byte GetVoxel(int x, int y, int z)
    {
        if (x < 0 || y < 0 | z < 0 || x >= Size || y >= Height || z >= Size) return 1;

        return _voxels[x + y * Size + z * Size * Height];
    }

    // Called at the end of a run.
    private void GenerateFaces(int runStart, int runStartYPos, int runStartYNeg, int runStartZPos, int runStartZNeg,
        int runEnd, int globalY, int globalZ, VoxelWorld voxelWorld)
    {
        if (voxelWorld.GetVoxel(runEnd + 1, globalY, globalZ) == 0)
        {
            GenerateFace((int)Direction.XPos, runEnd, globalY, globalZ);
        }

        if (voxelWorld.GetVoxel(runStart - 1, globalY, globalZ) == 0)
        {
            GenerateFace((int)Direction.XNeg, runStart, globalY, globalZ);
        }

        if (runStartYPos != -1)
        {
            var runLength = runEnd - runStartYPos;
            GenerateFaceWithLength((int)Direction.YPos, runLength, runStartYPos, globalY, globalZ);
        }

        if (runStartYNeg != -1)
        {
            var runLength = runEnd - runStartYNeg;
            GenerateFaceWithLength((int)Direction.YNeg, runLength, runStartYNeg, globalY, globalZ);
        }

        if (runStartZPos != -1)
        {
            var runLength = runEnd - runStartZPos;
            GenerateFaceWithLength((int)Direction.ZPos, runLength, runStartZPos, globalY, globalZ);
        }

        if (runStartZNeg != -1)
        {
            var runLength = runEnd - runStartZNeg;
            GenerateFaceWithLength((int)Direction.ZNeg, runLength, runStartZNeg, globalY, globalZ);
        }
    }

    public void Mesh(VoxelWorld voxelWorld)
    {
        _tesselator.Vertices.Clear();
        _tesselator.Colors.Clear();
        _tesselator.Indices.Clear();
        
        _tesselator.VertexCount = 0;
        _tesselator.IndexCount = 0;

        for (var localZ = 0; localZ < Size; ++localZ)
        {
            var globalZ = localZ + ChunkZ;
            for (var localY = 0; localY < Height; ++localY) // TODO: Change this to use MaxOccupiedY
            {
                var globalY = localY + ChunkY;

                var inRun = false;
                var runStart = 0;
                var runStartYPos = -1;
                var runStartYNeg = -1;
                var runStartZPos = -1;
                var runStartZNeg = -1;

                for (var localX = 0; localX < Size; ++localX)
                {
                    var globalX = localX + ChunkX;
                    var voxel = GetVoxel(localX, localY, localZ);

                    if (voxel == 0 && !inRun) continue;

                    if (voxel == 0)
                    {
                        // End run:
                        GenerateFaces(runStart, runStartYPos, runStartYNeg, runStartZPos, runStartZNeg, globalX - 1,
                            globalY, globalZ, voxelWorld);
                        runStartYPos = -1;
                        runStartYNeg = -1;
                        runStartZPos = -1;
                        runStartZNeg = -1;
                        inRun = false;
                        continue;
                    }

                    if (!inRun)
                    {
                        // Start run:
                        inRun = true;
                        runStart = globalX;
                    }

                    // Continue run:
                    if (runStartYPos == -1 && voxelWorld.GetVoxel(globalX, globalY + 1, globalZ) == 0) runStartYPos = globalX;
                    if (runStartYNeg == -1 && voxelWorld.GetVoxel(globalX, globalY - 1, globalZ) == 0) runStartYNeg = globalX;
                    if (runStartZPos == -1 && voxelWorld.GetVoxel(globalX, globalY, globalZ + 1) == 0) runStartZPos = globalX;
                    if (runStartZNeg == -1 && voxelWorld.GetVoxel(globalX, globalY, globalZ - 1) == 0) runStartZNeg = globalX;
                }
                
                if (inRun)
                {
                    // End run:
                    GenerateFaces(runStart, runStartYPos, runStartYNeg, runStartZPos, runStartZNeg, ChunkX + Size - 1,
                        globalY, globalZ, voxelWorld);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GenerateFace(int directionI, int x, int y, int z)
    {
        for (var i = 0; i < 6; i++)
        {
            _tesselator.Indices.Add(FaceIndices[directionI][i] + _tesselator.VertexCount);
            ++_tesselator.IndexCount;
        }
        
        var position = new Vector3(x, y, z);
        for (var i = 0; i < 4; i++)
        {
            _tesselator.Vertices.Add(FaceVertices[directionI][i] + position);
            _tesselator.Colors.Add(FaceColors[directionI]);
            ++_tesselator.VertexCount;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GenerateFaceWithLength(int directionI, int length, int x, int y, int z)
    {
        for (var i = 0; i < 6; i++)
        {
            _tesselator.Indices.Add(FaceIndices[directionI][i] + _tesselator.VertexCount);
            ++_tesselator.IndexCount;
        }
        
        _tesselator.Colors.Add(FaceColors[directionI]);
        _tesselator.Colors.Add(FaceColors[directionI]);
        _tesselator.Colors.Add(FaceColors[directionI]);
        _tesselator.Colors.Add(FaceColors[directionI]);

        var startPosition = new Vector3(x, y, z);
        var endPosition = new Vector3(x + length, y, z);
        _tesselator.Vertices.Add(FaceVertices[directionI][0] + startPosition);
        _tesselator.Vertices.Add(FaceVertices[directionI][1] + startPosition);
        _tesselator.Vertices.Add(FaceVertices[directionI][2] + endPosition);
        _tesselator.Vertices.Add(FaceVertices[directionI][3] + endPosition);
        _tesselator.VertexCount += 4;
    }

    public void Swap()
    {
        _mesh.Clear();
        _mesh.SetVertices(_tesselator.Vertices, 0, _tesselator.VertexCount, MeshUpdateFlags.DontValidateIndices);
        _mesh.SetColors(_tesselator.Colors, 0, _tesselator.VertexCount);
        _mesh.SetIndices(_tesselator.Indices, 0, _tesselator.IndexCount, MeshTopology.Triangles, 0, false);
    }
}