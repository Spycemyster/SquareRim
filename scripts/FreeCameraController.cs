using Godot;
using System;

public class FreeCameraController : KinematicBody
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";
    private Camera camera;

    [Export]
    private float lookSensitivity = 1.0f;

    private float minLookAngle = -90.0f;
    private float maxLookAngle = 90.0f;
    private float moveSpeed = 10f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        camera = GetNode<Camera>("Camera");
    }

    public override void _PhysicsProcess(float delta)
    {
        Vector3 right = GlobalTransform.basis.x;
        Vector3 forward = GlobalTransform.basis.z;
        Vector3 up = GlobalTransform.basis.y;

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

        bool isSprinting = Input.IsActionPressed("sprint");

        input = input.Normalized();
        Vector3 relativeDirection = forward * input.x + right * input.z + Vector3.Up * input.y; // + up * input.y;
        MoveAndSlide(relativeDirection * moveSpeed * (isSprinting ? 5 : 1));
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
