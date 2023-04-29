using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class VoxelWorld : MonoBehaviour
{
    public const int SizeInChunks = 40;
    private const int RadiusInChunks = SizeInChunks / 2;
    private const int Size = SizeInChunks * VoxelChunk.Size;
    private const int Height = VoxelChunk.Height;
    private const int ChunkCount = SizeInChunks * SizeInChunks;

    [SerializeField] private Transform player;
    [SerializeField] private GameObject chunkPrefab;

    private Vector3Int _playerChunkPosition;

    private readonly VoxelChunk[] _oldChunks = new VoxelChunk[ChunkCount];
    private readonly VoxelChunk[] _chunks = new VoxelChunk[ChunkCount];

    private readonly List<VoxelChunk> _nextMeshingJobs = new();
    private readonly ConcurrentQueue<VoxelChunk> _meshingJobs = new();
    private readonly ConcurrentQueue<VoxelChunk> _swappingJobs = new();

    private Thread _meshingThread;

    private void Start()
    {
        player.transform.position = new Vector3(Size * 0.5f, 0, Size * 0.5f);
        UpdateLoadedChunks();

        _meshingThread = new Thread(MeshingProc);
        _meshingThread.Start();
    }

    private void MeshingProc()
    {
        while (true)
        {
            while (_meshingJobs.TryPeek(out var chunk))
            {
                chunk.Mesh(this);
                _swappingJobs.Enqueue(chunk);

                _meshingJobs.TryDequeue(out _);
            }
        }
    }

    private void UpdatePlayerChunkPosition()
    {
        var playerPosition = player.transform.position;
        _playerChunkPosition = new Vector3Int((int)playerPosition.x / VoxelChunk.Size,
            (int)playerPosition.y / VoxelChunk.Height, (int)playerPosition.z / VoxelChunk.Size);
    }

    // Checks if the chunk borders a chunk that is about to be loaded.
    // Used when updating loaded chunks.
    private bool BordersNewChunk(int x, int z)
    {
        if (x < 0 || x >= SizeInChunks || z < 0 || z >= SizeInChunks) return false;

        return !_chunks[x + z * SizeInChunks];
    }

    private void UpdateLoadedChunks()
    {
        if (!_meshingJobs.IsEmpty || !_swappingJobs.IsEmpty) return;
        
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

        // Re-mesh old chunks that will border new chunks.
        for (var z = 0; z < SizeInChunks; ++z)
        {
            for (var x = 0; x < SizeInChunks; ++x)
            {
                var chunkI = x + z * SizeInChunks;
                
                if (!_chunks[chunkI]) continue;
                
                if (BordersNewChunk(x + 1, z) || BordersNewChunk(x - 1, z) ||
                    BordersNewChunk(x, z + 1) || BordersNewChunk(x, z - 1))
                {
                    _nextMeshingJobs.Add(_chunks[chunkI]);
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
                _nextMeshingJobs.Add(newChunk);
            }
        }
        
        var playerX = _playerChunkPosition.x * VoxelChunk.Size;
        var playerZ = _playerChunkPosition.z * VoxelChunk.Size;
        
        // Make sure that nearest chunks are meshed first.
        _nextMeshingJobs.Sort((a, b) =>
        {
            var aDistanceX = a.ChunkX - playerX;
            var aDistanceZ = a.ChunkZ - playerZ;
            var aDistanceSquared = aDistanceX * aDistanceX + aDistanceZ * aDistanceZ;
            var bDistanceX = b.ChunkX - playerX;
            var bDistanceZ = b.ChunkZ - playerZ;
            var bDistanceSquared = bDistanceX * bDistanceX + bDistanceZ * bDistanceZ;

            return aDistanceSquared.CompareTo(bDistanceSquared);
        });

        foreach (var nextJob in _nextMeshingJobs)
        {
            _meshingJobs.Enqueue(nextJob);
        }
        
        _nextMeshingJobs.Clear();
    }

    private void Update()
    {
        UpdateLoadedChunks();
        
        while (_swappingJobs.TryPeek(out var chunk))
        {
            chunk.Swap();
            
            _swappingJobs.TryDequeue(out _);
        }
    }

    public byte GetVoxel(int x, int y, int z)
    {
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