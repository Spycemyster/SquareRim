using System.Collections.Generic;
using Godot;

/// <summary>
/// A piece of terrain within our world that holds data and meshes for cubes.
/// </summary>
public class Chunk : Node
{
    /// <summary>
    /// A table of various points on a cube
    /// </summary>
    /// <value></value>
    private readonly static Vector3[] CUBE_CORNERS = new Vector3[8] {
        new Vector3(0, 0, 0), // 0
        new Vector3(1, 0, 0), // 1
        new Vector3(0, 0, 1), // 2
        new Vector3(1, 0, 1), // 3
        new Vector3(0, 1, 0), // 4
        new Vector3(1, 1, 0), // 5
        new Vector3(0, 1, 1), // 6
        new Vector3(1, 1, 1)  // 7
    };
    private List<Vector3> vertices;
    private List<Vector2> uvs;
    [Export]
    private SpatialMaterial material;
    [Export]
    private Color color;
    private ArrayMesh mesh;
    private uint[][][] data;

    private const uint BLOCK_TEXTURE_NUM = 5;
    private ChunkGenerator chunkGenerator;

    /// <summary>
    /// The number of pixels for each square face
    /// </summary>
    private const uint FACE_SIZE = 16;
    private int chunkX, chunkZ;
    private Vector3 worldOffset;

    public Chunk()
    {
        vertices = new List<Vector3>();
        uvs = new List<Vector2>();
        material = new SpatialMaterial();
        color = new Color(0.9f, 0.1f, 0.1f, 1f);
    }

    /// <summary>
    /// Clears the uvs and vertice data. Call GenerateMesh() afterwards to remove the chunk mesh.
    /// </summary>
    public void ClearBlocks()
    {
        uvs.Clear();
        vertices.Clear();
    }

    /// <summary>
    /// Set the chunk data based with a 3D matrix and an offset. Block data is accessed through
    /// index coordinates of [y][x][z].
    /// </summary>
    /// <param name="data"></param>
    /// <param name="worldOffset"></param>
    public void SetBlockData(ref uint[][][] data, Vector3 worldOffset, ChunkGenerator generator, int chunkX = 0, int chunkZ = 0)
    {
        chunkGenerator = generator;
        this.data = data;
        this.worldOffset = worldOffset;
        this.chunkX = chunkX;
        this.chunkZ = chunkZ;
    }

    /// <summary>
    /// Checks if the block at the position (x, y, z) is transparent.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    private bool isTransparent(ref uint[][][] data, int x, int y, int z, int chunkX, int chunkZ)
    {
        if (y < 0 || y >= data.Length)
        {
            return true;
        }

        if (x < 0)
        {
            Chunk chunk = chunkGenerator.GetChunkAt(chunkX - 1, chunkZ);
            return chunk == null || chunk.data[y][ChunkGenerator.CHUNK_SIZE - 1][z] == 0;
        }
        else if (x >= data[y].Length)
        {
            Chunk chunk = chunkGenerator.GetChunkAt(chunkX + 1, chunkZ);
            return chunk == null || chunk.data[y][0][z] == 0;
        }
        
        if (z < 0)
        {
            Chunk chunk = chunkGenerator.GetChunkAt(chunkX, chunkZ - 1);
            if (chunk == null)
            {
                return true;
            }
            else
            {
                return chunk.data[y][x][ChunkGenerator.CHUNK_SIZE - 1] == 0;
            }
        }
        else if (z >= data[y][x].Length)
        {
            Chunk chunk = chunkGenerator.GetChunkAt(chunkX, chunkZ + 1);
            if (chunk == null)
            {
                return true;
            }
            else
            {
                return chunk.data[y][x][0] == 0;
            }
        }
        return y >= 0 && x >= 0 && z >= 0 && y < data.Length && x < data[y].Length && z < data[y][x].Length && data[y][x][z] == 0;
    }

    public void ClearMesh()
    {
        ClearBlocks();
        GD.Print($"Setting empty mesh to chunk {chunkX},{chunkZ}");
        ArrayMesh mesh = new ArrayMesh();
        GetNode<MeshInstance>("MeshInstance").Mesh = mesh;
    }

    public void RegenerateMesh()
    {
        GenerateMesh();
    }

