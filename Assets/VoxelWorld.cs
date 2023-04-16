﻿using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class VoxelWorld : MonoBehaviour
{
    private const int SizeInChunks = 40;
    private const int HeightInChunks = 1;
    private const int Size = SizeInChunks * VoxelChunk.Size;
    private const int Height = HeightInChunks * VoxelChunk.Height;
    private const int ChunkCount = SizeInChunks * HeightInChunks * SizeInChunks;
    
    [SerializeField] private GameObject chunkPrefab;
    private VoxelChunk[] _chunks = new VoxelChunk[ChunkCount];
    private readonly Tesselator _tesselator = new();

    private void Start()
    {
        for (var z = 0; z < SizeInChunks; ++z)
        {
            for (var y = 0; y < HeightInChunks; ++y)
            {
                for (var x = 0; x < SizeInChunks; ++x)
                {
                    var newChunkObject = Instantiate(chunkPrefab);
                    var newChunk = newChunkObject.GetComponent<VoxelChunk>();
                    newChunk.ChunkX = VoxelChunk.Size * x;
                    newChunk.ChunkY = VoxelChunk.Height * y;
                    newChunk.ChunkZ = VoxelChunk.Size * z;
                    _chunks[x + y * SizeInChunks + z * SizeInChunks * HeightInChunks] = newChunk;
                }
            }
        }
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        
        MeshAll();
    }

    private void MeshAll()
    {
        double meshingTime = 0;
        for (var i = 0; i < ChunkCount; ++i)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _chunks[i].Mesh(this, _tesselator);
            stopwatch.Stop();
            meshingTime += stopwatch.Elapsed.TotalMilliseconds;
        }
        Debug.Log($"Meshed in: {meshingTime}, with an average of: {meshingTime / ChunkCount}ms");
    }
    
    public byte GetVoxel(int x, int y, int z)
    {
        if (x < 0 || y < 0 | z < 0 || x >= Size || y >= Height || z >= Size) return 0;
        
        var chunkX = x >> VoxelChunk.SizeShifts;
        var chunkY = y >> VoxelChunk.HeightShifts;
        var chunkZ = z >> VoxelChunk.SizeShifts;
        
        var localX = x & VoxelChunk.SizeAnd;
        var localY = y & VoxelChunk.HeightAnd;
        var localZ = z & VoxelChunk.SizeAnd;

        var voxelChunk = _chunks[chunkX + chunkY * SizeInChunks + chunkZ * SizeInChunks * HeightInChunks];

        return voxelChunk.GetVoxel(localX, localY, localZ);
    }
}