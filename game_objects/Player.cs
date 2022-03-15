using Godot;
using System;

public class Player : KinematicBody
{
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

    private Camera camera;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        camera = GetNode<Camera>("Camera");
        feetCast = GetNode<RayCast>("FeetCast");
    }
    public override void _PhysicsProcess(float delta)
    {
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
    }
}
