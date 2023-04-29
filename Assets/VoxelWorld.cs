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
    private const int RadiusInChunks = SizeInChunks / 2;
    private const int Size = SizeInChunks * VoxelChunk.Size;
    private const int Height = VoxelChunk.Height;
    private const int ChunkCount = SizeInChunks * SizeInChunks;
    private const int MaxTesselators = 10;

    [SerializeField] private Transform player;
    [SerializeField] private GameObject chunkPrefab;

    private Vector3Int _playerChunkPosition;

    private VoxelChunk[] _oldChunks = new VoxelChunk[ChunkCount];
    private VoxelChunk[] _chunks = new VoxelChunk[ChunkCount];

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
        UpdateLoadedChunks();

        // for (var z = 0; z < SizeInChunks; ++z)
        // {
        //     for (var x = 0; x < SizeInChunks; ++x)
        //     {
        //         var newChunkObject = Instantiate(chunkPrefab);
        //         var newChunk = newChunkObject.GetComponent<VoxelChunk>();
        //         newChunk.ChunkX = VoxelChunk.Size * x;
        //         newChunk.ChunkZ = VoxelChunk.Size * z;
        //         newChunk.Init();
        //         newChunk.MarkDirty();
        //         _chunks[x + z * SizeInChunks] = newChunk;
        //     }
        // }
        
        // UpdateLoadedChunks();
    }

    private void UpdatePlayerChunkPosition()
    {
        var playerPosition = player.transform.position;
        _playerChunkPosition = new Vector3Int((int)playerPosition.x / VoxelChunk.Size,
            (int)playerPosition.y / VoxelChunk.Height, (int)playerPosition.z / VoxelChunk.Size);
    }

    private void UpdateLoadedChunks()
    {
        var oldChunkPosition = _playerChunkPosition;
        UpdatePlayerChunkPosition();
        
        if (_playerChunkPosition == oldChunkPosition) return;
        
        var chunkMin = new Vector3Int(_playerChunkPosition.x - RadiusInChunks, _playerChunkPosition.y,
            _playerChunkPosition.z - RadiusInChunks);
        var chunkMax = new Vector3Int(_playerChunkPosition.x + RadiusInChunks, _playerChunkPosition.y,
            _playerChunkPosition.z + RadiusInChunks);

        Array.Copy(_chunks, _oldChunks, _chunks.Length);
        Array.Clear(_chunks, 0, _chunks.Length);

        // Save or remove old chunks.
        for (var z = 0; z < SizeInChunks; ++z)
        {
            var oldRelativeZ = oldChunkPosition.z - RadiusInChunks + z;
            for (var x = 0; x < SizeInChunks; ++x)
            {
                var oldRelativeX = oldChunkPosition.x - RadiusInChunks + x;
                var oldChunkI = x + z * SizeInChunks;

                if (!_oldChunks[oldChunkI]) continue;

                if (oldRelativeX >= chunkMin.x && oldRelativeX < chunkMax.x &&
                    oldRelativeZ >= chunkMin.z && oldRelativeZ < chunkMax.z)
                {
                    // The chunk is within the new bounds.
                    var newX = oldRelativeX - chunkMin.x;
                    var newZ = oldRelativeZ - chunkMin.z;

                    _chunks[newX + newZ * SizeInChunks] = _oldChunks[oldChunkI];
                }
                else
                {
                    // The chunk is outside of the new bounds, and should be destroyed.
                    Destroy(_oldChunks[oldChunkI].gameObject);
                    _oldChunks[oldChunkI] = null;
                }
            }
        }

        // Generate new chunks.
        for (var z = 0; z < SizeInChunks; ++z)
        {
            var relativeZ = _playerChunkPosition.z - RadiusInChunks + z;
            for (var x = 0; x < SizeInChunks; ++x)
            {
                var relativeX = _playerChunkPosition.x - RadiusInChunks + x;
                var chunkI = x + z * SizeInChunks;

                if (_chunks[chunkI]) continue;
                
                var newChunkObject = Instantiate(chunkPrefab);
                var newChunk = newChunkObject.GetComponent<VoxelChunk>();
                newChunk.ChunkX = VoxelChunk.Size * relativeX;
                newChunk.ChunkZ = VoxelChunk.Size * relativeZ;
                newChunk.Init();
                _chunks[chunkI] = newChunk;
                newChunk.MarkDirty();
            }
        }
    }

    private void Update()
    {
        UpdateLoadedChunks();
        
        var meshTime = 0.0;
        var chunksMeshed = 0;
        for (var i = 0; i < _chunks.Length; i++)
        {
            if (_chunks[i] && _chunks[i].IsDirty)
            {
                _stopwatch.Restart();
                _chunks[i].Mesh(this, _tesselator);
                _stopwatch.Stop();
                meshTime += _stopwatch.Elapsed.TotalMilliseconds;
                ++chunksMeshed;
                _chunks[i].Swap(_tesselator);
            }
        }

        if (chunksMeshed > 0)
        {
            Debug.Log($"Meshed {chunksMeshed} chunks in {meshTime} with an average of {meshTime / chunksMeshed}");
        }

        if (!Input.GetMouseButtonDown(0)) return;

        foreach (var chunk in _chunks)
        {
            chunk.MarkDirty();
        }
    }

    public byte GetVoxel(int x, int y, int z)
    {
        // if (x < 0 || y < 0 || z < 0 || x >= Size || y >= Height || z >= Size) return 1;

        var chunkX = x >> VoxelChunk.SizeShifts;
        var chunkZ = z >> VoxelChunk.SizeShifts;

        chunkX -= _playerChunkPosition.x - RadiusInChunks;
        chunkZ -= _playerChunkPosition.z - RadiusInChunks;

        if (chunkX < 0 || chunkX >= SizeInChunks || chunkZ < 0 || chunkZ >= SizeInChunks || y < 0 ||
            y >= Height) return 1;
        
        var chunk = _chunks[chunkX + chunkZ * SizeInChunks];

        var localX = x & VoxelChunk.SizeAnd;
        var localZ = z & VoxelChunk.SizeAnd;

        return chunk.GetVoxel(localX, y, localZ);
    }
}