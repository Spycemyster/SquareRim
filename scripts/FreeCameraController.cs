using Godot;
using System;
using System.Collections.Generic;

public class FreeCameraController : KinematicBody
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";
    private Camera camera;

    [Export]
    private float lookSensitivity = 0.7f;

    [Export]
    private NodePath chunkGeneratorPath_;

    private float minLookAngle = -89.9f;
    private float maxLookAngle = 89.9f;
    private float moveSpeed = 12f;
    private RayCast rayCast_;
    private ChunkGenerator chunkGenerator_;
    private MeshInstance blockOutline_, handBlock_;
    private uint id_;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        id_ = 1;
        camera = GetNode<Camera>("Camera");
        rayCast_ = camera.GetNode<RayCast>("RayCast");
        chunkGenerator_ = GetNode<ChunkGenerator>(chunkGeneratorPath_);
        blockOutline_ = GetNode<MeshInstance>("BlockOutline");
        handBlock_ = GetNode<MeshInstance>("Hand/MeshInstance");
        changeHand(id_);
    }

    private void changeHand(uint id)
    {
        if (id == 0)
        {
            id = Chunk.BLOCK_TEXTURE_NUM - 1;
        }
        else if (id >= Chunk.BLOCK_TEXTURE_NUM)
        {
            id = 1;
        }
        GD.Print($"ID changed to {id}");
        id_ = id;
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        Chunk.AddBackFace(vertices, uvs, Vector3.Zero, id);
        Chunk.AddFrontFace(vertices, uvs, Vector3.Zero, id);
        Chunk.AddLeftFace(vertices, uvs, Vector3.Zero, id);
        Chunk.AddRightFace(vertices, uvs, Vector3.Zero, id);
        Chunk.AddTopFace(vertices, uvs, Vector3.Zero, id);
        Chunk.AddBotFace(vertices, uvs, Vector3.Zero, id);
        SurfaceTool st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        st.SetMaterial((SpatialMaterial)handBlock_.MaterialOverride);

        for (int i = 0; i < vertices.Count; i++)
        {
            st.AddColor(new Color(1, 1, 1, 1));
            st.AddUv(uvs[i]);
            st.AddVertex(vertices[i]);
        }

        ArrayMesh mesh = new ArrayMesh();
        mesh = st.Commit();

        if (handBlock_.Mesh is ArrayMesh arrayMesh)
        {
            arrayMesh.ClearSurfaces();
        }
        handBlock_.Mesh = mesh;
    }

    public override void _PhysicsProcess(float delta)
    {
        Vector3 right = new Vector3(GlobalTransform.basis.x.x, 0, GlobalTransform.basis.x.z).Normalized();
        Vector3 forward = new Vector3(GlobalTransform.basis.z.x, 0, GlobalTransform.basis.z.z).Normalized();
        Vector3 up = GlobalTransform.basis.y;

        Vector3 input = GetInput();

        CheckBlockChange();

        if (Input.IsActionJustPressed("scroll_up"))
        {
            changeHand(id_ + 1);
        }
        else if (Input.IsActionJustPressed("scroll_down"))
        {
            changeHand(id_ - 1);
        }

        bool isSprinting = Input.IsActionPressed("sprint");

        Vector3 relativeDirection = forward * input.x + right * input.z + Vector3.Up * input.y;
        MoveAndSlide(relativeDirection * moveSpeed * (isSprinting ? 5 : 1));

        if (Input.IsActionJustPressed("save_world"))
        {
            GD.Print($"Dumping all chunk json files to the world {chunkGenerator_.WorldName}");
            chunkGenerator_.DumpWorld();
        }
    }

    private Vector3 GetInput()
    {
        Vector3 input = Vector3.Zero;
        if (Input.IsActionPressed("move_forward"))
        {
            input.x -= 1;
        }
        else if (Input.IsActionPressed("move_backward"))
        {
            input.x += 1;
        }

        if (Input.IsActionPressed("move_left"))
        {
            input.z -= 1;
        }
        else if (Input.IsActionPressed("move_right"))
        {
            input.z += 1;
        }

        if (Input.IsActionPressed("jump"))
        {
            input.y += 1;
        }
        else if (Input.IsActionPressed("crouch"))
        {
            input.y -= 1;
        }

        return input;
    }

    private void CheckBlockChange()
    {
        if (rayCast_.IsColliding())
        {
            blockOutline_.Visible = true;
            Vector3 cp = rayCast_.GetCollisionPoint();
            Vector3 cpNormal = rayCast_.GetCollisionNormal();
            Vector3 toPlacePosition = cp + cpNormal * 0.1f;
            toPlacePosition.x = Mathf.Floor(toPlacePosition.x + 0.5f);
            toPlacePosition.y = Mathf.Floor(toPlacePosition.y + 0.5f);
            toPlacePosition.z = Mathf.Floor(toPlacePosition.z + 0.5f);
            Vector3 toRemovePosition = cp - cpNormal * 0.1f;
            toRemovePosition.x = Mathf.Floor(toRemovePosition.x + 0.5f);
            toRemovePosition.y = Mathf.Floor(toRemovePosition.y + 0.5f);
            toRemovePosition.z = Mathf.Floor(toRemovePosition.z + 0.5f);

            // place currently equipped block at where we are looking at
            if (Input.IsActionJustPressed("second_attack"))
            {
                // get the collision point somewhere above the surface we collided at
                int x = positiveModulus((int)toPlacePosition.x, (int)ChunkGenerator.CHUNK_SIZE);
                int y = positiveModulus((int)toPlacePosition.y, (int)ChunkGenerator.CHUNK_HEIGHT);
                int z = positiveModulus((int)toPlacePosition.z, (int)ChunkGenerator.CHUNK_SIZE);
                Tuple<int, int> chunkKey = worldToChunk(toPlacePosition);
                Chunk c = chunkGenerator_.GetChunkAt(chunkKey.Item1, chunkKey.Item2);
                c.ChangeBlockAt(x, y, z, id_);
                
                c.RegenerateMesh();
                // if (x == 0)
                // {
                //     Chunk regen = chunkGenerator_.GetChunkAt(chunkKey.Item1 - 1, chunkKey.Item2);
                //     regen?.RegenerateMesh();
                // }
                // else if (x == ChunkGenerator.CHUNK_SIZE - 1)
                // {
                //     Chunk regen = chunkGenerator_.GetChunkAt(chunkKey.Item1 + 1, chunkKey.Item2);
                //     regen?.RegenerateMesh();
                // }
                
                // if (z == 0)
                // {
                //     Chunk regen = chunkGenerator_.GetChunkAt(chunkKey.Item1, chunkKey.Item2 - 1);
                //     regen?.RegenerateMesh();
                // }
                // else if (z == ChunkGenerator.CHUNK_SIZE - 1)
                // {
                //     Chunk regen = chunkGenerator_.GetChunkAt(chunkKey.Item1, chunkKey.Item2 + 1);
                //     regen?.RegenerateMesh();
                // }
            }
            // remove currently looked at block
            else if (Input.IsActionJustPressed("attack"))
            {
                int x = positiveModulus((int)toRemovePosition.x, (int)ChunkGenerator.CHUNK_SIZE);
                int y = positiveModulus((int)toRemovePosition.y, (int)ChunkGenerator.CHUNK_HEIGHT);
                int z = positiveModulus((int)toRemovePosition.z, (int)ChunkGenerator.CHUNK_SIZE);
                Tuple<int, int> chunkKey = worldToChunk(cp);
                Chunk c = chunkGenerator_.GetChunkAt(chunkKey.Item1, chunkKey.Item2);
                if (c != null)
                {
                    c.ChangeBlockAt(x, y, z, 0);
                    c.RegenerateMesh();
                }
                if (x == 0)
                {
                    Chunk regen = chunkGenerator_.GetChunkAt(chunkKey.Item1 - 1, chunkKey.Item2);
                    regen?.RegenerateMesh();
                }
                else if (x == ChunkGenerator.CHUNK_SIZE - 1)
                {
                    Chunk regen = chunkGenerator_.GetChunkAt(chunkKey.Item1 + 1, chunkKey.Item2);
                    regen?.RegenerateMesh();
                }
                
                if (z == 0)
                {
                    Chunk regen = chunkGenerator_.GetChunkAt(chunkKey.Item1, chunkKey.Item2 - 1);
                    regen?.RegenerateMesh();
                }
                else if (z == ChunkGenerator.CHUNK_SIZE - 1)
                {
                    Chunk regen = chunkGenerator_.GetChunkAt(chunkKey.Item1, chunkKey.Item2 + 1);
                    regen?.RegenerateMesh();
                }
            }
            // set the current id to the chunk
            else if (Input.IsActionJustPressed("special"))
            {
                int x = positiveModulus((int)toRemovePosition.x, (int)ChunkGenerator.CHUNK_SIZE);
                int y = positiveModulus((int)toRemovePosition.y, (int)ChunkGenerator.CHUNK_HEIGHT);
                int z = positiveModulus((int)toRemovePosition.z, (int)ChunkGenerator.CHUNK_SIZE);
                Tuple<int, int> chunkKey = worldToChunk(cp);
                Chunk c = chunkGenerator_.GetChunkAt(chunkKey.Item1, chunkKey.Item2);
                changeHand(c.GetBlockAt(x, y, z));
            }
            Transform tr = blockOutline_.GlobalTransform;
            tr.origin = new Vector3(toRemovePosition.x, toRemovePosition.y, toRemovePosition.z);
            tr.basis.Column0 = new Vector3(1,0,0);
            tr.basis.Column1 = new Vector3(0,1,0);
            tr.basis.Column2 = new Vector3(0,0,1);
            
            blockOutline_.GlobalTransform = tr;
        }
        else
        {
            blockOutline_.Visible = false;
        }
    }

    private int positiveModulus(int x, int m)
    {
        return (x % m + m) % m;
    }

    private Tuple<int, int> worldToChunk(Vector3 position)
    {
        return new Tuple<int, int>((int)(Mathf.Floor(position.x / ChunkGenerator.CHUNK_SIZE)), (int)(Mathf.Floor(position.z / ChunkGenerator.CHUNK_SIZE)));
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            Vector3 cameraRotation = RotationDegrees;
            cameraRotation.x -= mouseMotion.Relative.y * lookSensitivity;
            cameraRotation.x = Mathf.Clamp(cameraRotation.x, minLookAngle, maxLookAngle);
            cameraRotation.y -= mouseMotion.Relative.x * lookSensitivity;
            RotationDegrees = cameraRotation;
        }
    }


}