    /// <summary>
    /// Takes the current vertice/uv data and generate a mesh with it.
    /// </summary>
    public void GenerateMesh()
    {
        // data is a 3 dimensional array
        // data[i] represents a 2d horizontal plane at level i
        // data[i][j] represents a 1d line of blocks at level i and row j
        // data[i][j][k] represents a point at level i row j and column k
        for (int y = 0; y < data.Length; y++)
        {
            for (int x = 0; x < data[y].Length; x++)
            {
                for (int z = 0; z < data[y][x].Length; z++)
                {
                    Vector3 position = new Vector3(x, y, z) + worldOffset;
                    uint id = data[y][x][z];
                    
                    if (id >= BLOCK_TEXTURE_NUM)
                        GD.PushWarning($"No texture of ID {id} present on texture map");

                    // air block, don't do anything
                    if (id == 0)
                        continue;

                    #region Create a block for each position regardless of face
                    // AddBotFace(position, id);
                    // AddFrontFace(position, id);
                    // AddBackFace(position, id);
                    // AddTopFace(position, id);
                    // AddLeftFace(position, id);
                    // AddRightFace(position, id);
                    // continue;
                    #endregion

                    // add a bot face if the position at y - 1 is transparent
                    if (isTransparent(ref data, x, y - 1, z, chunkX, chunkZ))
                        AddBotFace(position, id);

                    // add a front face if the position at x + 1 is transparent
                    if (isTransparent(ref data, x + 1, y, z, chunkX, chunkZ))
                        AddFrontFace(position, id);

                    // add a left face if the position z - 1 is transparent
                    if (isTransparent(ref data, x, y, z - 1, chunkX, chunkZ))
                        AddLeftFace(position, id);

                    // add a right face if the position at z + 1 is transparent
                    if (isTransparent(ref data, x, y, z + 1, chunkX, chunkZ))
                        AddRightFace(position, id);

                    // add a top face if the position at y + 1 is transparent
                    if (isTransparent(ref data, x, y + 1, z, chunkX, chunkZ))
                        AddTopFace(position, id);

                    // add a back face if the position at x - 1 is transparent
                    if (isTransparent(ref data, x - 1, y, z, chunkX, chunkZ))
                        AddBackFace(position, id);
                }
            }
        }
        SurfaceTool st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        st.SetMaterial(material);

        for (int i = 0; i < vertices.Count; i++)
        {
            st.AddColor(color);
            st.AddUv(uvs[i]);
            st.AddVertex(vertices[i]);
        }

        ArrayMesh mesh = new ArrayMesh();
        mesh = st.Commit();

        MeshInstance mi = GetNode<MeshInstance>("MeshInstance");
        if (mi.Mesh is ArrayMesh arrayMesh)
        {
            arrayMesh.ClearSurfaces();
        }
        mi.Mesh = mesh;

        CollisionShape cs = GetNode<CollisionShape>("CollisionShape");
        cs.Shape = mesh.CreateTrimeshShape();

        ClearBlocks();
    }

    // texture mapping notes
    // - Texture comprises of a vertically expanding image and 6 faces that expand horizontally
    // - 16x16 pixels for each face, but we will abstract to FACE_SIZE instead
    // - Order will be as follows: Top, Bot, Right, Forward, Left, Backwards
    // - S_(u, v) -> u = 1 / 6.0f * index, v = 1 / BLOCK_NUM * BlockId
    // - E_(u, v) -> S_u + 1/6.0f, S_v + 1 / BLOCK_NUM;
    // - UV = S_(u, v) + uv_x * <1 / 6.0f, 0> + uv_y * <0, 1 / BLOCK_NUM * BlockId>
    
    private void AddBotFace(Vector3 offset, uint id = 0)
    {
        // TODO: These constants can probably be calculated at a higher stack
        Vector2 uvStart = new Vector2(1/6.0f, 1.0f / BLOCK_TEXTURE_NUM * (float)id);
        float dx = 1 / 6.0f;
        float dy = 1.0f / BLOCK_TEXTURE_NUM;

        // -y face (bottom)
        vertices.Add(CUBE_CORNERS[0] + offset);
        vertices.Add(CUBE_CORNERS[2] + offset);
        vertices.Add(CUBE_CORNERS[1] + offset);
        vertices.Add(CUBE_CORNERS[2] + offset);
        vertices.Add(CUBE_CORNERS[3] + offset);
        vertices.Add(CUBE_CORNERS[1] + offset);

        uvs.Add(uvStart + new Vector2(0, 0));
        uvs.Add(uvStart + new Vector2(dx, 0));
        uvs.Add(uvStart + new Vector2(0, dy));
        uvs.Add(uvStart + new Vector2(dx, 0));
        uvs.Add(uvStart + new Vector2(dx, dy));
        uvs.Add(uvStart + new Vector2(0, dy));
    }

    private void AddTopFace(Vector3 offset, uint id = 0)
    {
        // TODO: These constants can probably be calculated at a higher stack
        Vector2 uvStart = new Vector2(0, 1.0f / BLOCK_TEXTURE_NUM * (float)id);
        float dx = 1 / 6.0f;
        float dy = 1.0f / BLOCK_TEXTURE_NUM;
        
        // +y face (top)
        vertices.Add(CUBE_CORNERS[5] + offset);
        vertices.Add(CUBE_CORNERS[6] + offset);
        vertices.Add(CUBE_CORNERS[4] + offset);
        vertices.Add(CUBE_CORNERS[5] + offset);
        vertices.Add(CUBE_CORNERS[7] + offset);
        vertices.Add(CUBE_CORNERS[6] + offset);

        uvs.Add(uvStart + new Vector2(0, dy));
        uvs.Add(uvStart + new Vector2(dx, 0));
        uvs.Add(uvStart + new Vector2(0, 0));
        uvs.Add(uvStart + new Vector2(0, dy));
        uvs.Add(uvStart + new Vector2(dx, dy));
        uvs.Add(uvStart + new Vector2(dx, 0));
    }

