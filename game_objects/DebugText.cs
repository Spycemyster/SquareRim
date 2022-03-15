using Godot;
using System;

public class DebugText : Control
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";
    public static DebugText Instance;
    public int ChunksLoaded;
    public int ChunksQueued;
    private Label textLabel;
    public Vector3 RenderTargetPosition;
    private const int BYTES_TO_MEGABYTES = 1024 * 1024;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        textLabel = GetNode<Label>("DebugText");
        Instance = this;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        float totalMemory = (Performance.GetMonitor(Performance.Monitor.MemoryDynamic) 
        + Performance.GetMonitor(Performance.Monitor.MemoryStatic)) / BYTES_TO_MEGABYTES;
        float maxMemory = (Performance.GetMonitor(Performance.Monitor.MemoryStaticMax)
        + Performance.GetMonitor(Performance.Monitor.MemoryDynamicMax)) / BYTES_TO_MEGABYTES;
        textLabel.Text = $"RAM: {totalMemory}/{maxMemory} MB\nFPS {Engine.GetFramesPerSecond()}\n" +
            $"Chunks Loaded: {ChunksLoaded}\nChunks Queued: {ChunksQueued}\nPosition: {RenderTargetPosition.x}, {RenderTargetPosition.y}, {RenderTargetPosition.z}";
    }
}
