using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Godot;

/// <summary>
/// The chunk generator takes in player position and generates chunks around them. 
/// </summary>
public class ChunkGenerator : Node
{
    public const uint CHUNK_SIZE = 64;
    public const uint CHUNK_HEIGHT = 64;
    private PackedScene chunkResource;
    private RandomNumberGenerator rng = new RandomNumberGenerator();
    private OpenSimplexNoise tNoise, cNoise;
    private HashSet<Tuple<int, int>> generatedChunks;
    private ConcurrentQueue<Tuple<int, int>> chunksToGenerate;
    private ConcurrentQueue<Tuple<int, int>> chunksToRegen;
    private ConcurrentDictionary<Tuple<int, int>, Chunk> chunkMap;

    [Export]
    /// <summary>
    /// The spatial object that chunks get generated around.
    /// </summary>
    public NodePath RenderTargetPath;

    [Export]
    private int chunkRadius = 3;

    public override void _Ready()
    {
        base._Ready();
        chunkResource = GD.Load<PackedScene>("res://levels/Chunk.tscn");
        Initialize(null);

        chunksToGenerate = new ConcurrentQueue<Tuple<int, int>>();
        chunksToRegen = new ConcurrentQueue<Tuple<int, int>>();
        chunkMap = new ConcurrentDictionary<Tuple<int, int>, Chunk>();
        generatedChunks = new HashSet<Tuple<int, int>>();
        initializeChunks();
        System.Threading.Thread generationThread = new System.Threading.Thread(new ThreadStart(generate));
        generationThread.Start();
        
        System.Threading.Thread regenThread = new System.Threading.Thread(new ThreadStart(regenerate));
        regenThread.Start();
    }

    public Tuple<int, int>[] GetChunkAreas()
    {
        Tuple<int, int>[] arr = new Tuple<int, int>[chunkMap.Count];
        chunkMap.Keys.CopyTo(arr, 0);

        return arr;
    }

    public bool ChunkExistsAt(int x, int z)
    {
        return generatedChunks.Contains(new Tuple<int, int>(x, z));
    }

    public bool ChunkExistsAt(float x, float z)
    {
        return generatedChunks.Contains(new Tuple<int, int>((int)(x / CHUNK_SIZE), (int)(z / CHUNK_SIZE)));
    }

    /// <summary>
    /// Run this with another thread to empty the stack of chunks to generate.
    /// </summary>
    private void generate()
    {
        while (true)
        {
            if (chunksToGenerate.TryDequeue(out Tuple<int, int> res))
            {
                autoGenerateChunk(res.Item1, res.Item2);
            }
        }
    }

    private void regenerate()
    {
        while (true)
        {
            if (chunksToRegen.TryDequeue(out Tuple<int, int> res))
            {
                if (chunkMap.ContainsKey(res))
                {
                    chunkMap[res].RegenerateMesh();
                }
            }
        }
    }

    /// <summary>
    /// Loads initial chunks.
    /// </summary>
    private void initializeChunks()
    {
        for (int x = -chunkRadius; x <= chunkRadius; x++)
        {
            for (int y = -chunkRadius; y <= chunkRadius; y++)
            {
                autoGenerateChunk(x, y);
                generatedChunks.Add(new Tuple<int, int>(x, y));
            }
        }
    }

    public override void _Process(float delta)
    {
        // checks positions around the render target for areas without chunks loaded
        Spatial RenderTarget = GetNode<Spatial>(RenderTargetPath);
        DebugText.Instance.ChunksLoaded = generatedChunks.Count;
        DebugText.Instance.ChunksQueued = chunksToGenerate.Count;
        DebugText.Instance.RenderTargetPosition = RenderTarget.Transform.origin;
        int centerX = (int)(RenderTarget.GlobalTransform.origin.x / CHUNK_SIZE);
        int centerZ = (int)(RenderTarget.GlobalTransform.origin.z / CHUNK_SIZE);

        for (int x = centerX - chunkRadius; x <= centerX + chunkRadius; x++)
        {
            for (int z = centerZ - chunkRadius; z <= centerZ + chunkRadius; z++)
            {
                Tuple<int, int> chunkEntry = new Tuple<int, int>(x, z);
                if (generatedChunks.Contains(chunkEntry))
                {
                    continue;
                }
                else
                {
                    generatedChunks.Add(chunkEntry);
                    chunksToGenerate.Enqueue(chunkEntry);
                }
            }
        }
    }

