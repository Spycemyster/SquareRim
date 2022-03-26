using Godot;
using System;

public class Bouncer : KinematicBody, IInteractable
{
    public bool Interact(Player player)
    {
        return true;
    }

    public string GetInteractMessage()
    {
        return "Interact with Bouncer";
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }
}
