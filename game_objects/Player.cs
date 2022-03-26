using Godot;
using System;
using System.Collections.Generic;

public class Player : KinematicBody
{
    /// <summary>
    /// TODO: Implement some formula for calculating power level.
    /// Idea: (Attack + Health * Regen + Mana * Regen) * Level
    /// </summary>
    /// <value></value>
    public int PowerLevel
    {
        get { return 0; }
    }

    [Export]
    public uint Level
    {
        get { return mLevel; }
        set
        {
            mLevel = value;
            mNeededExperience = GetNeededExperience(mLevel);
        }
    }
    [Export]
    public uint CurrentExperience
    {
        get { return mCurrentExp; }
        set 
        {
            mCurrentExp = value;

            while (mCurrentExp >= NeededExperience)
            {
                mCurrentExp -= NeededExperience;
                Level++;
                GD.Print($"Player is now level {Level}");
            }
        }
    }
    private uint mNeededExperience;
    private uint mCurrentExp;
    private RayCast interactCast_;

    /// <summary>
    /// The amount of experience needed at this current level.
    /// </summary>
    /// <value></value>
    public uint NeededExperience
    {
        get { return mNeededExperience; }
    }

    [Export]
    public float MaxHealth
    {
        get
        {
            return mMaxHealth;
        }
        set 
        {
            mMaxHealth = value;
            CurrentHealth = value;
        }
    }
    private float mMaxHealth = 10;

    [Export]
    public float CurrentHealth
    {
        get { return mCurrentHealth; }
        set 
        {
            if (mCurrentHealth > value && value > 0)
            {
                OnHurt();
            }
            mCurrentHealth = Mathf.Max(0, value);
            if (mCurrentHealth == 0)
            {
                OnDeath();
            }
        }
    }
    private float mCurrentHealth = 10;
    [Export]
    private float moveSpeed = 8f;
    [Export]
    private float jumpMagnitude = 24f;
    [Export]
    private float mouseSensitivity = 0.1f;

    private const float MIN_ANGLE = -90.0f;
    private const float MAX_ANGLE = 90.0f;
    private Vector3 velocity;
    private RayCast feetCast;
    private PlayerHand mHand;
    private uint mLevel;
    private bool mIsDead;
    private PlayerHUD playerHUD_;

    private Camera camera;
    public uint GetNeededExperience(uint level)
    {
        return (uint)(5 * level * Mathf.Log(level) + 15); 
    }
    private Particles mParticles;
    public override void _Ready()
    {
        camera = GetNode<Camera>("Camera");
        feetCast = GetNode<RayCast>("FeetCast");
        mHand = GetNode<PlayerHand>("HandHolder/Hand");
        mParticles = GetNode<Particles>("BloodEmitter");
        interactCast_ = GetNode<RayCast>("Camera/InteractCast");
        playerHUD_ = GetNode<PlayerHUD>("PlayerHUD");
        CurrentHealth = MaxHealth;
        mIsDead = false;
    }

    protected virtual void OnHurt()
    {
        if (mParticles.Emitting)
            return;
        mParticles.Emitting = true;
        OneShotMethod<Particles> method = new OneShotMethod<Particles>((x) => x[0].Emitting = false,
            1f, this, mParticles);
        AddChild(method);
        method.Start();
    }
    protected virtual void OnDeath()
    {
        if (mIsDead)
        {
            return;
        }

        mIsDead = true;
        GD.Print("Dead");
    }
    public override void _PhysicsProcess(float delta)
    {
        //CurrentHealth -= delta;
        base._PhysicsProcess(delta);

        velocity.x = 0;
        velocity.z = 0;

        Vector3 forward = GlobalTransform.basis.z;
        Vector3 right = GlobalTransform.basis.x;
        
        Vector3 input = new Vector3(Input.GetActionStrength("move_backward") - Input.GetActionStrength("move_forward"),
            0, Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left"));
        bool isSprinting = Input.IsActionPressed("sprint");
        if (Input.IsActionPressed("jump") && (IsOnFloor() || (velocity.y <= 0 && feetCast.IsColliding())))
        {
            velocity.y += jumpMagnitude;
        }
        input = input.Normalized();
        Vector3 relativeDir = forward * input.x + input.z * right;
        float sprintMult = (isSprinting ? 2 : 1);
        velocity.x = relativeDir.x * moveSpeed * sprintMult;
        velocity.z = relativeDir.z * moveSpeed * sprintMult;

        velocity.y -= Globals.GRAVITY * delta;

        velocity = MoveAndSlide(velocity * (isSprinting ? 1 : 1), Vector3.Up);

        if (interactCast_.IsColliding())
        {
            if (interactCast_.GetCollider() is IInteractable interactable)
            {
                playerHUD_.InteractText = interactable.GetInteractMessage();

                if (Input.IsActionJustPressed("interact"))
                {
                    bool interacted = interactable.Interact(this);

                    if (interacted)
                    {
                        GD.Print("Successfully interacted with the interactable!");
                    }
                }
            }
        }
        else
        {
            playerHUD_.InteractText = "";
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (@event is InputEventMouseMotion mouse)
        {
            Vector3 cameraRotation = camera.RotationDegrees;
            cameraRotation.x -= mouse.Relative.y * mouseSensitivity;
            cameraRotation.x = Mathf.Clamp(cameraRotation.x, MIN_ANGLE, MAX_ANGLE);
            camera.RotationDegrees = cameraRotation;

            Vector3 bodyRotation = RotationDegrees;
            bodyRotation.y -= mouse.Relative.x * mouseSensitivity;
            RotationDegrees = bodyRotation;
        }

        if (@event is InputEventMouseButton mouseBtn)
        {
            if (mouseBtn.IsActionPressed("attack"))
            {
                mHand.Attack();
            }
        }
    }
}