    private void AddFrontFace(Vector3 offset, uint id = 0)
    {
        // TODO: These constants can probably be calculated at a higher stack
        Vector2 uvStart = new Vector2(1/6.0f * 3, 1.0f / BLOCK_TEXTURE_NUM * (float)id);
        float dx = 1 / 6.0f;
        float dy = 1.0f / BLOCK_TEXTURE_NUM;

        // +x face (forward)
        vertices.Add(CUBE_CORNERS[7] + offset);
        vertices.Add(CUBE_CORNERS[5] + offset);
        vertices.Add(CUBE_CORNERS[1] + offset);
        vertices.Add(CUBE_CORNERS[1] + offset);
        vertices.Add(CUBE_CORNERS[3] + offset);
        vertices.Add(CUBE_CORNERS[7] + offset);


        uvs.Add(uvStart + new Vector2(0, 0));
        uvs.Add(uvStart + new Vector2(dx, 0));
        uvs.Add(uvStart + new Vector2(dx, dy));
        uvs.Add(uvStart + new Vector2(dx, dy));
        uvs.Add(uvStart + new Vector2(0, dy));
        uvs.Add(uvStart + new Vector2(0, 0));
    }

    private void AddBackFace(Vector3 offset, uint id = 0)
    {
        // TODO: These constants can probably be calculated at a higher stack
        Vector2 uvStart = new Vector2(1/6.0f * 5, 1.0f / BLOCK_TEXTURE_NUM * (float)id);
        float dx = 1 / 6.0f;
        float dy = 1.0f / BLOCK_TEXTURE_NUM;
        
        // -x face (backward)
        vertices.Add(CUBE_CORNERS[4] + offset);
        vertices.Add(CUBE_CORNERS[6] + offset);
        vertices.Add(CUBE_CORNERS[2] + offset);
        vertices.Add(CUBE_CORNERS[2] + offset);
        vertices.Add(CUBE_CORNERS[0] + offset);
        vertices.Add(CUBE_CORNERS[4] + offset);

        uvs.Add(uvStart + new Vector2(0, 0));
        uvs.Add(uvStart + new Vector2(dx, 0));
        uvs.Add(uvStart + new Vector2(dx, dy));
        uvs.Add(uvStart + new Vector2(dx, dy));
        uvs.Add(uvStart + new Vector2(0, dy));
        uvs.Add(uvStart + new Vector2(0, 0));
    }

    private void AddLeftFace(Vector3 offset, uint id = 0)
    {
        // TODO: These constants can probably be calculated at a higher stack
        Vector2 uvStart = new Vector2(1/6.0f * 4, 1.0f / BLOCK_TEXTURE_NUM * (float)id);
        float dx = 1 / 6.0f;
        float dy = 1.0f / BLOCK_TEXTURE_NUM;

        // -z face (left)
        vertices.Add(CUBE_CORNERS[5] + offset);
        vertices.Add(CUBE_CORNERS[4] + offset);
        vertices.Add(CUBE_CORNERS[0] + offset);
        vertices.Add(CUBE_CORNERS[0] + offset);
        vertices.Add(CUBE_CORNERS[1] + offset);
        vertices.Add(CUBE_CORNERS[5] + offset);

        uvs.Add(uvStart + new Vector2(0, 0));
        uvs.Add(uvStart + new Vector2(dx, 0));
        uvs.Add(uvStart + new Vector2(dx, dy));
        uvs.Add(uvStart + new Vector2(dx, dy));
        uvs.Add(uvStart + new Vector2(0, dy));
        uvs.Add(uvStart + new Vector2(0, 0));
    }

    private void AddRightFace(Vector3 offset, uint id = 0)
    {
        // TODO: These constants can probably be calculated at a higher stack
        Vector2 uvStart = new Vector2(1/6.0f * 2, 1.0f / BLOCK_TEXTURE_NUM * (float)id);
        float dx = 1 / 6.0f;
        float dy = 1.0f / BLOCK_TEXTURE_NUM;

        // +z face (right)
        vertices.Add(CUBE_CORNERS[2] + offset);
        vertices.Add(CUBE_CORNERS[6] + offset);
        vertices.Add(CUBE_CORNERS[7] + offset);
        vertices.Add(CUBE_CORNERS[7] + offset);
        vertices.Add(CUBE_CORNERS[3] + offset);
        vertices.Add(CUBE_CORNERS[2] + offset);
        
        uvs.Add(uvStart + new Vector2(0, dy));
        uvs.Add(uvStart + new Vector2(0, 0));
        uvs.Add(uvStart + new Vector2(dx, 0));
        uvs.Add(uvStart + new Vector2(dx, 0));
        uvs.Add(uvStart + new Vector2(dx, dy));
        uvs.Add(uvStart + new Vector2(0, dy));
    }
}