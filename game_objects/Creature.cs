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
    private int mMaxHealth = 100;
    [Export]
    private float mHurtTimer = 0.2f;
    private int mCurrentHealth;

    [Export]
    private float mJumpMagnitude = 11f;

    [Export]
    private float mMovementSpeed = 5;

    private MeshInstance mMeshInst;
    private Color mMeshOriginalColor;
    private Timer mResetColorTimer;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        mCurrentHealth = mMaxHealth;
        rng = new RandomNumberGenerator();
        rng.Randomize();
        mMeshInst = GetNode<MeshInstance>("MeshInstance");
        mLeftRc = GetNode<RayCast>("LeftCast");
        mRightRc = GetNode<RayCast>("RightCast");
        mForwardRc = GetNode<RayCast>("ForwardCast");
        mEdgeRc = GetNode<RayCast>("EdgeGuardCast");
        mTargetTimer = GetNode<Timer>("TargetTimer");
        mTargetTimer.Connect("timeout", this, "randomizeTarget");
    }

    private void dealDamage(int damage, Godot.Object dealer)
    {
        mCurrentHealth -= damage;
        if (mCurrentHealth <= 0)
        {
            dealer.Call("GainExperience", 1);
            QueueFree();
            return;
        }
        SpatialMaterial newMaterial = new SpatialMaterial();
        newMaterial.AlbedoColor = new Color(1, 0, 0, 1);
        mMeshInst.MaterialOverride = newMaterial;
        if (mResetColorTimer != null)
        {
            mResetColorTimer.Start(mHurtTimer);
            return;
        }
        mResetColorTimer = new Timer();
        mResetColorTimer.WaitTime = mHurtTimer;
        mResetColorTimer.OneShot = true;
        mResetColorTimer.Connect("timeout", this, "resetColor");
        AddChild(mResetColorTimer);
        mResetColorTimer.Start();
    }

    private void resetColor()
    {
        mMeshInst.MaterialOverride = null;
        mResetColorTimer.QueueFree();
        mResetColorTimer = null;
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