using Godot;
using System;

public class PlaygroundScene : Spatial
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";
    private bool mIsPaused;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Input.SetMouseMode(Input.MouseMode.Captured);
        mIsPaused = false;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (Input.IsActionJustPressed("pause"))
        {
            mIsPaused = !mIsPaused;
            Input.SetMouseMode((mIsPaused) ? Input.MouseMode.Visible : Input.MouseMode.Captured);
            
        }
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
