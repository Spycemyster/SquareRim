using Godot;
using System;

public class Creature : KinematicBody
{
    
    private Vector3 mTarget;
    private RandomNumberGenerator rng;
    public float mDetectionRadius = 100;
    private const float DELTA = 0.5f;
    private Vector3 mVelocity;
    private RayCast mLeftRc, mRightRc, mForwardRc, mEdgeRc;
    private Timer mTargetTimer;
    private bool mIsResting = false;

    [Export]
    private float mJumpMagnitude = 11f;

    [Export]
    private float mMovementSpeed = 5;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        rng = new RandomNumberGenerator();
        rng.Randomize();
        mLeftRc = GetNode<RayCast>("LeftCast");
        mRightRc = GetNode<RayCast>("RightCast");
        mForwardRc = GetNode<RayCast>("ForwardCast");
        mEdgeRc = GetNode<RayCast>("EdgeGuardCast");
        mTargetTimer = GetNode<Timer>("TargetTimer");
        mTargetTimer.Connect("timeout", this, "randomizeTarget");
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);
        mVelocity.x = 0;
        mVelocity.z = 0;

        // creature position is close to the target
        Vector3 dirTo = mTarget - GlobalTransform.origin;
        dirTo.y = 0;
        dirTo = dirTo.Normalized();
        if (!mIsResting)
        {
            if (mEdgeRc.IsColliding())
            {
                mVelocity.x = dirTo.x * mMovementSpeed;
                mVelocity.z = dirTo.z * mMovementSpeed;
            }

            if (mForwardRc.IsColliding() || mLeftRc.IsColliding() || mRightRc.IsColliding())
            {
                mVelocity.y = mJumpMagnitude;
            }
        }
        mVelocity.y -= Globals.GRAVITY * delta; 
        float targetRot = Mathf.Atan2(dirTo.x, dirTo.z);
        float actualRot = Mathf.Rad2Deg(Mathf.LerpAngle(Rotation.y, targetRot, 0.1f));
        RotationDegrees = new Vector3(0, actualRot, 0);
        mVelocity = MoveAndSlide(mVelocity, Vector3.Up);

        if (GlobalTransform.origin.y < -ChunkGenerator.CHUNK_HEIGHT - 1)
        {
            QueueFree();
        }
    }

    private void randomizeTarget()
    {
        Vector3 delta = new Vector3(rng.RandfRange(-mDetectionRadius, mDetectionRadius), 
            0, rng.RandfRange(-mDetectionRadius, mDetectionRadius));
        mTarget = GlobalTransform.origin + delta;
        mTargetTimer.Start(rng.RandfRange(1, 5));
        float prob = rng.Randf();
        mIsResting = prob <= 0.2f;
    }
}