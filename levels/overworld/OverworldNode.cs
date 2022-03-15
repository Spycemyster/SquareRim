using Godot;
using System;

public class OverworldNode : Node
{
    bool isPaused = false;
    private Timer mSpawnTimer;
    private RandomNumberGenerator rng;
    private ChunkGenerator mChunkGenerator;
    private int mNumSpawned = 0;
    [Export]
    private int mMaxSpawned = 10;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        rng = new RandomNumberGenerator();
        rng.Randomize();
        Input.SetMouseMode(Input.MouseMode.Captured);
        mSpawnTimer = GetNode<Timer>("SpawnTimer");
        mSpawnTimer.Connect("timeout", this, "spawnCreature");
        mChunkGenerator = GetNode<ChunkGenerator>("ChunkGenerator");
    }

    private void spawnCreature()
    {
        if (mNumSpawned >= mMaxSpawned)
            return;
        Tuple<int, int>[] areas = mChunkGenerator.GetChunkAreas();
        Creature c = GD.Load<PackedScene>("res://game_objects/Creature.tscn").Instance<Creature>();
        Tuple<int, int> key = areas[rng.RandiRange(0, areas.Length - 1)];
        Vector3 trans = new Vector3(key.Item1 * ChunkGenerator.CHUNK_SIZE + rng.Randf() * ChunkGenerator.CHUNK_SIZE,
            0, key.Item2 * ChunkGenerator.CHUNK_SIZE + rng.Randf() * ChunkGenerator.CHUNK_SIZE);
        c.Translate(trans);
        AddChild(c);
        mSpawnTimer.Start(2);
        mNumSpawned++;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (Input.IsActionJustPressed("pause"))
        {
            isPaused = !isPaused;
            Input.SetMouseMode((isPaused) ? Input.MouseMode.Visible : Input.MouseMode.Captured);
            
        }
    }
}
