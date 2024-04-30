using Godot;
using Godot.Collections;

public partial class Player : RigidBody3D
{
    [Signal]
    public delegate void ResurrectedEventHandler();

    private const float MaxSpeed = 50f;
    private const float MinSpeed = 0f;
    private const float RollSpeed = 2f;
    private const float PitchSpeed = 2f;
    private const float YawSpeed = 1f;
    private const float Accel = 15f;

    private Node3D _jetModel;
    private Camera3D _thirdPersonCam;
    private Vector3 _initialCameraPos;
    private PackedScene _explosionPS;
    private bool _rollLeft = false;
    private bool _rollRight = false;
    private bool _pitchUp = false;
    private bool _pitchDown = false;
    private bool _yawLeft = false;
    private bool _yawRight = false;
    private bool _accelerate = false;
    private bool _decelerate = false;
    private bool _isAlive = true;
    private float _speed = MinSpeed;
    private Wing _frontLeftWing;
    private Wing _frontRightWing;
    private Wing _backLeftWing;
    private Wing _backRightWing;
    private Wing _rudder;

    public bool IsFiring = false;


    public override void _Ready()
    {
        _jetModel = GetNode<Node3D>("Jet");
        _thirdPersonCam = GetNode<Camera3D>("SpringArm3D/ThirdPersonCam");
        _initialCameraPos = _thirdPersonCam.Position;
        _explosionPS = GD.Load<PackedScene>("res://scenes/explosion.tscn");

        _frontLeftWing = GetNode<Wing>("FrontLeftWing");
        _frontRightWing = GetNode<Wing>("FrontRightWing");
        _backLeftWing = GetNode<Wing>("BackLeftWing");
        _backRightWing = GetNode<Wing>("BackLeftWing");
        _rudder = GetNode<Wing>("Rudder");
    }

    public override void _Process(double delta)
    {
        if (!_isAlive) return;
        if (Input.IsActionPressed("roll_left")) _rollLeft = true;
        if (Input.IsActionPressed("roll_right")) _rollRight = true;
        if (Input.IsActionPressed("pitch_up")) _pitchUp = true;
        if (Input.IsActionPressed("pitch_down")) _pitchDown = true;
        if (Input.IsActionPressed("yaw_left")) _yawLeft = true;
        if (Input.IsActionPressed("yaw_right")) _yawRight = true;
        if (Input.IsActionPressed("accelerate")) _accelerate = true;
        if (Input.IsActionPressed("decelerate")) _decelerate = true;

        if (Input.IsActionJustPressed("change_camera"))
        {
            if (GetViewport().GetCamera3D() == _thirdPersonCam)
                GetNode<Camera3D>("Jet/NoseCam").MakeCurrent();
            else
                _thirdPersonCam.MakeCurrent();
        }

        if (Input.IsActionPressed("fire"))
        {
            IsFiring = true;
            GetNode<Gun>("Jet/Gun").Fire();
        }
        else
            IsFiring = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float) delta;
        if (_rollLeft)
        {
            _rollLeft = false;
            _frontLeftWing.FlapUp();
            _frontRightWing.FlapDown();
        }
        if (_rollRight)
        {
            _rollRight = false;
            _frontRightWing.FlapUp();
            _frontLeftWing.FlapDown();
        }
        if (_pitchUp)
        {
            _pitchUp = false;
            _backLeftWing.FlapUp();
            _backRightWing.FlapUp();
        }
        if (_pitchDown)
        {
            _pitchDown = false;
            _backLeftWing.FlapDown();
            _backRightWing.FlapDown();
        }
        if (_yawLeft)
        {
            _yawLeft = false;
            _rudder.FlapDown();
        }
        if (_yawRight)
        {
            _yawRight = false;
            _rudder.FlapUp();
        }

        if (_accelerate)
        {
            _accelerate = false;
            if (LinearVelocity.Length() < MaxSpeed)
            {
                ApplyCentralForce(Accel * -Transform.Basis.Z);
            }
        }
        if (_decelerate)
        {
            _decelerate = false;
            if (LinearVelocity.Length() < MinSpeed)
            {
                ApplyCentralForce(Accel/4 * Transform.Basis.Z);
            }
        }

        Vector3 totalForce = Vector3.Zero;
        totalForce += ToLocal(_frontLeftWing.CalculateLift()) + ToLocal(_frontLeftWing.CalculateDrag());
        totalForce += ToLocal(_frontRightWing.CalculateLift()) + ToLocal(_frontRightWing.CalculateDrag());
        totalForce += ToLocal(_backLeftWing.CalculateLift()) + ToLocal(_backLeftWing.CalculateDrag());
        totalForce += ToLocal(_backRightWing.CalculateLift()) + ToLocal(_backRightWing.CalculateDrag());
        totalForce += ToLocal(_rudder.CalculateLift()) + ToLocal(_rudder.CalculateDrag());

        ApplyCentralForce(totalForce * deltaF);

        Vector3 totalTorque = Vector3.Zero;
        totalTorque += ToLocal(_frontLeftWing.CalculateRotatoryForce()).Cross(_frontLeftWing.Position);
        totalTorque += ToLocal(_frontRightWing.CalculateRotatoryForce()).Cross(_frontRightWing.Position);
        totalTorque += ToLocal(_backLeftWing.CalculateRotatoryForce()).Cross(_backLeftWing.Position);
        totalTorque += ToLocal(_backRightWing.CalculateRotatoryForce()).Cross(_backRightWing.Position);
        totalTorque += ToLocal(_rudder.CalculateRotatoryForce()).Cross(_rudder.Position);

        ApplyTorque(totalTorque * deltaF);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferChannel = 0, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void Die()
    {
        _isAlive = false;
        _jetModel.Visible = false;
        GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
        SceneTreeTimer timer = GetTree().CreateTimer(3f);
        timer.Timeout += Resurrect;
    }

    private void Resurrect()
    {
        Position = new Vector3(0, 3, 0);
        Rotation = Vector3.Zero;
        _isAlive = true;
        _jetModel.Visible = true;
        GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
        EmitSignal(SignalName.Resurrected);
    }

    public void KillOtherPlayer(long peerId)
    {
        RpcId(peerId, MethodName.Die);
    }
}