    public Chunk GetChunkAt(int x, int z)
    {
        Tuple<int, int> key = new Tuple<int, int>(x, z);
        if (chunkMap.ContainsKey(key))
            return chunkMap[key];

        return null;
    }

    /// <summary>
    /// Generate the chunk with the given chunk offset.
    /// </summary>
    /// <param name="chunkX"></param>
    /// <param name="chunkZ"></param>
    private void autoGenerateChunk(int chunkX, int chunkZ)
    {
        uint[][][] data = new uint[CHUNK_HEIGHT][][];
        for (uint y = 0; y < data.Length; y++)
        {
            data[y] = new uint[CHUNK_SIZE][];
            for (uint x = 0; x < CHUNK_SIZE; x++)
            {
                data[y][x] = new uint[CHUNK_SIZE];
            }
        }
        
        float div = 2f;
        for (uint x = 0; x < CHUNK_SIZE; x++)
        {
            for (uint z = 0; z < CHUNK_SIZE; z++)
            {
                float rX = x + chunkX * CHUNK_SIZE;
                float rZ = z + chunkZ * CHUNK_SIZE;
                uint val = (uint)((CHUNK_HEIGHT) * (tNoise.GetNoise2d(rX / div, rZ / div) + 1.0f) / 2.0f);
                for (uint y = 0; y < CHUNK_HEIGHT; y++)
                {
                    if (y == 0)
                    {
                        data[y][x][z] = 3;
                        continue;
                    }
                    float cave = (cNoise.GetNoise3d(rX / div, y / div, rZ / div) + 1.0f) / 2.0f;
                    if (cave < 0.35f)
                    {
                        data[y][x][z] = 0;
                        continue;
                    }
                    if (y > val)
                    {
                        data[y][x][z] = 0;
                        continue;
                    }
                    if (y < val)
                    {
                        if (y < val - 3)
                        {
                            data[y][x][z] = 3;
                        }
                        else
                        {
                            data[y][x][z] = 2;
                        }
                    }
                    if (y == val || y == CHUNK_HEIGHT - 1 || data[y + 1][x][z] == 0)
                    {
                        data[y][x][z] = 1;
                    }
                }
            }
        }
        Chunk chunk = chunkResource.Instance<Chunk>();
        chunk.SetBlockData(ref data, new Vector3(chunkX * CHUNK_SIZE, -CHUNK_HEIGHT, chunkZ * CHUNK_SIZE), this, chunkX, chunkZ);
        chunk.GenerateMesh();
        CallDeferred("add_child", chunk);
        //AddChild(chunk);
        chunkMap.AddOrUpdate(new Tuple<int, int>(chunkX, chunkZ), chunk, (key, value) => value = chunk);
        // update chunks around this chunk
        chunksToRegen.Enqueue(new Tuple<int, int>(chunkX - 1, chunkZ));
        chunksToRegen.Enqueue(new Tuple<int, int>(chunkX + 1, chunkZ));
        chunksToRegen.Enqueue(new Tuple<int, int>(chunkX, chunkZ + 1));
        chunksToRegen.Enqueue(new Tuple<int, int>(chunkX, chunkZ - 1));

        #region Single Threaded Regeneration
        // Chunk cl = GetChunkAt(chunkX - 1, chunkZ);
        // Chunk cr = GetChunkAt(chunkX + 1, chunkZ);
        // Chunk cu = GetChunkAt(chunkX, chunkZ + 1);
        // Chunk cd = GetChunkAt(chunkX, chunkZ - 1);
        // cl?.RegenerateMesh();
        // cr?.RegenerateMesh();
        // cu?.RegenerateMesh();
        // cd?.RegenerateMesh();
        #endregion
    }

    /// <summary>
    /// Initialize the noise generators for this generator.
    /// </summary>
    /// <param name="terrainNoise"></param>
    /// <param name="caveNoise"></param>
    public void Initialize(OpenSimplexNoise terrainNoise = null, OpenSimplexNoise caveNoise = null)
    {
        if (terrainNoise == null)
        {
            tNoise = new OpenSimplexNoise();
            tNoise.Seed = (int)rng.RandiRange(0, int.MaxValue);
            terrainNoise = tNoise;
        }
        else
        {
            tNoise = terrainNoise;
        }

        if (caveNoise == null)
        {
            cNoise = new OpenSimplexNoise();
            cNoise.Seed = (int)rng.RandiRange(0, int.MaxValue);
            caveNoise = cNoise;
        }
        else
        {
            cNoise = caveNoise;
        }
    }
}