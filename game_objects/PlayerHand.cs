using Godot;
using System;
using System.Collections.Generic;

public class PlayerHand : Spatial
{
    private AnimationPlayer mAnimation;
    private Area mSwordArea;
    private bool mCanAttack;
    private HashSet<Node> mHitNodes = new HashSet<Node>();

    [Export]
    private NodePath mCamera;
    public override void _Ready()
    {
        mCanAttack = true;
        mAnimation = GetNode<AnimationPlayer>("HandAnimation");
        mAnimation.Connect("animation_finished", this, "resetAttack");
        mSwordArea = GetNode<Area>("SwordArea");
        mSwordArea.Monitoring = false;
        mSwordArea.Connect("body_entered", this, "swordCollide");
    }

    public override void _Process(float delta)
    {
        Vector3 rotation = GetParent<Spatial>().RotationDegrees;
        rotation.x = GetNode<Camera>(mCamera).RotationDegrees.x;
        rotation.z = GetNode<Camera>(mCamera).RotationDegrees.z;
        GetParent<Spatial>().RotationDegrees = rotation;
    }

    private void swordCollide(Node node)
    {
        if (!mHitNodes.Contains(node) && node.HasMethod("dealDamage"))
        {
            node.Call("dealDamage", 1, this);
            mHitNodes.Add(node);
        }
    }

    private void resetAttack(string name)
    {
        mCanAttack = true;
        mSwordArea.Monitoring = false;
    }

    public void Attack()
    {
        if (!mCanAttack)
            return;

        mSwordArea.Monitoring = true;        
        mCanAttack = false;
        mHitNodes.Clear();
        mAnimation.Play("quick_slash");
    }
}
