using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class VoxelWorld : MonoBehaviour
{
    // private struct FinishedTesselator
    // {
    //     public Tesselator Tesselator;
    //     public VoxelChunk Chunk;
    // }

    public const int SizeInChunks = 40;
    private const int HeightInChunks = 1;
    private const int Size = SizeInChunks * VoxelChunk.Size;
    private const int Height = HeightInChunks * VoxelChunk.Height;
    private const int ChunkCount = SizeInChunks * HeightInChunks * SizeInChunks;
    private const int MaxTesselators = 10;

    [SerializeField] private Transform player;
    [SerializeField] private GameObject chunkPrefab;

    private VoxelChunkSemiGreed[] _chunks = new VoxelChunkSemiGreed[ChunkCount];
    private readonly Tesselator _tesselator = new();
    // private readonly ConcurrentQueue<Tesselator> _availableTesselators = new();
    // private readonly ConcurrentQueue<FinishedTesselator> _finishedTesselators = new();

    // private Thread _meshingThread;

    // private const int NoChunk = -1;
    // private int _chunkNeedingSwap = NoChunk;

    private Stopwatch _stopwatch = new();

    private void Start()
    {
        player.transform.position = new Vector3(Size * 0.5f, 0, Size * 0.5f);
        
        for (var z = 0; z < SizeInChunks; ++z)
        {
            for (var y = 0; y < HeightInChunks; ++y)
            {
                for (var x = 0; x < SizeInChunks; ++x)
                {
                    var newChunkObject = Instantiate(chunkPrefab);
                    var newChunk = newChunkObject.GetComponent<VoxelChunkSemiGreed>();
                    newChunk.ChunkX = VoxelChunk.Size * x;
                    newChunk.ChunkY = VoxelChunk.Height * y;
                    newChunk.ChunkZ = VoxelChunk.Size * z;
                    newChunk.MarkDirty();
                    _chunks[x + y * SizeInChunks + z * SizeInChunks * HeightInChunks] = newChunk;
                }
            }
        }
    }

    private void Update()
    {
        var meshTime = 0.0;
        for (var i = 0; i < _chunks.Length; i++)
        {
            if (_chunks[i].IsDirty)
            {
                _stopwatch.Restart();
                _chunks[i].Mesh(this, _tesselator);
                _stopwatch.Stop();
                meshTime += _stopwatch.Elapsed.TotalMilliseconds;
                _chunks[i].Swap(_tesselator);
            }
        }

        if (meshTime > 0.0)
        {
            Debug.Log($"Meshed in {meshTime} with an average of {meshTime / ChunkCount}");
        }

        if (!Input.GetMouseButtonDown(0)) return;

        foreach (var chunk in _chunks)
        {
            chunk.MarkDirty();
        }
        // MeshAll();
    }

    // private void MeshAll()
    // {
    //     double meshingTime = 0;
    //     for (var i = 0; i < ChunkCount; ++i)
    //     {
    //         var stopwatch = new Stopwatch();
    //         stopwatch.Start();
    //         _chunks[i].Mesh(this, _tesselator);
    //         stopwatch.Stop();
    //         meshingTime += stopwatch.Elapsed.TotalMilliseconds;
    //     }
    //     Debug.Log($"Meshed in: {meshingTime}, with an average of: {meshingTime / ChunkCount}ms");
    // }
    
    public byte GetVoxel(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x >= Size || y >= Height || z >= Size) return 1;
        
        var chunkX = x >> VoxelChunk.SizeShifts;
        var chunkY = y >> VoxelChunk.HeightShifts;
        var chunkZ = z >> VoxelChunk.SizeShifts;
        
        var localX = x & VoxelChunk.SizeAnd;
        var localY = y & VoxelChunk.HeightAnd;
        var localZ = z & VoxelChunk.SizeAnd;
        
        return _chunks[chunkX + chunkY * SizeInChunks + chunkZ * SizeInChunks * HeightInChunks].GetVoxel(localX, localY, localZ);
    }
}